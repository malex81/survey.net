using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System.Numerics;

namespace GpuSurvey.CodeGenStudy;
public static class CalcProc
{
	internal static Vector2 Round(this Vector2 v) => new(XMath.Round(v.X), XMath.Round(v.Y));
	internal static Vector2 Floor(this Vector2 v) => new(XMath.Floor(v.X), XMath.Floor(v.Y));

	//static bool ContainsPoint(this PixelSize size, Vector2 pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < size.Width && pos.Y < size.Height;

	static uint[] UnfoldColor(uint c) => [(c & 0xff000000) >> 24, (c & 0x00ff0000) >> 16, (c & 0x0000ff00) >> 8, c & 0x000000ff];
	static uint FoldColor(uint[] cc) => (cc[0] << 24) + (cc[1] << 16) + (cc[2] << 8) + cc[3];

	#region GetPixel variants
	//static uint GetPixel(this ArrayView<uint> source, PixelSize size, int x, int y)
	//	=> 0 <= x && x < size.Width && 0 <= y && y < size.Height ? source[x + y * size.Width] : 0;

	//public static uint GetPixel(this ArrayView<uint> source, PixelSize size, Index2D ind)
	//	=> source.GetPixel(size, ind.X, ind.Y);

	//public static uint GetPixelClamped(this ArrayView<uint> source, PixelSize size, Index2D ind)
	//	=> source[XMath.Clamp(ind.X, 0, size.Width - 1) + XMath.Clamp(ind.Y, 0, size.Height - 1) * size.Width];

	//public static XColor GetColorClamped(this ArrayView<uint> source, PixelSize size, Index2D ind) => source.GetPixelClamped(size, ind);
	#endregion

	#region Interpolation
	//public static uint GetNearestPixel(this ArrayView<uint> source, PixelSize size, Vector2 pos)
	//	=> size.ContainsPoint(pos) ? source.GetPixelClamped(size, pos.Round().ToIndex()) : 0;

	public static uint MixColors(uint color1, uint color2, float w)
	{
		var (cc1, cc2) = (UnfoldColor(color1), UnfoldColor(color2));
		var res = new uint[4];
		for (int i = 0; i < 4; i++)
		{
			var (_c1, _c2) = ((int)cc1[i], (int)cc2[i]);
			res[i] = (uint)XMath.Round(_c1 * (1 - w) + _c2 * w);
			//res[i] = (uint)XMath.Round(cc1[i] * (1 - w) + cc2[i] * w);
		}
		return FoldColor(res);
	}
	//static uint MixColors4(uint c00, uint c01, uint c10, uint c11, float w0, float w1)
	//{
	//	var cc = new uint[] { c00, c01, c10, c11 };
	//	var ww = new float[] { (1 - w0) * (1 - w1), w0 * (1 - w1), (1 - w0) * w1, w0 * w1 };
	//	XColor res = new();
	//	for (int i = 0; i < 4; i++)
	//		res += ww[i] * XColor.FromUint(cc[i]);
	//	return res;
	//}

	#endregion
}
