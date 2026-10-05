#region References

using System.Linq;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ToggleSwitchTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ContentControlsAreLogicalChildren()
	{
		var target = new ToggleSwitch();
		var on = new Border();
		var off = new Border();

		target.OnContent = on;
		target.OffContent = off;

		CornerstoneTest.IsTrue(target.GetLogicalChildren().Contains(on));
		CornerstoneTest.IsTrue(target.GetLogicalChildren().Contains(off));

		target.OnContent = "On";
		target.OffContent = null;

		CornerstoneTest.IsFalse(target.GetLogicalChildren().Contains(on));
		CornerstoneTest.IsFalse(target.GetLogicalChildren().Contains(off));
	}

	[PresentationTestMethod]
	public void DeclaredPropertiesRoundTrip()
	{
		var target = new ToggleSwitch();
		var transitions = new Transitions();
		var onTemplate = new TextTemplate();
		var offTemplate = new TextTemplate();

		CornerstoneTest.AreEqual("On", target.OnContent);
		CornerstoneTest.AreEqual("Off", target.OffContent);
		CornerstoneTest.IsNull(target.OnContentTemplate);
		CornerstoneTest.IsNull(target.OffContentTemplate);
		CornerstoneTest.IsNull(target.KnobTransitions);
		CornerstoneTest.IsNull(target.OnContentPresenter);
		CornerstoneTest.IsNull(target.OffContentPresenter);
		CornerstoneTest.IsFalse(target.IsChecked);

		target.OnContent = "Yes";
		target.OffContent = "No";
		target.OnContentTemplate = onTemplate;
		target.OffContentTemplate = offTemplate;
		target.KnobTransitions = transitions;

		CornerstoneTest.AreEqual("Yes", target.OnContent);
		CornerstoneTest.AreEqual("No", target.OffContent);
		CornerstoneTest.IsTrue(ReferenceEquals(onTemplate, target.OnContentTemplate));
		CornerstoneTest.IsTrue(ReferenceEquals(offTemplate, target.OffContentTemplate));
		CornerstoneTest.IsTrue(ReferenceEquals(transitions, target.KnobTransitions));
	}

	[PresentationTestMethod]
	public void DragPastHalfChecksTheSwitch()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var parts = Show(new ToggleSwitch());

		Press(parts, new Point(0, 0));
		Move(parts, new Point(30, 0));

		CornerstoneTest.IsTrue(parts.Target.Classes.Contains(":dragging"));
		CornerstoneTest.AreEqual(30, Canvas.GetLeft(parts.Knobs));

		Release(parts, new Point(30, 0));

		CornerstoneTest.IsTrue(parts.Target.IsChecked);
		CornerstoneTest.IsFalse(parts.Target.Classes.Contains(":dragging"));
		CornerstoneTest.AreEqual(40, Canvas.GetLeft(parts.Knobs));
	}

	[PresentationTestMethod]
	public void DragBelowHalfLeavesTheSwitchUnchecked()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var parts = Show(new ToggleSwitch());

		Press(parts, new Point(0, 0));
		Move(parts, new Point(10, 0));
		Release(parts, new Point(10, 0));

		CornerstoneTest.IsFalse(parts.Target.IsChecked);
		CornerstoneTest.IsFalse(parts.Target.Classes.Contains(":dragging"));
		CornerstoneTest.AreEqual(0, Canvas.GetLeft(parts.Knobs));
	}

	[PresentationTestMethod]
	public void KnobFollowsIsChecked()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var parts = Show(new ToggleSwitch());

		CornerstoneTest.AreEqual(0, Canvas.GetLeft(parts.Knobs));
		CornerstoneTest.IsTrue(ReferenceEquals(parts.On, parts.Target.OnContentPresenter));
		CornerstoneTest.IsTrue(ReferenceEquals(parts.Off, parts.Target.OffContentPresenter));

		parts.Target.IsChecked = true;

		CornerstoneTest.AreEqual(40, Canvas.GetLeft(parts.Knobs));

		var transitions = new Transitions();
		parts.Target.KnobTransitions = transitions;

		CornerstoneTest.IsTrue(ReferenceEquals(transitions, parts.Knobs.Transitions));
	}

	[PresentationTestMethod]
	public void SmallMoveDoesNotDrag()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var parts = Show(new ToggleSwitch());

		Press(parts, new Point(0, 0));
		Move(parts, new Point(2, 0));
		Release(parts, new Point(2, 0));

		CornerstoneTest.IsFalse(parts.Target.IsChecked);
		CornerstoneTest.IsFalse(parts.Target.Classes.Contains(":dragging"));
	}

	private static void Move(SwitchParts parts, Point position)
	{
		parts.Knobs.RaiseEvent(new PointerEventArgs(
			InputElement.PointerMovedEvent,
			parts.Knobs,
			parts.Pointer,
			parts.Window,
			position,
			1,
			new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
			KeyModifiers.None));
	}

	private static void Press(SwitchParts parts, Point position)
	{
		parts.Knobs.RaiseEvent(new PointerPressedEventArgs(
			parts.Knobs,
			parts.Pointer,
			parts.Window,
			position,
			1,
			new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
			KeyModifiers.None));
	}

	private static void Release(SwitchParts parts, Point position)
	{
		parts.Knobs.RaiseEvent(new PointerReleasedEventArgs(
			parts.Knobs,
			parts.Pointer,
			parts.Window,
			position,
			1,
			new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
			KeyModifiers.None,
			MouseButton.Left));
	}

	private static SwitchParts Show(ToggleSwitch target)
	{
		var knobs = new Panel { Name = "PART_MovingKnobs", Width = 80, Height = 20 };
		var knob = new Panel { Name = "PART_SwitchKnob", Width = 40, Height = 20 };
		var on = new ContentPresenter { Name = "PART_OnContentPresenter" };
		var off = new ContentPresenter { Name = "PART_OffContentPresenter" };
		knob.Arrange(new Rect(0, 0, 40, 20));
		target.Template = new FuncControlTemplate<ToggleSwitch>((_, scope) =>
		{
			var root = new Panel();
			root.Children.Add(knobs.RegisterInNameScope(scope));
			root.Children.Add(knob.RegisterInNameScope(scope));
			root.Children.Add(on.RegisterInNameScope(scope));
			root.Children.Add(off.RegisterInNameScope(scope));
			return root;
		});

		var window = new Window
		{
			Width = 120,
			Height = 40,
			Content = target
		};
		window.Show();
		target.ApplyTemplate();

		return new SwitchParts(target, window, knobs, on, off);
	}

	#endregion

	#region Nested Types

	private sealed class TextTemplate : IDataTemplate
	{
		#region Methods

		public Control Build(object param)
		{
			return new TextBlock { Text = param as string };
		}

		public bool Match(object data)
		{
			return true;
		}

		#endregion
	}

	private sealed class SwitchParts
	{
		#region Constructors

		public SwitchParts(ToggleSwitch target, Window window, Panel knobs, ContentPresenter on, ContentPresenter off)
		{
			Target = target;
			Window = window;
			Knobs = knobs;
			On = on;
			Off = off;
			Pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
		}

		#endregion

		#region Properties

		public Panel Knobs { get; }

		public ContentPresenter Off { get; }

		public ContentPresenter On { get; }

		public Pointer Pointer { get; }

		public ToggleSwitch Target { get; }

		public Window Window { get; }

		#endregion
	}

	#endregion
}
