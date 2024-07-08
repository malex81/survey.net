using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ImageProcessing.SurveyCurves.ViewModels;

public record UniformDataSample(string Name, double[] Data);
public record SmoothFunc(string Name, Func<double[], (double[], double[])> Func);

public partial class ChartModel : ObservableObject
{
	public static readonly UniformDataSample[] InputSamples = [
		new("Единицы", [1, 1, 1, 1, 1, 1, 1, 1, 1, 1]),
		new("Простой", [1, 1, 1, 5, 5, 5, 4, 3, 2, 1, 2, 1, 6, 1]),
	];

	public static readonly SmoothFunc[] SmoothFuncs = [
		new("Temp", SimpleSmooth)
	];

	[ObservableProperty]
	private UniformDataSample currentSample = InputSamples[1];
	[ObservableProperty]
	private SmoothFunc currentSmooth = SmoothFuncs[0];

	public ChartModel()
	{
	}

	static (double[], double[]) SimpleSmooth(double[] data)
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
