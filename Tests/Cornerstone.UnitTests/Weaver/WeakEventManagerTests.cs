#region References

using System;
using NUnit.Framework;
using Sample.Shared.Build;

#endregion

namespace Cornerstone.UnitTests.Weaver;

public class WeakEventManagerTests : CornerstoneUnitTest
{
	#region Methods

	[Theory]
	public void WeakEventHandler()
	{
		string lastEvent = null;
		var source = new SampleEventSource2<int>();

		LocalProcessing();
		RunGarbageCollection();

		source.OnPropertyChanged("Test2");
		AreEqual(null, lastEvent);
		return;

		void LocalProcessing()
		{
			var tracker = new PropertyChangeEventTracker<SampleEventSource2<int>>(source, x => lastEvent = x);
			tracker.SubscribeEvents();
			source.OnPropertyChanged("Test");
			AreEqual("Test", lastEvent);
			lastEvent = null;
		}
	}

	[Theory]
	public void WeakEventManager()
	{
		string lastEvent = null;
		var source = new SampleEventSource<int>();

		LocalProcessing();
		RunGarbageCollection();

		source.OnPropertyChanged("Test2");
		AreEqual(null, lastEvent);
		return;

		void LocalProcessing()
		{
			var tracker = new PropertyChangeEventTracker<SampleEventSource<int>>(source, x => lastEvent = x);
			tracker.SubscribeEventsUsingWeak();
			source.OnPropertyChanged("Test");
			AreEqual("Test", lastEvent);
			tracker = null;
			lastEvent = null;
		}
	}

	private static void RunGarbageCollection()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.WaitForFullGCApproach();
	}

	#endregion
}