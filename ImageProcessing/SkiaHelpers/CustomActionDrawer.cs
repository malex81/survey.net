using Avalonia;
using ImageProcessing.Helpers;
using Multivox.Avalonia.Base.SkiaHelpers;
using SkiaSharp;
using System;

namespace ImageProcessing.SkiaHelpers;
public class CustomActionDrawer : SimpleDrawOperation
{
	private readonly Action<SKCanvas> drawAction;
	private readonly DisposableList dispList = [];

	public CustomActionDrawer(Action<SKCanvas> drawAction, Visual? parent = null)
	{
		this.drawAction = drawAction;
		IsHitTestVisible = true;
		parent?.GetObservable(Visual.BoundsProperty).Subscribe(b =>
		{
			Bounds = b;
		}).DisposeWith(dispList);
	}

	protected override void DrawSkia(SKCanvas canvas) => drawAction(canvas);

	protected override void Dispose(bool disposing)
	{
		dispList.Dispose();
		base.Dispose(disposing);
	}
}
