#region References

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class SafeEnumerableOldPresentationListTests
{
	#region Methods

	[PresentationTestMethod]
	public void CollectionChangedIsRaisedForMutationsAfterCopy()
	{
		var target = new SafeEnumerableOldPresentationList<string> { "foo" };
		var events = new List<NotifyCollectionChangedEventArgs>();

		target.CollectionChanged += (_, e) => events.Add(e);

		foreach (var item in target)
		{
			target.Add("bar");
		}

		target.Add("baz");

		CornerstoneTest.AreEqual(2, events.Count);
		CornerstoneTest.All(events, e => CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, e.Action));
		CornerstoneTest.AreEqual("bar", events[0].NewItems!.Cast<string>().Single());
		CornerstoneTest.AreEqual("baz", events[1].NewItems!.Cast<string>().Single());
	}

	[PresentationTestMethod]
	public void EnumerationThroughInterfaceIsSafe()
	{
		var target = new SafeEnumerableOldPresentationList<string> { "foo", "bar" };
		var seen = new List<string>();

		foreach (var item in (IEnumerable<string>) target)
		{
			seen.Add(item);
			target.Add("baz");
		}

		CornerstoneTest.AreEqual(["foo", "bar"], seen);
		CornerstoneTest.AreEqual(["foo", "bar", "baz", "baz"], target);
	}

	[PresentationTestMethod]
	public void EnumeratorIteratesSnapshotTakenAtCreation()
	{
		var target = new SafeEnumerableOldPresentationList<string> { "foo", "bar", "baz" };
		var seen = new List<string>();

		foreach (var item in target)
		{
			seen.Add(item);
			target.Remove(item);
		}

		CornerstoneTest.AreEqual(["foo", "bar", "baz"], seen);
		CornerstoneTest.Empty(target);
	}

	[PresentationTestMethod]
	public void ListIsCopiedDuringNestedEnumerations()
	{
		var target = new SafeEnumerableOldPresentationList<string>();
		var initialInner = target.InnerForTests;
		var firstItems = new List<string>();
		var secondItems = new List<string>();

		target.Add("foo");

		foreach (var i in target)
		{
			target.Add("bar");

			var firstInner = target.InnerForTests;
			CornerstoneTest.NotSame(initialInner, firstInner);

			foreach (var j in target)
			{
				target.Add("baz");

				var secondInner = target.InnerForTests;
				CornerstoneTest.NotSame(firstInner, secondInner);

				secondItems.Add(j);
			}

			firstItems.Add(i);
		}

		CornerstoneTest.AreEqual(["foo"], firstItems);
		CornerstoneTest.AreEqual(["foo", "bar"], secondItems);
		CornerstoneTest.AreEqual(["foo", "bar", "baz", "baz"], target);

		var finalInner = target.InnerForTests;
		target.Add("final");
		CornerstoneTest.Same(finalInner, target.InnerForTests);
	}

	[PresentationTestMethod]
	public void ListIsCopiedOnlyOnceDuringEnumeration()
	{
		var target = new SafeEnumerableOldPresentationList<string>();
		var inner = target.InnerForTests;

		target.Add("foo");

		foreach (var item in target)
		{
			target.Add("bar");
			CornerstoneTest.NotSame(inner, target.InnerForTests);
			inner = target.InnerForTests;
			target.Add("baz");
			CornerstoneTest.Same(inner, target.InnerForTests);
		}
	}

	[PresentationTestMethod]
	public void ListIsCopiedWhenMutatedDuringEnumeration()
	{
		var target = new SafeEnumerableOldPresentationList<string>();
		var inner = target.InnerForTests;

		target.Add("foo");

		foreach (var item in target)
		{
			CornerstoneTest.Same(inner, target.InnerForTests);
			target.Add("bar");
			CornerstoneTest.NotSame(inner, target.InnerForTests);
			CornerstoneTest.AreEqual("foo", item);
		}

		CornerstoneTest.AreEqual(["foo", "bar"], target);
	}

	[PresentationTestMethod]
	public void ListIsNotCopiedAfterEnumeration()
	{
		var target = new SafeEnumerableOldPresentationList<string>();
		var inner = target.InnerForTests;

		target.Add("foo");

		foreach (var item in target)
		{
			target.Add("bar");
			CornerstoneTest.NotSame(inner, target.InnerForTests);
			inner = target.InnerForTests;
			CornerstoneTest.AreEqual("foo", item);
		}

		target.Add("baz");
		CornerstoneTest.Same(inner, target.InnerForTests);
	}

	[PresentationTestMethod]
	public void ListIsNotCopiedOutsideEnumeration()
	{
		var target = new SafeEnumerableOldPresentationList<string>();
		var inner = target.InnerForTests;

		target.Add("foo");
		target.Add("bar");
		target.Remove("foo");
		target.Insert(0, "baz");
		target[0] = "qux";
		target.Move(0, 1);
		target.Clear();

		CornerstoneTest.Same(inner, target.InnerForTests);
	}

	[PresentationTestMethod]
	public void PropertyChangedIsRaisedForCountAfterCopy()
	{
		var target = new SafeEnumerableOldPresentationList<string> { "foo" };
		var countChanged = 0;

		target.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(target.Count))
			{
				++countChanged;
			}
		};

		foreach (var i in target)
		{
			target.Add("bar");
		}

		target.Add("baz");

		CornerstoneTest.AreEqual(2, countChanged);
	}

	[PresentationTestMethod]
	public void ValidatorIsInvokedAfterCopy()
	{
		var target = new SafeEnumerableOldPresentationList<string> { "foo" };
		var validated = new List<string>();

		target.Validate = validated.Add;

		foreach (var item in target)
		{
			target.Add("bar");
		}

		target.Add("baz");

		CornerstoneTest.AreEqual(["bar", "baz"], validated);
	}

	#endregion
}