using Avalonia;
using System.Numerics;

namespace ImageProcessing.Helpers;
public static class MetricHelper
{
	public static Vector2 ToVector(this Size sz) => new((float)sz.Width, (float)sz.Height);
	public static Vector2 ToVector(this Point p) => new((float)p.X, (float)p.Y);
}
