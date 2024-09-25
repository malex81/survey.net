using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ILGPU;
using ILGPU.Runtime;
using ImageProcessing.Helpers;
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

public record struct BitmapDrawParams(Matrix3x2 Transform);

public static class RenderKernel
{
	public record struct ImageInfo(int Width, int Height, Matrix3x2 Transform);

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
			Vector2 v = Vector2.Transform(new(ind.X, ind.Y), tr);
			var (x, y) = (v.X, v.Y);
			output[ind] = 0 <= x && x < info.Width && 0 <= y && y < info.Height ? src[(int)x + (int)y * info.Width] : 0xffffaf56;
			if ((output[ind] & 0xff000000) != 0xff000000)
				output[ind] = 0xffffaf36;
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

			kernel(ind, output, imageBuffer.View, new(srcSize.Width, srcSize.Height, tr));
			accelerator.Synchronize();
		}, release.Dispose);
	}
}
