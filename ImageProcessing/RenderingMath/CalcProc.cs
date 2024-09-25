using Avalonia;
using ILGPU;
using ILGPU.Algorithms;
using System.Numerics;

namespace ImageProcessing.RenderingMath;
public static class CalcProc
{
	#region GetPixel variants
	static uint GetPixel(this ArrayView<uint> source, PixelSize size, int x, int y)
		=> 0 <= x && x < size.Width && 0 <= y && y < size.Height ? source[x + y * size.Width] : 0;

	public static uint GetPixel(this ArrayView<uint> source, PixelSize size, PixelPoint pos)
		=> source.GetPixel(size, pos.X, pos.Y);

	public static uint GetPixel(this ArrayView<uint> source, PixelSize size, Vector2 pos)
		=> source.GetPixel(size, pos.ToPixel());

	public static uint GetPixelClamped(this ArrayView<uint> source, PixelSize size, PixelPoint pos)
		=> source[XMath.Clamp(pos.X, 0, size.Width - 1) + XMath.Clamp(pos.Y, 0, size.Height - 1) * size.Width];
	#endregion
}
