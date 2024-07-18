using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Avalonia.Threading;
using HarfBuzzSharp;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.OpenCL;
using ImageProcessing.Helpers;
using ImageProcessing.SkiaHelpers;
using ScottPlot.Colormaps;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using AvCtrl = Avalonia.Controls;

namespace ImageProcessing.SurveyImageSmooth.Views;
internal class ImagePresenter : AvCtrl.Control
{
	#region SourceBitmap Direct Avalonia Property
	private Bitmap? sourceBitmap = default;

	public static readonly DirectProperty<ImagePresenter, Bitmap?> SourceBitmapProperty =
		AvaloniaProperty.RegisterDirect<ImagePresenter, Bitmap?>(nameof(SourceBitmap), o => o.SourceBitmap, (o, v) => o.SourceBitmap = v);

	public Bitmap? SourceBitmap
	{
		get => sourceBitmap;
		set
		{
			SetAndRaise(SourceBitmapProperty, ref sourceBitmap, value);
			UpdateSource();
		}
	}
	#endregion SourceBitmap Direct Avalonia Property

	#region TimingText Direct Avalonia Property
	private string timingText = "0.0";

	public static readonly DirectProperty<ImagePresenter, string> TimingTextProperty =
		AvaloniaProperty.RegisterDirect<ImagePresenter, string>(nameof(TimingText), o => o.TimingText, (o, v) => o.TimingText = v);

	public string TimingText
	{
		get => timingText;
		private set { SetAndRaise(TimingTextProperty, ref timingText, value); }
	}
	#endregion TimingText Direct Avalonia Property

	private static readonly Context? gpuContext;
	private static readonly Device? gpuDevice;

	static Device ChooseBestDevice(Context context) => (context.Devices
			.Where(d => d.AcceleratorType != AcceleratorType.CPU)
			.OrderByDescending(d => d.MaxNumThreads)
			.FirstOrDefault()
		?? context.Devices.FirstOrDefault())
		?? throw new NotSupportedException("GPU acceleration not support");

	static ImagePresenter()
	{
		if (AvCtrl.Design.IsDesignMode) return;
		gpuContext = Context.Create(builder => builder.Default().EnableAlgorithms());
		gpuDevice = ChooseBestDevice(gpuContext);
		//gpuDevice = gpuContext.GetPreferredDevice(preferCPU: false);
	}

	[MemberNotNullWhen(false, nameof(gpuContext), nameof(gpuDevice))]
	static bool IsDesignMode => AvCtrl.Design.IsDesignMode;

	static void KernelProc(Index2D ind, ArrayView2D<uint, Stride2D.DenseX> src, ArrayView2D<uint, Stride2D.DenseX> output)
	{
		//var outSize = output.IntExtent;
		//var k1 = (1.0 * outSize.X) / outSize.Y;
		//var k2 = (1.0 * ind.X) / ind.Y;
		//output[ind] = k1 > k2 ? 0xffff0000 : 0xff00ff00;
		//output[ind] = ind.X > 100 ? 0xffff0000 : 0xff00ff00;
		var srcSize = src.IntExtent;
		output[ind] = ind.X < srcSize.X && ind.Y < srcSize.Y ? src[ind] : 0xaa00aaa0;
	}

	private readonly CustomActionDrawer skDrawer;
	private IDisposable? sourcePeg;

	private Accelerator? accelerator;
	private Action<Index2D, ArrayView2D<uint, Stride2D.DenseX>, ArrayView2D<uint, Stride2D.DenseX>>? kernel;
	private ArrayView2D<uint, Stride2D.DenseX> sourceView;

	public ImagePresenter()
	{
		skDrawer = new(SkiaDraw, this);
	}

	void UpdateSource()
	{
		if (IsDesignMode) return;
		lock (gpuContext)
		{
			sourcePeg?.Dispose();
			sourcePeg = sourceBitmap != null ? LoadImageSource() : null;
		}
		InvalidateVisual();
	}

	unsafe IDisposable LoadImageSource()
	{
		if (sourceBitmap == null || IsDesignMode) throw new InvalidOperationException();
		var _disp = new DisposableList();
		accelerator = gpuDevice.CreateAccelerator(gpuContext).DisposeWith(_disp);
		kernel = accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<uint, Stride2D.DenseX>, ArrayView2D<uint, Stride2D.DenseX>>(KernelProc);
		var srcSize = sourceBitmap.PixelSize;
		var buff = new uint[srcSize.Width, srcSize.Height];
		fixed (uint* p = buff)
		{
			sourceBitmap.CopyPixels(new PixelRect(srcSize), (IntPtr)p, buff.Length*4, srcSize.Width * 4);
		}
		var imageBuffer = accelerator.Allocate2DDenseX(buff).DisposeWith(_disp);
		sourceView = imageBuffer.View;
		return _disp;
	}

	[MemberNotNullWhen(true, nameof(sourceView), nameof(kernel), nameof(accelerator), nameof(gpuContext))]
	bool CanRender => kernel != null && accelerator != null && Bounds.Width > 0 && Bounds.Height > 0;

	SKBitmap? RenderResultImage()
	{
		if (!CanRender) return null;
		var bmpSize = PixelSize.FromSize(Bounds.Size, 1);
		var buffSize = new Index2D(bmpSize.Width, bmpSize.Height);
		lock (gpuContext)
		{
			using var outputBuffer = accelerator.Allocate2DDenseX<uint>(buffSize);
			kernel(buffSize, sourceView, outputBuffer.View);
			accelerator.Synchronize();
			var data = outputBuffer.GetRawData();
			//var bmp = new SKBitmap(bmpSize.Width, bmpSize.Height);
			//fixed (byte* p = data.Array)
			//{
			//	IntPtr ptr = (IntPtr)p;
			//	bmp.SetPixels(ptr);
			//}

			var bmp = new SKBitmap();
			// pin the managed array so that the GC doesn't move it
			var gcHandle = GCHandle.Alloc(data.Array, GCHandleType.Pinned);
			// install the pixels with the color type of the pixel data
			var info = new SKImageInfo(bmpSize.Width, bmpSize.Height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
			var res = bmp.InstallPixels(info, gcHandle.AddrOfPinnedObject(), info.RowBytes, (addr, ctx) => { gcHandle.Free(); });
			return bmp;
		}
	}

	void SkiaDraw(SKCanvas canvas)
	{
		var sw = Stopwatch.StartNew();
		using var img = RenderResultImage();
		sw.Stop();
		rndMeanTime = rndMeanTime > 0 ? 0.98 * rndMeanTime + 0.02 * sw.Elapsed.TotalMilliseconds : sw.Elapsed.TotalMilliseconds;
		InvalidateTimingText();
		if (img != null)
			canvas.DrawBitmap(img, Bounds.ToSKRect());
	}

	public override void Render(DrawingContext context) => context.Custom(skDrawer);

	#region Render FPS text
	double rndMeanTime = -1;
	IDisposable? invalidTimer;
	void InvalidateTimingText()
	{
		invalidTimer ??= DispatcherTimer.RunOnce(() =>
		{
			TimingText = $"FPS: {1000 / rndMeanTime:g4} ({rndMeanTime:g4}ms)";
			invalidTimer = null;
		}, TimeSpan.FromSeconds(0.1));
	}
	#endregion

}
