using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;

namespace ImageProcessing.SurveyDragDrop.Views;

public partial class SandboxControl : UserControl, IDisposable
{
	private DragWnd? dragWnd;

	public SandboxControl()
	{
		InitializeComponent();

		DragDrop.SetAllowDrop(this, true);
		AddHandler(DragDrop.DragOverEvent, (s, e) =>
		{
			var pos = this.PointToScreen(e.GetPosition(this));
			UpdateDragWndPosition(pos);
		});
	}

	void UpdateDragWndPosition(PixelPoint pos)
	{
		if (dragWnd != null)
			dragWnd.Position = pos - new PixelPoint(50, 20);
	}

	void ToggleDragWnd()
	{
		if (dragWnd != null)
		{
			dragWnd.Close();
			dragWnd = null;
		}
		else
		{
			dragWnd = new();
			dragWnd.Show();
		}
	}
	void CloseDragWnd()
	{
		dragWnd?.Close();
		dragWnd = null;
	}

	protected override async void OnPointerPressed(PointerPressedEventArgs e)
	{
		if (dragWnd == null)
		{
			dragWnd = new();
			dragWnd.Show();
			//var mainWnd = (TopLevel.GetTopLevel(this) as WindowBase)!;
			//mainWnd.Activate();
			//e.Pointer.Capture(this);
			DragDrop.SetAllowDrop(dragWnd, true);
			void onDragOverPreview(object? sender, DragEventArgs e)
			{
				//RaiseEvent(e);
				//var pos = dragWnd.PointToScreen(e.GetPosition(dragWnd));
				//UpdateDragWndPosition(pos);
			}
			dragWnd.AddHandler(DragDrop.DragOverEvent, onDragOverPreview);
			await DragDrop.DoDragDrop(e, new DataObject(), DragDropEffects.Copy);
			dragWnd.RemoveHandler(DragDrop.DragOverEvent, onDragOverPreview);
			CloseDragWnd();
		}
		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		CloseDragWnd();
		base.OnPointerReleased(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		//if (dragWnd != null)
		//{
		//	if (e.Pointer.Captured == null)
		//	{
		//		e.Pointer.Capture(this);
		//	}
		//	var pos = this.PointToScreen(e.GetPosition(this));
		//	dragWnd.Position = pos - new PixelPoint(40, 40);
		//}
		base.OnPointerMoved(e);
	}

	public void Dispose()
	{
		CloseDragWnd();
		GC.SuppressFinalize(this);
	}
}