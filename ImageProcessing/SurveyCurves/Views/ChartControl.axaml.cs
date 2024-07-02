using Avalonia.Controls;
using ScottPlot.Avalonia;
using System;

namespace ImageProcessing.SurveyCurves.Views;
/* Chart - ScottPlot.NET
 * https://scottplot.net/cookbook/5.0/ - docs and examples
 * https://scottplot.net/demo/5.0/ - demos
 */
public partial class ChartControl : UserControl
{
	static readonly double[] testData1 = [1, 1, 1, 5, 5, 5, 4, 3, 2, 1, 2, 1, 6, 1];

	public ChartControl()
	{
		InitializeComponent();

		ShowSmothedData(testData1, SimpleSmoth);
	}

	void ShowSmothedData(double[] data, Func<double[], (double[], double[])> smoth)
	{
		AddBars(data);
		AddCurve(smoth(data));
		Chart.Refresh();
	}

	void AddBars(double[] data)
	{
		Chart.Plot.Add.Bars(data);
	}

	void AddCurve((double[] x, double[] y) data)
	{
		Chart.Plot.Add.Scatter(data.x, data.y);
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
