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

public enum SmoothType { None, Bilinear, BSpline2, Biсubic, Blur };
public record struct BitmapDrawParams(Matrix3x2 Transform, SmoothType Smooth);

public static class RenderKernel
{
	public record struct ImageInfo(PixelSize Size, Matrix3x2 Transform, SmoothType Smooth);

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

	public unsafe static RenderEntry DrawBitmapKernel(this Accelerator accelerator, Bitmap sourceBmp, Func<BitmapDrawParams> obtainParams)
	{
		var kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<uint, Stride2D.DenseX>, ArrayView<uint>, ImageInfo>((ind, output, src, info) =>
		{
			var tr = info.Transform;
			Vector2 v = Vector2.Transform(ind.ToVector(), tr);
			output[ind] = info.Smooth switch
			{
				SmoothType.None => src.GetNearestPixel(info.Size, v),
				SmoothType.Bilinear => src.GetBilinearPixel(info.Size, v),
				SmoothType.BSpline2 => src.GetBSpline2Pixel(info.Size, v),
				SmoothType.Biсubic => src.GetBicubicPixel(info.Size, v),
				_ => 0
			};
		});

		DisposableList release = [];

		var srcSize = sourceBmp.PixelSize;
		var buff = new uint[srcSize.Width * srcSize.Height * 4];
		fixed (uint* p = buff)
		{
			sourceBmp.CopyPixels(new PixelRect(srcSize), (IntPtr)p, buff.Length * 4, srcSize.Width * 4);
		}
		var imageBuffer = accelerator.Allocate1D(buff).DisposeWith(release);

		return new((ind, output) =>
		{
			var dp = obtainParams();
			Matrix3x2.Invert(dp.Transform, out var tr);

			kernel(ind, output, imageBuffer.View, new(srcSize, tr, dp.Smooth));
			accelerator.Synchronize();
		}, release.Dispose);
	}
}
