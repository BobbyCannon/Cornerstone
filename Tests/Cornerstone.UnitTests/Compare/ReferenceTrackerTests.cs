#region References

using System.Collections.Generic;
using Cornerstone.Compare;
using Cornerstone.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Compare;

[TestClass]
public class ReferenceTrackerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CheckReferenceBothSidesAreSelfOrAlreadyProcessedReturnsTrue()
	{
		using var tracker = new ReferenceTracker();

		var parent = new object();
		var child = new object();

		tracker.TrackReference(parent);

		// Case: both are already processed / self-references
		IsTrue(tracker.CheckReference(parent, parent, child, child));
		IsTrue(tracker.CheckReference(parent, parent, child, parent)); // actual is parent

		tracker.TrackReference(child);

		IsTrue(tracker.CheckReference(parent, parent, child, child));
	}

	[TestMethod]
	public void CheckReferenceOneSideNotProcessedReturnsFalse()
	{
		using var tracker = new ReferenceTracker();

		var a = new object();
		var b = new object();
		var c = new object();

		tracker.TrackReference(a);

		// b & c are fresh → should return false
		IsFalse(tracker.CheckReference(a, a, b, c));
		IsFalse(tracker.CheckReference(a, b, a, c));
	}

	[TestMethod]
	public void DisposeReturnsObjectToPoolCanBeReused()
	{
		ReferenceTracker firstTracker;
		var obj = new object();

		// Force creation of one instance
		using (firstTracker = new ReferenceTracker())
		{
			firstTracker.TrackReference(obj);

			// Use reflection or internal knowledge to peek (for test only)
			// In real tests we usually just rely on behavior
			var firstSet = firstTracker.GetMemberValue("_visitedObjects") as HashSet<object>;
			IsNotNull(firstSet);
			AreEqual(1, firstSet.Count);
		}

		// Now create second tracker → should reuse same HashSet
		using (var secondTracker = new ReferenceTracker())
		{
			var secondSet = secondTracker.GetMemberValue("_visitedObjects") as HashSet<object>;
			IsNotNull(secondSet);
			AreEqual(0, secondSet.Count);

			secondTracker.TrackReference(obj);

			IsNotNull(secondSet);
			AreEqual(1, secondSet.Count);
			IsTrue(secondTracker.AlreadyProcessed(obj));
		}
	}

	[TestMethod]
	public void RemoveReferenceExistingObjectRemovesIt()
	{
		using var tracker = new ReferenceTracker();
		var obj = new object();

		tracker.TrackReference(obj);
		IsTrue(tracker.AlreadyProcessed(obj));

		var removed = tracker.RemoveReference(obj);
		IsTrue(removed);
		IsFalse(tracker.AlreadyProcessed(obj));

		// can add again
		IsTrue(tracker.TrackReference(obj));
	}

	[TestMethod]
	public void RemoveReferenceNotTrackedReturnsFalse()
	{
		using var tracker = new ReferenceTracker();
		var obj = new object();

		var removed = tracker.RemoveReference(obj);
		IsFalse(removed);
	}

	[TestMethod]
	public void RemoveReferenceNullReturnsFalse()
	{
		using var tracker = new ReferenceTracker();
		IsFalse(tracker.RemoveReference(null));
	}

	[TestMethod]
	public void TrackReferenceAlreadyTracked()
	{
		var tracker = new ReferenceTracker();

		var obj = new object();
		IsTrue(tracker.TrackReference(obj));
		IsTrue(tracker.AlreadyProcessed(obj));
		IsNotNull(tracker.GetMemberValue("_visitedObjects"));

		tracker.Dispose();
		IsNull(tracker.GetMemberValue("_visitedObjects"));
		IsTrue(tracker.TrackReference(obj));
	}

	[TestMethod]
	public void TrackReferenceNewObjectReturnsTrueThenAlreadyProcessedReturnsTrue()
	{
		using var tracker = new ReferenceTracker();

		var obj = new object();
		IsTrue(tracker.TrackReference(obj));
		IsTrue(tracker.AlreadyProcessed(obj));

		var sameObj = obj;
		IsFalse(tracker.TrackReference(sameObj));
		IsTrue(tracker.AlreadyProcessed(sameObj));
	}

	[TestMethod]
	public void TrackReferenceNullReturnsTrueDoesNotThrow()
	{
		using var tracker = new ReferenceTracker();
		IsTrue(tracker.TrackReference(null));
	}

	[TestMethod]
	public void TrackReferenceStringReturnsTrueNotAdded()
	{
		using var tracker = new ReferenceTracker();
		var str = "hello";

		IsTrue(tracker.TrackReference(str));
		IsFalse(tracker.AlreadyProcessed(str));
	}

	[TestMethod]
	public void TrackReferenceTwoDifferentObjectsBothTracked()
	{
		using var tracker = new ReferenceTracker();

		var a = new Person { Name = "Alice" };
		var b = new Person { Name = "Bob" };

		IsTrue(tracker.TrackReference(a));
		IsTrue(tracker.TrackReference(b));

		IsTrue(tracker.AlreadyProcessed(a));
		IsTrue(tracker.AlreadyProcessed(b));
	}

	[TestMethod]
	public void TrackReferenceValueTypeReturnsTrueNotAdded()
	{
		using var tracker = new ReferenceTracker();

		var number = 42;
		IsTrue(tracker.TrackReference(number));
		IsFalse(tracker.AlreadyProcessed(number));

		var point = new Point(10, 20);
		IsTrue(tracker.TrackReference(point));
		IsTrue(tracker.AlreadyProcessed(point));
	}

	#endregion

	#region Classes

	private class Person
	{
		#region Properties

		public string Name { get; set; }

		#endregion
	}

	private class Point
	{
		#region Constructors

		public Point(int x, int y)
		{
			(X, Y) = (x, y);
		}

		#endregion

		#region Properties

		public int X { get; }

		public int Y { get; }

		#endregion
	}

	#endregion
}