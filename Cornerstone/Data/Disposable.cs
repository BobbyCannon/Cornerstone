#region References

using System;
using System.Threading;

#endregion

namespace Cornerstone.Data;

public sealed class Disposable<T> : IDisposable
{
	#region Fields

	private volatile Action<T> _disposeAction;

	#endregion

	#region Constructors

	private Disposable(T value, Action<T> disposeAction)
	{
		Value = value;
		_disposeAction = disposeAction;
	}

	#endregion

	#region Properties

	public bool IsDisposed => _disposeAction == null;

	public T Value { get; }

	#endregion

	#region Methods

	public static IDisposable Create(Action callback)
	{
		return new Disposable<bool>(true, _ => callback());
	}

	public static Disposable<T> Create(T value, Action<T> dispose)
	{
		return new(value, dispose ?? throw new ArgumentNullException(nameof(dispose)));
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _disposeAction, null)?.Invoke(Value);
	}

	#endregion
}

public static class Disposable
{
	#region Methods

	public static IDisposable Create(Action callback)
	{
		return Disposable<bool>.Create(false, _ => callback());
	}

	#endregion
}