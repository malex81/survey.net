using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace Multivox.Avalonia.Base.SkiaHelpers;
/* 
 * Custom draw: https://github.com/AvaloniaUI/Avalonia/blob/master/samples/RenderDemo/Pages/CustomSkiaPage.cs
 */
public abstract class SimpleDrawOperation : ICustomDrawOperation
{
	public SimpleDrawOperation()
	{
	}

	public bool IsHitTestVisible { get; set; } = false;
	public Rect Bounds { get; set; }
	public bool Equals(ICustomDrawOperation? other) => other == this;
	public bool HitTest(Point p)
	{
		if (!IsHitTestVisible) return false;
		return Bounds.Contains(p);
	}

	protected abstract void DrawSkia(SKCanvas canvas);

	public virtual void Render(ImmediateDrawingContext context)
	{
		var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
		if (leaseFeature == null) return;
		using var lease = leaseFeature.Lease();
		lease.SkCanvas.ClipRect(Bounds.ToSKRect());
		DrawSkia(lease.SkCanvas);
	}

	#region Dispose
	protected virtual void Dispose(bool disposing) { }
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	~SimpleDrawOperation() => Dispose(false);
	#endregion
}
