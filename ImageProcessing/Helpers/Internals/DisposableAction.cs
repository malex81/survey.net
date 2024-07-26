using System;

namespace ImageProcessing.Helpers.Internals
{
	internal class DisposableAction : IDisposable
	{
		Action<IDisposable>? dispAction;
		private readonly bool obligatory;

		public DisposableAction(Action<IDisposable> onDispose, bool obligatory)
		{
			dispAction = onDispose;
			this.obligatory = obligatory;
		}
		public DisposableAction(Action onDispose, bool obligatory)
		{
			dispAction = s => onDispose();
			this.obligatory = obligatory;
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (dispAction != null)
			{
				dispAction(this);
				dispAction = null;
			}
			GC.SuppressFinalize(this);
		}

		~DisposableAction()
		{
			if (obligatory)
				throw new InvalidOperationException("Dispose was not called in class DisposableAction");
		}

		#endregion
	}
}
