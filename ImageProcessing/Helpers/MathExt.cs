using System.Numerics;

namespace ImageProcessing.Helpers;
public static class MathExt
{
	public static float Sqr(float v) => v * v;
	public static double Sqr(double v) => v * v;
	public static float Cube(float v) => v * v * v;
	public static double Cube(double v) => v * v * v;

	public static float GetScale(this Matrix3x2 tr)
	{
		var v0 = Vector2.Transform(new(0, 0), tr);
		var v1 = Vector2.Transform(new(1, 0), tr);
		return (v1 - v0).Length();
	}
}
