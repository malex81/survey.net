using ILGPU.Algorithms;
using System;

namespace ImageProcessing.RenderingMath;
public readonly struct XColor(float a, float r, float g, float b)
{

	static (uint, uint, uint, uint) UnfoldColor(uint c) => ((c & 0xff000000) >> 24, (c & 0x00ff0000) >> 16, (c & 0x0000ff00) >> 8, c & 0x000000ff);
	static uint FoldColor(uint[] cc) => (cc[0] << 24) + (cc[1] << 16) + (cc[2] << 8) + cc[3];

	public static XColor FromUint(uint v)
	{
		var (a, r, g, b) = UnfoldColor(v);
		return new(a, r, g, b);
	}

	private readonly float a = a;
	private readonly float r = r;
	private readonly float g = g;
	private readonly float b = b;

	readonly float[] Components => [a, r, g, b];

	public readonly uint ToUint()
	{
		var ccF = Components;
		var cc = new uint[4];
		for (int i = 0; i < 4; i++)
			cc[i] = XMath.Clamp((uint)XMath.Round(ccF[i]), 0, 255);
		return FoldColor(cc);
	}

	public static implicit operator uint(XColor c) => c.ToUint();

	public static XColor operator -(XColor c) => new(-c.a, -c.r, -c.g, -c.b);
	public static XColor operator +(XColor c1, XColor c2) => new(c1.a + c2.a, c1.r + c2.r, c1.g + c2.g, c1.b + c2.b);
	public static XColor operator -(XColor a, XColor b) => a + (-b);
	public static XColor operator *(XColor c, float k) => new(c.a * k, c.r * k, c.g * k, c.b * k);
	public static XColor operator *(float k, XColor c) => c * k;
	public static XColor operator /(XColor c, float k)
	{
		if (k == 0)
			throw new DivideByZeroException();
		return c * (1 / k);
	}
}
