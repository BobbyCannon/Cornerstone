#region References

using System;
using System.Reactive.Disposables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class TestObservable : IObservable<bool>
{
	#region Properties

	public int SubscribedCount { get; private set; }

	#endregion

	#region Methods

	public IDisposable Subscribe(IObserver<bool> observer)
	{
		++SubscribedCount;
		observer.OnNext(true);
		return Disposable.Create(() => --SubscribedCount);
	}

	#endregion
}