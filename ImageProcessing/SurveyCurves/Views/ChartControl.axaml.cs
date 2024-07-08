using Avalonia.Controls;
using ImageProcessing.SurveyCurves.ViewModels;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.ComponentModel;

namespace ImageProcessing.SurveyCurves.Views;
/* Chart - ScottPlot.NET
 * https://scottplot.net/cookbook/5.0/ - docs and examples
 * https://scottplot.net/demo/5.0/ - demos
 */
public partial class ChartControl : UserControl
{
	private readonly ChartModel model = new();

	public ChartControl()
	{
		InitializeComponent();
		DataContext = model;
		model.PropertyChanged += Model_PropertyChanged;
		RefreshChartData();
	}

	private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e is { PropertyName: nameof(model.CurrentSample) or nameof(model.CurrentSmooth) })
			RefreshChartData();
	}

	void RefreshChartData()
	{
		Chart.Plot.Clear();
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
