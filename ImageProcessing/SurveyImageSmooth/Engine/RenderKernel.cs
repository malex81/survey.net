using Avalonia;
using Avalonia.Media.Imaging;
using ILGPU;
using ILGPU.Runtime;
using ImageProcessing.Helpers;
using System;

namespace ImageProcessing.SurveyImageSmooth.Engine;

public record RenderEntry(Action<Index2D, ArrayView2D<uint, Stride2D.DenseX>> Exec, Action? Free) : IDisposable
{
	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Free?.Invoke();
	}
}

public static class RenderKernel
{
	public record struct ImageInfo(int Width, int Height, uint RestColor, uint ShiftX);

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

	public unsafe static RenderEntry DrawBitmapKernel(this Accelerator accelerator, Bitmap sourceBmp)
	{
		var kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<uint, Stride2D.DenseX>, ArrayView<uint>, ImageInfo>((ind, output, src, info) =>
		{
			var (x, y) = (ind.X - info.ShiftX, (int)(ind.Y + 200*(MathF.Sin((float)info.ShiftX / 100)-1.2)));
			output[ind] = 0 <= x && x < info.Width && 0 <= y && y < info.Height ? src[x + y * info.Width] : info.RestColor;
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

		uint restColor = 0xff00a000;
		uint nextColor()
		{
			restColor += 0x00030201;
			restColor |= 0xff000000;
			return restColor;
		}
		uint shiftX = 0;
		uint nextShift()
		{
			shiftX += 7;
			if (shiftX > 1500) shiftX = 0;
			return shiftX;
		}

		return new((ind, output) =>
		{
			kernel(ind, output, imageBuffer.View, new(srcSize.Width, srcSize.Height, nextColor(), nextShift()));
			accelerator.Synchronize();
		}, release.Dispose);
	}
}
