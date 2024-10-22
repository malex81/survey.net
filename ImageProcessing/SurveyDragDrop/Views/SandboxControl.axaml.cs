using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ImageProcessing.SurveyDragDrop.Views;

public partial class SandboxControl : UserControl
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

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		dragWnd = new();
		dragWnd.Show();
		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		dragWnd?.Close();
		dragWnd = null;
		base.OnPointerReleased(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		if (dragWnd != null)
		{
			var pos = this.PointToScreen(e.GetPosition(this));
			dragWnd.Position = pos - new PixelPoint(40, 40);
		}
		base.OnPointerMoved(e);
	}
}