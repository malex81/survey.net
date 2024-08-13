using Avalonia.Controls;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime;
using ILGPU;
using ImageProcessing.Helpers;
using System;
using Avalonia.Media.Imaging;
using System.Linq;
using Avalonia;
using SkiaSharp;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace ImageProcessing.SurveyImageSmooth.Engine;
internal class ImageRendering(Control control)
{
	private static readonly Context gpuContext;
	private static readonly Device gpuDevice;

	static readonly AcceleratorType[] accPriority = [AcceleratorType.Cuda, AcceleratorType.OpenCL, AcceleratorType.CPU];

	static Device ChooseBestDevice(Context context) => context.Devices
			//.Where(d => d.AcceleratorType != AcceleratorType.CPU)
			.OrderBy(d => Array.IndexOf(accPriority, d.AcceleratorType))
			.ThenByDescending(d => d.MaxNumThreads)
			.FirstOrDefault()
		//?? context.Devices.FirstOrDefault())
		?? throw new NotSupportedException("GPU acceleration not support");

	static ImageRendering()
	{
		gpuContext = Context.Create(builder => builder.Default().EnableAlgorithms());
		gpuDevice = ChooseBestDevice(gpuContext);
		//gpuDevice = gpuContext.GetPreferredDevice(preferCPU: true);
	}

	private readonly Control control = control;
	private DisposableList? sourcePeg;
	private Accelerator? accelerator;
	private RenderEntry? renderEntry;

	public unsafe void LoadImageSource(Bitmap sourceBmp)
	{
		if (sourcePeg != null) throw new InvalidOperationException();
		sourcePeg = [];
		accelerator = gpuDevice.CreateAccelerator(gpuContext).DisposeWith(sourcePeg);
		//renderEntry = accelerator.SimpleTestKernel().DisposeWith(sourcePeg);
		renderEntry = accelerator.DrawBitmapKernel(sourceBmp).DisposeWith(sourcePeg);
	}

	public void FreeSource()
	{
		sourcePeg?.Dispose();
		sourcePeg = null;
		renderEntry = null;
	}

	[MemberNotNullWhen(true, nameof(accelerator), nameof(renderEntry))]
	bool CanRender => renderEntry != null;

	public SKBitmap? RenderBitmap()
	{
		if (!CanRender) return null;

		var bmpSize = PixelSize.FromSize(control.Bounds.Size, 1);
		var buffSize = new Index2D(bmpSize.Width, bmpSize.Height);

		using var outputBuffer = accelerator.Allocate2DDenseX<uint>(buffSize);
		//kernel(buffSize, sourceView, outputBuffer.View);
		//accelerator.Synchronize();
		renderEntry.Exec(buffSize, outputBuffer);
		var data = outputBuffer.GetRawData();

		var bmp = new SKBitmap();
		// pin the managed array so that the GC doesn't move it
		var gcHandle = GCHandle.Alloc(data.Array, GCHandleType.Pinned);
		// install the pixels with the color type of the pixel data
		var info = new SKImageInfo(bmpSize.Width, bmpSize.Height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
		var res = bmp.InstallPixels(info, gcHandle.AddrOfPinnedObject(), info.RowBytes, (addr, ctx) => { gcHandle.Free(); });
		return bmp;
	}
}
