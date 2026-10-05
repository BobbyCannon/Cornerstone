#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class ScrollBarTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(PointerType.Touch)]
	[DataRow(PointerType.Pen)]
	public void ContextRequestedShouldBeHandledForTouchOrPenInput(PointerType pointerType)
	{
		var target = CreateScrollBar();

		var args = CreateContextRequested(target, pointerType);
		target.RaiseEvent(args);

		CornerstoneTest.IsTrue(args.Handled);
	}

	[PresentationTestMethod]
	public void ContextRequestedShouldNotBeHandledForMouseInput()
	{
		var target = CreateScrollBar();

		var args = CreateContextRequested(target, PointerType.Mouse);
		target.RaiseEvent(args);

		CornerstoneTest.IsFalse(args.Handled);
	}

	[PresentationTestMethod]
	public void FocusKeyInputShouldScroll()
	{
		// Vertical scrollbar
		var target = CreateScrollBar(Orientation.Vertical);

		// Page down and page up
		KeyDown(target, Key.PageDown);
		CornerstoneTest.AreEqual(target.LargeChange, target.Value);
		KeyDown(target, Key.PageUp);
		CornerstoneTest.AreEqual(0, target.Value);

		// Small scrolling with arrow keys
		KeyDown(target, Key.Down);
		CornerstoneTest.AreEqual(target.SmallChange, target.Value);
		KeyDown(target, Key.Up);
		CornerstoneTest.AreEqual(0, target.Value);

		// Vertical scrollbars shouldn't scroll for left and right keys
		KeyDown(target, Key.Right);
		CornerstoneTest.AreEqual(0, target.Value);
		KeyDown(target, Key.Left);
		CornerstoneTest.AreEqual(0, target.Value);

		// Horizontal scrollbar
		target = CreateScrollBar(Orientation.Horizontal);

		// Small scrolling with arrow keys
		KeyDown(target, Key.Right);
		CornerstoneTest.AreEqual(target.SmallChange, target.Value);
		KeyDown(target, Key.Left);
		CornerstoneTest.AreEqual(0, target.Value);

		// Horizontal scrollbars shouldn't scroll for up and down keys
		KeyDown(target, Key.Down);
		CornerstoneTest.AreEqual(0, target.Value);
		KeyDown(target, Key.Up);
		CornerstoneTest.AreEqual(0, target.Value);
	}

	[PresentationTestMethod]
	public void LineDownShouldIncreaseValueBySmallChange()
	{
		var target = CreateScrollBar(value: 50, smallChange: 5);

		target.LineDown();

		CornerstoneTest.AreEqual(55, target.Value);
	}

	[PresentationTestMethod]
	public void LineLeftShouldDecreaseValueBySmallChange()
	{
		var target = CreateScrollBar(Orientation.Horizontal, value: 50, smallChange: 5);

		target.LineLeft();

		CornerstoneTest.AreEqual(45, target.Value);
	}

	[PresentationTestMethod]
	public void LineRightShouldIncreaseValueBySmallChange()
	{
		var target = CreateScrollBar(Orientation.Horizontal, value: 50, smallChange: 5);

		target.LineRight();

		CornerstoneTest.AreEqual(55, target.Value);
	}

	[PresentationTestMethod]
	public void LineUpShouldDecreaseValueBySmallChange()
	{
		var target = CreateScrollBar(value: 50, smallChange: 5);

		target.LineUp();

		CornerstoneTest.AreEqual(45, target.Value);
	}

	[PresentationTestMethod]
	[DataRow(Orientation.Vertical)]
	[DataRow(Orientation.Horizontal)]
	public void OrientationShouldSetMatchingPseudoClass(Orientation orientation)
	{
		var target = new ScrollBar { Orientation = orientation };

		CornerstoneTest.AreEqual(orientation == Orientation.Vertical, target.Classes.Contains(":vertical"));
		CornerstoneTest.AreEqual(orientation == Orientation.Horizontal, target.Classes.Contains(":horizontal"));
	}

	[PresentationTestMethod]
	public void PageDownShouldIncreaseValueByLargeChange()
	{
		var target = CreateScrollBar(value: 50, largeChange: 10);

		target.PageDown();

		CornerstoneTest.AreEqual(60, target.Value);
	}

	[PresentationTestMethod]
	public void PageLeftShouldDecreaseValueByLargeChange()
	{
		var target = CreateScrollBar(Orientation.Horizontal, value: 50, largeChange: 10);

		target.PageLeft();

		CornerstoneTest.AreEqual(40, target.Value);
	}

	[PresentationTestMethod]
	public void PageRightShouldIncreaseValueByLargeChange()
	{
		var target = CreateScrollBar(Orientation.Horizontal, value: 50, largeChange: 10);

		target.PageRight();

		CornerstoneTest.AreEqual(60, target.Value);
	}

	[PresentationTestMethod]
	public void PageUpShouldDecreaseValueByLargeChange()
	{
		var target = CreateScrollBar(value: 50, largeChange: 10);

		target.PageUp();

		CornerstoneTest.AreEqual(40, target.Value);
	}

	[PresentationTestMethod]
	public void ScrollBarCanAutoHide()
	{
		var target = new ScrollBar();

		target.Visibility = ScrollBarVisibility.Auto;
		target.ViewportSize = 1;
		target.Maximum = 0;

		CornerstoneTest.IsFalse(target.IsVisible);
	}

	[PresentationTestMethod]
	public void ScrollBarShouldHideWhenVisibilitySetToHidden()
	{
		var target = new ScrollBar();

		target.Visibility = ScrollBarVisibility.Hidden;
		target.Minimum = 0;
		target.Maximum = 100;
		target.ViewportSize = 10;

		CornerstoneTest.IsFalse(target.IsVisible);
	}

	[PresentationTestMethod]
	public void ScrollBarShouldNotAutoHideWhenViewportSizeIsNaN()
	{
		var target = new ScrollBar();

		target.Visibility = ScrollBarVisibility.Auto;
		target.Minimum = 0;
		target.Maximum = 100;
		target.ViewportSize = double.NaN;

		CornerstoneTest.IsTrue(target.IsVisible);
	}

	[PresentationTestMethod]
	public void ScrollBarShouldNotAutoHideWhenVisibilitySetToVisible()
	{
		var target = new ScrollBar();

		target.Visibility = ScrollBarVisibility.Visible;
		target.Minimum = 0;
		target.Maximum = 100;
		target.ViewportSize = 100;

		CornerstoneTest.IsTrue(target.IsVisible);
	}

	[PresentationTestMethod]
	public void ScrollHereShouldSetValueWithinBounds()
	{
		var target = CreateScrollBar(minimum: 0, maximum: 100, value: 0);

		// No exception even though no pointer position was recorded yet.
		target.ScrollHere();

		CornerstoneTest.InRange(target.Value, target.Minimum, target.Maximum);
	}

	[PresentationTestMethod]
	public void ScrollMethodsShouldRaiseScrollEvent()
	{
		var target = CreateScrollBar(value: 50, smallChange: 5, largeChange: 10);
		var events = new List<ScrollEventType>();
		target.Scroll += (_, e) => events.Add(e.ScrollEventType);

		target.LineDown();
		target.PageDown();
		target.ScrollToHome();
		target.ScrollToEnd();

		CornerstoneTest.AreEqual(new[]
		{
			ScrollEventType.SmallIncrement,
			ScrollEventType.LargeIncrement,
			ScrollEventType.LargeDecrement,
			ScrollEventType.LargeIncrement
		}, events);
	}

	[PresentationTestMethod]
	public void ScrollMethodsShouldRespectValueBounds()
	{
		var target = CreateScrollBar(minimum: 20, maximum: 80, value: 25, smallChange: 10);

		target.LineUp();
		CornerstoneTest.AreEqual(20, target.Value); // Should not go below Minimum

		target.Value = 75;
		target.LineDown();
		CornerstoneTest.AreEqual(80, target.Value); // Should not go above Maximum
	}

	[PresentationTestMethod]
	public void ScrollToEndShouldSetValueToMaximum()
	{
		var target = CreateScrollBar(minimum: 0, maximum: 90, value: 50);

		target.ScrollToEnd();

		CornerstoneTest.AreEqual(90, target.Value);
	}

	[PresentationTestMethod]
	public void ScrollToHomeShouldSetValueToMinimum()
	{
		var target = CreateScrollBar(minimum: 10, maximum: 100, value: 50);

		target.ScrollToHome();

		CornerstoneTest.AreEqual(10, target.Value);
	}

	[PresentationTestMethod]
	public void SettingTrackValueAfterSettingValueShouldUpdateValue()
	{
		var target = new ScrollBar
		{
			Template = new FuncControlTemplate<ScrollBar>(Template)
		};

		target.ApplyTemplate();

		var track = (Track) target.GetTemplateDescendants().First(x => x.Name == "track");
		target.Value = 25;
		track.Value = 50;

		CornerstoneTest.AreEqual(50, target.Value);
	}

	[PresentationTestMethod]
	public void SettingTrackValueShouldUpdateValue()
	{
		var target = new ScrollBar
		{
			Template = new FuncControlTemplate<ScrollBar>(Template)
		};

		target.ApplyTemplate();
		var track = (Track) target.GetTemplateDescendants().First(x => x.Name == "track");
		track.Value = 50;

		CornerstoneTest.AreEqual(50, target.Value);
	}

	[PresentationTestMethod]
	public void SettingValueShouldUpdateTrackValue()
	{
		var target = new ScrollBar
		{
			Template = new FuncControlTemplate<ScrollBar>(Template)
		};

		target.ApplyTemplate();
		var track = (Track) target.GetTemplateDescendants().First(x => x.Name == "track");
		target.Value = 50;

		CornerstoneTest.AreEqual(50, track.Value);
	}

	[PresentationTestMethod]
	public void ThumbDragCompleteEventShouldRaiseScrollEvent()
	{
		var target = new ScrollBar
		{
			Template = new FuncControlTemplate<ScrollBar>(Template)
		};

		target.ApplyTemplate();

		var track = (Track) target.GetTemplateDescendants().First(x => x.Name == "track");

		var raisedEvent = CornerstoneTest.Raises<ScrollEventArgs>(handler => target.Scroll += handler, handler => target.Scroll -= handler, () =>
		{
			var ev = new VectorEventArgs
			{
				RoutedEvent = Thumb.DragCompletedEvent,
				Vector = new Vector(0, 0)
			};

			track.Thumb!.RaiseEvent(ev);
		});

		CornerstoneTest.AreEqual(ScrollEventType.EndScroll, raisedEvent.Arguments.ScrollEventType);
	}

	[PresentationTestMethod]
	public void ThumbDragDeltaEventShouldRaiseScrollEvent()
	{
		var target = new ScrollBar
		{
			Template = new FuncControlTemplate<ScrollBar>(Template)
		};

		target.ApplyTemplate();

		var track = (Track) target.GetTemplateDescendants().First(x => x.Name == "track");

		var raisedEvent = CornerstoneTest.Raises<ScrollEventArgs>(handler => target.Scroll += handler, handler => target.Scroll -= handler, () =>
		{
			var ev = new VectorEventArgs
			{
				RoutedEvent = Thumb.DragDeltaEvent,
				Vector = new Vector(0, 0)
			};

			track.Thumb!.RaiseEvent(ev);
		});

		CornerstoneTest.AreEqual(ScrollEventType.ThumbTrack, raisedEvent.Arguments.ScrollEventType);
	}

	private static ContextRequestedEventArgs CreateContextRequested(ScrollBar target, PointerType pointerType)
	{
		var pointer = new Pointer(Pointer.GetNextFreeId(), pointerType, true);
		var pointerArgs = new PointerPressedEventArgs(
			target,
			pointer,
			target,
			default,
			1,
			new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
			KeyModifiers.None);

		return new ContextRequestedEventArgs(pointerArgs);
	}

	private static ScrollBar CreateScrollBar(
		Orientation orientation = Orientation.Vertical,
		double minimum = 0,
		double maximum = 100,
		double value = 0,
		double smallChange = 1,
		double largeChange = 10)
	{
		var target = new ScrollBar
		{
			Orientation = orientation,
			Template = new FuncControlTemplate<ScrollBar>(Template),
			Minimum = minimum,
			Maximum = maximum,
			Value = value,
			SmallChange = smallChange,
			LargeChange = largeChange
		};

		target.ApplyTemplate();
		return target;
	}

	private static void KeyDown(IInputElement target, Key key)
	{
		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key
		});
	}

	private static Control Template(ScrollBar control, INameScope scope)
	{
		return new Border
		{
			Child = new Track
			{
				Name = "track",
				[!Track.MinimumProperty] = control[!RangeBase.MinimumProperty],
				[!Track.MaximumProperty] = control[!RangeBase.MaximumProperty],
				[!!Track.ValueProperty] = control[!!RangeBase.ValueProperty],
				[!Track.ViewportSizeProperty] = control[!ScrollBar.ViewportSizeProperty],
				[!Track.OrientationProperty] = control[!ScrollBar.OrientationProperty],
				Thumb = new Thumb
				{
					Template = new FuncControlTemplate<Thumb>(ThumbTemplate)
				}
			}.RegisterInNameScope(scope)
		};
	}

	private static Control ThumbTemplate(Thumb control, INameScope scope)
	{
		return new Border
		{
			Background = Brushes.Gray
		};
	}

	#endregion
}