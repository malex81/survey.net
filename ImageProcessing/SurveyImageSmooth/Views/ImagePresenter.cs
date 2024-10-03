using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Avalonia.Threading;
using ILGPU;
using ILGPU.Runtime;
using ImageProcessing.MouseTools;
using ImageProcessing.SkiaHelpers;
using ImageProcessing.SurveyImageSmooth.Engine;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ImageProcessing.SurveyImageSmooth.Views;
internal class ImagePresenter : Control
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

	#region DrawParams Direct Avalonia Property
	private BitmapDrawParams drawParams = default;

	public static readonly DirectProperty<ImagePresenter, BitmapDrawParams> DrawParamsProperty =
		AvaloniaProperty.RegisterDirect<ImagePresenter, BitmapDrawParams>(nameof(DrawParams), o => o.DrawParams, (o, v) => o.DrawParams = v);

	public BitmapDrawParams DrawParams
	{
		get => drawParams;
		set
		{
			SetAndRaise(DrawParamsProperty, ref drawParams, value);
			InvalidateVisual();
		}
	}
	#endregion DrawParams Direct Avalonia Property

	private readonly CustomActionDrawer skDrawer;
	private readonly ImageRendering? imageRendering;

	public ImagePresenter()
	{
		skDrawer = new(SkiaDraw, this);
		_ = new MouseDragHandler(this);
		if (!Design.IsDesignMode)
			imageRendering = new(this);
	}

	void UpdateSource()
	{
		if (imageRendering == null) return;
		lock (imageRendering)
		{
			imageRendering.FreeSource();
			if (sourceBitmap != null)
				imageRendering.BuildDrawKernel(sourceBitmap, () => drawParams);
		}
		InvalidateVisual();
	}

	[MemberNotNullWhen(true, nameof(imageRendering))]
	bool CanRender => imageRendering != null && Bounds.Width > 0 && Bounds.Height > 0;

	SKBitmap? RenderResultImage()
	{
		if (!CanRender) return null;
		lock (imageRendering)
		{
			return imageRendering.RenderBitmap();
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

	public override void Render(DrawingContext context)
	{
		context.Custom(skDrawer);
		//Dispatcher.UIThread.Post(InvalidateVisual);
	}

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
