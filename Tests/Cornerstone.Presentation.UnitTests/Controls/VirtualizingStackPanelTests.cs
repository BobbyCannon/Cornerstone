#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class VirtualizingStackPanelTests : ScopedTestBase
{
	#region Fields

	private static readonly FuncDataTemplate<ItemWithHeight> CanvasWithHeightTemplate = new((_, _) =>
		new CanvasCountingMeasureArrangeCalls
		{
			Width = 100,
			[!Layoutable.HeightProperty] = new Binding("Height")
		});

	private static readonly FuncDataTemplate<ItemWithWidth> CanvasWithWidthTemplate = new((_, _) =>
		new CanvasCountingMeasureArrangeCalls
		{
			Height = 100,
			[!Layoutable.WidthProperty] = new Binding("Width")
		});

	#endregion

	#region Methods

	[PresentationTestMethod]
	[DataRow(0d, 0, 8, 1, 9)]
	[DataRow(0.5d, 0, 17, 0, 17)]
	public void AlternatingBackgroundsShouldBeCorrectAfterScrolling(double bufferFactor,
		int firstIndex1,
		int lastIndex1,
		int firstIndex2,
		int lastIndex2)
	{
		// Issue #12381.
		static void AssertColors(VirtualizingStackPanel target)
		{
			var containers = target.GetRealizedContainers()!
				.Cast<ListBoxItem>()
				.ToList();

			for (var i = target.FirstRealizedIndex; i <= target.LastRealizedIndex; i++)
			{
				var container = CornerstoneTest.IsType<ListBoxItem>(target.ContainerFromIndex(i));
				var expectedBackground = (i % 2) == 0 ? Colors.Green : Colors.Red;
				var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(container.Background);

				CornerstoneTest.AreEqual(expectedBackground, brush.Color);
			}
		}

		using var app = App();
		var styles = new[]
		{
			new Style(x => x.OfType<ListBoxItem>())
			{
				Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.White) }
			},
			new Style(x => x.OfType<ListBoxItem>().NthChild(2, 1))
			{
				Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Green) }
			},
			new Style(x => x.OfType<ListBoxItem>().NthChild(2, 0))
			{
				Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) }
			}
		};
		var (target, scroll, itemsControl) = CreateUnrootedTarget<ListBox>(bufferFactor: bufferFactor);

		// We need to display an odd number of items to reproduce the issue.
		var root = CreateRoot(itemsControl, new(100, 90), styles);
		root.LayoutManager.ExecuteInitialLayoutPass();

		var containers = target.GetRealizedContainers()!
			.Cast<ListBoxItem>()
			.ToList();

		CornerstoneTest.AreEqual(firstIndex1, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex1, target.LastRealizedIndex);
		AssertColors(target);

		scroll.Offset = new Vector(0, 10);
		target.UpdateLayout();

		CornerstoneTest.AreEqual(firstIndex2, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex2, target.LastRealizedIndex);
		AssertColors(target);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void CanBindItemIsVisible(double bufferFactor)
	{
		using var app = App();
		var style = CreateIsVisibleBindingStyle();
		var items = Enumerable.Range(0, 100).Select(x => new ItemWithIsVisible(x)).ToList();
		var (target, scroll, itemsControl) = CreateTarget(items, styles: new[] { style }, bufferFactor: bufferFactor);
		var container = target.ContainerFromIndex(2)!;

		CornerstoneTest.IsTrue(container.IsVisible);
		CornerstoneTest.AreEqual(20, container.Bounds.Top);

		items[2].IsVisible = false;
		Layout(target);

		CornerstoneTest.IsFalse(container.IsVisible);

		// Next container should be in correct position.
		CornerstoneTest.AreEqual(20, target.ContainerFromIndex(3)!.Bounds.Top);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10)]
	[DataRow(0.5d, 15)]
	public void ContainerClearingIsRaisedWhenScrolling(double bufferFactor, int expectedRaised)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var raised = 0;

		itemsControl.ContainerClearing += (s, e) => ++raised;

		scroll.Offset = new Vector(0, 200);
		Layout(target);

		CornerstoneTest.AreEqual(expectedRaised, raised);
	}

	[PresentationTestMethod]
	[DataRow(0d, 9)]
	[DataRow(0.5d, 19)]
	public void ContainerIndexChangedIsRaisedOnInsert(double bufferFactor, int expectedRaised)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;
		var raised = 0;
		var index = 1;

		itemsControl.ContainerIndexChanged += (s, e) =>
		{
			++raised;
			CornerstoneTest.AreEqual(index, e.OldIndex);
			CornerstoneTest.AreEqual(++index, e.NewIndex);
		};

		items.Insert(index, "new");

		CornerstoneTest.AreEqual(expectedRaised, raised);
	}

	[PresentationTestMethod]
	[DataRow(0d, 8)]
	[DataRow(0.5d, 18)]
	public void ContainerIndexChangedIsRaisedOnRemove(double bufferFactor, int expectedRaised)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;
		var raised = 0;
		var index = 1;

		itemsControl.ContainerIndexChanged += (s, e) =>
		{
			++raised;
			CornerstoneTest.AreEqual(index + 1, e.OldIndex);
			CornerstoneTest.AreEqual(index++, e.NewIndex);
		};

		items.RemoveAt(index);

		CornerstoneTest.AreEqual(expectedRaised, raised);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, 20)]
	[DataRow(0.5d, 20, 15)]
	public void ContainerIndexChangedIsRaisedWhenItemInsertedBeforeRealizedElements(double bufferFactor, int expectedRaised, int index)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;
		var raised = 0;

		itemsControl.ContainerIndexChanged += (s, e) =>
		{
			++raised;
			CornerstoneTest.AreEqual(index, e.OldIndex);
			CornerstoneTest.AreEqual(++index, e.NewIndex);
		};

		scroll.Offset = new Vector(0, 200);
		Layout(target);

		items.Insert(10, "new");

		CornerstoneTest.AreEqual(expectedRaised, raised);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, 20)]
	[DataRow(0.5d, 20, 15)]
	public void ContainerIndexChangedIsRaisedWhenItemRemovedBeforeRealizedElements(double bufferFactor, int expectedRaised, int index)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;
		var raised = 0;

		itemsControl.ContainerIndexChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(index, e.OldIndex);
			CornerstoneTest.AreEqual(index - 1, e.NewIndex);
			++index;
			++raised;
		};

		scroll.Offset = new Vector(0, 200);
		Layout(target);

		items.RemoveAt(10);

		CornerstoneTest.AreEqual(expectedRaised, raised);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10)]
	[DataRow(0.5d, 15)]
	public void ContainerPreparedIsRaisedWhenScrolling(double bufferFactor, int expectedRaised)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var raised = 0;

		itemsControl.ContainerPrepared += (s, e) => ++raised;

		scroll.Offset = new Vector(0, 200);
		Layout(target);

		CornerstoneTest.AreEqual(expectedRaised, raised);
	}

	[PresentationTestMethod]
	[DataRow(0d,
		10,
		11,
		"-1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10",
		10)]
	[DataRow(0.5d,
		20,
		21,
		"-1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20",
		20)]
	public void CreatesElementsOnItemInsert1(double bufferFactor,
		int firstCount,
		int secondCount,
		string indexesRaw,
		int thirdCount)
	{
		using var app = App();
		var (target, _, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;

		CornerstoneTest.AreEqual(firstCount, target.GetRealizedElements().Count);

		items.Insert(0, "new");

		CornerstoneTest.AreEqual(secondCount, target.GetRealizedElements().Count);

		var indexes = GetRealizedIndexes(target, itemsControl);

		// Blank space inserted in realized elements and subsequent indexes updated.
		CornerstoneTest.AreEqual(indexesRaw.Split(", ").Select(int.Parse).ToArray(), indexes);

		var elements = target.GetRealizedElements().ToList();
		Layout(target);

		indexes = GetRealizedIndexes(target, itemsControl);

		// After layout an element for the new element is created.
		CornerstoneTest.AreEqual(Enumerable.Range(0, thirdCount), indexes);

		// But apart from the new element and the removed last element, all existing elements
		// should be the same.
		elements[0] = target.GetRealizedElements().ElementAt(0);
		elements.RemoveAt(elements.Count - 1);
		CornerstoneTest.AreEqual(elements, target.GetRealizedElements());
	}

	[PresentationTestMethod]
	[DataRow(0d,
		10,
		11,
		"0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10",
		10)]
	[DataRow(0.5d,
		20,
		21,
		"0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20",
		20)]
	public void CreatesElementsOnItemInsert2(double bufferFactor,
		int firstCount,
		int secondCount,
		string indexesRaw,
		int thirdCount)
	{
		using var app = App();
		var (target, _, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;

		CornerstoneTest.AreEqual(firstCount, target.GetRealizedElements().Count);

		items.Insert(2, "new");

		CornerstoneTest.AreEqual(secondCount, target.GetRealizedElements().Count);

		var indexes = GetRealizedIndexes(target, itemsControl);

		// Blank space inserted in realized elements and subsequent indexes updated.
		CornerstoneTest.AreEqual(indexesRaw.Split(", ").Select(int.Parse).ToArray(), indexes);

		var elements = target.GetRealizedElements().ToList();
		Layout(target);

		indexes = GetRealizedIndexes(target, itemsControl);

		// After layout an element for the new element is created.
		CornerstoneTest.AreEqual(Enumerable.Range(0, thirdCount), indexes);

		// But apart from the new element and the removed last element, all existing elements
		// should be the same.
		elements[2] = target.GetRealizedElements().ElementAt(2);
		elements.RemoveAt(elements.Count - 1);
		CornerstoneTest.AreEqual(elements, target.GetRealizedElements());
	}

	[PresentationTestMethod]
	[DataRow(0d, 10)]
	[DataRow(0.5d, 20)]
	public void CreatesInitialItems(double bufferFactor, int expectedCount)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		CornerstoneTest.AreEqual(1000, scroll.Extent.Height);

		AssertRealizedItems(target, itemsControl, 0, expectedCount);
	}

	[PresentationTestMethod]
	[DataRow(0d, 2)]
	[DataRow(0.5d, 2)]
	public void CreatesReassignedItems(double bufferFactor, int expectedCount)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(Array.Empty<object>(), bufferFactor: bufferFactor);

		CornerstoneTest.Empty(itemsControl.GetRealizedContainers());

		itemsControl.ItemsSource = new[] { "foo", "bar" };
		Layout(target);

		AssertRealizedItems(target, itemsControl, 0, expectedCount);
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/10968
	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void DoesNotRealizeItemsIfSelfOutsideViewport(double bufferFactor)
	{
		using var app = App();
		var (panel, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(bufferFactor: bufferFactor);
		itemsControl.Margin = new Thickness(0.0, 200.0, 0.0, 0.0);

		var scrollContentPresenter = new ScrollContentPresenter
		{
			Width = 100,
			Height = 100,
			Content = itemsControl
		};

		var root = CreateRoot(scrollContentPresenter);
		root.LayoutManager.ExecuteInitialLayoutPass();
		CornerstoneTest.AreEqual(1, panel.VisualChildren.Count);

		scrollContentPresenter.Content = null;
		root.LayoutManager.ExecuteLayoutPass();

		scrollContentPresenter.Content = itemsControl;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, panel.VisualChildren.Count);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void DoesNotRecycleFocusedElement(double bufferFactor)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		var focused = target.GetRealizedElements().First()!;
		focused.Focusable = true;
		focused.Focus();
		CornerstoneTest.IsTrue(target.GetRealizedElements().First()!.IsKeyboardFocusWithin);

		scroll.Offset = new Vector(0, 200);
		Layout(target);

		CornerstoneTest.All(target.GetRealizedElements(), x => CornerstoneTest.IsFalse(x!.IsKeyboardFocusWithin));
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void DoesNotThrowWhenEstimatingViewportWithAncestorMargin(double bufferFactor)
	{
		// Issue #11272
		using var app = App();
		var (_, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(bufferFactor: bufferFactor);
		var container = new Decorator { Margin = new Thickness(100) };
		var root = new TestRoot(true, container);

		root.LayoutManager.ExecuteInitialLayoutPass();

		container.Child = itemsControl;

		root.LayoutManager.ExecuteLayoutPass();
	}

	[PresentationTestMethod]
	[DataRow(0d,
		4, 5,
		8, 11)]
	[DataRow(0.5d,
		3, 6,
		6, 13)]
	public void ExtentAndOffsetShouldBeUpdatedWhenContainersResize(double bufferFactor,
		int firstIndex1, int lastIndex1,
		int firstIndex2, int lastIndex2)
	{
		using var app = App();

		// All containers start off with a height of 50 (2 containers fit in viewport).
		var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 50)).ToList();
		var (target, scroll, itemsControl) = CreateTarget(items, CanvasWithHeightTemplate, bufferFactor: bufferFactor);

		// Scroll to the 5th item (containers 4 and 5 should be visible).
		target.ScrollIntoView(5);
		CornerstoneTest.AreEqual(firstIndex1, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex1, target.LastRealizedIndex);

		// The extent should be 500 (10 * 50) and the offset should be 200 (4 * 50).
		var container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(5));
		CornerstoneTest.AreEqual(new Rect(0, 250, 100, 50), container.Bounds);
		CornerstoneTest.AreEqual(new Size(100, 100), scroll.Viewport);
		CornerstoneTest.AreEqual(new Size(100, 1000), scroll.Extent);
		CornerstoneTest.AreEqual(new Vector(0, 200), scroll.Offset);

		// Update the height of all items to 25 and run a layout pass.
		foreach (var item in items)
		{
			item.Height = 25;
		}
		target.UpdateLayout();

		// The extent should be updated to reflect the new heights. The offset should be
		// unchanged but the first realized index should be updated to 8 (200 / 25).
		CornerstoneTest.AreEqual(new Size(100, 100), scroll.Viewport);
		CornerstoneTest.AreEqual(new Size(100, 500), scroll.Extent);
		CornerstoneTest.AreEqual(new Vector(0, 200), scroll.Offset);
		CornerstoneTest.AreEqual(firstIndex2, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex2, target.LastRealizedIndex);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void FiresCorrectContainerLifecycleEventsOnReplace(double bufferFactor)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;
		var events = new List<string>();

		itemsControl.ContainerPrepared += (s, e) => events.Add($"Prepared #{e.Container.GetHashCode()} = {e.Index}");
		itemsControl.ContainerClearing += (s, e) => events.Add($"Clearing #{e.Container.GetHashCode()}");
		itemsControl.ContainerIndexChanged += (s, e) => events.Add($"IndexChanged #{e.Container.GetHashCode()} {e.OldIndex} -> {e.NewIndex}");

		var toReplace = target.GetRealizedElements().ElementAt(2)!;
		items[2] = "New Item";

		CornerstoneTest.AreEqual(new[] { $"Clearing #{toReplace.GetHashCode()}" }, events);
		events.Clear();

		itemsControl.UpdateLayout();

		CornerstoneTest.AreEqual(new[] { $"Prepared #{toReplace.GetHashCode()} = 2" }, events);
		events.Clear();
	}

	[PresentationTestMethod]
	[DataRow(0d,
		4, 7,
		3, 6,
		3, 7)]
	[DataRow(0.5d,
		0, 7,
		0, 7,
		0, 9)]
	public void FocusedContainerIsPositionedCorrectlywhenContainerSizeChangeCausesItToBeMovedIntoVisibleViewport(double bufferFactor,
		int firstIndex1, int lastIndex1,
		int firstIndex2, int lastIndex2,
		int firstIndex3, int lastIndex3)
	{
		using var app = App();

		// All containers start off with a height of 25 (4 containers fit in viewport).
		var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 25)).ToList();
		var (target, scroll, itemsControl) = CreateTarget(items, CanvasWithHeightTemplate, bufferFactor: bufferFactor);

		// Scroll to the 5th item (containers 4-7 should be visible).
		target.ScrollIntoView(7);
		CornerstoneTest.AreEqual(firstIndex1, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex1, target.LastRealizedIndex);

		// Focus the 7th item.
		var container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(7));
		container.Focusable = true;
		container.Focus();

		// Scroll up to the 3rd item (containers 3-6 should still be visible).
		target.ScrollIntoView(3);
		CornerstoneTest.AreEqual(firstIndex2, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex2, target.LastRealizedIndex);

		// Update the height of all items to 20 and run a layout pass.
		foreach (var item in items)
		{
			item.Height = 20;
		}
		target.UpdateLayout();

		// The focused container should now be inside the realized range.
		CornerstoneTest.AreEqual(firstIndex3, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex3, target.LastRealizedIndex);

		// The container should be positioned correctly.
		container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(7));
		CornerstoneTest.AreEqual(new Rect(0, 140, 100, 20), container.Bounds);
	}

	[PresentationTestMethod]
	[DataRow(0d,
		4, 5,
		8, 11)]
	[DataRow(0.5d,
		3, 6,
		6, 13)]
	public void FocusedContainerIsPositionedCorrectlywhenContainerSizeChangeCausesItToBeMovedOutOfVisibleViewport(double bufferFactor,
		int firstIndex1, int lastIndex1,
		int firstIndex2, int lastIndex2)
	{
		using var app = App();

		// All containers start off with a height of 50 (2 containers fit in viewport).
		var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 50)).ToList();
		var (target, scroll, itemsControl) = CreateTarget(items, CanvasWithHeightTemplate, bufferFactor: bufferFactor);

		// Scroll to the 5th item (containers 4 and 5 should be visible).
		target.ScrollIntoView(5);
		CornerstoneTest.AreEqual(firstIndex1, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex1, target.LastRealizedIndex);

		// Focus the 5th item.
		var container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(5));
		container.Focusable = true;
		container.Focus();

		// Update the height of all items to 25 and run a layout pass.
		foreach (var item in items)
		{
			item.Height = 25;
		}
		target.UpdateLayout();

		// The focused container should now be outside the realized range.
		CornerstoneTest.AreEqual(firstIndex2, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(lastIndex2, target.LastRealizedIndex);

		// The container should still exist and be positioned outside the visible viewport.
		container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(5));
		CornerstoneTest.AreEqual(new Rect(0, 125, 100, 25), container.Bounds);
	}

	[PresentationTestMethod]
	public void FocusedContainerIsPositionedOutsideViewportWhenScrolledPastItemsWithDifferentHeights()
	{
		using var app = App();

		var items = Enumerable.Range(0, 20)
			.Select(x => new ItemWithHeight(x, x < 10 ? 10 : 50))
			.ToList();

		var (target, scroll, _) = CreateTarget(items, CanvasWithHeightTemplate);

		var focused = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(5));
		focused.Focusable = true;
		focused.Focus();

		target.ScrollIntoView(15);
		Layout(target);

		CornerstoneTest.IsTrue(target.FirstRealizedIndex > 5);

		var firstRealized = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(target.FirstRealizedIndex));
		focused = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(5));

		// The focused container's position is estimated, as it's outside the realized range.
		// The estimate must never place it before the panel origin...
		CornerstoneTest.IsTrue(focused.Bounds.Top >= 0);

		// ...must keep it above the realized range rather than overlapping it...
		CornerstoneTest.IsTrue(focused.Bounds.Bottom <= firstRealized.Bounds.Top);

		// ...and must keep it out of the viewport, so it can't appear as a ghost item.
		CornerstoneTest.IsTrue(focused.Bounds.Bottom <= scroll.Offset.Y);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void FocusedElementLosingFocusDoesNotResetSelection(double bufferFactor)
	{
		using var app = App();
		var (target, scroll, listBox) = CreateTarget<ListBox, VirtualizingStackPanel>(
			styles: new[]
			{
				new Style(x => x.OfType<ListBoxItem>())
				{
					Setters =
					{
						new Setter(ListBoxItem.TemplateProperty, ListBoxItemTemplate())
					}
				}
			}, bufferFactor: bufferFactor);

		listBox.SelectedIndex = 0;

		var selectedContainer = target.GetRealizedElements().First()!;
		selectedContainer.Focusable = true;
		selectedContainer.Focus();

		scroll.Offset = new Vector(0, 500);
		Layout(target);

		var newFocused = target.GetRealizedElements().First()!;
		newFocused.Focusable = true;
		newFocused.Focus();

		CornerstoneTest.AreEqual(0, listBox.SelectedIndex);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void FocusedElementOutsideRealizedRangeIsNotArrangedInViewport(double bufferFactor)
	{
		using var app = App();

		// Item 0 is much taller than the others, so the average-based position estimated for
		// it once it has left the realized range lands inside the viewport (#17935).
		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, x == 0 ? 100 : 10));
		var (target, scroll, itemsControl) = CreateTarget(
			items,
			CanvasWithHeightTemplate,
			bufferFactor: bufferFactor);

		var focused = target.GetRealizedElements().First()!;
		focused.Focusable = true;
		focused.Focus();
		CornerstoneTest.IsTrue(focused.IsKeyboardFocusWithin);

		scroll.Offset = new Vector(0, 160);
		Layout(target);

		var viewport = new Rect(scroll.Offset.X, scroll.Offset.Y, scroll.Viewport.Width, scroll.Viewport.Height);
		CornerstoneTest.IsTrue(focused.IsKeyboardFocusWithin);
		CornerstoneTest.IsFalse(focused.Bounds.Intersects(viewport));

		scroll.Offset = new Vector(0, 0);
		Layout(target);

		CornerstoneTest.Same(focused, target.GetRealizedElements().First());
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), focused.Bounds);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void FocusingAnotherElementRecyclesOriginalFocusElement(double bufferFactor)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		var originalFocused = target.GetRealizedElements().First()!;
		originalFocused.Focusable = true;
		originalFocused.Focus();

		scroll.Offset = new Vector(0, 500);
		Layout(target);

		var newFocused = target.GetRealizedElements().First()!;
		newFocused.Focusable = true;
		newFocused.Focus();

		CornerstoneTest.IsFalse(originalFocused.IsVisible);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10)]
	[DataRow(0.5d, 20)] // Buffer factor of 0.5. Since at start there is no room, the 10 additional items are just appended
	public void InitializesInitialControlItems(double bufferFactor, int expectedCount)
	{
		using var app = App();
		var items = Enumerable.Range(0, 100).Select(x => new Button { Width = 25, Height = 10 });
		var (target, scroll, itemsControl) = CreateTarget(items, null, bufferFactor: bufferFactor);

		CornerstoneTest.AreEqual(1000, scroll.Extent.Height);

		AssertRealizedControlItems<Button>(target, itemsControl, 0, expectedCount);
	}

	[PresentationTestMethod]
	[DataRow(0d, 20)]
	[DataRow(0.5d, 15)]
	public void InsertingItemBeforeViewportPreservesFirstRealizedIndex(double bufferFactor, int firstIndex)
	{
		// Issue #12744
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;

		// Scroll down 20 items.
		scroll.Offset = new Vector(0, 200);
		target.UpdateLayout();
		CornerstoneTest.AreEqual(firstIndex, target.FirstRealizedIndex);

		// Insert an item at the beginning.
		items.Insert(0, "New Item");
		target.UpdateLayout();

		// The first realized index should still be 20 as the scroll should be unchanged.
		CornerstoneTest.AreEqual(firstIndex, target.FirstRealizedIndex);
		CornerstoneTest.AreEqual(new(0, 200), scroll.Offset);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void IsVisibleBindingPersistsAfterScrolling(double bufferFactor)
	{
		using var app = App();
		var style = CreateIsVisibleBindingStyle();
		var items = Enumerable.Range(0, 100).Select(x => new ItemWithIsVisible(x)).ToList();
		var (target, scroll, itemsControl) = CreateTarget(items, styles: new[] { style }, bufferFactor: bufferFactor);
		var container = target.ContainerFromIndex(2)!;

		CornerstoneTest.IsTrue(container.IsVisible);
		CornerstoneTest.AreEqual(20, container.Bounds.Top);

		items[2].IsVisible = false;
		scroll.Offset = new Vector(0, 200);
		Layout(target);

		scroll.Offset = new Vector(0, 0);
		Layout(target);

		container = target.ContainerFromIndex(2)!;
		CornerstoneTest.IsFalse(container.IsVisible);
	}

	[PresentationTestMethod]
	public void ItemOfContentSizedPanelIsPositionedAtStart()
	{
		using var app = App();

		var items = new ObservableCollection<string>();
		var (target, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(items);

		// Size the control to its content rather than to the viewport.
		itemsControl.VerticalAlignment = VerticalAlignment.Top;

		CreateRoot(itemsControl).LayoutManager.ExecuteInitialLayoutPass();

		items.Add("Item 0");
		Layout(target);

		var container = CornerstoneTest.IsAssignableFrom<Control>(target.ContainerFromIndex(0));

		CornerstoneTest.AreEqual(0, container.Bounds.Y);
		CornerstoneTest.AreEqual(container.Bounds.Height, target.Bounds.Height);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, "4,9")]
	[DataRow(0.5d, 20, "4,9,14,19")]
	public void NthChildSelectorWorks(double bufferFactor, int count, string indexesRaw)
	{
		using var app = App();

		var style = new Style(x => x.OfType<ContentPresenter>().NthChild(5, 0))
		{
			Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) }
		};

		var (target, _, _) = CreateTarget(styles: new[] { style }, bufferFactor: bufferFactor);
		var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

		CornerstoneTest.AreEqual(count, realized.Count);

		for (var i = 0; i < count; ++i)
		{
			var container = realized[i];
			var index = target.IndexFromContainer(container);
			var redIndexes = indexesRaw.Split(",").Select(int.Parse).ToArray();
			var expectedBackground = redIndexes.Contains(i) ? Brushes.Red : null;

			CornerstoneTest.AreEqual(i, index);
			CornerstoneTest.AreEqual(expectedBackground, container.Background);
		}
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/12838
	[PresentationTestMethod]
	[DataRow(0d, 10, "4,9")]
	[DataRow(0.5d, 20, "4,9,14,19")]
	public void NthChildSelectorWorksForItemTemplateChildren(double bufferFactor, int count, string indexesRaw)
	{
		using var app = App();

		var style = new Style(x => x.OfType<ContentPresenter>().NthChild(5, 0).Child().OfType<Canvas>())
		{
			Setters = { new Setter(Panel.BackgroundProperty, Brushes.Red) }
		};

		var (target, _, _) = CreateTarget(styles: new[] { style }, bufferFactor: bufferFactor);
		var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

		CornerstoneTest.AreEqual(count, realized.Count);

		for (var i = 0; i < count; ++i)
		{
			var container = realized[i];
			var index = target.IndexFromContainer(container);
			var redIndexes = indexesRaw.Split(",").Select(int.Parse).ToArray();
			var expectedBackground = redIndexes.Contains(i) ? Brushes.Red : null;

			CornerstoneTest.AreEqual(i, index);
			CornerstoneTest.AreEqual(expectedBackground, ((Canvas) container.Child!).Background);
		}
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, "0,5")]
	[DataRow(0.5d, 20, "0,5,10,15")]
	public void NthLastChildSelectorWorks(double bufferFactor, int count, string indexesRaw)
	{
		using var app = App();

		var style = new Style(x => x.OfType<ContentPresenter>().NthLastChild(5, 0))
		{
			Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) }
		};

		var (target, _, _) = CreateTarget(styles: new[] { style }, bufferFactor: bufferFactor);
		var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

		CornerstoneTest.AreEqual(count, realized.Count);

		for (var i = 0; i < count; ++i)
		{
			var container = realized[i];
			var index = target.IndexFromContainer(container);
			var redIndexes = indexesRaw.Split(",").Select(int.Parse).ToArray();
			var expectedBackground = redIndexes.Contains(i) ? Brushes.Red : null;

			CornerstoneTest.AreEqual(i, index);
			CornerstoneTest.AreEqual(expectedBackground, container.Background);
		}
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/12838
	[PresentationTestMethod]
	[DataRow(0d, 10, "0,5")]
	[DataRow(0.5d, 20, "0,5,10,15")]
	public void NthLastChildSelectorWorksForItemTemplateChildren(double bufferFactor, int count, string indexesRaw)
	{
		using var app = App();

		var style = new Style(x => x.OfType<ContentPresenter>().NthLastChild(5, 0).Child().OfType<Canvas>())
		{
			Setters = { new Setter(Panel.BackgroundProperty, Brushes.Red) }
		};

		var (target, _, _) = CreateTarget(styles: new[] { style }, bufferFactor: bufferFactor);
		var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

		CornerstoneTest.AreEqual(count, realized.Count);

		for (var i = 0; i < count; ++i)
		{
			var container = realized[i];
			var index = target.IndexFromContainer(container);
			var redIndexes = indexesRaw.Split(",").Select(int.Parse).ToArray();
			var expectedBackground = redIndexes.Contains(i) ? Brushes.Red : null;

			CornerstoneTest.AreEqual(i, index);
			CornerstoneTest.AreEqual(expectedBackground, ((Canvas) container.Child!).Background);
		}
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void RecyclingAHiddenControlShowsIt(double bufferFactor)
	{
		using var app = App();
		var style = CreateIsVisibleBindingStyle();
		var itemsList = Enumerable.Range(0, 3).Select(x => new ItemWithIsVisible(x)).ToList();
		var items = new ObservableCollection<ItemWithIsVisible>(itemsList);
		var (target, scroll, itemsControl) = CreateTarget(items, styles: new[] { style }, bufferFactor: bufferFactor);
		var container = target.ContainerFromIndex(2)!;

		CornerstoneTest.IsTrue(container.IsVisible);
		CornerstoneTest.AreEqual(20, container.Bounds.Top);

		items[2].IsVisible = false;
		Layout(target);

		CornerstoneTest.IsFalse(container.IsVisible);

		items.RemoveAt(2);
		items.Add(new ItemWithIsVisible(3));
		Layout(target);

		CornerstoneTest.IsTrue(container.IsVisible);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void RemovesControlItemsFromPanelOnItemRemove(double bufferFactor)
	{
		using var app = App();
		var items = new ObservableCollection<Button>(Enumerable.Range(0, 100).Select(x => new Button { Width = 25, Height = 10 }));
		var (target, scroll, itemsControl) = CreateTarget(items, null, bufferFactor: bufferFactor);

		CornerstoneTest.AreEqual(1000, scroll.Extent.Height);

		var removed = items[1];
		items.RemoveAt(1);

		CornerstoneTest.IsNull(removed.Parent);
		CornerstoneTest.IsNull(removed.VisualParent);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void RemovingItemOfFocusedElementClearsFocus(double bufferFactor)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;

		var focused = target.GetRealizedElements().First()!;
		focused.Focusable = true;
		focused.Focus();
		CornerstoneTest.IsTrue(focused.IsKeyboardFocusWithin);
		CornerstoneTest.AreEqual(focused, KeyboardNavigation.GetTabOnceActiveElement(itemsControl));

		scroll.Offset = new Vector(0, 200);
		Layout(target);

		items.RemoveAt(0);

		CornerstoneTest.All(target.GetRealizedElements(), x => CornerstoneTest.IsFalse(x!.IsKeyboardFocusWithin));
		CornerstoneTest.All(target.GetRealizedElements(), x => CornerstoneTest.NotSame(focused, x));
	}

	[PresentationTestMethod]
	[DataRow(0d, 90, 10)]
	[DataRow(0.5d, 80, 20)]
	public void RemovingRangeToHaveLessThanAPageOfItemsWhenScrolledToEndUpdatesViewport(double bufferFactor, int firstIndex, int count)
	{
		using var app = App();
		var items = new OldPresentationList<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
		var (target, scroll, itemsControl) = CreateTarget(items, bufferFactor: bufferFactor);

		scroll.Offset = new Vector(0, 900);
		Layout(target);

		AssertRealizedItems(target, itemsControl, firstIndex, count);

		items.RemoveRange(0, 95);
		Layout(target);

		AssertRealizedItems(target, itemsControl, 0, 5);
		CornerstoneTest.AreEqual(new Vector(0, 0), scroll.Offset);
	}

	[PresentationTestMethod]
	[DataRow(0d, 90, 10, 10)]
	[DataRow(0.5d, 80, 0, 20)]
	public void RemovingRangeWhenScrolledToEndUpdatesViewport(double bufferFactor, int firstIndex, int secondIndex, int count)
	{
		using var app = App();
		var items = new OldPresentationList<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
		var (target, scroll, itemsControl) = CreateTarget(items, bufferFactor: bufferFactor);

		scroll.Offset = new Vector(0, 900);
		Layout(target);

		AssertRealizedItems(target, itemsControl, firstIndex, count);

		items.RemoveRange(0, 80);
		Layout(target);

		AssertRealizedItems(target, itemsControl, secondIndex, count);
		CornerstoneTest.AreEqual(new Vector(0, 100), scroll.Offset);
	}

	[PresentationTestMethod]
	[DataRow(0d, 90, 10, 10)]
	[DataRow(0.5d, 80, 0, 20)]
	public void ResettingCollectionToHaveLessItemsWhenScrolledToEndUpdatesViewport(double bufferFactor, int firstIndex, int secondIndex, int count)
	{
		using var app = App();
		var items = new ResettingCollection(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
		var (target, scroll, itemsControl) = CreateTarget(items, bufferFactor: bufferFactor);

		scroll.Offset = new Vector(0, 900);
		Layout(target);

		AssertRealizedItems(target, itemsControl, firstIndex, count);

		items.Reset(Enumerable.Range(0, 20).Select(x => $"Item {x}"));
		Layout(target);

		AssertRealizedItems(target, itemsControl, secondIndex, count);
		CornerstoneTest.AreEqual(new Vector(0, 100), scroll.Offset);
	}

	[PresentationTestMethod]
	[DataRow(0d, 90, 10)]
	[DataRow(0.5d, 80, 20)]
	public void ResettingCollectionToHaveLessThanAPageOfItemsWhenScrolledToEndUpdatesViewport(double bufferFactor, int firstIndex, int count)
	{
		using var app = App();
		var items = new ResettingCollection(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
		var (target, scroll, itemsControl) = CreateTarget(items, bufferFactor: bufferFactor);

		scroll.Offset = new Vector(0, 900);
		Layout(target);

		AssertRealizedItems(target, itemsControl, firstIndex, count);

		items.Reset(Enumerable.Range(0, 5).Select(x => $"Item {x}"));
		Layout(target);

		AssertRealizedItems(target, itemsControl, 0, 5);
		CornerstoneTest.AreEqual(new Vector(0, 0), scroll.Offset);
	}

	[PresentationTestMethod]
	[DataRow(0d, 15, 5, 190, 210, 110)]
	[DataRow(0.5d, 10, 10, 253, 273, 173)]
	public void ScrollIntoViewCorrectlyScrollsDownToAPageOfLargerItems(double bufferFactor, int firstIndex, int count, int y, int extentHeight, int offset)
	{
		using var app = App();

		// First 10 items have height of 10, next 10 have height of 20.
		var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, ((x / 10) + 1) * 10));
		var (target, scroll, itemsControl) = CreateTarget(items, CanvasWithHeightTemplate, bufferFactor: bufferFactor);

		// Scroll the last item into view.
		target.ScrollIntoView(19);

		// At the time of the scroll, the average item height is 10, so the requested item
		// should be placed at 190 (19 * 10) which therefore results in an extent of 210 to
		// accommodate the item height of 20. This is obviously not a perfect answer, but
		// it's the best we can do without knowing the actual item heights.
		var container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(19));
		CornerstoneTest.AreEqual(new Rect(0, y, 100, 20), container.Bounds);
		CornerstoneTest.AreEqual(new Size(100, 100), scroll.Viewport);
		CornerstoneTest.AreEqual(new Size(100, extentHeight), scroll.Extent);
		CornerstoneTest.AreEqual(new Vector(0, offset), scroll.Offset);

		// Items 15-19 should be visible.
		AssertRealizedItems(target, itemsControl, firstIndex, count);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, 10)]
	[DataRow(0.5d, 5, 15)]
	public void ScrollIntoViewCorrectlyScrollsDownToAPageOfSmallerItems(double bufferFactor, int firstIndex, int count)
	{
		using var app = App();

		// First 10 items have height of 20, next 10 have height of 10.
		var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, ((29 - x) / 10) * 10));
		var (target, scroll, itemsControl) = CreateTarget(items, CanvasWithHeightTemplate, bufferFactor: bufferFactor);

		// Scroll the last item into view.
		target.ScrollIntoView(19);

		// At the time of the scroll, the average item height is 20, so the requested item
		// should be placed at 380 (19 * 20) which therefore results in an extent of 390 to
		// accommodate the item height of 10. This is obviously not a perfect answer, but
		// it's the best we can do without knowing the actual item heights.
		var container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(19));
		CornerstoneTest.AreEqual(new Rect(0, 380, 100, 10), container.Bounds);
		CornerstoneTest.AreEqual(new Size(100, 100), scroll.Viewport);
		CornerstoneTest.AreEqual(new Size(100, 390), scroll.Extent);
		CornerstoneTest.AreEqual(new Vector(0, 290), scroll.Offset);

		// Items 10-19 should be visible.
		AssertRealizedItems(target, itemsControl, firstIndex, count);
	}

	[PresentationTestMethod]
	[DataRow(0d, 15, 5, 190, 210, 110)]
	[DataRow(0.5d, 10, 10, 253, 273, 173)]
	public void ScrollIntoViewCorrectlyScrollsRightToAPageOfLargerItems(double bufferFactor, int firstIndex, int count, int x, int extentWidth, int offset)
	{
		using var app = App();

		// First 10 items have width of 10, next 10 have width of 20.
		var items = Enumerable.Range(0, 20).Select(x => new ItemWithWidth(x, ((x / 10) + 1) * 10));
		var (target, scroll, itemsControl) = CreateTarget(items,
			CanvasWithWidthTemplate,
			orientation: Orientation.Horizontal,
			bufferFactor: bufferFactor);

		// Scroll the last item into view.
		target.ScrollIntoView(19);

		// At the time of the scroll, the average item width is 10, so the requested item
		// should be placed at 190 (19 * 10) which therefore results in an extent of 210 to
		// accommodate the item width of 20. This is obviously not a perfect answer, but
		// it's the best we can do without knowing the actual item widths.
		var container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(19));
		CornerstoneTest.AreEqual(new Rect(x, 0, 20, 100), container.Bounds);
		CornerstoneTest.AreEqual(new Size(100, 100), scroll.Viewport);
		CornerstoneTest.AreEqual(new Size(extentWidth, 100), scroll.Extent);
		CornerstoneTest.AreEqual(new Vector(offset, 0), scroll.Offset);

		// Items 15-19 should be visible.
		AssertRealizedItems(target, itemsControl, firstIndex, count);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, 10)]
	[DataRow(0.5d, 5, 15)]
	public void ScrollIntoViewCorrectlyScrollsRightToAPageOfSmallerItems(double bufferFactor, int firstIndex, int count)
	{
		using var app = App();

		// First 10 items have width of 20, next 10 have width of 10.
		var items = Enumerable.Range(0, 20).Select(x => new ItemWithWidth(x, ((29 - x) / 10) * 10));
		var (target, scroll, itemsControl) = CreateTarget(items,
			CanvasWithWidthTemplate,
			orientation: Orientation.Horizontal,
			bufferFactor: bufferFactor);

		// Scroll the last item into view.
		target.ScrollIntoView(19);

		// At the time of the scroll, the average item width is 20, so the requested item
		// should be placed at 380 (19 * 20) which therefore results in an extent of 390 to
		// accommodate the item width of 10. This is obviously not a perfect answer, but
		// it's the best we can do without knowing the actual item widths.
		var container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(19));
		CornerstoneTest.AreEqual(new Rect(380, 0, 10, 100), container.Bounds);
		CornerstoneTest.AreEqual(new Size(100, 100), scroll.Viewport);
		CornerstoneTest.AreEqual(new Size(390, 100), scroll.Extent);
		CornerstoneTest.AreEqual(new Vector(290, 0), scroll.Offset);

		// Items 10-19 should be visible.
		AssertRealizedItems(target, itemsControl, firstIndex, count);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void ScrollIntoViewOnEffectivelyInvisiblePanelDoesNotCreateGhostElements(double bufferFactor)
	{
		var items = new[] { "foo", "bar", "baz" };
		var (target, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(items, bufferFactor: bufferFactor);
		var container = new Decorator { Margin = new Thickness(100), Child = itemsControl };
		var root = new TestRoot(true, container);

		root.LayoutManager.ExecuteInitialLayoutPass();

		// Clear the items and do a layout to recycle all elements.
		itemsControl.ItemsSource = null;
		root.LayoutManager.ExecuteLayoutPass();

		// Should have no realized elements and no unrealized elements.
		CornerstoneTest.AreEqual(0, target.GetRealizedElements().Count);
		CornerstoneTest.AreEqual(0, target.Children.Count);

		// Make the panel effectively invisible and set items.
		container.IsVisible = false;
		itemsControl.ItemsSource = items;

		// Try to scroll into view while effectively invisible.
		target.ScrollIntoView(0);

		// Make the panel visible and layout.
		container.IsVisible = true;
		root.LayoutManager.ExecuteLayoutPass();

		// Should have 3 realized elements and no unrealized elements.
		CornerstoneTest.AreEqual(3, target.GetRealizedElements().Count);
		CornerstoneTest.AreEqual(3, target.Children.Count);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void ScrollIntoViewWithTargetRectOutsideViewportShouldScrollToItem(double bufferFactor)
	{
		using var app = App();
		var items = Enumerable.Range(0, 101).Select(x => new ItemWithHeight(x, (x * 100) + 1));
		var itemTemplate = new FuncDataTemplate<ItemWithHeight>((x, _) =>
			new Border
			{
				Height = 10,
				[!Layoutable.WidthProperty] = new Binding("Height")
			});
		var (target, scroll, itemsControl) = CreateTarget(
			items,
			itemTemplate,
			new[]
			{
				new Style(x => x.OfType<ScrollViewer>())
				{
					Setters =
					{
						new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Visible)
					}
				}
			},
			bufferFactor: bufferFactor);
		itemsControl.ContainerPrepared += (_, ev) =>
		{
			ev.Container.AddHandler(Control.RequestBringIntoViewEvent, (_, e) =>
			{
				var dataContext = (ItemWithHeight) e.TargetObject!.DataContext!;
				e.TargetRect = new Rect(dataContext.Height - 50, 0, 50, 10);
			});
		};

		target.ScrollIntoView(100);

		CornerstoneTest.AreEqual(9901, scroll.Offset.X);
	}

	[PresentationTestMethod]
	[DataRow(25, Orientation.Vertical)]
	[DataRow(99, Orientation.Vertical)]
	[DataRow(25, Orientation.Horizontal)]
	[DataRow(99, Orientation.Horizontal)]
	public void ScrollIntoViewWithVariableSizeItemsKeepsTargetInViewport(int targetIndex, Orientation orientation)
	{
		using var app = App();

		var firstHalfSize = targetIndex < 60 ? 20 : 40;
		var secondHalfSize = targetIndex < 60 ? 40 : 20;
		var horizontal = orientation == Orientation.Horizontal;
		IEnumerable<object> items = horizontal ? Enumerable.Range(0, 100).Select(x => new ItemWithWidth(x, x < 50 ? firstHalfSize : secondHalfSize)) : Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, x < 50 ? firstHalfSize : secondHalfSize));
		Optional<IDataTemplate> itemTemplate = horizontal ? CanvasWithWidthTemplate : CanvasWithHeightTemplate;
		var (target, scroll, _) = CreateTarget(items, itemTemplate, orientation: orientation);

		target.ScrollIntoView(60);
		target.ScrollIntoView(targetIndex);

		var container = CornerstoneTest.IsType<ContentPresenter>(target.ContainerFromIndex(targetIndex));
		var message = $"Bounds={container.Bounds}, Offset={scroll.Offset}, Viewport={scroll.Viewport}, Extent={scroll.Extent}";

		var containerStart = horizontal ? container.Bounds.Left : container.Bounds.Top;
		var containerEnd = horizontal ? container.Bounds.Right : container.Bounds.Bottom;
		var viewportStart = horizontal ? scroll.Offset.X : scroll.Offset.Y;
		var viewportEnd = viewportStart + (horizontal ? scroll.Viewport.Width : scroll.Viewport.Height);

		CornerstoneTest.IsTrue(containerStart > 0, message);
		CornerstoneTest.IsTrue(containerStart >= viewportStart, message);
		CornerstoneTest.IsTrue(containerEnd <= viewportEnd, message);
	}

	[PresentationTestMethod]
	[DataRow(Orientation.Vertical, 0d)]
	[DataRow(Orientation.Vertical, 0.5d)]
	[DataRow(Orientation.Horizontal, 0d)]
	[DataRow(Orientation.Horizontal, 0.5d)]
	public void ScrollToEndArrivesAtEndWhenItemsHaveDifferentSizes(Orientation orientation, double bufferFactor)
	{
		using var app = App();
		var horizontal = orientation == Orientation.Horizontal;
		var items = Enumerable.Range(0, 100)
			.Select(x => horizontal ? (object) new ItemWithWidth(x, x < 60 ? 20 : 50) : new ItemWithHeight(x, x < 60 ? 20 : 50))
			.ToList();
		var (target, scroll, itemsControl) = CreateUnrootedTarget<ItemsControl>(
			items,
			horizontal ? CanvasWithWidthTemplate : CanvasWithHeightTemplate,
			orientation,
			bufferFactor);
		scroll.Template = ScrollViewerTemplateWithScrollBars();
		CreateRoot(itemsControl).LayoutManager.ExecuteInitialLayoutPass();

		(horizontal ? scroll.HorizontalScrollBar : scroll.VerticalScrollBar)?.ScrollToEnd();
		Layout(target);

		CornerstoneTest.AreEqual(horizontal ? scroll.Extent.Width - scroll.Viewport.Width : scroll.Extent.Height - scroll.Viewport.Height, horizontal ? scroll.Offset.X : scroll.Offset.Y);
		CornerstoneTest.AreEqual(items.Count - 1, target.LastRealizedIndex);
		CornerstoneTest.AreEqual(horizontal ? target.ViewPort.Right : target.ViewPort.Bottom, horizontal ? target.GetRealizedElements().Last()!.Bounds.Right : target.GetRealizedElements().Last()!.Bounds.Bottom);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void ScrollingBackToFocusedElementUsesCorrectElement(double bufferFactor)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		var focused = target.GetRealizedElements().First()!;
		focused.Focusable = true;
		focused.Focus();
		CornerstoneTest.IsTrue(focused.IsKeyboardFocusWithin);

		scroll.Offset = new Vector(0, 200);
		Layout(target);

		scroll.Offset = new Vector(0, 0);
		Layout(target);

		CornerstoneTest.Same(focused, target.GetRealizedElements().First());
	}

	[PresentationTestMethod]
	public void ScrollingDownDoesNotMeasureOrArrangeUntilExtendedViewPortBoundsAreReached()
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();

		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithHeightTemplate,
				bufferFactor: 0.5d);

		CornerstoneTest.IsTrue(target.LastRealizedIndex == 19, $"Should show 20 items but last realized index was {target.LastRealizedIndex}");

		// reset counters
		target.ResetMeasureArrangeCounters();

		// shows 20 items, each is 10 high.
		// visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
		// until then no measure-arrange call should happen

		var count = 0;

		// Scroll down until the extended viewport bounds are reached
		while (target.LastRealizedIndex < 20)
		{
			scroll.Offset = new Vector(0, scroll.Offset.Y + 5);
			Layout(target);
			count++;
			if (count > 1000)
			{
				throw new InvalidOperationException("infinite scroll detected");
			}
		}

		// Assert
		CornerstoneTest.IsTrue(target.Measured == 1, "should be measured only once");
		CornerstoneTest.IsTrue(target.Arranged == 1, "should be arranged only once");

		// the first 5 additional items will be reused when scrolling down, but the remaining 10 visible + 5 additional not touched at all
		var expectedUntouchedItems =
			items.Skip(5 /*additional items*/).Take(15).ToList();
		foreach (var itm in expectedUntouchedItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
		}

		var newAdditionalItems = items.Skip(20).Take(5);
		foreach (var itm in newAdditionalItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
		}
	}

	[PresentationTestMethod]
	public void ScrollingDownToEndOfListOnlyMeasuresOnceWhenLastItemIsReached()
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();

		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithHeightTemplate,
				bufferFactor: 0.5d);

		// scroll a bit down so we are near the end of the list
		scroll.Offset = new Vector(0, 800); // so we render 75 to 95 with a buffer size of 5
		Layout(target);

		CornerstoneTest.IsTrue(target.LastRealizedIndex == 94, $"Should show 20 items but last realized index was {target.LastRealizedIndex}");

		// reset counters
		target.ResetMeasureArrangeCounters();

		// shows 20 items, each is 10 high.
		// visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
		// until then no measure-arrange call should happen

		var initialLastRealizedIndex = target.LastRealizedIndex;

		var count = 0;

		// Scroll down until we reached the very last item
		while (target.LastRealizedIndex < 99)
		{
			scroll.Offset = new Vector(0, scroll.Offset.Y + 5);
			Layout(target);
			count++;
			if (count > 1000)
			{
				throw new InvalidOperationException("infinite scroll detected");
			}
		}

		// Assert
		CornerstoneTest.IsTrue(target.Measured == 1, "should be measured only once even though we are at the end of the list");
		CornerstoneTest.IsTrue(target.Arranged == 1, "should be arranged only once even though we are at the end of the list");

		// the first 5 additional items will be reused when scrolling down, but the remaining 10 visible + 5 additional not touched at all
		var expectedUntouchedItems =
			items.Skip((initialLastRealizedIndex + 1) - 15).Take(15).ToList();
		foreach (var itm in expectedUntouchedItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
		}

		var newAdditionalItems = items.Skip(initialLastRealizedIndex + 1).Take(5);
		foreach (var itm in newAdditionalItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
		}
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void ScrollingDownWithLargerElementDoesNotCauseJumpAndArrivesAtEnd(double bufferFactor)
	{
		using var app = App();

		var items = Enumerable.Range(0, 1000).Select(x => new ItemWithHeight(x)).ToList();
		items[20].Height = 200;

		var (target, scroll, itemsControl) = CreateTarget(items, CanvasWithHeightTemplate, bufferFactor: bufferFactor);

		var index = target.FirstRealizedIndex;

		// Scroll down to the larger element.
		while (target.LastRealizedIndex < (items.Count - 1))
		{
			scroll.LineDown();
			Layout(target);

			CornerstoneTest.IsTrue(target.FirstRealizedIndex >= index, $"{target.FirstRealizedIndex} is not greater or equal to {index}");

			if ((scroll.Offset.Y + scroll.Viewport.Height) == scroll.Extent.Height)
			{
				CornerstoneTest.AreEqual(items.Count - 1, target.LastRealizedIndex);
			}

			index = target.FirstRealizedIndex;
		}
	}

	[PresentationTestMethod]
	public void ScrollingLeftDoesNotMeasureOrArrangeUntilExtendedViewPortBoundsAreReached()
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidthAndMeasureArrangeCount(x)).ToList();

		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithWidthTemplate,
				orientation: Orientation.Horizontal,
				bufferFactor: 0.5d);

		// scroll a bit down so we are not near the start of the list
		scroll.Offset = new Vector(200, 0);
		Layout(target);

		CornerstoneTest.IsTrue(target.FirstRealizedIndex == 15, $"Should show items from 20 to 30 (so 15 to 35 including additional items) but first realized index was {target.FirstRealizedIndex}");

		// reset counters
		target.ResetMeasureArrangeCounters();

		// shows 20 items, each is 10 high.
		// visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
		// until then no measure-arrange call should happen

		var initialFirstRealizedIndex = target.FirstRealizedIndex;
		var count = 0;

		// Scroll down until the extended viewport bounds are reached
		while (target.FirstRealizedIndex >= 15)
		{
			scroll.Offset = new Vector(scroll.Offset.X - 5, 0);
			Layout(target);
			count++;
			if (count > 1000)
			{
				throw new InvalidOperationException("infinite scroll detected");
			}
		}

		// Assert
		CornerstoneTest.IsTrue(target.Measured == 1, "should be measured only once");
		CornerstoneTest.IsTrue(target.Arranged == 1, "should be arranged only once");

		// the last 5 additional items will be reused when scrolling up, but the remaining 10 visible + 5 additional not touched at all
		var expectedUntouchedItems = items.Skip(initialFirstRealizedIndex + 1).Take(15).ToList();
		foreach (var itm in expectedUntouchedItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
		}

		// now that we scrolled up to index 19, items 18,17,16,15 and 14 should be the "additional" ones
		var newAdditionalItems = items.Skip(initialFirstRealizedIndex - 6).Take(6);
		foreach (var itm in newAdditionalItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
		}
	}

	[PresentationTestMethod]
	public void ScrollingLeftToStartOfListOnlyMeasuresOnceWhenFirstItemIsReached()
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidthAndMeasureArrangeCount(x)).ToList();

		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithWidthTemplate,
				orientation: Orientation.Horizontal,
				bufferFactor: 0.5d);

		// scroll a bit down so we are not near the start of the list
		scroll.Offset = new Vector(105, 0);
		Layout(target);

		CornerstoneTest.IsTrue(target.FirstRealizedIndex == 5, $"Should show items from 10 to 20 (so 5 to 25 including additional items) but first realized index was {target.FirstRealizedIndex}");

		// reset counters
		target.ResetMeasureArrangeCounters();

		// shows 20 items, each is 10 high.
		// visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
		// until then no measure-arrange call should happen

		var count = 0;

		// Scroll down until the extended viewport bounds are reached
		while (target.FirstRealizedIndex > 0)
		{
			scroll.Offset = new Vector(scroll.Offset.X - 5, 0);
			Layout(target);
			count++;
			if (count > 1000)
			{
				throw new InvalidOperationException("infinite scroll detected");
			}
		}

		// Assert
		CornerstoneTest.IsTrue(target.Measured == 1, "should be measured only once even though we are at the start of the list");
		CornerstoneTest.IsTrue(target.Arranged == 1, "should be arranged only once even though we are at the start of the list");

		// the last 5 additional items will be reused when scrolling up, but the remaining 10 visible + 5 additional not touched at all
		var expectedMeasuredItems = items.Take(20).ToList();
		foreach (var itm in expectedMeasuredItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 1, $"{itm.Caption} should be arranged but was {itm.Arranged} times");
		}

		// now that we scrolled up to index 19, items 18,17,16,15 and 14 should be the "additional" ones
		var untouchedItems = items.Skip(20).ToList();
		foreach (var itm in untouchedItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 0, $"{itm.Caption} should not be measured but was {itm.Arranged} times");
		}
	}

	[PresentationTestMethod]
	public void ScrollingRightDoesNotMeasureOrArrangeUntilExtendedViewPortBoundsAreReached()
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidthAndMeasureArrangeCount(x)).ToList();

		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithWidthTemplate,
				orientation: Orientation.Horizontal,
				bufferFactor: 0.5d);

		CornerstoneTest.IsTrue(target.LastRealizedIndex == 19, $"Should show 20 items but last realized index was {target.LastRealizedIndex}");

		// reset counters
		target.ResetMeasureArrangeCounters();

		// shows 20 items, each is 10 high.
		// visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
		// until then no measure-arrange call should happen
		var count = 0;

		// Scroll down until the extended viewport bounds are reached
		while (target.LastRealizedIndex < 20)
		{
			scroll.Offset = new Vector(scroll.Offset.X + 5, 0);
			Layout(target);
			count++;
			if (count > 1000)
			{
				throw new InvalidOperationException("infinite scroll detected");
			}
		}

		// Assert
		CornerstoneTest.IsTrue(target.Measured == 1, "should be measured only once");
		CornerstoneTest.IsTrue(target.Arranged == 1, "should be arranged only once");

		// the first 5 additional items will be reused when scrolling down, but the remaining 10 visible + 5 additional not touched at all
		var expectedUntouchedItems =
			items.Skip(5 /*additional items*/).Take(15).ToList();
		foreach (var itm in expectedUntouchedItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
		}

		var newAdditionalItems = items.Skip(20).Take(5);
		foreach (var itm in newAdditionalItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
		}
	}

	[PresentationTestMethod]
	public void ScrollingRightToEndOfListOnlyMeasuresOnceWhenLastItemIsReached()
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidthAndMeasureArrangeCount(x)).ToList();

		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithWidthTemplate,
				orientation: Orientation.Horizontal,
				bufferFactor: 0.5d);

		// scroll a bit down so we are near the end of the list
		scroll.Offset = new Vector(800, 0); // so we render 75 to 95 with a buffer size of 5
		Layout(target);

		CornerstoneTest.IsTrue(target.LastRealizedIndex == 94, $"Should show 20 items but last realized index was {target.LastRealizedIndex}");

		// reset counters
		target.ResetMeasureArrangeCounters();

		// shows 20 items, each is 10 high.
		// visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
		// until then no measure-arrange call should happen

		var initialLastRealizedIndex = target.LastRealizedIndex;

		var count = 0;

		// Scroll down until we reached the very last item
		while (target.LastRealizedIndex < 99)
		{
			scroll.Offset = new Vector(scroll.Offset.X + 5, 0);
			Layout(target);
			count++;
			if (count > 1000)
			{
				throw new InvalidOperationException("infinite scroll detected");
			}
		}

		// Assert
		CornerstoneTest.IsTrue(target.Measured == 1, "should be measured only once even though we are at the end of the list");
		CornerstoneTest.IsTrue(target.Arranged == 1, "should be arranged only once even though we are at the end of the list");

		// the first 5 additional items will be reused when scrolling down, but the remaining 10 visible + 5 additional not touched at all
		var expectedUntouchedItems =
			items.Skip((initialLastRealizedIndex + 1) - 15).Take(15).ToList();
		foreach (var itm in expectedUntouchedItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
		}

		var newAdditionalItems = items.Skip(initialLastRealizedIndex + 1).Take(5);
		foreach (var itm in newAdditionalItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
		}
	}

	[PresentationTestMethod]
	public void ScrollingUpDoesNotMeasureOrArrangeUntilExtendedViewPortBoundsAreReached()
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();

		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithHeightTemplate,
				bufferFactor: 0.5d);

		// scroll a bit down so we are not near the start of the list
		scroll.Offset = new Vector(0, 200);
		Layout(target);

		CornerstoneTest.IsTrue(target.FirstRealizedIndex == 15, $"Should show items from 20 to 30 (so 15 to 35 including additional items) but first realized index was {target.FirstRealizedIndex}");

		// reset counters
		target.ResetMeasureArrangeCounters();

		// shows 20 items, each is 10 high.
		// visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
		// until then no measure-arrange call should happen

		var initialFirstRealizedIndex = target.FirstRealizedIndex;

		var count = 0;

		// Scroll down until the extended viewport bounds are reached
		while (target.FirstRealizedIndex >= 15)
		{
			scroll.Offset = new Vector(0, scroll.Offset.Y - 5);
			Layout(target);
			count++;
			if (count > 1000)
			{
				throw new InvalidOperationException("infinite scroll detected");
			}
		}

		// Assert
		CornerstoneTest.IsTrue(target.Measured == 1, "should be measured only once");
		CornerstoneTest.IsTrue(target.Arranged == 1, "should be arranged only once");

		// the last 5 additional items will be reused when scrolling up, but the remaining 10 visible + 5 additional not touched at all
		var expectedUntouchedItems = items.Skip(initialFirstRealizedIndex + 1).Take(15).ToList();
		foreach (var itm in expectedUntouchedItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 0, $"{itm.Caption} should not be arranged but was {itm.Arranged} times");
		}

		// now that we scrolled up to index 19, items 18,17,16,15 and 14 should be the "additional" ones
		var newAdditionalItems = items.Skip(initialFirstRealizedIndex - 6).Take(6);
		foreach (var itm in newAdditionalItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 1, $"{itm.Caption} should be measured but was {itm.Arranged} times");
		}
	}

	[PresentationTestMethod]
	[DataRow(0d, 10)]
	[DataRow(0.5d, 20)]
	public void ScrollingUpToIndexDoesNotCreateAPageOfUnrealizedElements(double bufferFactor, int expectedCount)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		scroll.ScrollToEnd();
		Layout(target);
		target.ScrollIntoView(20);

		CornerstoneTest.AreEqual(expectedCount, target.Children.Count);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void ScrollingUpToLargerElementDoesNotCauseJump(double bufferFactor)
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();
		items[20].Height = 200;

		var (target, scroll, itemsControl) = CreateTarget(items, CanvasWithHeightTemplate, bufferFactor: bufferFactor);

		// Scroll past the larger element.
		scroll.Offset = new Vector(0, 600);
		Layout(target);

		// Precondition checks
		CornerstoneTest.IsTrue(target.FirstRealizedIndex > 20);

		var index = target.FirstRealizedIndex;

		// Scroll up to the top.
		while (scroll.Offset.Y > 0)
		{
			scroll.LineUp();
			Layout(target);

			CornerstoneTest.IsTrue(target.FirstRealizedIndex <= index, $"{target.FirstRealizedIndex} is not less than {index}");
			index = target.FirstRealizedIndex;
		}
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void ScrollingUpToSmallerElementDoesNotCauseJump(double bufferFactor)
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, 30)).ToList();
		items[20].Height = 25;

		var (target, scroll, itemsControl) = CreateTarget(items,
			CanvasWithHeightTemplate,
			bufferFactor: bufferFactor);

		var additionalItemsCount = bufferFactor == 0d
			? 1

			// buffer factor of 0.5 and 7 visible items => will be rounded up to 4
			// => when we scroll up and are near the _extended_ viewport,
			// 4 additional items will be inserted above the current viewport
			: Math.Round(target.Children.Count * target.CacheLength, MidpointRounding.AwayFromZero);

		// Scroll past the larger element.
		scroll.Offset = new Vector(0, 25 * items[0].Height);
		Layout(target);

		// Precondition checks
		CornerstoneTest.IsTrue(target.FirstRealizedIndex > 20);

		var index = target.FirstRealizedIndex;

		// Scroll up to the top.
		while (scroll.Offset.Y > 0)
		{
			scroll.Offset = scroll.Offset - new Vector(0, 5);
			Layout(target);

			CornerstoneTest.IsTrue(target.FirstRealizedIndex <= index, $"{target.FirstRealizedIndex} is not less than {index}");
			CornerstoneTest.IsTrue((index - target.FirstRealizedIndex) <= additionalItemsCount, $"FirstIndex changed from {index} to {target.FirstRealizedIndex}");

			index = target.FirstRealizedIndex;
		}
	}

	[PresentationTestMethod]
	public void ScrollingUpToStartOfListOnlyMeasuresOnceWhenFirstItemIsReached()
	{
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeightAndMeasureArrangeCount(x)).ToList();

		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithHeightTemplate,
				bufferFactor: 0.5d);

		// scroll a bit down so we are not near the start of the list
		scroll.Offset = new Vector(0, 105);
		Layout(target);

		CornerstoneTest.IsTrue(target.FirstRealizedIndex == 5, $"Should show items from 10 to 20 (so 5 to 25 including additional items) but first realized index was {target.FirstRealizedIndex}");

		// reset counters
		target.ResetMeasureArrangeCounters();

		// shows 20 items, each is 10 high.
		// visible are 10 => need to scroll down 100px until the next 5 (visible*BufferFactor) additional items are added.
		// until then no measure-arrange call should happen

		var count = 0;

		// Scroll down until the extended viewport bounds are reached
		while (target.FirstRealizedIndex > 0)
		{
			scroll.Offset = new Vector(0, scroll.Offset.Y - 5);
			Layout(target);
			count++;
			if (count > 1000)
			{
				throw new InvalidOperationException("infinite scroll detected");
			}
		}

		// Assert
		CornerstoneTest.IsTrue(target.Measured == 1, "should be measured only once even though we are at the start of the list");
		CornerstoneTest.IsTrue(target.Arranged == 1, "should be arranged only once even though we are at the start of the list");

		// the last 5 additional items will be reused when scrolling up, but the remaining 10 visible + 5 additional not touched at all
		var expectedMeasuredItems = items.Take(20).ToList();
		foreach (var itm in expectedMeasuredItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 1, $"{itm.Caption} should be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 1, $"{itm.Caption} should be arranged but was {itm.Arranged} times");
		}

		// now that we scrolled up to index 19, items 18,17,16,15 and 14 should be the "additional" ones
		var untouchedItems = items.Skip(20).ToList();
		foreach (var itm in untouchedItems)
		{
			CornerstoneTest.IsTrue(itm.Measured == 0, $"{itm.Caption} should not be measured but was {itm.Measured} times");
			CornerstoneTest.IsTrue(itm.Arranged == 0, $"{itm.Caption} should not be measured but was {itm.Arranged} times");
		}
	}

	[PresentationTestMethod]
	[DataRow(0d, 20, 10)]
	[DataRow(0.5d, 15, 20)]
	public void ScrollsDownMoreThanAPage(double bufferFactor, int expectedFirstIndex, int expectedCount)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		scroll.Offset = new Vector(0, 200);
		Layout(target);

		AssertRealizedItems(target, itemsControl, expectedFirstIndex, expectedCount);
	}

	[PresentationTestMethod]
	[DataRow(0d, 1, 10)]
	[DataRow(0.5d, 0, 20)]
	public void ScrollsDownOneItem(double bufferFactor, int expectedFirstIndex, int expectedCount)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		scroll.Offset = new Vector(0, 10);
		Layout(target);

		AssertRealizedItems(target, itemsControl, expectedFirstIndex, expectedCount);
	}

	[PresentationTestMethod]
	[DataRow(0d, 11, 10)]
	[DataRow(0.5d, 6, 20)]
	public void ScrollsDownToIndex(double bufferFactor, int expectedFirstIndex, int expectedCount)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		target.ScrollIntoView(20);

		AssertRealizedItems(target, itemsControl, expectedFirstIndex, expectedCount);
	}

	[PresentationTestMethod]
	[DataRow(0d, 90, 20, 10)]
	[DataRow(0.5d, 80, 15, 20)]
	public void ScrollsUpToIndex(double bufferFactor, int firstRealizedIndex, int expectedFirstIndex, int expectedCount)
	{
		using var app = App();
		var (target, scroll, itemsControl) = CreateTarget(bufferFactor: bufferFactor);

		scroll.ScrollToEnd();
		Layout(target);

		CornerstoneTest.AreEqual(firstRealizedIndex, target.FirstRealizedIndex);

		target.ScrollIntoView(20);

		AssertRealizedItems(target, itemsControl, expectedFirstIndex, expectedCount);
	}

	[PresentationTestMethod]
	public void ShrinkingViewportThenGrowingBackTriggersRemeasure()
	{
		// Regression test for stale _extendedViewport comparison in OnEffectiveViewportChanged.
		//
		// When the viewport shrinks (e.g., ComboBox popup shrinks during filtering),
		// OnEffectiveViewportChanged doesn't trigger a measure (needsMeasure=false because
		// the smaller viewport is within the old extended viewport). The _extendedViewport
		// comparison baseline is NOT updated. When the viewport later grows back,
		// OnEffectiveViewportChanged compares against the stale large _extendedViewport,
		// concludes "no significant change", and skips the measure. This prevents item
		// realization when the only measure trigger is OnEffectiveViewportChanged.
		//
		// The fix uses a separate _lastKnownExtendedViewport that is always updated,
		// so the comparison correctly detects viewport growth after a shrink.
		//
		// Key: ScrollContentPresenter passes infinite height for vertical scroll, so
		// the panel's MeasureOverride is NOT called from the layout cascade when only
		// the root size changes. OnEffectiveViewportChanged is the sole measure trigger.
		using var app = App();

		var items = Enumerable.Range(0, 20).Select(x => $"Item {x}");
		var (target, scroll, itemsControl) =
			CreateUnrootedTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items, bufferFactor: 0);
		var root = CreateRoot(itemsControl, new Size(100, 100));

		root.LayoutManager.ExecuteInitialLayoutPass();

		// Initial state: viewport 0-100, 10 items visible, _extendedViewport = (0,0,100,100)
		AssertRealizedItems(target, itemsControl, 0, 10);

		// Shrink viewport (simulates popup shrinking when items are filtered).
		// Panel MeasureOverride is NOT called (ScrollContentPresenter passes infinite height).
		// OnEffectiveViewportChanged fires with small viewport but needsMeasure=false
		// because the small viewport is within the old _extendedViewport.
		root.ClientSize = new Size(100, 10);
		root.InvalidateMeasure();
		Layout(target);

		// Reset counters after shrink
		target.ResetMeasureArrangeCounters();

		// Grow viewport back (simulates popup growing when filter is removed).
		// Panel MeasureOverride is NOT called from layout cascade (same infinite constraint).
		// OnEffectiveViewportChanged is the ONLY path to trigger a remeasure.
		root.ClientSize = new Size(100, 100);
		root.InvalidateMeasure();
		Layout(target);

		// Without fix: OnEffectiveViewportChanged compares new viewport (0-100) against
		// stale _extendedViewport (0-100, never updated during shrink). Sees no change.
		// needsMeasure=false. No remeasure triggered. Measure count = 0.
		//
		// With fix: compares against _lastKnownExtendedViewport (0-10, updated during
		// shrink). Detects that viewport grew past it (100 > 10). needsMeasure=true.
		// InvalidateMeasure called. Measure count >= 1.
		CornerstoneTest.IsTrue(target.Measured >= 1, "Panel should be re-measured when viewport grows back after a previous shrink. " +
			"OnEffectiveViewportChanged must detect viewport growth by comparing against " +
			"the last known extended viewport, not the stale _extendedViewport.");
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void SupportsNullRecycleKeyWhenClearingItems(double bufferFactor)
	{
		using var app = App();
		var (_, _, itemsControl) = CreateUnrootedTarget<NonRecyclingItemsControl>(bufferFactor: bufferFactor);
		var root = CreateRoot(itemsControl);

		root.LayoutManager.ExecuteInitialLayoutPass();

		var firstItem = itemsControl.ContainerFromIndex(0)!;
		itemsControl.ItemsSource = null;

		Layout(itemsControl);

		CornerstoneTest.IsNull(firstItem.Parent);
		CornerstoneTest.IsNull(firstItem.VisualParent);
		CornerstoneTest.Empty(itemsControl.ItemsPanelRoot!.Children);
	}

	[PresentationTestMethod]
	[DataRow(0d, 20)]
	[DataRow(0.5d, 200)]
	public void SupportsNullRecycleKeyWhenScrolling(double bufferFactor, int offset)
	{
		using var app = App();
		var (_, scroll, itemsControl) = CreateUnrootedTarget<NonRecyclingItemsControl>(bufferFactor: bufferFactor);
		var root = CreateRoot(itemsControl);

		root.LayoutManager.ExecuteInitialLayoutPass();

		var firstItem = itemsControl.ContainerFromIndex(0)!;
		scroll.Offset = new(0, offset);

		Layout(itemsControl);

		CornerstoneTest.IsNull(firstItem.Parent);
		CornerstoneTest.IsNull(firstItem.VisualParent);
		CornerstoneTest.DoesNotContain(itemsControl.ItemsPanelRoot!.Children, firstItem);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, "0, 1, 2, 3, 4, 5, -1, 7, 8, 9")]
	[DataRow(0.5d, 20, "0, 1, 2, 3, 4, 5, -1, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19")]
	public void UpdatesElementsOnItemMove(double bufferFactor, int firstCount, string indexesRaw)
	{
		using var app = App();
		var (target, _, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (ObservableCollection<string>) itemsControl.ItemsSource!;

		CornerstoneTest.AreEqual(firstCount, target.GetRealizedElements().Count);

		var toMove = target.GetRealizedElements().ElementAt(2);
		items.Move(2, 6);

		// Container being moved should have been recycled.
		CornerstoneTest.DoesNotContain(target.GetRealizedElements(), toMove);
		CornerstoneTest.IsFalse(toMove!.IsVisible);

		var indexes = GetRealizedIndexes(target, itemsControl);

		// Item removed from realized elements at old position and space inserted at new position.
		CornerstoneTest.AreEqual(indexesRaw.Split(", ").Select(int.Parse).ToArray(), indexes);

		Layout(target);

		indexes = GetRealizedIndexes(target, itemsControl);

		// After layout the missing container should have been created.
		CornerstoneTest.AreEqual(Enumerable.Range(0, firstCount), indexes);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void UpdatesElementsOnItemMoved(double bufferFactor)
	{
		// Arrange

		using var app = App();

		var actualItems = new OldPresentationList<string>(Enumerable
			.Range(0, 100)
			.Select(x => $"Item {x}"));

		var (target, _, itemsControl) = CreateTarget(actualItems, bufferFactor: bufferFactor);

		var expectedRealizedElementContents = new[] { 1, 2, 0, 3, 4, 5, 6, 7, 8, 9 }
			.Select(x => $"Item {x}");

		// Act

		actualItems.Move(0, 2);
		Layout(target);

		// Assert

		var actualRealizedElementContents = target
			.GetRealizedElements()
			.Cast<ContentPresenter>()
			.Select(x => x.Content);

		CornerstoneTest.Equivalent(expectedRealizedElementContents, actualRealizedElementContents);
	}

	[PresentationTestMethod]
	[DataRow(0d)]
	[DataRow(0.5d)]
	public void UpdatesElementsOnItemRangeMoved(double bufferFactor)
	{
		// Arrange

		using var app = App();

		var actualItems = new OldPresentationList<string>(Enumerable
			.Range(0, 100)
			.Select(x => $"Item {x}"));

		var (target, _, itemsControl) = CreateTarget(actualItems, bufferFactor: bufferFactor);

		var expectedRealizedElementContents = new[] { 2, 0, 1, 3, 4, 5, 6, 7, 8, 9 }
			.Select(x => $"Item {x}");

		// Act

		actualItems.MoveRange(0, 2, 3);
		Layout(target);

		// Assert

		var actualRealizedElementContents = target
			.GetRealizedElements()
			.Cast<ContentPresenter>()
			.Select(x => x.Content);

		CornerstoneTest.Equivalent(expectedRealizedElementContents, actualRealizedElementContents);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, 9)]
	[DataRow(0.5d, 20, 19)]
	public void UpdatesElementsOnItemRemove(double bufferFactor, int firstCount, int secondCount)
	{
		using var app = App();
		var (target, _, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (IList) itemsControl.ItemsSource!;

		CornerstoneTest.AreEqual(firstCount, target.GetRealizedElements().Count);

		var toRecycle = target.GetRealizedElements().ElementAt(2);
		items.RemoveAt(2);

		var indexes = GetRealizedIndexes(target, itemsControl);

		// Item removed from realized elements and subsequent row indexes updated.
		CornerstoneTest.AreEqual(Enumerable.Range(0, secondCount), indexes);

		var elements = target.GetRealizedElements().ToList();
		Layout(target);

		indexes = GetRealizedIndexes(target, itemsControl);

		// After layout an element for the newly visible last row is created and indexes updated.
		CornerstoneTest.AreEqual(Enumerable.Range(0, firstCount), indexes);

		// And the removed row should now have been recycled as the last row.
		elements.Add(toRecycle);
		CornerstoneTest.AreEqual(elements, target.GetRealizedElements());
	}

	[PresentationTestMethod]
	[DataRow(0d, 10, "0, 1, -1, 3, 4, 5, 6, 7, 8, 9")]
	[DataRow(0.5d, 20, "0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19")]
	public void UpdatesElementsOnItemReplace(double bufferFactor, int firstCount, string indexesRaw)
	{
		using var app = App();
		var (target, _, itemsControl) = CreateTarget(bufferFactor: bufferFactor);
		var items = (ObservableCollection<string>) itemsControl.ItemsSource!;

		CornerstoneTest.AreEqual(firstCount, target.GetRealizedElements().Count);

		var toReplace = target.GetRealizedElements().ElementAt(2);
		items[2] = "new";

		// Container being replaced should have been recycled.
		CornerstoneTest.DoesNotContain(target.GetRealizedElements(), toReplace);
		CornerstoneTest.IsFalse(toReplace!.IsVisible);

		var indexes = GetRealizedIndexes(target, itemsControl);

		// Item removed from realized elements at old position and space inserted at new position.
		CornerstoneTest.AreEqual(indexesRaw.Split(", ").Select(int.Parse).ToArray(), indexes);

		Layout(target);

		indexes = GetRealizedIndexes(target, itemsControl);

		// After layout the missing container should have been created.
		CornerstoneTest.AreEqual(Enumerable.Range(0, firstCount), indexes);
	}

	[PresentationTestMethod]
	public void WhenHorizontalCalculatesViewPortAtEndOfList()
	{
		// Arrange
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidth(x)).ToList();
		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithWidthTemplate,
				orientation: Orientation.Horizontal,
				bufferFactor: 0.5d);

		// Act
		scroll.Offset = new Vector(900, 0); // scroll to end
		Layout(target);

		// Assert
		CornerstoneTest.AreEqual(900, target.ViewPort.Left);
		CornerstoneTest.AreEqual(1000, target.ViewPort.Right);

		CornerstoneTest.AreEqual(800, target.LastMeasuredExtendedViewPort.Left);
		CornerstoneTest.AreEqual(1000, target.LastMeasuredExtendedViewPort.Right);
	}

	[PresentationTestMethod]
	public void WhenHorizontalCalculatesViewPortAtStartOfList()
	{
		// Arrange
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidth(x)).ToList();

		// Act
		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithWidthTemplate,
				orientation: Orientation.Horizontal,
				bufferFactor: 0.5d);

		// Assert
		CornerstoneTest.AreEqual(0, target.ViewPort.Left);
		CornerstoneTest.AreEqual(100, target.ViewPort.Right);

		CornerstoneTest.AreEqual(0, target.LastMeasuredExtendedViewPort.Left);
		CornerstoneTest.AreEqual(200, target.LastMeasuredExtendedViewPort.Right);
	}

	[PresentationTestMethod]
	public void WhenHorizontalCalculatesViewPortInMiddleOfList()
	{
		// Arrange
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithWidth(x)).ToList();
		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithWidthTemplate,
				orientation: Orientation.Horizontal,
				bufferFactor: 0.5d);

		// Act
		scroll.Offset = new Vector(500, 0); // scroll to end
		Layout(target);

		// Assert
		CornerstoneTest.AreEqual(500, target.ViewPort.Left);
		CornerstoneTest.AreEqual(600, target.ViewPort.Right);

		CornerstoneTest.AreEqual(450, target.LastMeasuredExtendedViewPort.Left);
		CornerstoneTest.AreEqual(650, target.LastMeasuredExtendedViewPort.Right);
	}

	[PresentationTestMethod]
	public void WhenVerticalCalculatesViewPortAtEndOfList()
	{
		// Arrange
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();
		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithHeightTemplate,
				bufferFactor: 0.5d);

		// Act
		scroll.Offset = new Vector(0, 910); // scroll to end
		Layout(target);

		// Assert
		CornerstoneTest.AreEqual(900, target.ViewPort.Top);
		CornerstoneTest.AreEqual(1000, target.ViewPort.Bottom);

		CornerstoneTest.AreEqual(800, target.LastMeasuredExtendedViewPort.Top);
		CornerstoneTest.AreEqual(1000, target.LastMeasuredExtendedViewPort.Bottom);
	}

	[PresentationTestMethod]
	public void WhenVerticalCalculatesViewPortAtStartOfList()
	{
		// Arrange
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();

		// Act
		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithHeightTemplate,
				bufferFactor: 0.5d);

		// Assert
		CornerstoneTest.AreEqual(0, target.ViewPort.Top);
		CornerstoneTest.AreEqual(100, target.ViewPort.Bottom);

		CornerstoneTest.AreEqual(0, target.LastMeasuredExtendedViewPort.Top);
		CornerstoneTest.AreEqual(200, target.LastMeasuredExtendedViewPort.Bottom);
	}

	[PresentationTestMethod]
	public void WhenVerticalCalculatesViewPortInMiddleOfList()
	{
		// Arrange
		using var app = App();

		var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();
		var (target, scroll, itemsControl) =
			CreateTarget<ItemsControl, VirtualizingStackPanelCountingMeasureArrange>(
				items,
				CanvasWithHeightTemplate,
				bufferFactor: 0.5d);

		// Act
		scroll.Offset = new Vector(0, 500); // scroll to end
		Layout(target);

		// Assert
		CornerstoneTest.AreEqual(500, target.ViewPort.Top);
		CornerstoneTest.AreEqual(600, target.ViewPort.Bottom);

		CornerstoneTest.AreEqual(450, target.LastMeasuredExtendedViewPort.Top);
		CornerstoneTest.AreEqual(650, target.LastMeasuredExtendedViewPort.Bottom);
	}

	private static IDisposable App()
	{
		return UnitTestApplication.Start(TestServices.RealFocus);
	}

	private static void AssertRealizedControlItems<TContainer>(
		VirtualizingStackPanel target,
		ItemsControl itemsControl,
		int firstIndex,
		int count)
	{
		CornerstoneTest.All(target.GetRealizedContainers()!, x => CornerstoneTest.IsType<TContainer>(x));
		CornerstoneTest.All(target.GetRealizedContainers()!, x => CornerstoneTest.Same(target, x.VisualParent));
		CornerstoneTest.All(target.GetRealizedContainers()!, x => CornerstoneTest.Same(itemsControl, x.Parent));

		var childIndexes = target.GetRealizedContainers()!
			.Select(x => itemsControl.IndexFromContainer(x))
			.Where(x => x >= 0)
			.OrderBy(x => x)
			.ToList();
		CornerstoneTest.AreEqual(Enumerable.Range(firstIndex, count), childIndexes);
	}

	private static void AssertRealizedItems(
		VirtualizingStackPanel target,
		ItemsControl itemsControl,
		int firstIndex,
		int count)
	{
		CornerstoneTest.All(target.GetRealizedContainers()!, x => CornerstoneTest.Same(target, x.VisualParent));
		CornerstoneTest.All(target.GetRealizedContainers()!, x => CornerstoneTest.Same(itemsControl, x.Parent));

		var childIndexes = target.GetRealizedContainers()!
			.Select(x => itemsControl.IndexFromContainer(x))
			.Where(x => x >= 0)
			.OrderBy(x => x)
			.ToList();
		CornerstoneTest.AreEqual(Enumerable.Range(firstIndex, count), childIndexes);

		var visibleChildren = target.Children
			.Where(x => x.IsVisible)
			.ToList();
		CornerstoneTest.AreEqual(count, visibleChildren.Count);
	}

	private static Style CreateIsVisibleBindingStyle()
	{
		return new Style(x => x.OfType<ContentPresenter>())
		{
			Setters =
			{
				new Setter(Visual.IsVisibleProperty, new Binding("IsVisible"))
			}
		};
	}

	private static TestRoot CreateRoot(
		Control child,
		Size? clientSize = null,
		IEnumerable<Style> styles = null)
	{
		var root = new TestRoot(true, child);
		root.ClientSize = clientSize ?? new(100, 100);

		if (styles is not null)
		{
			root.Styles.AddRange(styles);
		}

		return root;
	}

	private static (VirtualizingStackPanel, ScrollViewer, ItemsControl) CreateTarget(
		IEnumerable<object> items = null,
		Optional<IDataTemplate> itemTemplate = default,
		IEnumerable<Style> styles = null,
		Orientation orientation = Orientation.Vertical,
		double bufferFactor = 0.0d)
	{
		return CreateTarget<ItemsControl, VirtualizingStackPanel>(
			items,
			itemTemplate,
			styles,
			orientation,
			bufferFactor);
	}

	private static (TStackPanel, ScrollViewer, T) CreateTarget<T, TStackPanel>(
		IEnumerable<object> items = null,
		Optional<IDataTemplate> itemTemplate = default,
		IEnumerable<Style> styles = null,
		Orientation orientation = Orientation.Vertical,
		double bufferFactor = 0.0d)
		where T : ItemsControl, new()
		where TStackPanel : VirtualizingStackPanel, new()
	{
		var (target, scroll, itemsControl) = CreateUnrootedTarget<T, TStackPanel>(items, itemTemplate, orientation, bufferFactor);

		var root = CreateRoot(itemsControl, styles: styles);

		root.LayoutManager.ExecuteInitialLayoutPass();

		return (target, scroll, itemsControl);
	}

	private static (VirtualizingStackPanel, ScrollViewer, T) CreateUnrootedTarget<T>(
		IEnumerable<object> items = null,
		Optional<IDataTemplate> itemTemplate = default,
		Orientation orientation = Orientation.Vertical,
		double bufferFactor = 0.0d)
		where T : ItemsControl, new()
	{
		return CreateUnrootedTarget<T, VirtualizingStackPanel>(items, itemTemplate, orientation, bufferFactor);
	}

	private static (TStackPanel, ScrollViewer, T) CreateUnrootedTarget<T, TStackPanel>(
		IEnumerable<object> items = null,
		Optional<IDataTemplate> itemTemplate = default,
		Orientation orientation = Orientation.Vertical,
		double bufferFactor = 0.0d)
		where T : ItemsControl, new()
		where TStackPanel : VirtualizingStackPanel, new()
	{
		var target = new TStackPanel
		{
			Orientation = orientation,
			CacheLength = bufferFactor
		};

		items ??= new ObservableCollection<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));

		var presenter = new ItemsPresenter
		{
			[~ItemsPresenter.ItemsPanelProperty] = new TemplateBinding(ItemsPresenter.ItemsPanelProperty)
		};

		var scroll = new ScrollViewer
		{
			Name = "PART_ScrollViewer",
			Content = presenter
		};

		if (orientation == Orientation.Horizontal)
		{
			scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		}

		scroll.Template = ScrollViewerTemplate();

		var itemsControl = new T
		{
			ItemsSource = items,
			Template = new FuncControlTemplate<T>((_, ns) => scroll.RegisterInNameScope(ns)),
			ItemsPanel = new FuncTemplate<Panel>(() => target),
			ItemTemplate = itemTemplate.GetValueOrDefault(DefaultItemTemplate())
		};

		return (target, scroll, itemsControl);
	}

	private static IDataTemplate DefaultItemTemplate()
	{
		return new FuncDataTemplate<object>((x, _) => new Canvas { Width = 100, Height = 10 });
	}

	private static IReadOnlyList<int> GetRealizedIndexes(VirtualizingStackPanel target, ItemsControl itemsControl)
	{
		return target.GetRealizedElements()
			.Select(x => x is null ? -1 : itemsControl.IndexFromContainer(x))
			.ToList();
	}

	private static void Layout(Control target)
	{
		target.GetLayoutManager()?.ExecuteLayoutPass();
	}

	private static IControlTemplate ListBoxItemTemplate()
	{
		return new FuncControlTemplate<ListBoxItem>((x, ns) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				Width = 100,
				Height = 10
			}.RegisterInNameScope(ns));
	}

	private static IControlTemplate ScrollViewerTemplate()
	{
		return new FuncControlTemplate<ScrollViewer>((x, ns) =>
			new ScrollContentPresenter
			{
				Name = "PART_ScrollContentPresenter"
			}.RegisterInNameScope(ns));
	}

	private static IControlTemplate ScrollViewerTemplateWithScrollBars()
	{
		return new FuncControlTemplate<ScrollViewer>((_, ns) =>
		{
			var presenter = new ScrollContentPresenter
			{
				Name = "PART_ScrollContentPresenter"
			}.RegisterInNameScope(ns);

			var horizontalScrollBar = new ScrollBar
			{
				Name = "PART_HorizontalScrollBar",
				Orientation = Orientation.Horizontal,
				VerticalAlignment = VerticalAlignment.Bottom
			}.RegisterInNameScope(ns);

			var verticalScrollBar = new ScrollBar
			{
				Name = "PART_VerticalScrollBar",
				Orientation = Orientation.Vertical,
				HorizontalAlignment = HorizontalAlignment.Right
			}.RegisterInNameScope(ns);

			return new Panel { Children = { presenter, horizontalScrollBar, verticalScrollBar } };
		});
	}

	#endregion

	#region Interfaces

	private interface ICountMeasureArrangeCalls
	{
		#region Properties

		int Arranged { get; set; }
		int Measured { get; set; }

		#endregion
	}

	#endregion

	#region Classes

	private class CanvasCountingMeasureArrangeCalls : Canvas
	{
		#region Methods

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (DataContext is ICountMeasureArrangeCalls itm)
			{
				itm.Arranged++;
			}

			return base.ArrangeOverride(finalSize);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (DataContext is ICountMeasureArrangeCalls itm)
			{
				itm.Measured++;
			}

			return base.MeasureOverride(availableSize);
		}

		#endregion
	}

	private class ItemWithHeight : NotifyingBase
	{
		#region Fields

		private double _height;

		#endregion

		#region Constructors

		public ItemWithHeight(int index, double height = 10)
		{
			Caption = $"Item {index}";
			Height = height;
		}

		#endregion

		#region Properties

		public string Caption { get; }

		public double Height
		{
			get => _height;
			set => SetField(ref _height, value);
		}

		#endregion
	}

	[DebuggerDisplay("{DebuggerDisplay}")]
	private class ItemWithHeightAndMeasureArrangeCount : ItemWithHeight, ICountMeasureArrangeCalls
	{
		#region Constructors

		public ItemWithHeightAndMeasureArrangeCount(int index, double height = 10) : base(index, height)
		{
		}

		#endregion

		#region Properties

		public int Arranged { get; set; }

		public int Measured { get; set; }

		private string DebuggerDisplay => $"{Caption} (height: {Height} m:{Measured} a: {Arranged})";

		#endregion
	}

	private class ItemWithIsVisible : NotifyingBase
	{
		#region Fields

		private bool _isVisible = true;

		#endregion

		#region Constructors

		public ItemWithIsVisible(int index)
		{
			Caption = $"Item {index}";
		}

		#endregion

		#region Properties

		public string Caption { get; set; }

		public bool IsVisible
		{
			get => _isVisible;
			set => SetField(ref _isVisible, value);
		}

		#endregion
	}

	private class ItemWithWidth : NotifyingBase
	{
		#region Fields

		private double _width;

		#endregion

		#region Constructors

		public ItemWithWidth(int index, double width = 10)
		{
			Caption = $"Item {index}";
			Width = width;
		}

		#endregion

		#region Properties

		public string Caption { get; }

		public double Width
		{
			get => _width;
			set => SetField(ref _width, value);
		}

		#endregion
	}

	[DebuggerDisplay("{DebuggerDisplay}")]
	private class ItemWithWidthAndMeasureArrangeCount : ItemWithWidth, ICountMeasureArrangeCalls
	{
		#region Constructors

		public ItemWithWidthAndMeasureArrangeCount(int index, double width = 10) : base(index, width)
		{
		}

		#endregion

		#region Properties

		public int Arranged { get; set; }

		public int Measured { get; set; }

		private string DebuggerDisplay => $"{Caption} (width: {Width} m:{Measured} a: {Arranged})";

		#endregion
	}

	private class NonRecyclingItemsControl : ItemsControl
	{
		#region Properties

		protected override Type StyleKeyOverride => typeof(ItemsControl);

		#endregion

		#region Methods

		protected internal override bool NeedsContainerOverride(object item, int index, out object recycleKey)
		{
			recycleKey = null;
			return true;
		}

		#endregion
	}

	private class ResettingCollection : List<string>, INotifyCollectionChanged
	{
		#region Constructors

		public ResettingCollection(IEnumerable<string> items)
		{
			AddRange(items);
		}

		#endregion

		#region Methods

		public void Reset(IEnumerable<string> items)
		{
			Clear();
			AddRange(items);
			CollectionChanged?.Invoke(
				this,
				new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		#endregion

		#region Events

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion
	}

	private class VirtualizingStackPanelCountingMeasureArrange : VirtualizingStackPanel
	{
		#region Properties

		public int Arranged { get; set; }
		public int Measured { get; set; }

		#endregion

		#region Methods

		public void ResetMeasureArrangeCounters()
		{
			// reset counters
			Measured = 0;
			Arranged = 0;
			foreach (var itm in Items.OfType<ICountMeasureArrangeCalls>())
			{
				itm.Measured = 0;
				itm.Arranged = 0;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Arranged++;
			return base.ArrangeOverride(finalSize);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			Measured++;
			return base.MeasureOverride(availableSize);
		}

		#endregion
	}

	#endregion
}