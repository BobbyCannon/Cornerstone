#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class SingleOrQueueTests
{
	#region Methods

	[PresentationTestMethod]
	public void DequeueThrowsWhenEmpty()
	{
		var queue = new SingleOrQueue<object>();

		Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
	}

	[PresentationTestMethod]
	public void EnqueueAddsElement()
	{
		var queue = new SingleOrQueue<int>();

		queue.Enqueue(1);

		CornerstoneTest.IsFalse(queue.Empty);

		CornerstoneTest.AreEqual(1, queue.Dequeue());
	}

	[PresentationTestMethod]
	public void MultipleElementsDequeuedInCorrectOrder()
	{
		var queue = new SingleOrQueue<int>();

		queue.Enqueue(1);
		queue.Enqueue(2);
		queue.Enqueue(3);
		CornerstoneTest.AreEqual(1, queue.Dequeue());
		CornerstoneTest.AreEqual(2, queue.Dequeue());
		CornerstoneTest.AreEqual(3, queue.Dequeue());
	}

	[PresentationTestMethod]
	public void NewSingleOrQueueIsEmpty()
	{
		CornerstoneTest.IsTrue(new SingleOrQueue<object>().Empty);
	}

	#endregion
}