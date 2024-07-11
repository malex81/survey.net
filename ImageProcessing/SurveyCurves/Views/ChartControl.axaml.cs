using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using ImageProcessing.SurveyCurves.ViewModels;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using System;
using System.ComponentModel;

namespace ImageProcessing.SurveyCurves.Views;
/* Chart - ScottPlot.NET
 * https://scottplot.net/cookbook/5.0/ - docs and examples
 * https://scottplot.net/demo/5.0/ - demos
 */
public partial class ChartControl : UserControl
{
	private readonly static Cursor NoneCursor = new(StandardCursorType.None);

	private readonly ChartModel model = new();
	private readonly Crosshair cross;

	public ChartControl()
	{
		InitializeComponent();
		DataContext = model;
		model.PropertyChanged += Model_PropertyChanged;

		cross = Chart.Plot.Add.Crosshair(0, 0);
		cross.IsVisible = false;
		cross.MarkerShape = MarkerShape.OpenCircle;
		cross.MarkerSize = 10;

		Chart.PointerMoved += Chart_PointerMoved;
		Chart.PointerExited += Chart_PointerExited;
		RefreshChartData();
	}

	private void Chart_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
	{
		cross.IsVisible = false;
	}

	private void Chart_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
	{
		if (sender is not Control ctrl) return;
		var point = e.GetCurrentPoint(ctrl);
		Pixel mousePixel = new(point.Position.X, point.Position.Y);
		Coordinates mouseLocation = Chart.Plot.GetCoordinates(mousePixel);
		cross.Position = mouseLocation;
		cross.IsVisible = true;
		Chart.Cursor = NoneCursor;
		Chart.Refresh();
	}

	private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e is { PropertyName: nameof(model.CurrentSample) or nameof(model.CurrentSmooth) })
			RefreshChartData();
	}

	void RefreshChartData()
	{
		Chart.Plot.Clear();

		Chart.Cursor = null;
		Chart.Plot.PlottableList.Add(cross);
		cross.IsVisible = false;

		ShowSmothedData(model.CurrentSample.Data, model.CurrentSmooth.Func);
	}

	void ShowSmothedData(double[] data, Func<double[], (double[], double[])> smoth)
	{
		AddBars(data);
		AddCurve(smoth(data));
		Chart.Plot.Axes.AutoScale();
		Chart.Refresh();
	}

	void AddBars(double[] data)
	{
		var bars = Chart.Plot.Add.Bars(data);
		bars.Color = new Color(0, 80, 255, 100);
	}

	void AddCurve((double[] x, double[] y) data)
	{
		var scatter = Chart.Plot.Add.Scatter(data.x, data.y);
		scatter.Color = Colors.Green;
		scatter.LineWidth = 2;
	}	
}
