#region References

using System;
using Cornerstone.Avalonia.TreeDataGrid.Experimental.Data.Core;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Experimental.Data;

internal class ObservableEx
{
	#region Methods

	public static IObservable<T> SingleValue<T>(T value)
	{
		return new SingleValueImpl<T>(value);
	}

	#endregion

	#region Classes

	private sealed class SingleValueImpl<T> : IObservable<T>
	{
		#region Fields

		private readonly T _value;

		#endregion

		#region Constructors

		public SingleValueImpl(T value)
		{
			_value = value;
		}

		#endregion

		#region Methods

		public IDisposable Subscribe(IObserver<T> observer)
		{
			observer.OnNext(_value);
			return Disposable.Empty;
		}

		#endregion
	}

	#endregion
}