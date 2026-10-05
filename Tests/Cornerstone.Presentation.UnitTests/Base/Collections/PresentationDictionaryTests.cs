#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Collections;

[TestClass]
public class PresentationDictionaryTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddingItemShouldRaiseCollectionChanged()
	{
		var target = new PresentationDictionary<string, string>();
		var tracker = new CollectionChangedTracker(target);

		target.Add("foo", "bar");

		CornerstoneTest.IsNotNull(tracker.Args);
		CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, tracker.Args.Action);
		CornerstoneTest.AreEqual(-1, tracker.Args.NewStartingIndex);
		CornerstoneTest.IsNotNull(tracker.Args.NewItems);
		CornerstoneTest.AreEqual(1, tracker.Args.NewItems.Count);
		CornerstoneTest.AreEqual(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.NewItems[0]);
	}

	[PresentationTestMethod]
	public void AddingItemShouldRaisePropertyChanged()
	{
		var target = new PresentationDictionary<string, string>();
		var tracker = new PropertyChangedTracker(target);

		target.Add("foo", "bar");

		CornerstoneTest.AreEqual(new[] { "Count", "Item[foo]" }, tracker.Names);
	}

	[PresentationTestMethod]
	public void AssigningItemShouldRaiseCollectionChangedAdd()
	{
		var target = new PresentationDictionary<string, string>();
		var tracker = new CollectionChangedTracker(target);

		target["foo"] = "bar";

		CornerstoneTest.IsNotNull(tracker.Args);
		CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, tracker.Args.Action);
		CornerstoneTest.AreEqual(-1, tracker.Args.NewStartingIndex);
		CornerstoneTest.IsNotNull(tracker.Args.NewItems);
		CornerstoneTest.AreEqual(1, tracker.Args.NewItems.Count);
		CornerstoneTest.AreEqual(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.NewItems[0]);
	}

	[PresentationTestMethod]
	public void AssigningItemShouldRaiseCollectionChangedReplace()
	{
		var target = new PresentationDictionary<string, string>();

		target["foo"] = "baz";
		var tracker = new CollectionChangedTracker(target);
		target["foo"] = "bar";

		CornerstoneTest.IsNotNull(tracker.Args);
		CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Replace, tracker.Args.Action);
		CornerstoneTest.AreEqual(-1, tracker.Args.NewStartingIndex);
		CornerstoneTest.IsNotNull(tracker.Args.NewItems);
		CornerstoneTest.AreEqual(1, tracker.Args.NewItems.Count);
		CornerstoneTest.AreEqual(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.NewItems[0]);
	}

	[PresentationTestMethod]
	public void AssigningItemShouldRaisePropertyChangedAdd()
	{
		var target = new PresentationDictionary<string, string>();
		var tracker = new PropertyChangedTracker(target);

		target["foo"] = "bar";

		CornerstoneTest.AreEqual(new[] { "Count", "Item[foo]" }, tracker.Names);
	}

	[PresentationTestMethod]
	public void AssigningItemShouldRaisePropertyChangedReplace()
	{
		var target = new PresentationDictionary<string, string>();

		target["foo"] = "baz";
		var tracker = new PropertyChangedTracker(target);
		target["foo"] = "bar";

		CornerstoneTest.AreEqual(new[] { "Item[foo]" }, tracker.Names);
	}

	[PresentationTestMethod]
	public void ClearingCollectionShouldRaiseCollectionChanged()
	{
		var target = new PresentationDictionary<string, string>();

		target["foo"] = "bar";
		target["baz"] = "qux";
		var tracker = new CollectionChangedTracker(target);
		target.Clear();

		CornerstoneTest.IsNotNull(tracker.Args);
		CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, tracker.Args.Action);
		CornerstoneTest.AreEqual(-1, tracker.Args.OldStartingIndex);
		CornerstoneTest.IsNotNull(tracker.Args.OldItems);
		CornerstoneTest.AreEqual(2, tracker.Args.OldItems.Count);
		CornerstoneTest.AreEqual(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.OldItems[0]);
	}

	[PresentationTestMethod]
	public void ClearingCollectionShouldRaisePropertyChanged()
	{
		var target = new PresentationDictionary<string, string>();

		target["foo"] = "bar";
		target["baz"] = "qux";
		var tracker = new PropertyChangedTracker(target);
		target.Clear();

		CornerstoneTest.AreEqual(new[] { "Count", CommonPropertyNames.IndexerName }, tracker.Names);
	}

	[PresentationTestMethod]
	public void ConstructorShouldInitializeWithProvidedCollection()
	{
		var initialCollection = new Dictionary<string, string>
		{
			{ "key1", "value1" },
			{ "key2", "value2" }
		};

		var target = new PresentationDictionary<string, string>(initialCollection, null);

		CornerstoneTest.AreEqual(2, target.Count);
		CornerstoneTest.AreEqual("value1", target["key1"]);
		CornerstoneTest.AreEqual("value2", target["key2"]);
	}

	[PresentationTestMethod]
	public void ConstructorShouldThrowArgumentNullExceptionWhenCollectionIsNull()
	{
		Assert.Throws<ArgumentNullException>(() =>
		{
			var target = new PresentationDictionary<string, string>(null!, null);
		});
	}

	[PresentationTestMethod]
	public void RemoveMethodShouldRemoveItemFromCollection()
	{
		var target = new PresentationDictionary<string, string> { { "foo", "bar" } };
		CornerstoneTest.AreEqual(target.Count, 1);

		target.Remove("foo");
		CornerstoneTest.AreEqual(target.Count, 0);
	}

	[PresentationTestMethod]
	public void RemovingItemShouldRaiseCollectionChanged()
	{
		var target = new PresentationDictionary<string, string>();

		target["foo"] = "bar";
		var tracker = new CollectionChangedTracker(target);
		target.Remove("foo");

		CornerstoneTest.IsNotNull(tracker.Args);
		CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, tracker.Args.Action);
		CornerstoneTest.AreEqual(-1, tracker.Args.OldStartingIndex);
		CornerstoneTest.IsNotNull(tracker.Args.OldItems);
		CornerstoneTest.AreEqual(1, tracker.Args.OldItems.Count);
		CornerstoneTest.AreEqual(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.OldItems[0]);
	}

	[PresentationTestMethod]
	public void RemovingItemShouldRaisePropertyChanged()
	{
		var target = new PresentationDictionary<string, string>();

		target["foo"] = "bar";
		var tracker = new PropertyChangedTracker(target);
		target.Remove("foo");

		CornerstoneTest.AreEqual(new[] { "Count", "Item[foo]" }, tracker.Names);
	}

	#endregion
}