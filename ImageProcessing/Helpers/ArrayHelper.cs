using System;

namespace ImageProcessing.Helpers;
public static class ArrayHelper
{
	public static T GetClamped<T>(this T[] arr, int ind) => arr[Math.Clamp(ind, 0, arr.Length - 1)];
}
