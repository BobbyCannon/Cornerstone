#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class KeyboardNavigationTestsXY : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(Key.Left, NavigationDirection.Left)]
	[DataRow(Key.Right, NavigationDirection.Right)]
	[DataRow(Key.Up, NavigationDirection.Up)]
	[DataRow(Key.Down, NavigationDirection.Down)]
	public void ArrowKeyShouldFocusElement(Key key, NavigationDirection direction)
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var candidate = new Button();
		var current = new Button();
		current[direction switch
		{
			NavigationDirection.Left => XYFocus.LeftProperty,
			NavigationDirection.Right => XYFocus.RightProperty,
			NavigationDirection.Up => XYFocus.UpProperty,
			NavigationDirection.Down => XYFocus.DownProperty,
			_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
		}] = candidate;
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = new Canvas
			{
				Children = { current, candidate }
			}
		};
		window.Show();
		CornerstoneTest.IsTrue(current.Focus());

		var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = current };
		window.RaiseEvent(args);

		CornerstoneTest.AreEqual(candidate, FocusManager.GetFocusManager(current)!.GetFocusedElement());
		CornerstoneTest.IsTrue(args.Handled);
	}

	[PresentationTestMethod]
	[DataRow(Key.Left)]
	[DataRow(Key.Right)]
	[DataRow(Key.Up)]
	[DataRow(Key.Down)]
	public void ArrowKeyShouldNotBeHandledIfNoFocus(Key key)
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var current = new Button();
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = new Canvas
			{
				Children = { current }
			}
		};
		window.Show();
		CornerstoneTest.IsTrue(current.Focus());

		var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = current };
		window.RaiseEvent(args);

		CornerstoneTest.AreEqual(current, FocusManager.GetFocusManager(current)!.GetFocusedElement());
		CornerstoneTest.IsFalse(args.Handled);
	}

	[PresentationTestMethod]
	public void CanFocusAnyElementIfNothingWasFocused()
	{
		// In the future we might auto-focus any element, but for now XY algorithm should be aware of Cornerstone specifics.
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var candidate = new Button();
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = new Canvas
			{
				Children = { candidate }
			}
		};
		window.Show();

		CornerstoneTest.IsNull(FocusManager.GetFocusManager(window)!.GetFocusedElement());

		var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down, Source = window };
		window.RaiseEvent(args);

		CornerstoneTest.AreEqual(candidate, FocusManager.GetFocusManager(window)!.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void CanFocusChildOfCurrentFocused()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var candidate = new Button { Height = 20, Width = 20 };
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = candidate,
			Height = 30
		};
		window.Show();

		CornerstoneTest.IsNull(KeyboardNavigationHandler.GetNext(window, NavigationDirection.Down));
	}

	[PresentationTestMethod]
	public void CannotFocusAcrossXYFocusBoundaries()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var current = new Button { Height = 20 };
		var candidate = new Button { Height = 20 };
		var currentParent = new StackPanel
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Orientation = Orientation.Vertical,
			Spacing = 20,
			Children = { current }
		};
		var candidateParent = new StackPanel
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Orientation = Orientation.Vertical,
			Spacing = 20,
			Children = { candidate }
		};

		var grandparent = new StackPanel
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Disabled,
			Orientation = Orientation.Vertical,
			Spacing = 20,
			Children = { currentParent, candidateParent }
		};

		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = grandparent,
			Height = 300
		};
		window.Show();

		CornerstoneTest.IsNull(KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down));
	}

	[PresentationTestMethod]
	public void ClippedElementShouldNotBeFocused()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var current = new Button { Height = 20 };
		var candidate = new Button { Height = 20 };
		var parent = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 20,
			Children = { current, candidate }
		};
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = parent,
			Height = 30
		};
		window.Show();

		CornerstoneTest.IsNull(KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down));
	}

	[PresentationTestMethod]
	public void ClippedElementShouldNotFocusedIfInsideOfScrollViewer()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var current = new Button { Height = 20 };
		var candidate = new Button { Height = 20 };
		var parent = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 20,
			Children = { current, candidate }
		};
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = new ScrollViewer
			{
				Content = parent
			},
			Height = 30
		};
		window.Show();

		CornerstoneTest.AreEqual(candidate, KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down));
	}

	[PresentationTestMethod]
	[DataRow(1, NavigationDirection.Down, 2)]
	[DataRow(1, NavigationDirection.Up, -1)]
	[DataRow(1, NavigationDirection.Left, 3)]
	[DataRow(1, NavigationDirection.Right, 2)]
	[DataRow(2, NavigationDirection.Down, 3)]
	[DataRow(2, NavigationDirection.Up, 1)]
	[DataRow(2, NavigationDirection.Left, 1)]
	[DataRow(2, NavigationDirection.Right, -1)]
	[DataRow(3, NavigationDirection.Down, 4)]
	[DataRow(3, NavigationDirection.Up, 2)]
	[DataRow(3, NavigationDirection.Left, -1)]
	[DataRow(3, NavigationDirection.Right, 1)]
	[DataRow(4, NavigationDirection.Down, -1)]
	[DataRow(4, NavigationDirection.Up, 3)]
	[DataRow(4, NavigationDirection.Left, 3)]
	[DataRow(4, NavigationDirection.Right, 2)]
	public void NavigationDirectionDistanceFocusDependingOnDirection(int from, NavigationDirection direction, int to)
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var (canvas, buttons) = CreateXYTestLayout();
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = canvas
		};
		window.Show();

		var fromButton = buttons[from - 1];
		fromButton.SetValue(XYFocus.UpNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
		fromButton.SetValue(XYFocus.LeftNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
		fromButton.SetValue(XYFocus.RightNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);
		fromButton.SetValue(XYFocus.DownNavigationStrategyProperty, XYFocusNavigationStrategy.NavigationDirectionDistance);

		var result = KeyboardNavigationHandler.GetNext(fromButton, direction) as Button;

		CornerstoneTest.AreEqual(to, result == null ? -1 : Array.IndexOf(buttons, result) + 1);
	}

	[PresentationTestMethod]
	public void ParentCanOverrideNavigationWhenDirectionalIsSet()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		// With double stack panel layout we have something like this:
		// [ [ EXPECTED, CURRENT ] CANDIDATE ]
		// Where normally from Current focus would go to the Candidate.
		// But since we set `XYFocus.Right` on nested StackPanel, it should be used instead.
		// But ONLY if Candidate isn't part of that nested StackPanel (it isn't). 

		var current = new Button();
		var candidate = new Button();
		var expectedOverride = new Button();
		var parent = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children = { expectedOverride, current },
			[XYFocus.RightProperty] = expectedOverride,

			// Property value to simplify test.
			[XYFocus.RightNavigationStrategyProperty] = XYFocusNavigationStrategy.RectilinearDistance
		};
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Children = { parent, candidate }
			}
		};
		window.Show();

		CornerstoneTest.AreEqual(expectedOverride, KeyboardNavigationHandler.GetNext(current, NavigationDirection.Right));
	}

	[PresentationTestMethod]
	[DataRow(1, NavigationDirection.Down, 4)]
	[DataRow(1, NavigationDirection.Up, -1)]
	[DataRow(1, NavigationDirection.Left, -1)]
	[DataRow(1, NavigationDirection.Right, 2)]

	// TODO: [DataRow(2, NavigationDirection.Down, 4)] Actual: 3
	// TODO: [DataRow(2, NavigationDirection.Up, -1)] Actual 1
	[DataRow(2, NavigationDirection.Left, 1)]
	[DataRow(2, NavigationDirection.Right, -1)]
	[DataRow(3, NavigationDirection.Down, 4)]

	// TODO: [DataRow(3, NavigationDirection.Up, 1)] Actual: 2
	[DataRow(3, NavigationDirection.Left, -1)]

	// TODO: [DataRow(3, NavigationDirection.Right, 4)] Actual: 1
	[DataRow(4, NavigationDirection.Down, -1)]
	[DataRow(4, NavigationDirection.Up, 1)]
	[DataRow(4, NavigationDirection.Left, 3)]
	[DataRow(4, NavigationDirection.Right, 2)]
	public void ProjectionFocusDependingOnDirection(int from, NavigationDirection direction, int to)
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var (canvas, buttons) = CreateXYTestLayout();
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = canvas
		};
		window.Show();

		var fromButton = buttons[from - 1];
		fromButton.SetValue(XYFocus.UpNavigationStrategyProperty, XYFocusNavigationStrategy.Projection);
		fromButton.SetValue(XYFocus.LeftNavigationStrategyProperty, XYFocusNavigationStrategy.Projection);
		fromButton.SetValue(XYFocus.RightNavigationStrategyProperty, XYFocusNavigationStrategy.Projection);
		fromButton.SetValue(XYFocus.DownNavigationStrategyProperty, XYFocusNavigationStrategy.Projection);

		var result = KeyboardNavigationHandler.GetNext(fromButton, direction) as Button;

		CornerstoneTest.AreEqual(to, result == null ? -1 : Array.IndexOf(buttons, result) + 1);
	}

	[PresentationTestMethod]
	[DataRow(1, NavigationDirection.Down, 3)]
	[DataRow(1, NavigationDirection.Up, -1)]
	[DataRow(1, NavigationDirection.Left, 3)]
	[DataRow(1, NavigationDirection.Right, 2)]
	[DataRow(2, NavigationDirection.Down, 3)]
	[DataRow(2, NavigationDirection.Up, 1)]
	[DataRow(2, NavigationDirection.Left, 1)]
	[DataRow(2, NavigationDirection.Right, -1)]
	[DataRow(3, NavigationDirection.Down, 4)]
	[DataRow(3, NavigationDirection.Up, 1)]
	[DataRow(3, NavigationDirection.Left, -1)]
	[DataRow(3, NavigationDirection.Right, 1)]
	[DataRow(4, NavigationDirection.Down, -1)]
	[DataRow(4, NavigationDirection.Up, 3)]
	[DataRow(4, NavigationDirection.Left, 3)]
	[DataRow(4, NavigationDirection.Right, 2)]
	public void RectilinearDistanceFocusDependingOnDirection(int from, NavigationDirection direction, int to)
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var (canvas, buttons) = CreateXYTestLayout();
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = canvas
		};
		window.Show();

		var fromButton = buttons[from - 1];
		fromButton.SetValue(XYFocus.UpNavigationStrategyProperty, XYFocusNavigationStrategy.RectilinearDistance);
		fromButton.SetValue(XYFocus.LeftNavigationStrategyProperty, XYFocusNavigationStrategy.RectilinearDistance);
		fromButton.SetValue(XYFocus.RightNavigationStrategyProperty, XYFocusNavigationStrategy.RectilinearDistance);
		fromButton.SetValue(XYFocus.DownNavigationStrategyProperty, XYFocusNavigationStrategy.RectilinearDistance);

		var result = KeyboardNavigationHandler.GetNext(fromButton, direction) as Button;

		CornerstoneTest.AreEqual(to, result == null ? -1 : Array.IndexOf(buttons, result) + 1);
	}

	[PresentationTestMethod]
	public void UsesXYDirectionalOverrides()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var left = new Button();
		var right = new Button();
		var up = new Button();
		var down = new Button();
		var center = new Button
		{
			[XYFocus.LeftProperty] = left,
			[XYFocus.RightProperty] = right,
			[XYFocus.UpProperty] = up,
			[XYFocus.DownProperty] = down
		};
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = new Canvas
			{
				Children =
				{
					left, right, up, down, center
				}
			}
		};
		window.Show();

		CornerstoneTest.AreEqual(left, KeyboardNavigationHandler.GetNext(center, NavigationDirection.Left));
		CornerstoneTest.AreEqual(right, KeyboardNavigationHandler.GetNext(center, NavigationDirection.Right));
		CornerstoneTest.AreEqual(up, KeyboardNavigationHandler.GetNext(center, NavigationDirection.Up));
		CornerstoneTest.AreEqual(down, KeyboardNavigationHandler.GetNext(center, NavigationDirection.Down));
	}

	[PresentationTestMethod]
	public void XYDirectionalOverrideDiscardedIfNotPartOfTheSameRoot()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var left = new Button();
		var center = new Button
		{
			[XYFocus.LeftProperty] = left
		};
		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = center
		};
		window.Show();

		CornerstoneTest.IsNull(KeyboardNavigationHandler.GetNext(center, NavigationDirection.Left));
	}

	[PresentationTestMethod]
	public void XYFocusSkipsEffectivelyDisabledControls()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var current = new TestControl { Height = 20, Width = 20, IsEnabled = true, IsVisible = true, Focusable = true, ShouldEnable = true };
		var disabled = new TestControl { Height = 20, Width = 20, IsEnabled = true, IsVisible = true, Focusable = true, ShouldEnable = false };
		var candidate = new TestControl { Height = 20, Width = 20, IsEnabled = true, IsVisible = true, Focusable = true, ShouldEnable = true };

		var parent = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 20,
			Children = { current, disabled, candidate }
		};

		var window = new Window
		{
			[XYFocus.NavigationModesProperty] = XYFocusNavigationModes.Enabled,
			Content = parent,
			Height = 300
		};
		window.Show();

		CornerstoneTest.AreEqual(candidate, KeyboardNavigationHandler.GetNext(current, NavigationDirection.Down));
	}

	private static (Canvas canvas, Button[] buttons) CreateXYTestLayout()
	{
		//  111
		//  111
		//  111
		//         2
		// 3
		//
		//   4
		Button x1, x2, x3, x4;
		var canvas = new Canvas
		{
			Width = 500,
			Children =
			{
				(x1 = new Button
				{
					Content = "A",
					[Canvas.LeftProperty] = 50, [Canvas.TopProperty] = 0, Width = 150, Height = 150
				}),
				(x2 = new Button
				{
					Content = "B",
					[Canvas.LeftProperty] = 400, [Canvas.TopProperty] = 150, Width = 50, Height = 50
				}),
				(x3 = new Button
				{
					Content = "C",
					[Canvas.LeftProperty] = 0, [Canvas.TopProperty] = 200, Width = 50, Height = 50
				}),
				(x4 = new Button
				{
					Content = "D",
					[Canvas.LeftProperty] = 100, [Canvas.TopProperty] = 300, Width = 50, Height = 50
				})
			}
		};

		return (canvas, new[] { x1, x2, x3, x4 });
	}

	#endregion

	#region Classes

	private class TestControl : Decorator
	{
		#region Fields

		private bool _shouldEnable;

		#endregion

		#region Properties

		public bool ShouldEnable
		{
			get => _shouldEnable;
			set
			{
				_shouldEnable = value;
				UpdateIsEffectivelyEnabled();
			}
		}

		protected override bool IsEnabledCore => IsEnabled && _shouldEnable;

		#endregion
	}

	#endregion
}