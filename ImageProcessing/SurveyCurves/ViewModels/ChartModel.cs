using CommunityToolkit.Mvvm.ComponentModel;
using ImageProcessing.Helpers;
using System;
using System.Linq;

namespace ImageProcessing.SurveyCurves.ViewModels;

public record UniformDataSample(string Name, double[] Data);
public record SmoothFunc(string Name, Func<double[], (double[], double[])> Func);

public partial class ChartModel : ObservableObject
{
	const double smoothStep = 0.05;

	public static readonly UniformDataSample[] InputSamples = [
		new("Единицы", Enumerable.Repeat(1.0, 20).ToArray()),
		new("Простой", [1, 1, 1, 5, 5, 5, 4, 3, 2, 1, 1, 2, 2.2, 2.4, 3, 3.6, 1, 7, 1.1, 1, 6, 0.5, 1, 6.7, 7, 4]),
	];

	public static readonly SmoothFunc[] SmoothFuncs = [
		new("B-Spline linear", BSpline1),
		new("B-Spline square", BSpline2),
		new("B-Spline 1.5", BSpline2_1),
		new("Cubic interpolation", CubicInterpolation),
		new("Temp", SimpleSmooth),
	];

	[ObservableProperty]
	private UniformDataSample currentSample = InputSamples[1];
	[ObservableProperty]
	private SmoothFunc currentSmooth = SmoothFuncs[0];

	public ChartModel()
	{
	}

	static double GetMeanDerivative(double v1, double v2)
	{
		if (v1 * v2 <= 0) return 0;
		var vMax = 3 * Math.Min(Math.Abs(v1), Math.Abs(v2));
		var v = (v1 + v2) / 2;
		return Math.Abs(v) > vMax ? Math.Sign(v) * vMax : v;
	}
	static (double[], double[]) CubicInterpolation(double[] data)
	{
		int count = (int)Math.Ceiling((data.Length - 1) / smoothStep);
		var (xx, yy) = (new double[count], new double[count]);
		for (int i = 0; i < count; i++)
		{
			var x = i * smoothStep;
			xx[i] = x;
			var x0 = (int)Math.Floor(x);
			var (ym1, y0, y1, y2) = (data.GetClamped(x0 - 1), data.GetClamped(x0), data.GetClamped(x0 + 1), data.GetClamped(x0 + 2));
			var (_vm1, _v0, _v1) = (y0 - ym1, y1 - y0, y2 - y1);
			var (v0, v1) = (GetMeanDerivative(_vm1, _v0), GetMeanDerivative(_v1, _v0));//(_vm1 * _v0 > 0 ? (_vm1 + _v0) / 2 : 0, _v1 * _v0 > 0 ? (_v1 + _v0) / 2 : 0);
			var a = v0 + v1 - 2 * (y1 - y0);
			var b = 3 * (y1 - y0) - 2 * v0 - v1;
			var t = x - x0;
			yy[i] = a * MathExt.Cube(t) + b * MathExt.Sqr(t) + v0 * t + y0;
		}
		return (xx, yy);
	}

	static (double[], double[]) BSpline1(double[] data)
	{
		int count = (int)Math.Ceiling((data.Length - 1) / smoothStep);
		var (xx, yy) = (new double[count], new double[count]);
		for (int i = 0; i < count; i++)
		{
			var x = i * smoothStep;
			xx[i] = x;
			var xn = (int)Math.Floor(x);
			var xn1 = xn + 1;
			yy[i] = (x - xn) * data.GetClamped(xn1) + (xn1 - x) * data.GetClamped(xn);
		}
		return (xx, yy);
	}

	static (double[], double[]) BSpline2(double[] data)
	{
		int count = (int)Math.Ceiling((data.Length - 1) / smoothStep);
		var (xx, yy) = (new double[count], new double[count]);
		for (int i = 0; i < count; i++)
		{
			var x = i * smoothStep;
			xx[i] = x;
			var x1 = x + 0.5;
			var n = (int)Math.Floor(x1);
			var t = x1 - n;
			yy[i] = (0.5 + t * (1 - t)) * data.GetClamped(n) + 0.5 * MathExt.Sqr(1 - t) * data.GetClamped(n - 1) + 0.5 * MathExt.Sqr(t) * data.GetClamped(n + 1);
		}
		return (xx, yy);
	}

	static (double[], double[]) BSpline2_1(double[] data)
	{
		int count = (int)Math.Ceiling((data.Length - 1) / smoothStep);
		var (xx, yy) = (new double[count], new double[count]);
		for (int i = 0; i < count; i++)
		{
			var x = i * smoothStep;
			xx[i] = x;
			var x1 = x + 0.25;
			var n = (int)Math.Floor(x1);
			var n2 = (int)Math.Floor(x1 * 2);
			var t = x1 * 2 - n2;
			var (dn, dn1) = (data.GetClamped(n), data.GetClamped(n + 1));
			var (d1, d2, d3) = n2 == 2 * n ? ((data.GetClamped(n - 1) + dn) / 2, dn, (dn1 + dn) / 2) : (dn, (dn1 + dn) / 2, dn1);
			yy[i] = d1 * 0.5 * MathExt.Sqr(1 - t) + d2 * (0.5 + t * (1 - t)) + d3 * 0.5 * MathExt.Sqr(t);
		}
		return (xx, yy);
	}

	static (double[], double[]) SimpleSmooth(double[] data)
	{
		int count = (int)Math.Floor((data.Length - 1) / smoothStep);
		var (xx, yy) = (new double[count], new double[count]);
		for (int i = 0; i < count; i++)
		{
			var x = i * smoothStep;
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
