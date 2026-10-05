#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Collections;

[TestClass]
public class OldPresentationListTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddRangeIEnumerableShouldRaiseCountPropertyChanged()
	{
		var target = new OldPresentationList<int>(1, 2, 3, 4, 5);
		var raised = false;

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(e.PropertyName, nameof(target.Count));
			CornerstoneTest.AreEqual(target.Count, 7);
			raised = true;
		};

		target.AddRange(Enumerable.Range(6, 2));

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void AddRangeItemsShouldRaiseCorrectCollectionChanged()
	{
		var target = new OldPresentationList<object>();

		var eventItems = new List<object>();

		target.CollectionChanged += (sender, args) =>
		{
			CornerstoneTest.IsNotNull(args.NewItems);
			eventItems.AddRange(args.NewItems.Cast<object>());
		};

		target.AddRange(Enumerable.Range(0, 10).Select(i => new object()));

		CornerstoneTest.AreEqual(eventItems, target);
	}

	[PresentationTestMethod]
	public void AddRangeWithNullShouldThrowException()
	{
		var target = new OldPresentationList<int>();

		Assert.Throws<ArgumentNullException>(() => target.AddRange(null!));
	}

	[PresentationTestMethod]
	public void AddingItemShouldRaiseCollectionChanged()
	{
		var target = new OldPresentationList<int>(1, 2);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
			CornerstoneTest.IsNotNull(e.NewItems);
			CornerstoneTest.AreEqual(new[] { 3 }, e.NewItems.Cast<int>());
			CornerstoneTest.AreEqual(2, e.NewStartingIndex);

			raised = true;
		};

		target.Add(3);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void AddingItemsShouldRaiseCollectionChanged()
	{
		var target = new OldPresentationList<int>(1, 2);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
			CornerstoneTest.IsNotNull(e.NewItems);
			CornerstoneTest.AreEqual(new[] { 3, 4 }, e.NewItems.Cast<int>());
			CornerstoneTest.AreEqual(2, e.NewStartingIndex);

			raised = true;
		};

		target.AddRange(new[] { 3, 4 });

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void CanCopyToArrayOfBaseType()
	{
		var target = new OldPresentationList<string> { "foo", "bar", "baz" };
		var result = new object[3];

		((IList) target).CopyTo(result, 0);

		CornerstoneTest.AreEqual(target, result);
	}

	[PresentationTestMethod]
	public void CanCopyToArrayOfSameType()
	{
		var target = new OldPresentationList<string> { "foo", "bar", "baz" };
		var result = new string[3];

		target.CopyTo(result, 0);

		CornerstoneTest.AreEqual(target, result);
	}

	[PresentationTestMethod]
	public void ClearingItemsShouldRaiseCollectionChangedRemove()
	{
		var target = new OldPresentationList<int>(1, 2, 3);
		var raised = false;

		target.ResetBehavior = ResetBehavior.Remove;
		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
			CornerstoneTest.IsNotNull(e.OldItems);
			CornerstoneTest.AreEqual(new[] { 1, 2, 3 }, e.OldItems.Cast<int>());
			CornerstoneTest.AreEqual(0, e.OldStartingIndex);

			raised = true;
		};

		target.Clear();

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void ClearingItemsShouldRaiseCollectionChangedReset()
	{
		var target = new OldPresentationList<int>(1, 2, 3);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Reset, e.Action);

			raised = true;
		};

		target.Clear();

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void InsertRangePastEndShouldThrowException()
	{
		var target = new OldPresentationList<int>();

		Assert.Throws<ArgumentOutOfRangeException>(() => target.InsertRange(1, new List<int> { 1 }));
	}

	[PresentationTestMethod]
	public void InsertRangeWithNullShouldThrowException()
	{
		var target = new OldPresentationList<int>();

		Assert.Throws<ArgumentNullException>(() => target.InsertRange(1, null!));
	}

	[PresentationTestMethod]
	public void InsertingItemShouldRaiseCollectionChanged()
	{
		var target = new OldPresentationList<int>(1, 2);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
			CornerstoneTest.IsNotNull(e.NewItems);
			CornerstoneTest.AreEqual(new[] { 3 }, e.NewItems.Cast<int>());
			CornerstoneTest.AreEqual(1, e.NewStartingIndex);

			raised = true;
		};

		target.Insert(1, 3);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void InsertingItemsShouldRaiseCollectionChanged()
	{
		var target = new OldPresentationList<int>(1, 2);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
			CornerstoneTest.IsNotNull(e.NewItems);
			CornerstoneTest.AreEqual(new[] { 3, 4 }, e.NewItems.Cast<int>());
			CornerstoneTest.AreEqual(1, e.NewStartingIndex);

			raised = true;
		};

		target.InsertRange(1, new[] { 3, 4 });

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void ItemsPassedToConstructorShouldAppearInList()
	{
		var items = new[] { 1, 2, 3 };
		var target = new OldPresentationList<int>(items);

		CornerstoneTest.AreEqual(items, target);
	}

	[PresentationTestMethod]
	public void MoveRangeCanMoveToEnd()
	{
		OldPresentationList<int> target = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

		target.MoveRange(0, 5, 9);

		CornerstoneTest.AreEqual(new[] { 6, 7, 8, 9, 10, 1, 2, 3, 4, 5 }, target);
	}

	[PresentationTestMethod]
	public void MoveRangeRaisesCorrectCollectionChangedEvent()
	{
		OldPresentationList<int> target = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

		AssertEvent(target, () => target.MoveRange(0, 9, 9), [
			new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Move,
				new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
				9,
				0)
		]);

		CornerstoneTest.AreEqual(new[] { 10, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, target);
	}

	[PresentationTestMethod]
	public void MoveRangeShouldMoveOneItem()
	{
		OldPresentationList<int> target = [1, 2, 3];

		AssertEvent(target, () => target.MoveRange(0, 1, 1), [
			new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Move,
				new[] { 1 },
				1,
				0)
		]);

		CornerstoneTest.AreEqual(new[] { 2, 1, 3 }, target);
	}

	[PresentationTestMethod]
	public void MoveRangeShouldUpdateCollection()
	{
		var target = new OldPresentationList<int>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

		target.MoveRange(4, 3, 0);

		CornerstoneTest.AreEqual(new[] { 5, 6, 7, 1, 2, 3, 4, 8, 9, 10 }, target);
	}

	[PresentationTestMethod]
	public void MoveShouldMoveOneItem()
	{
		OldPresentationList<int> target = [1, 2, 3];

		AssertEvent(target, () => target.Move(0, 1), [
			new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Move,
				new[] { 1 },
				1,
				0)
		]);

		CornerstoneTest.AreEqual(new[] { 2, 1, 3 }, target);
	}

	[PresentationTestMethod]
	public void MoveShouldUpdateCollection()
	{
		var target = new OldPresentationList<int>(1, 2, 3);

		target.Move(2, 0);

		CornerstoneTest.AreEqual(new[] { 3, 1, 2 }, target);
	}

	[PresentationTestMethod]
	public void MovingItemShouldRaiseCollectionChanged()
	{
		var target = new OldPresentationList<int>(1, 2, 3);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Move, e.Action);
			CornerstoneTest.IsNotNull(e.OldItems);
			CornerstoneTest.AreEqual(new[] { 3 }, e.OldItems.Cast<int>());
			CornerstoneTest.IsNotNull(e.NewItems);
			CornerstoneTest.AreEqual(new[] { 3 }, e.NewItems.Cast<int>());
			CornerstoneTest.AreEqual(2, e.OldStartingIndex);
			CornerstoneTest.AreEqual(0, e.NewStartingIndex);

			raised = true;
		};

		target.Move(2, 0);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void MovingItemsShouldRaiseCollectionChanged()
	{
		var target = new OldPresentationList<int>(1, 2, 3);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Move, e.Action);
			CornerstoneTest.IsNotNull(e.OldItems);
			CornerstoneTest.AreEqual(new[] { 2, 3 }, e.OldItems.Cast<int>());
			CornerstoneTest.IsNotNull(e.NewItems);
			CornerstoneTest.AreEqual(new[] { 2, 3 }, e.NewItems.Cast<int>());
			CornerstoneTest.AreEqual(1, e.OldStartingIndex);
			CornerstoneTest.AreEqual(0, e.NewStartingIndex);

			raised = true;
		};

		target.MoveRange(1, 2, 0);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void RemoveAllShouldHandleEmptyList()
	{
		var target = new OldPresentationList<string>();
		var toRemove = new[] { "Item 5", "Item 6", "Item 7" };
		var raised = 0;

		target.CollectionChanged += (s, e) => { ++raised; };

		target.RemoveAll(toRemove);

		CornerstoneTest.AreEqual(0, raised);
	}

	[PresentationTestMethod]
	public void RemoveAllShouldNotSendNotificationForItemsNotPresent()
	{
		var target = new OldPresentationList<string>(Enumerable.Range(0, 10).Select(x => $"Item {x}"));
		var toRemove = new[] { "Item 5", "Item 6", "Item 7", "Not present" };
		var raised = 0;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
			CornerstoneTest.AreEqual(5, e.OldStartingIndex);
			CornerstoneTest.AreEqual(toRemove.Take(3).ToArray(), e.OldItems);
			++raised;
		};

		target.RemoveAll(toRemove);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void RemoveAllShouldSendMultipleNotificationsForNonSequentialRange()
	{
		var target = new OldPresentationList<string>(Enumerable.Range(0, 10).Select(x => $"Item {x}"));
		var raised = 0;
		var toRemove = new[]
		{
			new[] { "Item 2", "Item 3" },
			new[] { "Item 5", "Item 6" }
		};

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);

			if (raised == 0)
			{
				CornerstoneTest.AreEqual(5, e.OldStartingIndex);
				CornerstoneTest.AreEqual(toRemove[1], e.OldItems);
			}
			else
			{
				CornerstoneTest.AreEqual(2, e.OldStartingIndex);
				CornerstoneTest.AreEqual(toRemove[0], e.OldItems);
			}

			++raised;
		};

		target.RemoveAll(toRemove[0].Concat(toRemove[1]));

		CornerstoneTest.AreEqual(2, raised);
	}

	[PresentationTestMethod]
	public void RemoveAllShouldSendMultipleNotificationsForSequentialRangeWithNonsequentialDuplicateSourceItems()
	{
		var items = Enumerable.Range(0, 10).Select(x => $"Item {x}");
		var target = new OldPresentationList<string>(items.Concat(items));
		var raised = 0;
		var toRemove = new[] { "Item 5", "Item 6", "Item 7" };

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);

			if (raised == 0)
			{
				CornerstoneTest.AreEqual(15, e.OldStartingIndex);
				CornerstoneTest.AreEqual(toRemove, e.OldItems);
			}
			else
			{
				CornerstoneTest.AreEqual(5, e.OldStartingIndex);
				CornerstoneTest.AreEqual(toRemove, e.OldItems);
			}

			++raised;
		};

		target.RemoveAll(toRemove);

		CornerstoneTest.AreEqual(2, raised);
	}

	[PresentationTestMethod]
	public void RemoveAllShouldSendSingleNotificationForSequentialRange()
	{
		var target = new OldPresentationList<string>(Enumerable.Range(0, 10).Select(x => $"Item {x}"));
		var toRemove = new[] { "Item 5", "Item 6", "Item 7" };
		var raised = 0;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
			CornerstoneTest.AreEqual(5, e.OldStartingIndex);
			CornerstoneTest.AreEqual(toRemove, e.OldItems);
			++raised;
		};

		target.RemoveAll(toRemove);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void RemoveAllShouldSendSingleNotificationForSequentialRangeWithDuplicateSourceItems()
	{
		var items = Enumerable.Range(0, 20).Select(x => $"Item {x / 2}");
		var target = new OldPresentationList<string>(items);
		var toRemove = new[] { "Item 5", "Item 6", "Item 7" };
		var raised = 0;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
			CornerstoneTest.AreEqual(10, e.OldStartingIndex);

			CornerstoneTest.AreEqual(new[]
			{
				"Item 5",
				"Item 5",
				"Item 6",
				"Item 6",
				"Item 7",
				"Item 7"
			}, e.OldItems);
			++raised;
		};

		target.RemoveAll(toRemove);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void RemoveAllWithNullShouldThrowException()
	{
		var target = new OldPresentationList<int>();

		Assert.Throws<ArgumentNullException>(() => target.RemoveAll(null!));
	}

	[PresentationTestMethod]
	public void RemovingItemShouldRaiseCollectionChanged()
	{
		var target = new OldPresentationList<int>(1, 2, 3);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
			CornerstoneTest.IsNotNull(e.OldItems);
			CornerstoneTest.AreEqual(new[] { 3 }, e.OldItems.Cast<int>());
			CornerstoneTest.AreEqual(2, e.OldStartingIndex);

			raised = true;
		};

		target.Remove(3);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void ReplacingItemShouldRaiseCollectionChanged()
	{
		var target = new OldPresentationList<int>(1, 2);
		var raised = false;

		target.CollectionChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(target, s);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Replace, e.Action);
			CornerstoneTest.IsNotNull(e.OldItems);
			CornerstoneTest.AreEqual(new[] { 2 }, e.OldItems.Cast<int>());
			CornerstoneTest.IsNotNull(e.NewItems);
			CornerstoneTest.AreEqual(new[] { 3 }, e.NewItems.Cast<int>());
			CornerstoneTest.AreEqual(1, e.OldStartingIndex);
			CornerstoneTest.AreEqual(1, e.NewStartingIndex);

			raised = true;
		};

		target[1] = 3;

		CornerstoneTest.IsTrue(raised);
	}

	/// <summary>
	/// Assert that <paramref name="items" /> emits <paramref name="expectedEvents" /> when performing <paramref name="action" />.
	/// </summary>
	/// <param name="items"> The event source. </param>
	/// <param name="action"> The action to perform. </param>
	/// <param name="expectedEvents"> The expected events. </param>
	private static void AssertEvent(INotifyCollectionChanged items, Action action, NotifyCollectionChangedEventArgs[] expectedEvents)
	{
		var callCount = 0;
		items.CollectionChanged += OnCollectionChanged;

		CornerstoneTest.Multiple(() =>
		{
			try
			{
				action();
			}
			finally
			{
				items.CollectionChanged -= OnCollectionChanged;
			}

			CornerstoneTest.AreEqual(expectedEvents.Length, callCount);
		});

		return;

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs actualEvent)
		{
			CornerstoneTest.Multiple(() =>
			{
				CornerstoneTest.IsTrue(callCount < expectedEvents.Length);
				CornerstoneTest.AreEqual(expectedEvents[callCount].Action, actualEvent.Action);
				CornerstoneTest.AreEqual(expectedEvents[callCount].NewItems, actualEvent.NewItems);
				CornerstoneTest.AreEqual(expectedEvents[callCount].NewStartingIndex, actualEvent.NewStartingIndex);
				CornerstoneTest.AreEqual(expectedEvents[callCount].OldItems, actualEvent.OldItems);
				CornerstoneTest.AreEqual(expectedEvents[callCount].OldStartingIndex, actualEvent.OldStartingIndex);
			});

			++callCount;
		}
	}

	#endregion
}