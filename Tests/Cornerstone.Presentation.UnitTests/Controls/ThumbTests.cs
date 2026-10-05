#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ThumbTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AdjustDragShiftsTheNextDelta()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = Show();
		Vector delta = default;
		target.DragDelta += (_, e) => delta = e.Vector;

		Press(target, new Point(10, 10));
		target.AdjustDrag(new Vector(5, 0));
		Move(target, new Point(20, 10));

		CornerstoneTest.AreEqual(new Vector(5, 0), delta);
	}

	[PresentationTestMethod]
	public void CaptureLostCompletesTheDrag()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = Show();
		var completed = 0;
		Vector vector = default;
		target.DragCompleted += (_, e) =>
		{
			completed++;
			vector = e.Vector;
		};

		Press(target, new Point(8, 4));
		target.RaiseEvent(new PointerCaptureLostEventArgs(target, new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true)));

		CornerstoneTest.AreEqual(1, completed);
		CornerstoneTest.AreEqual(new Vector(8, 4), vector);
		CornerstoneTest.IsFalse(target.Classes.Contains(":pressed"));
	}

	[PresentationTestMethod]
	public void MoveWithoutAPressDoesNotDrag()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = Show();
		var deltas = 0;
		target.DragDelta += (_, _) => deltas++;

		Move(target, new Point(12, 4));

		CornerstoneTest.AreEqual(0, deltas);
		CornerstoneTest.IsFalse(target.Classes.Contains(":pressed"));
	}

	[PresentationTestMethod]
	public void PressMoveAndReleaseReportTheDrag()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = Show();
		Vector started = default;
		Vector delta = default;
		Vector completed = default;
		var startedCount = 0;
		var deltaCount = 0;
		var completedCount = 0;
		target.DragStarted += (_, e) =>
		{
			startedCount++;
			started = e.Vector;
		};
		target.DragDelta += (_, e) =>
		{
			deltaCount++;
			delta = e.Vector;
		};
		target.DragCompleted += (_, e) =>
		{
			completedCount++;
			completed = e.Vector;
		};

		Press(target, new Point(10, 6));

		CornerstoneTest.AreEqual(1, startedCount);
		CornerstoneTest.AreEqual(new Vector(10, 6), started);
		CornerstoneTest.IsTrue(target.Classes.Contains(":pressed"));

		Move(target, new Point(18, 9));

		CornerstoneTest.AreEqual(1, deltaCount);
		CornerstoneTest.AreEqual(new Vector(8, 3), delta);

		Release(target, new Point(18, 9));

		CornerstoneTest.AreEqual(1, completedCount);
		CornerstoneTest.AreEqual(new Vector(18, 9), completed);
		CornerstoneTest.IsFalse(target.Classes.Contains(":pressed"));
	}

	[PresentationTestMethod]
	public void ReleaseWithoutAPressDoesNotComplete()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = Show();
		var completed = 0;
		target.DragCompleted += (_, _) => completed++;

		Release(target, new Point(4, 4));

		CornerstoneTest.AreEqual(0, completed);
	}

	private static Point InRoot(Thumb target, Point position)
	{
		var origin = target.TranslatePoint(new Point(0, 0), (Visual)target.VisualRoot).Value;
		return new Point(origin.X + position.X, origin.Y + position.Y);
	}

	private static void Move(Thumb target, Point position)
	{
		target.RaiseEvent(new PointerEventArgs(
			InputElement.PointerMovedEvent,
			target,
			new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
			(Visual)target.VisualRoot,
			InRoot(target, position),
			1,
			new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
			KeyModifiers.None));
	}

	private static void Press(Thumb target, Point position)
	{
		target.RaiseEvent(new PointerPressedEventArgs(
			target,
			new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
			(Visual)target.VisualRoot,
			InRoot(target, position),
			1,
			new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
			KeyModifiers.None));
	}

	private static void Release(Thumb target, Point position)
	{
		target.RaiseEvent(new PointerReleasedEventArgs(
			target,
			new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
			(Visual)target.VisualRoot,
			InRoot(target, position),
			1,
			new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
			KeyModifiers.None,
			MouseButton.Left));
	}

	private static Thumb Show()
	{
		var target = new Thumb
		{
			Width = 40,
			Height = 20
		};
		var window = new Window
		{
			Width = 120,
			Height = 80,
			Content = target
		};
		window.Show();
		return target;
	}

	#endregion
}
