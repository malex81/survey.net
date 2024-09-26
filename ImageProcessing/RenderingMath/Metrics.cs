using Avalonia;
using ILGPU;
using System.Numerics;

namespace ImageProcessing.RenderingMath;
public static class Metrics
{
	public static PixelPoint ToPixel(this Vector2 v) => new((int)v.X, (int)v.Y);
}
