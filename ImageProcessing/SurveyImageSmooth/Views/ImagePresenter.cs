using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;
using ImageProcessing.SkiaHelpers;
using ScottPlot.Colormaps;
using SkiaSharp;

namespace ImageProcessing.SurveyImageSmooth.Views;
internal class ImagePresenter : Control
{
	private readonly CustomActionDrawer skDrawer;

	public ImagePresenter()
	{
		skDrawer = new(SkiaDraw, this);
	}

	void SkiaDraw(SKCanvas canvas)
	{
		//using (SKPaint draftPaint = new() { FilterQuality = SKFilterQuality.Medium })
		//	canvas.DrawBitmap(bmp, Bounds.ToSKRect(), draftPaint);
	}

	public override void Render(DrawingContext context) => context.Custom(skDrawer);
}
