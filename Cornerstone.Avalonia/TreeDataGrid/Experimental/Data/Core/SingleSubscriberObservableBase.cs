#region References

using System;
using Avalonia.Threading;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Experimental.Data.Core;

internal abstract class SingleSubscriberObservableBase<T> : IObservable<T>, IDisposable
{
	#region Fields

	private bool _completed;
	private Exception _error;
	private IObserver<T> _observer;

	#endregion

	#region Methods

	public virtual void Dispose()
	{
		Unsubscribed();
		_observer = null;
	}

	public IDisposable Subscribe(IObserver<T> observer)
	{
		_ = observer ?? throw new ArgumentNullException(nameof(observer));
		Dispatcher.UIThread.VerifyAccess();

		if (_observer != null)
		{
			throw new InvalidOperationException("The observable can only be subscribed once.");
		}

		if (_error != null)
		{
			observer.OnError(_error);
		}
		else if (_completed)
		{
			observer.OnCompleted();
		}
		else
		{
			_observer = observer;
			Subscribed();
		}

		return this;
	}

	protected void PublishCompleted()
	{
		_completed = true;

		if (_observer != null)
		{
			_observer.OnCompleted();
			Unsubscribed();
			_observer = null;
		}
	}

	protected void PublishError(Exception error)
	{
		_error = error;

		if (_observer != null)
		{
			_observer.OnError(error);
			Unsubscribed();
			_observer = null;
		}
	}

	protected void PublishNext(T value)
	{
		_observer?.OnNext(value);
	}

	protected abstract void Subscribed();

	protected abstract void Unsubscribed();

	#endregion
}