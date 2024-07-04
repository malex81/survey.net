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
		if (e is { PropertyName: nameof(model.CurrentSample) or "" })
			RefreshChartData();
	}

	void RefreshChartData()
	{
		Chart.Plot.Clear();
		ShowSmothedData(model.CurrentSample.Data, SimpleSmoth);
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

	static (double[], double[]) SimpleSmoth(double[] data)
	{
		const double step = 0.1;
		int count = (int)Math.Floor((data.Length - 1) / step);
		var (xx, yy) = (new double[count], new double[count]);
		for (int i = 0; i < count; i++)
		{
			var x = i * step;
			xx[i] = x;
			var x1 = Math.Floor(x);
			var x2 = x1 + 1;
			var (y1, y2) = (data[(int)x1], data[(int)x2]);
			var (k1, k2) = (x2 - x, x - x1);
			yy[i] = k1 * k1 * k1 * y1 + k2 * k2 * k2 * y2;
		}
		return (xx, yy);
	}
}
