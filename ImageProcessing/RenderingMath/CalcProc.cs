using Avalonia;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using ImageProcessing.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SkiaSharp;
using System;
using System.Numerics;

namespace ImageProcessing.RenderingMath;
public static class CalcProc
{
	internal static Vector2 Round(this Vector2 v) => new(XMath.Round(v.X), XMath.Round(v.Y));
	internal static Vector2 Floor(this Vector2 v) => new(XMath.Floor(v.X), XMath.Floor(v.Y));

	static bool ContainsPoint(this PixelSize size, Vector2 pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < size.Width && pos.Y < size.Height;

	static uint[] UnfoldColor(uint c) => [(c & 0xff000000) >> 24, (c & 0x00ff0000) >> 16, (c & 0x0000ff00) >> 8, c & 0x000000ff];
	static uint FoldColor(uint[] cc) => (cc[0] << 24) + (cc[1] << 16) + (cc[2] << 8) + cc[3];

	#region GetPixel variants
	static uint GetPixel(this ArrayView<uint> source, PixelSize size, int x, int y)
		=> 0 <= x && x < size.Width && 0 <= y && y < size.Height ? source[x + y * size.Width] : 0;

	public static uint GetPixel(this ArrayView<uint> source, PixelSize size, Index2D ind)
		=> source.GetPixel(size, ind.X, ind.Y);

	public static uint GetPixelClamped(this ArrayView<uint> source, PixelSize size, Index2D ind)
		=> source[XMath.Clamp(ind.X, 0, size.Width - 1) + XMath.Clamp(ind.Y, 0, size.Height - 1) * size.Width];

	public static XColor GetColorClamped(this ArrayView<uint> source, PixelSize size, Index2D ind) => source.GetPixelClamped(size, ind);
	#endregion

	#region Interpolation
	public static uint GetNearestPixel(this ArrayView<uint> source, PixelSize size, Vector2 pos)
		=> size.ContainsPoint(pos) ? source.GetPixelClamped(size, pos.Round().ToIndex()) : 0;

	static uint MixColors(uint color1, uint color2, float w)
	{
		var (cc1, cc2) = (UnfoldColor(color1), UnfoldColor(color2));
		var res = new uint[4];
		for (int i = 0; i < 4; i++)
			res[i] = (uint)XMath.Round(cc1[i] * (1 - w) + cc2[i] * w);
		return FoldColor(res);
	}
	static uint MixColors4(uint c00, uint c01, uint c10, uint c11, float w0, float w1)
	{
		var cc = new uint[] { c00, c01, c10, c11 };
		var ww = new float[] { (1 - w0) * (1 - w1), w0 * (1 - w1), (1 - w0) * w1, w0 * w1 };
		XColor res = new();
		for (int i = 0; i < 4; i++)
			res += ww[i] * XColor.FromUint(cc[i]);
		return res;
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
		//return MixColors(MixColors(c00, c01, diff.Y), MixColors(c10, c11, diff.Y), diff.X);
		//return MixColors(diff.Y<0.5?c00:c01, diff.Y < 0.5 ? c10 : c11, diff.X);
		return MixColors4(c00, c01, c10, c11, diff.Y, diff.X);
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

	public static uint GetBSpline1_5Pixel(this ArrayView<uint> source, PixelSize size, Vector2 pos)
	{
		if (!size.ContainsPoint(pos)) return 0;
		var v0 = new Vector2(pos.X + 0.25f, pos.Y + 0.25f);
		var ind0 = v0.Floor().ToIndex();
		var v2 = (2 * v0).Floor();
		var ind2 = v2.ToIndex();
		var diff = 2 * v0 - v2;

		var yy = new uint[3];
		for (int indY = 0; indY < 3; indY++)
		{
			uint[] cc = [
				source.GetPixelClamped(size, ind0 + new Index2D(-1, indY - 1)),
				source.GetPixelClamped(size, ind0 + new Index2D(0, indY - 1)),
				source.GetPixelClamped(size, ind0 + new Index2D(1, indY - 1))
			];
			uint[] ccX = ind2.X == 2 * ind0.X ? [MixColors(cc[0], cc[1], 0.5f), cc[1], MixColors(cc[2], cc[1], 0.5f)]
											: [cc[1], MixColors(cc[2], cc[1], 0.5f), cc[2]];
			yy[indY] = BSpline2MixColors(ccX[0], ccX[1], ccX[2], diff.X);
		}
		uint[] ccY = ind2.Y == 2 * ind0.Y ? [MixColors(yy[0], yy[1], 0.5f), yy[1], MixColors(yy[2], yy[1], 0.5f)]
										: [yy[1], MixColors(yy[2], yy[1], 0.5f), yy[2]];
		return BSpline2MixColors(ccY[0], ccY[1], ccY[2], diff.Y);
	}

	static float GetMeanDerivative(float v1, float v2)
	{
		if (v1 * v2 <= 0) return 0;
		var vMax = 3 * XMath.Min(XMath.Abs(v1), XMath.Abs(v2));
		var v = (v1 + v2) / 2;
		return XMath.Abs(v) > vMax ? XMath.Sign(v) * vMax : v;
	}
	static float CalcCubicValue(float ym1, float y0, float y1, float y2, float t)
	{
		var (_vm1, _v0, _v1) = (y0 - ym1, y1 - y0, y2 - y1);
		var (v0, v1) = (GetMeanDerivative(_vm1, _v0), GetMeanDerivative(_v1, _v0));
		var a = v0 + v1 - 2 * (y1 - y0);
		var b = 3 * (y1 - y0) - 2 * v0 - v1;
		return a * MathExt.Cube(t) + b * MathExt.Sqr(t) + v0 * t + y0;
	}
	public static uint GetBicubicPixel(this ArrayView<uint> source, PixelSize size, Vector2 pos)
	{
		if (!size.ContainsPoint(pos)) return 0;

		var v1 = new Vector2(XMath.Floor(pos.X), XMath.Floor(pos.Y));
		var ind0 = v1.ToIndex();
		var diff = pos - v1;

		var yy = new float[4, 4];
		for (int iy = 0; iy < 4; iy++)
		{
			var xx = new uint[4, 4];
			for (int ix = 0; ix < 4; ix++)
			{
				var cc = UnfoldColor(source.GetPixelClamped(size, ind0 + new Index2D(ix - 1, iy - 1)));
				for (int ic = 0; ic < 4; ic++)
					xx[ix, ic] = cc[ic];
			}
			for (int ic = 0; ic < 4; ic++)
				yy[iy, ic] = CalcCubicValue(xx[0, ic], xx[1, ic], xx[2, ic], xx[3, ic], diff.X);
		}
		var res = new uint[4];
		for (int ic = 0; ic < 4; ic++)
			res[ic] = (uint)XMath.Clamp(CalcCubicValue(yy[0, ic], yy[1, ic], yy[2, ic], yy[3, ic], diff.Y), 0, 255);
		return FoldColor(res);
	}

	/*
	public static T GetAbstractPixelClamped<T>(this ArrayView<T> source, PixelSize size, Index2D ind) where T : unmanaged, INumber<T>
			=> source[XMath.Clamp(ind.X, 0, size.Width - 1) + XMath.Clamp(ind.Y, 0, size.Height - 1) * size.Width];

	static XColor GetMeanDerivative(XColor c1, XColor c2)
	{
		float[] res = new float[4];
		for (int i = 0; i < 4; i++)
		{
			var (v1, v2) = (c1.Components[i], c2.Components[i]);
			if (v1 * v2 <= 0) return 0;
			var vMax = 3 * XMath.Min(XMath.Abs(v1), XMath.Abs(v2));
			var v = (v1 + v2) / 2;
			res[i] = XMath.Abs(v) > vMax ? XMath.Sign(v) * vMax : v;
		}
		return new(res[0], res[1], res[2], res[3]);
	}

	static XColor CalcCubicValue(XColor ym1, XColor y0, XColor y1, XColor y2, float t)
	{
		var (_vm1, _v0, _v1) = (y0 - ym1, y1 - y0, y2 - y1);
		var (v0, v1) = (GetMeanDerivative(_vm1, _v0), GetMeanDerivative(_v1, _v0));
		var a = v0 + v1 - 2f * (y1 - y0);
		var b = 3f * (y1 - y0) - 2f * v0 - v1;
		return a * MathExt.Cube(t) + b * MathExt.Sqr(t) + v0 * t + y0;
	}

	public static T GetBicubicPixel<T>(this ArrayView<T> source, PixelSize size, Vector2 pos) where T : unmanaged, INumber<T>
	{
		if (!size.ContainsPoint(pos)) return T.CreateChecked(0);

		var v1 = new Vector2(XMath.Floor(pos.X), XMath.Floor(pos.Y));
		var ind0 = v1.ToIndex();
		var diff = pos - v1;

		var yy = new XColor[4];
		for (int iy = 0; iy < 4; iy++)
		{
			var xx = new XColor[4];
			for (int ix = 0; ix < 4; ix++)
			{
				var cc = source.GetAbstractPixelClamped(size, ind0 + new Index2D(ix - 1, iy - 1));
				xx[ix] = uint.CreateChecked(cc);
				//if (cc is uint) xx[ix] = uint.CreateChecked(cc);
				//else if (cc is float cf) xx[ix] = new(0, 0, 0, cf);
			}
			yy[iy] = CalcCubicValue(xx[0], xx[1], xx[2], xx[3], diff.X);
		}
		var res = CalcCubicValue(yy[0], yy[1], yy[2], yy[3], diff.Y);
		return res.A == 0 ? T.CreateChecked(res.B) : T.CreateChecked(res.ToUint());
	}
	*/
	#endregion

	#region Prefilters
	public static uint GetConvolutionPixel<TStride>(this ArrayView<uint> source, PixelSize size, Index2D ind, ArrayView2D<float, TStride> matrix, bool holdAlpha)
		where TStride : struct, IStride2D
	{
		var dim = matrix.IntExtent;
		var (wShift, hShift) = (dim.X / 2, dim.Y / 2);
		/*	float[] res = new float[4];
			for (int xi = 0; xi < dim.X; xi++)
				for (int yi = 0; yi < dim.Y; yi++)
				{
					var pix = source.GetPixelClamped(size, ind + new Index2D(xi - wShift, yi - hShift));
					var cc = UnfoldColor(pix);
					for (int ic = 0; ic < 4; ic++)
					{
						res[ic] += cc[ic] * matrix[xi, yi];
					}
				}
			var ccRes = new uint[4];
			for (int i = 0; i < 4; i++)
				ccRes[i] = XMath.Clamp((uint)XMath.Round(res[i]), 0, 255);
			return FoldColor(ccRes);
		*/
		XColor res = 0;
		for (int xi = 0; xi < dim.X; xi++)
			for (int yi = 0; yi < dim.Y; yi++)
				res += source.GetColorClamped(size, ind + new Index2D(xi - wShift, yi - hShift)) * matrix[xi, yi];
		if (holdAlpha)
			res = res with { A = source.GetColorClamped(size, ind).A };
		return res;

	}
	public static uint GetConvolutionPixel(this ArrayView<uint> source, PixelSize size, Index2D ind, float[,] matrix, bool holdAlpha)
	{
		var (mWidth, mHeight) = (matrix.GetLength(0), matrix.GetLength(1));
		var (wShift, hShift) = (mWidth / 2, mHeight / 2);
		XColor res = 0;
		for (int xi = 0; xi < mWidth; xi++)
			for (int yi = 0; yi < mHeight; yi++)
				res += source.GetColorClamped(size, ind + new Index2D(xi - wShift, yi - hShift)) * matrix[xi, yi];
		if (holdAlpha)
			res = res with { A = source.GetColorClamped(size, ind).A };
		return res;
	}
	public static uint GetEdgePixel(this ArrayView<uint> source, PixelSize size, Index2D ind)
	{
		//float[,] matrix = {{ 0, -1, 0},
		//					{-1, 4, -1},
		//					{0, -1, 0}};
		float[,] matrix = {{ -1, -1, -1},
							{-1, 8, -1},
							{-1, -1, -1}};
		return source.GetConvolutionPixel(size, ind, matrix, true);
	}
	// https://ru.wikipedia.org/wiki/%D0%A0%D0%B0%D0%B7%D0%BC%D1%8B%D1%82%D0%B8%D0%B5_%D0%BF%D0%BE_%D0%93%D0%B0%D1%83%D1%81%D1%81%D1%83
	// https://github.com/m4rs-mt/ILGPU.Samples/blob/master/Src/SharedMemory/Program.cs - shared memory exampl
	public static float[,] ComputeGaussianMatrix(float sigma)
	{
		byte num = (byte)XMath.Ceiling(3 * sigma);
		var dim = num * 2 - 1;
		var mid = num - 1;
		var res = new float[dim, dim];
		var sigma2 = MathExt.Sqr(sigma);
		var a = 1 / (2 * XMath.PI * sigma2);
		var b = -1 / (2 * sigma2);
		res[mid, mid] = a;
		for (int i = 1; i <= mid; i++)
			for (int j = 0; j <= i; j++)
			{
				var G = a * XMath.Exp(b * (i * i + j * j));
				res[mid + i, mid + j] = G;
				res[mid - i, mid - j] = G;
				res[mid - i, mid + j] = G;
				if (j != 0)
					res[mid + i, mid - j] = G;
				if (i != j)
				{
					res[mid + j, mid + i] = G;
					res[mid - j, mid - i] = G;
					res[mid + j, mid - i] = G;
					if (j != 0)
						res[mid - j, mid + i] = G;
				}
			}
		return res;
	}
	#endregion
}
