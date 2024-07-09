using System;

namespace ImageProcessing.Helpers;
public static class ArrayHelper
{
	public static T SafeGet<T>(this T[] arr, int ind) => arr[Math.Clamp(ind, 0, arr.Length - 1)];
}
