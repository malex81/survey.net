using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using ImageProcessing.Helpers;
using ImageProcessing.SkiaHelpers;
using ScottPlot.Colormaps;
using SkiaSharp;
using System;

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
		set { SetAndRaise(SourceBitmapProperty, ref sourceBitmap, value); }
	}
	#endregion SourceBitmap Direct Avalonia Property

	private readonly CustomActionDrawer skDrawer;
	private IDisposable? sourcePeg;

	public ImagePresenter()
	{
		skDrawer = new(SkiaDraw, this);
	}

	void UpdateSource()
	{
		sourcePeg?.Dispose();
		sourcePeg = sourceBitmap != null ? LoadImageSource() : null;
	}

	IDisposable LoadImageSource()
	{
		if (sourceBitmap == null) throw new InvalidOperationException("sourceBitmap == null");
		var disp = new DisposableList();

		return disp;
	}

	void SkiaDraw(SKCanvas canvas)
	{
		//using (SKPaint draftPaint = new() { FilterQuality = SKFilterQuality.Medium })
		//	canvas.DrawBitmap(bmp, Bounds.ToSKRect(), draftPaint);
	}

	public override void Render(DrawingContext context) => context.Custom(skDrawer);
}
