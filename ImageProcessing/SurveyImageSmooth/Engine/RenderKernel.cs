using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Optimization.Optimizers;
using ILGPU.Runtime;
using ImageProcessing.Helpers;
using ImageProcessing.RenderingMath;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static ILGPU.IR.Analyses.Uniforms;

namespace ImageProcessing.SurveyImageSmooth.Engine;

public record RenderEntry(Action<Index2D, ArrayView2D<uint, Stride2D.DenseX>> Exec, Action? Free) : IDisposable
{
	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Free?.Invoke();
	}
}

public enum PrefilterType { None, FindEdges, GaussianBlur, AutoBlur };
public enum InterpolationType { None, Bilinear, BSpline2, BSpline1_5, Biсubic };
public record struct BitmapDrawParams(Matrix3x2 Transform, PrefilterType Prefilter, InterpolationType Interpolation);

public static class RenderKernel
{
	public record struct ImageSource(ArrayView<uint> Data, PixelSize Size);
	public record struct ImageInfo(
		ImageSource Source,
		ArrayView2D<uint, Stride2D.DenseX> Otput,
		Matrix3x2 Transform,
		InterpolationType Interpolation);
	public record struct PrefilterConvolution(
		ImageSource Source,
		ArrayView<uint> Otput,
		ArrayView2D<float, Stride2D.DenseX> Matrix,
		byte HoldAlpha);

	static void PrefilterKernel(Index1D ind, PrefilterConvolution prm)
	{
		var ind2 = ind.ToIndex2D(prm.Source.Size.Width);
		prm.Otput[ind] = prm.Source.Data.GetConvolutionPixel(prm.Source.Size, ind2, prm.Matrix, prm.HoldAlpha > 0);
	}
	static void InterpolationKernel(Index2D ind, ImageInfo img)
	{
		var tr = img.Transform;
		Vector2 v = Vector2.Transform(ind.ToVector(), tr);
		img.Otput[ind] = img.Interpolation switch
		{
			InterpolationType.None => img.Source.Data.GetNearestPixel(img.Source.Size, v),
			InterpolationType.Bilinear => img.Source.Data.GetBilinearPixel(img.Source.Size, v),
			InterpolationType.BSpline2 => img.Source.Data.GetBSpline2Pixel(img.Source.Size, v),
			InterpolationType.BSpline1_5 => img.Source.Data.GetBSpline1_5Pixel(img.Source.Size, v),
			InterpolationType.Biсubic => img.Source.Data.GetBicubicPixel(img.Source.Size, v),
			_ => 0
		};
	}
	public unsafe static RenderEntry DrawBitmapKernel(this Accelerator accelerator, Bitmap sourceBmp, Func<BitmapDrawParams> obtainParams)
	{
		var prefKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, PrefilterConvolution>(PrefilterKernel);
		var interKernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, ImageInfo>(InterpolationKernel);

		DisposableList release = [];

		var srcSize = sourceBmp.PixelSize;
		var buff = new uint[srcSize.Width * srcSize.Height * 4];
		fixed (uint* p = buff)
		{
			sourceBmp.CopyPixels(new PixelRect(srcSize), (IntPtr)p, buff.Length * 4, srcSize.Width * 4);
		}
		var imageBuffer = accelerator.Allocate1D(buff).DisposeWith(release);
		var prefilteredBuffer = accelerator.Allocate1D<uint>(buff.Length).DisposeWith(release);

		//static TRes Call<TRes>(Func<TRes> f) => f();

		const float sigmaTl = 1.2f;
		float lastSigma = 0;
		bool NeedBlurUpdate(float sigma, out float[,] matrix)
		{
			var res = lastSigma == 0 || sigma != 0 && (sigma / lastSigma > sigmaTl || lastSigma / sigma > sigmaTl);
			matrix = res ? CalcProc.ComputeGaussianMatrix(sigma) : new float[0, 0];
			if (res)
				lastSigma = sigma;
			return res;
		}

		return new((ind, output) =>
		{
			var dp = obtainParams();
			Matrix3x2.Invert(dp.Transform, out var tr);
			var imgSource = new ImageSource(imageBuffer.View, srcSize);
			if (dp.Prefilter != PrefilterType.None)
			{
				var gSigma = dp.Prefilter switch
				{
					PrefilterType.AutoBlur => 0.3f / dp.Transform.GetScale(),
					PrefilterType.GaussianBlur => 6,
					_ => 0
				};
				float[,] convMatrix;
				if (gSigma == 0)
				{
					lastSigma = gSigma;
					convMatrix = dp.Prefilter switch
					{
						PrefilterType.FindEdges => new float[,]
													{{ -1, -1, -1 },
													{ -1, 8, -1 },
													{ -1, -1, -1 }},
						_ => new float[0, 0]
					};
				}
				else
				{
					if (gSigma < 0.5f) convMatrix = new float[0, 0];
					else if (!NeedBlurUpdate(gSigma, out convMatrix))
						imgSource.Data = prefilteredBuffer.View;
				}
				if (convMatrix.Length > 0)
				{
					var convMatrixBuff = accelerator.Allocate2DDenseX(convMatrix);
					prefKernel(buff.Length, new(imgSource,
													prefilteredBuffer.View,
													convMatrixBuff.View,
													(byte)(dp.Prefilter == PrefilterType.FindEdges ? 1 : 0)));
					//accelerator.Synchronize();
					imgSource.Data = prefilteredBuffer.View;
				}
			}
			interKernel(ind, new(imgSource, output, tr, dp.Interpolation));
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
