﻿using System;
using System.Collections.Generic;

namespace ImageProcessing.Helpers;
public static class IEnumerableExtensions
{
	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (var item in source)
			action(item);
	}
}
