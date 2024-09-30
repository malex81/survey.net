using Avalonia;
using ILGPU;
using ILGPU.Algorithms;
using ImageProcessing.Helpers;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImageProcessing.RenderingMath;
public static class CalcProc
{
	#region GetPixel variants
	static uint GetPixel(this ArrayView<uint> source, PixelSize size, int x, int y)
		=> 0 <= x && x < size.Width && 0 <= y && y < size.Height ? source[x + y * size.Width] : 0;

	public static uint GetPixel(this ArrayView<uint> source, PixelSize size, Index2D ind)
		=> source.GetPixel(size, ind.X, ind.Y);

	public static uint GetPixel(this ArrayView<uint> source, PixelSize size, Vector2 pos)
		=> source.GetPixel(size, pos.ToIndex());

	public static uint GetPixelClamped(this ArrayView<uint> source, PixelSize size, Index2D ind)
		=> source[XMath.Clamp(ind.X, 0, size.Width - 1) + XMath.Clamp(ind.Y, 0, size.Height - 1) * size.Width];
	#endregion

	static bool ContainsPoint(this PixelSize size, Vector2 pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < size.Width && pos.Y < size.Height;

	static uint[] UnfoldColor(uint c) => [(c & 0xff000000) >> 24, (c & 0x00ff0000) >> 16, (c & 0x0000ff00) >> 8, c & 0x000000ff];
	static uint FoldColor(uint[] cc) => (cc[0] << 24) + (cc[1] << 16) + (cc[2] << 8) + cc[3];

	static uint MixColors(uint color1, uint color2, float w)
	{
		var (cc1, cc2) = (UnfoldColor(color1), UnfoldColor(color2));
		var res = new uint[4];
		for (int i = 0; i < 4; i++)
		{
			res[i] = (uint)(cc1[i] * (1 - w) + cc2[i] * w);
		}
		return FoldColor(res);
	}

	public static uint GetBilinearPixel(this ArrayView<uint> source, PixelSize size, Vector2 pos)
	{
		if (!size.ContainsPoint(pos)) return 0;
		var v1 = new Vector2(XMath.Floor(pos.X), XMath.Floor(pos.Y));
		var ind0 = v1.ToIndex();
		var diff = pos - v1;
		var (c00, c01, c10, c11) = (source.GetPixelClamped(size, ind0),
			source.GetPixelClamped(size, ind0 + new Index2D(0, 1)),
			source.GetPixelClamped(size, ind0 + new Index2D(1, 0)),
			source.GetPixelClamped(size, ind0 + new Index2D(1, 1)));
		return MixColors(MixColors(c00, c01, diff.Y), MixColors(c10, c11, diff.Y), diff.X);
	}

	static uint BSpline2MixColors(uint prev, uint cur, uint next, float t)
	{
		var (cc, cp, cn) = (UnfoldColor(cur), UnfoldColor(prev), UnfoldColor(next));
		var res = new uint[4];
		for (int i = 0; i < 4; i++)
		{
			res[i] = (uint)XMath.Round((0.5f + t * (1f - t)) * cc[i] + 0.5f * MathExt.Sqr(1f - t) * cp[i] + 0.5f * MathExt.Sqr(t) * cn[i]);
		}
		return FoldColor(res);
	}
	public static uint GetBSpline2Pixel(this ArrayView<uint> source, PixelSize size, Vector2 pos)
	{
		if (!size.ContainsPoint(pos)) return 0;
		var v0 = new Vector2(pos.X + 0.5f, pos.Y + 0.5f);
		var v1 = new Vector2(XMath.Floor(v0.X), XMath.Floor(v0.Y));
		var ind0 = v1.ToIndex();
		var diff = v0 - v1;
		var yy = new uint[3];
		for (int indY = 0; indY < 3; indY++)
		{
			yy[indY] = BSpline2MixColors(
				source.GetPixelClamped(size, ind0 + new Index2D(-1, indY - 1)),
				source.GetPixelClamped(size, ind0 + new Index2D(0, indY - 1)),
				source.GetPixelClamped(size, ind0 + new Index2D(1, indY - 1)),
				diff.X);
		}
		return BSpline2MixColors(yy[0], yy[1], yy[2], diff.Y);
	}
}
