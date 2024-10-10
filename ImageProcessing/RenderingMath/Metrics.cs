using Avalonia;
using ILGPU;
using System.Numerics;

namespace ImageProcessing.RenderingMath;
public static class Metrics
{
	public static PixelPoint ToPixel(this Vector2 v) => new((int)v.X, (int)v.Y);
	public static Index1D ToIndex1D(this Index2D ind2, int stride) => ind2.X + ind2.Y * stride;
	public static Index2D ToIndex2D(this Index1D ind1, int stride) => (ind1 % stride, ind1 / stride);
}
