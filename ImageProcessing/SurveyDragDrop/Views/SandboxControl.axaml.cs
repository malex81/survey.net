using Avalonia;
using Avalonia.Controls;
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

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		if (dragWnd == null)
		{
			dragWnd = new();
			dragWnd.Show();
		}
		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		//CloseDragWnd();
		base.OnPointerReleased(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		if (dragWnd != null)
		{
			if (e.Pointer.Captured == null)
			{
				e.Pointer.Capture(this);
			}
			var pos = this.PointToScreen(e.GetPosition(this));
			dragWnd.Position = pos - new PixelPoint(40, 40);
		}
		base.OnPointerMoved(e);
	}

	public void Dispose()
	{
		CloseDragWnd();
		GC.SuppressFinalize(this);
	}
}