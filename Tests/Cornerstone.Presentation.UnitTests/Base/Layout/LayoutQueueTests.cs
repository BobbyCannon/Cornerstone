#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class LayoutQueueTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldDequeue()
	{
		var target = new LayoutQueue<string>(_ => true);
		var refQueue = new Queue<string>();
		var items = new[] { "1", "2", "3" };

		foreach (var item in items)
		{
			target.Enqueue(item);
			refQueue.Enqueue(item);
		}

		while (refQueue.Count > 0)
		{
			CornerstoneTest.AreEqual(refQueue.Dequeue(), target.Dequeue());
		}
	}

	[PresentationTestMethod]
	public void ShouldEnqueue()
	{
		var target = new LayoutQueue<string>(_ => true);
		var refQueue = new Queue<string>();
		var items = new[] { "1", "2", "3" };

		foreach (var item in items)
		{
			target.Enqueue(item);
			refQueue.Enqueue(item);
		}

		CornerstoneTest.AreEqual(refQueue, target);
	}

	[PresentationTestMethod]
	public void ShouldEnqueueUniqueElements()
	{
		var target = new LayoutQueue<string>(_ => true);

		var items = new[] { "1", "2", "3", "1" };

		foreach (var item in items)
		{
			target.Enqueue(item);
		}

		CornerstoneTest.AreEqual(3, target.Count);
		CornerstoneTest.AreEqual(items.Take(3), target);
	}

	[PresentationTestMethod]
	public void ShouldEnqueueWhenConditionTrueAfterLoopWhenLimitMet()
	{
		var target = new LayoutQueue<string>(_ => true);

		//1
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(1, target.Count);

		target.BeginLoop(3);

		target.Dequeue();

		//2
		target.Enqueue("Foo");
		target.Dequeue();

		//3
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(1, target.Count);

		target.Dequeue();

		//4 more than limit shouldn't be added to queue
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(0, target.Count);

		target.EndLoop();

		//after loop should be added once
		CornerstoneTest.AreEqual(1, target.Count);
		CornerstoneTest.AreEqual("Foo", target.First());
	}

	[PresentationTestMethod]
	public void ShouldntCountUniqueEnqueueForLimitInLoop()
	{
		var target = new LayoutQueue<string>(_ => true);

		//1
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(1, target.Count);

		target.BeginLoop(3);

		target.Dequeue();

		//2
		target.Enqueue("Foo");
		target.Enqueue("Foo");
		target.Dequeue();

		//3
		target.Enqueue("Foo");
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(1, target.Count);

		target.Dequeue();

		//4 more than limit shouldn't be added
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(0, target.Count);
	}

	[PresentationTestMethod]
	public void ShouldntEnqueueMoreThanLimitInLoop()
	{
		var target = new LayoutQueue<string>(_ => true);

		//1
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(1, target.Count);

		target.BeginLoop(3);

		target.Dequeue();

		//2
		target.Enqueue("Foo");
		target.Dequeue();

		//3
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(1, target.Count);

		target.Dequeue();

		//4 more than limit shouldn't be added
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(0, target.Count);
	}

	[PresentationTestMethod]
	public void ShouldntEnqueueWhenConditionFalseAfterLoopWhenLimitMet()
	{
		var target = new LayoutQueue<string>(_ => false);

		//1
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(1, target.Count);

		target.BeginLoop(3);

		target.Dequeue();

		//2
		target.Enqueue("Foo");
		target.Dequeue();

		//3
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(1, target.Count);

		target.Dequeue();

		//4 more than limit shouldn't be added
		target.Enqueue("Foo");

		CornerstoneTest.AreEqual(0, target.Count);

		target.EndLoop();

		CornerstoneTest.AreEqual(0, target.Count);
	}

	#endregion
}