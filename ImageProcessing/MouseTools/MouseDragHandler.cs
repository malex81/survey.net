using Avalonia.Controls;
using Avalonia.Input;
using ImageProcessing.Helpers;
using ScottPlot.Interactivity.UserActions;
using System;

namespace ImageProcessing.MouseTools;
public class MouseDragHandler : IDisposable
{
	private readonly Control target;
	private readonly DisposableList dispList = [];

	private DragState? initState;

	public MouseDragHandler(Control target)
    {
		this.target = target;
		target.AddHandler(InputElement.PointerPressedEvent, OnMouseDown);
		target.AddHandler(InputElement.PointerReleasedEvent, OnMouseUp);
		target.AddHandler(InputElement.PointerMovedEvent, OnMouseMove);

		dispList.AddAction(() =>
		{
			target.RemoveHandler(InputElement.PointerPressedEvent, OnMouseDown);
			target.RemoveHandler(InputElement.PointerReleasedEvent, OnMouseUp);
			target.RemoveHandler(InputElement.PointerMovedEvent, OnMouseMove);
		});
	}

	private void OnMouseDown(object? sender, PointerPressedEventArgs e)
	{
		if (target.DataContext is not IMouseDragModel dragModel) return;
		var point = e.GetCurrentPoint(target);
		var mouseBtn = point.Properties.PointerUpdateKind.GetMouseButton();
		initState = new(point.Position, point.Position, mouseBtn);
		dragModel.DragStart(initState);
	}

	private void OnMouseUp(object? sender, PointerReleasedEventArgs e)
	{
		initState = null;
	}

	private void OnMouseMove(object? sender, PointerEventArgs e)
	{
		if (target.DataContext is not IMouseDragModel dragModel || initState == null) return;
		var point = e.GetCurrentPoint(target);
		dragModel.DragProcess(initState with { CurrentPos = point.Position });
	}

	public void Dispose()
	{
		dispList.Dispose();
		GC.SuppressFinalize(this);
	}
}
