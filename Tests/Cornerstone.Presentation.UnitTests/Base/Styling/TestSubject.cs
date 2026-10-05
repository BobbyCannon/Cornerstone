#region References

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

internal class TestSubject<T> : IObserver<T>, IObservable<T>
{
	#region Fields

	private readonly T _initial;

	private readonly List<IObserver<T>> _subscribers = new();

	#endregion

	#region Constructors

	public TestSubject(T initial)
	{
		_initial = initial;
	}

	#endregion

	#region Properties

	public int SubscriberCount => _subscribers.Count;

	#endregion

	#region Methods

	public void OnCompleted()
	{
		foreach (var subscriber in _subscribers.ToArray())
		{
			subscriber.OnCompleted();
		}
	}

	public void OnError(Exception error)
	{
		foreach (var subscriber in _subscribers.ToArray())
		{
			subscriber.OnError(error);
		}
	}

	public void OnNext(T value)
	{
		foreach (var subscriber in _subscribers.ToArray())
		{
			subscriber.OnNext(value);
		}
	}

	public IDisposable Subscribe(IObserver<T> observer)
	{
		_subscribers.Add(observer);
		observer.OnNext(_initial);
		return Disposable.Create(() => _subscribers.Remove(observer));
	}

	#endregion
}