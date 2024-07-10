using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageProcessing.Helpers
{
	public static class DisposableHelper
	{
		class EmptyDisposable : IDisposable { public void Dispose() { } }
		public readonly static IDisposable Empty = new EmptyDisposable();

		public static T DisposeWith<T>(this T elem, ICollection<IDisposable> dispList) where T : IDisposable
		{
			dispList.Add(elem);
			return elem;
		}

		public static IDisposable FromAction(Action action, bool obligatory = true) => new Internals.DisposableAction(action, obligatory);
		public static IDisposable FromAction(Action<IDisposable> action, bool obligatory = true) => new Internals.DisposableAction(action, obligatory);
		public static void DisposeAll<T>(this IEnumerable<T> list) => list?.OfType<IDisposable>().ForEach(d => d.Dispose());
		public static void AddAction(this ICollection<IDisposable> dispList, Action action) => dispList.Add(FromAction(action));
		public static IDisposable ToDisposable(this IEnumerable<IDisposable> collection) => new DisposableList(collection);
	}
}
