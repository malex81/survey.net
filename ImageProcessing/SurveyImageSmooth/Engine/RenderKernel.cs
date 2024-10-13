using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Optimization.Optimizers;
using ILGPU.Runtime;
using ImageProcessing.Helpers;
using ImageProcessing.RenderingMath;
using System;
using System.Numerics;

namespace ImageProcessing.SurveyImageSmooth.Engine;

public record RenderEntry(Action<Index2D, ArrayView2D<uint, Stride2D.DenseX>> Exec, Action? Free) : IDisposable
{
	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Free?.Invoke();
	}
}

public enum PrefilterType { None, FindEdges, GausianBlur };
public enum InterpolationType { None, Bilinear, BSpline2, BSpline1_5, Biсubic };
public record struct BitmapDrawParams(Matrix3x2 Transform, PrefilterType Prefilter, InterpolationType Interpolation);

public static class RenderKernel
{
	public record struct ImageInfo(PixelSize Size, Matrix3x2 Transform, PrefilterType Prefilter, InterpolationType Interpolation);

	public unsafe static RenderEntry DrawBitmapKernel(this Accelerator accelerator, Bitmap sourceBmp, Func<BitmapDrawParams> obtainParams)
	{
		var prefilterKernel = accelerator.LoadStreamKernel<ArrayView<uint>, ArrayView<uint>, ImageInfo>((output, src, info) =>
		{
			Index1D ind = Grid.GlobalIndex.X;
			if (ind >= output.Length) return;
			var ind2 = ind.ToIndex2D(info.Size.Width);
			output[ind] = info.Prefilter switch
			{
				PrefilterType.FindEdges => src.GetEdgePixel(info.Size, ind2),
				PrefilterType.GausianBlur => src.GetGaussianBlurPixel(info.Size, ind2, 1),
				_ => src[ind]
			};

		});

		var kernel = accelerator.LoadAutoGroupedStreamKernel((Action<Index2D, ArrayView2D<uint, Stride2D.DenseX>, ArrayView<uint>, ImageInfo>)((ind, output, src, info) =>
		{
			var tr = info.Transform;
			Vector2 v = Vector2.Transform(ind.ToVector(), tr);
			output[ind] = info.Interpolation switch
			{
				InterpolationType.None => src.GetNearestPixel(info.Size, v),
				InterpolationType.Bilinear => src.GetBilinearPixel(info.Size, v),
				InterpolationType.BSpline2 => src.GetBSpline2Pixel(info.Size, v),
				InterpolationType.BSpline1_5 => src.GetBSpline1_5Pixel(info.Size, v),
				InterpolationType.Biсubic => src.GetBicubicPixel(info.Size, v),
				_ => 0
			};
		}));
		DisposableList release = [];

		var srcSize = sourceBmp.PixelSize;
		var buff = new uint[srcSize.Width * srcSize.Height * 4];
		fixed (uint* p = buff)
		{
			sourceBmp.CopyPixels(new PixelRect(srcSize), (IntPtr)p, buff.Length * 4, srcSize.Width * 4);
		}
		var imageBuffer = accelerator.Allocate1D(buff).DisposeWith(release);
		var prefilteredBuffer = accelerator.Allocate1D<uint>(buff.Length).DisposeWith(release);

		return new((ind, output) =>
		{
			var dp = obtainParams();
			Matrix3x2.Invert(dp.Transform, out var tr);
			var imgInfo = new ImageInfo(srcSize, tr, dp.Prefilter, dp.Interpolation);
			var _buff = imageBuffer;
			if (imgInfo.Prefilter != PrefilterType.None)
			{
				var groupSize = 128;
				KernelConfig dimension = ((buff.Length + groupSize - 1) / groupSize, groupSize);
				prefilterKernel(dimension, prefilteredBuffer.View, imageBuffer.View, imgInfo);
				//accelerator.Synchronize();
				_buff = prefilteredBuffer;
			}
			kernel(ind, output, _buff.View, imgInfo);
			accelerator.Synchronize();
		}, release.Dispose);
	}

	#region Experiments
	public static RenderEntry SimpleTestKernel(this Accelerator accelerator)
	{
		var kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<uint, Stride2D.DenseX>>((ind, output) =>
		{
			var outSize = output.IntExtent;
			var k1 = (1.0 * outSize.X) / outSize.Y;
			var k2 = (1.0 * ind.X) / ind.Y;
			output[ind] = k1 > k2 ? 0xffff0000 : 0xff00ff00;
		});
		return new((ind, output) =>
		{
			kernel(ind, output);
			accelerator.Synchronize();
		}, null);
	}
	#endregion
}
