#region References

using System;
using System.Threading;

#endregion

namespace Cornerstone.Internal;

/// <summary>
/// Invokes an action when it is disposed.
/// </summary>
/// <remarks>
/// This class ensures the callback is invoked at most once,
/// even when Dispose is called on multiple threads.
/// </remarks>
internal sealed class CallbackOnDispose : IDisposable
{
	#region Fields

	private Action _action;

	#endregion

	#region Constructors

	public CallbackOnDispose(Action action)
	{
		_action = action ?? throw new ArgumentNullException(nameof(action));
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		var a = Interlocked.Exchange(ref _action, null);
		a?.Invoke();
	}

	#endregion
}