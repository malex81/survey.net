using ILGPU.Algorithms;
using System;

namespace ImageProcessing.RenderingMath;
public readonly record struct XColor(float A, float R, float G, float B)
{
	static (uint, uint, uint, uint) UnfoldColor(uint c) => ((c & 0xff000000) >> 24, (c & 0x00ff0000) >> 16, (c & 0x0000ff00) >> 8, c & 0x000000ff);
	static uint FoldColor(uint[] cc) => (cc[0] << 24) + (cc[1] << 16) + (cc[2] << 8) + cc[3];

	public static XColor FromUint(uint v)
	{
		var (a, r, g, b) = UnfoldColor(v);
		return new(a, r, g, b);
	}

	readonly float[] Components => [A, R, G, B];

	public readonly uint ToUint()
	{
		var ccF = Components;
		var cc = new uint[4];
		for (int i = 0; i < 4; i++)
			cc[i] = XMath.Clamp((uint)XMath.Round(ccF[i]), 0, 255);
		return FoldColor(cc);
	}

	public static implicit operator uint(XColor c) => c.ToUint();
	public static implicit operator XColor(uint v) => FromUint(v);

	public static XColor operator -(XColor c) => new(-c.A, -c.R, -c.G, -c.B);
	public static XColor operator +(XColor c1, XColor c2) => new(c1.A + c2.A, c1.R + c2.R, c1.G + c2.G, c1.B + c2.B);
	public static XColor operator -(XColor a, XColor b) => a + (-b);
	public static XColor operator *(XColor c, float k) => new(c.A * k, c.R * k, c.G * k, c.B * k);
	public static XColor operator *(float k, XColor c) => c * k;
	public static XColor operator /(XColor c, float k)
	{
		if (k == 0)
			throw new DivideByZeroException();
		return c * (1 / k);
	}
}
