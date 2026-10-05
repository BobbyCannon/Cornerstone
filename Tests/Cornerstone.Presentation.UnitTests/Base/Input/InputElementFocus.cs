#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class InputElementFocus
{
	#region Methods

	[PresentationTestMethod]
	public void CanClearFocus()
	{
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = target = new Button()
			};

			target.Focus();
			root.FocusManager.Focus(null);

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void CanGetDirectionalNextElementWithFocusedElementOption()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var target5 = new Button { Focusable = true, Content = "5" };
			var searchStack = new StackPanel
			{
				Children =
				{
					target3,
					target4
				}
			};
			var container = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Children =
				{
					target1,
					target2,
					searchStack,
					target5
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			root.InvalidateMeasure();
			root.ExecuteInitialLayoutPass();

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);
			CornerstoneTest.IsNull(focusManager.GetFocusedElement());

			// Search root is right of the specified focused element, should return the first focusable element in the search root
			var next = focusManager.FindNextElement(NavigationDirection.Right, new FindNextElementOptions
			{
				SearchRoot = searchStack,
				FocusedElement = target1
			});

			CornerstoneTest.AreEqual(next, target3);

			// Search root is left of the specified focused element, should return the first focusable element in the search root
			next = focusManager.FindNextElement(NavigationDirection.Left, new FindNextElementOptions
			{
				SearchRoot = searchStack,
				FocusedElement = target5
			});

			CornerstoneTest.AreEqual(next, target3);

			// Search root isn't to the right of the specified focused element, should return null
			next = focusManager.FindNextElement(NavigationDirection.Right, new FindNextElementOptions
			{
				SearchRoot = searchStack,
				FocusedElement = target5
			});

			CornerstoneTest.IsNull(next);
		}
	}

	[PresentationTestMethod]
	public void CanGetDirectionalNextElementWithOptions()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var target5 = new Button { Focusable = true, Content = "5" };
			var seachStack = new StackPanel
			{
				Children =
				{
					target3,
					target4
				}
			};
			var container = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Children =
				{
					target1,
					target2,
					seachStack,
					target5
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			root.InvalidateMeasure();
			root.ExecuteInitialLayoutPass();

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);
			target1.Focus();

			var options = new FindNextElementOptions
			{
				SearchRoot = seachStack
			};

			// Search root is right of the current focus, should return the first focusable element in the search root
			var next = focusManager.FindNextElement(NavigationDirection.Right, options);

			CornerstoneTest.AreEqual(next, target3);

			target5.Focus();

			// Search root is right of the current focus, should return the first focusable element in the search root
			next = focusManager.FindNextElement(NavigationDirection.Left, options);

			CornerstoneTest.AreEqual(next, target3);

			// Search root isn't to the right of the current focus, should return null
			next = focusManager.FindNextElement(NavigationDirection.Right, options);

			CornerstoneTest.IsNull(next);
		}
	}

	[PresentationTestMethod]
	public void CanGetFirstFocusableElement()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					target2,
					target3,
					target4
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var firstFocusable = FocusManager.FindFirstFocusableElement(container);

			CornerstoneTest.AreEqual(target1, firstFocusable);

			firstFocusable = (root.FocusManager as FocusManager)?.FindFirstFocusableElement();

			CornerstoneTest.AreEqual(target1, firstFocusable);
		}
	}

	[PresentationTestMethod]
	public void CanGetLastFocusableElement()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					target2,
					target3,
					target4
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var lastFocusable = FocusManager.FindLastFocusableElement(container);

			CornerstoneTest.AreEqual(target4, lastFocusable);

			lastFocusable = (root.FocusManager as FocusManager)?.FindLastFocusableElement();

			CornerstoneTest.AreEqual(target4, lastFocusable);
		}
	}

	[PresentationTestMethod]
	public void CanGetNextElement()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					target2,
					target3,
					target4
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);
			target1.Focus();

			var next = focusManager.FindNextElement(NavigationDirection.Next);

			CornerstoneTest.AreEqual(next, target2);
		}
	}

	[PresentationTestMethod]
	public void CanGetNextElementOutOfContainerWithTabNavigationOnce()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var inside = new Button { Focusable = true, Content = "inside" };
			var after = new Button { Focusable = true, Content = "after" };

			// The focused element has to sit at least one level below the Once container:
			// GetFocusParent(focused) must resolve to something *deeper* than that container,
			// otherwise the reset below happens to land on the correct node and the walk
			// terminates by accident.
			var once = new StackPanel
			{
				[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
				Children =
				{
					new StackPanel { Children = { inside } }
				}
			};
			var root = new TestRoot
			{
				Child = new StackPanel
				{
					Children = { once, after }
				}
			};

			var focusManager = FocusManager.GetFocusManager(inside);
			CornerstoneTest.IsNotNull(focusManager);
			inside.Focus();

			// Before the fix this call never returned: on every Once hit the parent walk was
			// reset to the focused element's parent, so it oscillated between the same two
			// nodes forever and burned 100% CPU on the UI thread.
			var next = focusManager.FindNextElement(NavigationDirection.Next);

			CornerstoneTest.AreEqual(after, next);
		}
	}

	[PresentationTestMethod]
	public void CanGetNextElementWithFocusedElementOption()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					target2,
					target3,
					target4
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);
			CornerstoneTest.IsNull(focusManager.GetFocusedElement());

			var next = focusManager.FindNextElement(
				NavigationDirection.Next,
				new FindNextElementOptions { FocusedElement = target1 });

			CornerstoneTest.AreEqual(next, target2);
		}
	}

	[PresentationTestMethod]
	public void CanGetPreviousElement()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					target2,
					target3,
					target4
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);
			target3.Focus();

			// Must return the closest preceding sibling: not target1 (which merely comes
			// first) and not target4 (which comes after the focused element).
			var previous = focusManager.FindNextElement(NavigationDirection.Previous);

			CornerstoneTest.AreEqual(target2, previous);
		}
	}

	[PresentationTestMethod]
	public void CanGetPreviousElementOutOfContainerWithTabNavigationOnce()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var before = new Button { Name = "before", Focusable = true, Content = "before" };
			var inside = new Button { Name = "inside", Focusable = true, Content = "inside" };

			// Same shape as the Next case. The Once container itself must stay unfocusable,
			// otherwise GetPreviousTabStop returns it before reaching the faulty branch.
			var once = new StackPanel
			{
				[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
				Children =
				{
					new StackPanel { Children = { inside } }
				}
			};
			var root = new TestRoot
			{
				Child = new StackPanel
				{
					Children = { before, once }
				}
			};

			var focusManager = FocusManager.GetFocusManager(inside);
			CornerstoneTest.IsNotNull(focusManager);
			inside.Focus();

			// Before the fixes this call never returned. The walk must leave the Once
			// container and land on the element preceding it.
			var previous = focusManager.FindNextElement(NavigationDirection.Previous);

			CornerstoneTest.AreEqual(before, previous);
		}
	}

	[PresentationTestMethod]
	public void ControlFocusVsisiblePseudoclassShouldBeAppliedOnTabAndDirectionalFocus()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Decorator { Focusable = true };
			var target2 = new Decorator { Focusable = true };
			var root = new TestRoot
			{
				Child = new StackPanel
				{
					Children =
					{
						target1,
						target2
					}
				}
			};

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			target1.Focus();
			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsFalse(target1.Classes.Contains(":focus-visible"));
			CornerstoneTest.IsFalse(target2.IsFocused);
			CornerstoneTest.IsFalse(target2.Classes.Contains(":focus-visible"));

			target2.Focus(NavigationMethod.Tab);
			CornerstoneTest.IsFalse(target1.IsFocused);
			CornerstoneTest.IsFalse(target1.Classes.Contains(":focus-visible"));
			CornerstoneTest.IsTrue(target2.IsFocused);
			CornerstoneTest.IsTrue(target2.Classes.Contains(":focus-visible"));

			target1.Focus(NavigationMethod.Directional);
			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(target1.Classes.Contains(":focus-visible"));
			CornerstoneTest.IsFalse(target2.IsFocused);
			CornerstoneTest.IsFalse(target2.Classes.Contains(":focus-visible"));
		}
	}

	[PresentationTestMethod]
	public void ControlFocusWithinPseudoClassShouldBeApplied()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Decorator { Focusable = true };
			var target2 = new Decorator { Focusable = true };
			var root = new TestRoot
			{
				Child = new StackPanel
				{
					Children =
					{
						target1,
						target2
					}
				}
			};

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			target1.Focus();
			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(target1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(target1.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root.Child.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root.Child.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root.IsKeyboardFocusWithin);
		}
	}

	[PresentationTestMethod]
	public void ControlFocusWithinPseudoClassShouldBeAppliedandRemoved()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Decorator { Focusable = true };
			var target2 = new Decorator { Focusable = true };
			var panel1 = new Panel { Children = { target1 } };
			var panel2 = new Panel { Children = { target2 } };
			var root = new TestRoot
			{
				Child = new StackPanel
				{
					Children =
					{
						panel1,
						panel2
					}
				}
			};

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			target1.Focus();
			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(target1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(target1.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(panel1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(panel1.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root.Child.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root.Child.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root.IsKeyboardFocusWithin);

			target2.Focus();

			CornerstoneTest.IsFalse(target1.IsFocused);
			CornerstoneTest.IsFalse(target1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsFalse(target1.IsKeyboardFocusWithin);
			CornerstoneTest.IsFalse(panel1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsFalse(panel1.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root.Child.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root.Child.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root.IsKeyboardFocusWithin);

			CornerstoneTest.IsTrue(target2.IsFocused);
			CornerstoneTest.IsTrue(target2.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(target2.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(panel2.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(panel2.IsKeyboardFocusWithin);
		}
	}

	[PresentationTestMethod]
	public void ControlFocusWithinPseudoclassShouldBeRemovedFocusMovesToDifferentRoot()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Decorator { Focusable = true };
			var target2 = new Decorator { Focusable = true };

			var root1 = new TestRoot
			{
				Child = new StackPanel
				{
					Children =
					{
						target1
					}
				}
			};

			var root2 = new TestRoot
			{
				Child = new StackPanel
				{
					Children =
					{
						target2
					}
				}
			};

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			target1.Focus();
			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(target1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(target1.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root1.Child.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root1.Child.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root1.IsKeyboardFocusWithin);

			CornerstoneTest.AreEqual(KeyboardDevice.Instance!.FocusedElement, target1);

			target2.Focus();

			CornerstoneTest.IsFalse(target1.IsFocused);
			CornerstoneTest.IsFalse(target1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsFalse(target1.IsKeyboardFocusWithin);
			CornerstoneTest.IsFalse(root1.Child.Classes.Contains(":focus-within"));
			CornerstoneTest.IsFalse(root1.Child.IsKeyboardFocusWithin);
			CornerstoneTest.IsFalse(root1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsFalse(root1.IsKeyboardFocusWithin);

			CornerstoneTest.IsTrue(target2.IsFocused);
			CornerstoneTest.IsTrue(target2.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(target2.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root2.Child.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root2.Child.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root2.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root2.IsKeyboardFocusWithin);
		}
	}

	[PresentationTestMethod]
	public void ControlFocusWithinPseudoclassShouldBeRemovedWhenRemovedFromTree()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Decorator { Focusable = true };
			var target2 = new Decorator { Focusable = true };
			var root = new TestRoot
			{
				Child = new StackPanel
				{
					Children =
					{
						target1,
						target2
					}
				}
			};

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			target1.Focus();
			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(target1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(target1.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root.Child.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root.Child.IsKeyboardFocusWithin);
			CornerstoneTest.IsTrue(root.Classes.Contains(":focus-within"));
			CornerstoneTest.IsTrue(root.IsKeyboardFocusWithin);

			var keyboardDevice = KeyboardDevice.Instance!;
			CornerstoneTest.AreEqual(keyboardDevice.FocusedElement, target1);

			root.Child = null;

			CornerstoneTest.IsNull(keyboardDevice.FocusedElement);

			CornerstoneTest.IsFalse(target1.IsFocused);
			CornerstoneTest.IsFalse(target1.Classes.Contains(":focus-within"));
			CornerstoneTest.IsFalse(target1.IsKeyboardFocusWithin);
			CornerstoneTest.IsFalse(root.Classes.Contains(":focus-within"));
			CornerstoneTest.IsFalse(root.IsKeyboardFocusWithin);
		}
	}

	[PresentationTestMethod]
	public void DisabledControlsShouldNotReceiveFocus()
	{
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = target = new Button { IsEnabled = false }
			};

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());

			target.Focus();

			CornerstoneTest.IsFalse(target.IsFocused);
			CornerstoneTest.IsFalse(target.IsKeyboardFocusWithin);

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void EffectivelyDisabledControlsShouldNotReceiveFocus()
	{
		var target = new Button();
		Panel container;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = container = new Panel
				{
					IsEnabled = false,
					Children = { target }
				}
			};

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());

			target.Focus();

			CornerstoneTest.IsFalse(target.IsFocused);
			CornerstoneTest.IsFalse(target.IsKeyboardFocusWithin);

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void EffectivelyInvisibleControlsShouldNotReceiveFocus()
	{
		var target = new Button();
		Panel container;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = container = new Panel
				{
					IsVisible = false,
					Children = { target }
				}
			};

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());

			target.Focus();

			CornerstoneTest.IsFalse(target.IsFocused);
			CornerstoneTest.IsFalse(target.IsKeyboardFocusWithin);

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void FocusInScopeShouldMatchRedirectedElementWhenFocusRedirected()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var first = new Button { Name = "First" };
		var second = new Button { Name = "Second" };
		var third = new Button { Name = "Third" };

		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					first,
					second,
					third
				}
			}
		};

		var focusManager = (FocusManager) root.FocusManager;

		// Focus the first element
		first.Focus();
		CornerstoneTest.Same(first, focusManager.GetFocusedElement(root));

		// Redirect focus change
		second.GettingFocus += (_, e) => e.TrySetNewFocusedElement(third);

		// Move the focus to the second element: it should fail
		var focusResult = focusManager.Focus(second);
		CornerstoneTest.IsFalse(focusResult);
		CornerstoneTest.Same(third, KeyboardDevice.Instance?.FocusedElement);

		// FocusedElement for the scope should have moved to the redirected element
		var newFocusedElementInScope = focusManager.GetFocusedElement(root);
		CornerstoneTest.Same(third, newFocusedElementInScope);
	}

	[PresentationTestMethod]
	public void FocusInScopeShouldNotChangeWhenFocusCanceled()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var first = new Button { Name = "First" };
		var second = new Button { Name = "Second" };

		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					first,
					second
				}
			}
		};

		var focusManager = (FocusManager) root.FocusManager;

		// Focus the first element
		first.Focus();
		CornerstoneTest.Same(first, focusManager.GetFocusedElement(root));

		// Cancel focus change
		second.GettingFocus += (_, e) => e.TryCancel();

		// Move the focus to the second element: it should fail
		var focusResult = focusManager.Focus(second);
		CornerstoneTest.IsFalse(focusResult);
		CornerstoneTest.Same(first, KeyboardDevice.Instance?.FocusedElement);

		// FocusedElement for the scope should remain the same
		var newFocusedElementInScope = focusManager.GetFocusedElement(root);
		CornerstoneTest.Same(first, newFocusedElementInScope);
	}

	[PresentationTestMethod]
	public void FocusPseudoclassShouldBeAppliedOnFocus()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Decorator { Focusable = true };
			var target2 = new Decorator { Focusable = true };
			var root = new TestRoot
			{
				Child = new StackPanel
				{
					Children =
					{
						target1,
						target2
					}
				}
			};

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			target1.Focus();
			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(target1.Classes.Contains(":focus"));
			CornerstoneTest.IsFalse(target2.IsFocused);
			CornerstoneTest.IsFalse(target2.Classes.Contains(":focus"));

			target2.Focus(NavigationMethod.Tab);
			CornerstoneTest.IsFalse(target1.IsFocused);
			CornerstoneTest.IsFalse(target1.Classes.Contains(":focus"));
			CornerstoneTest.IsTrue(target2.IsFocused);
			CornerstoneTest.IsTrue(target2.Classes.Contains(":focus"));
		}
	}

	[PresentationTestMethod]
	public void FocusShouldBeClearedWhenControlIsDisabled()
	{
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = target = new Button()
			};

			target.Focus();
			target.IsEnabled = false;

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void FocusShouldBeClearedWhenControlIsEffectivelyDisabled()
	{
		Border container;
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = container = new Border
				{
					Child = target = new Button()
				}
			};

			target.Focus();
			container.IsEnabled = false;

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void FocusShouldBeClearedWhenControlIsEffectivelyHidden()
	{
		Border container;
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = container = new Border
				{
					Child = target = new Button()
				}
			};

			target.Focus();
			container.IsVisible = false;

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void FocusShouldBeClearedWhenControlIsHidden()
	{
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = target = new Button()
			};

			target.Focus();
			target.IsVisible = false;

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void FocusShouldBeClearedWhenControlIsRemovedFromVisualTree()
	{
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = target = new Button()
			};

			target.Focus();
			root.Child = null;

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void FocusShouldMoveAccordingToDirection()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					target2,
					target3,
					target4
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);

			var hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Previous);

			CornerstoneTest.IsTrue(target4.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);
		}
	}

	[PresentationTestMethod]
	public void FocusShouldMoveAccordingToXYDirection()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var target4 = new Button { Focusable = true, Content = "4" };
			var center = new Button
			{
				[XYFocus.LeftProperty] = target1,
				[XYFocus.RightProperty] = target2,
				[XYFocus.UpProperty] = target3,
				[XYFocus.DownProperty] = target4
			};
			var container = new Canvas
			{
				Children =
				{
					target1,
					target2,
					target3,
					target4,
					center
				}
			};

			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);

			center.Focus();

			var options = new FindNextElementOptions
			{
				SearchRoot = container
			};

			var hasMoved = focusManager.TryMoveFocus(NavigationDirection.Up, options);
			CornerstoneTest.IsTrue(target3.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);
		}
	}

	[PresentationTestMethod]
	public void FocusShouldNotBeRestoredToDetachedControl()
	{
		using var app = UnitTestApplication.Start(
			TestServices.StyledWindow.With(keyboardDevice: () => new KeyboardDevice()));
		var button = new Button { Name = "Button" };
		MenuItem topLevelMenu;
		MenuItem childMenu;
		var menu = new Menu
		{
			Items =
			{
				(topLevelMenu = new MenuItem
				{
					Header = "Foo",
					Items =
					{
						(childMenu = new MenuItem { Header = "Bar" })
					}
				})
			}
		};
		var panel = new StackPanel();
		panel.Children.Add(button);
		panel.Children.Add(menu);

		var window = new Window
		{
			Content = panel,
			Name = "Window1"
		};

		window.Show();

		// Focus the button
		button.Focus();
		CornerstoneTest.Same(button, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(button, window.FocusManager.GetFocusedElement());

		// Open the menu and focus the child menu
		menu.Open();
		topLevelMenu.IsSubMenuOpen = true;
		childMenu.Focus();

		// Remove the previously focused button.
		panel.Children.Remove(button);

		// Close the menus.
		menu.Close();
		topLevelMenu.Close();

		window.PlatformImpl?.Activated?.Invoke();

		// When window is activated, focus should be empty
		CornerstoneTest.Same(null, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(null, window.FocusManager.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void FocusShouldNotGetRestoredToEnabledControl()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var sp = new StackPanel();
			var target = new Button();
			var target1 = new Button();
			target.Click += (s, e) => target.IsEnabled = false;
			target1.Click += (s, e) => target.IsEnabled = true;
			sp.Children.Add(target);
			sp.Children.Add(target1);
			var root = new TestRoot
			{
				Child = sp
			};

			target.Focus();
			target.RaiseEvent(new AccessKeyEventArgs("b1", false));
			CornerstoneTest.IsFalse(target.IsEnabled);
			CornerstoneTest.IsFalse(target.IsFocused);
			target1.RaiseEvent(new AccessKeyEventArgs("b2", false));
			CornerstoneTest.IsTrue(target.IsEnabled);
			CornerstoneTest.IsFalse(target.IsFocused);
		}
	}

	[PresentationTestMethod]
	public void FocusShouldReturnToFirstWindowWhenSecondIsClosed()
	{
		using var app = UnitTestApplication.Start(
			TestServices.StyledWindow.With(keyboardDevice: () => new KeyboardDevice()));
		var first = new Button { Name = "FirstButton" };
		var second = new Button { Name = "SecondButton" };

		var window1 = new Window
		{
			Content = first
		};

		var window2 = new Window
		{
			Content = second
		};

		window1.Show();

		// Focus the first button in the first window
		first.Focus();
		CornerstoneTest.Same(first, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(first, window1.FocusManager.GetFocusedElement());

		window2.Show();

		// Focus the second button in the second window
		second.Focus();
		CornerstoneTest.Same(second, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(second, window2.FocusManager.GetFocusedElement());

		// Close the second window, focus should be lost
		window2.Close();
		CornerstoneTest.IsNull(KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.IsNull(window2.FocusManager.GetFocusedElement());

		// Activate the first window again
		window1.PlatformImpl?.Activated?.Invoke();

		// Focus should have moved back to the first button in the first window
		CornerstoneTest.Same(first, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(first, window1.FocusManager.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void FocusShouldSetFocusManagerCurrent()
	{
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = target = new Button()
			};

			target.Focus();

			CornerstoneTest.Same(target, root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void FocusShouldSkipElementsWithFocusableEqualFalse()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var skip1 = new Button { Focusable = false, Content = "s1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var skip2 = new Button { Focusable = false, Content = "s2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					skip1,
					target2,
					skip2,
					target3
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);

			var hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target2.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target3.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);
		}
	}

	[PresentationTestMethod]
	public void FocusShouldSkipElementsWithIsTabStopEqualFalse()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var skip1 = new Button { IsTabStop = false, Focusable = true, Content = "s1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var skip2 = new Button { IsTabStop = false, Focusable = true, Content = "s2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					skip1,
					target2,
					skip2,
					target3
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);

			var hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target2.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target3.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);
		}
	}

	[PresentationTestMethod]
	public void FocusShouldSkipEmptyContainers()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var skip1 = new StackPanel();
			var target2 = new Button { Focusable = true, Content = "2" };
			var skip2 = new StackPanel();
			var target3 = new Button { Focusable = true, Content = "3" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					skip1,
					target2,
					skip2,
					target3
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);

			var hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target2.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target3.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);
		}
	}

	[PresentationTestMethod]
	public void FocusShouldSkipTextBlockElements()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var skip1 = new TextBlock { Focusable = false, Text = "s1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var skip2 = new TextBlock { Focusable = false, Text = "s2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var container = new StackPanel
			{
				Children =
				{
					target1,
					skip1,
					target2,
					skip2,
					target3
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var focusManager = FocusManager.GetFocusManager(container);
			CornerstoneTest.IsNotNull(focusManager);

			var hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target1.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target2.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);

			hasMoved = focusManager.TryMoveFocus(NavigationDirection.Next);

			CornerstoneTest.IsTrue(target3.IsFocused);
			CornerstoneTest.IsTrue(hasMoved);
		}
	}

	[PresentationTestMethod]
	public void FocusShouldStayInActiveWindow()
	{
		using var app = UnitTestApplication.Start(
			TestServices.StyledWindow.With(keyboardDevice: () => new KeyboardDevice()));
		var first = new Button { Name = "FirstButton" };
		var second = new Button { Name = "SecondButton" };

		var window1 = new Window
		{
			Content = first
		};

		var window2 = new Window
		{
			Content = second
		};

		window1.Show();

		// Focus the first button in the first window
		first.Focus();
		CornerstoneTest.Same(first, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(first, window1.FocusManager.GetFocusedElement());

		window2.Show();

		// Focus the second button in the second window
		second.Focus();
		CornerstoneTest.Same(second, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(second, window2.FocusManager.GetFocusedElement());

		// Activate the first window again
		window1.PlatformImpl?.Activated?.Invoke();

		// Focus should have moved back to the first button in the first window
		CornerstoneTest.Same(first, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(first, window1.FocusManager.GetFocusedElement());

		// Close the second window
		window2.Close();

		// Focus should still be in the first window
		CornerstoneTest.Same(first, KeyboardDevice.Instance?.FocusedElement);
		CornerstoneTest.Same(first, window1.FocusManager.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void GetFirstFocusableElementSkipsUnfocusableElements()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var skip1 = new Button { Focusable = false, Content = "s1" };
			var skip2 = new TextBlock { Text = "s2" };
			var skip3 = new StackPanel();
			var target4 = new Button { Focusable = true, Content = "4" };
			var target5 = new Button { Focusable = true, Content = "5" };
			var container = new StackPanel
			{
				Children =
				{
					skip1,
					skip2,
					skip3,
					target4,
					target5
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var firstFocusable = FocusManager.FindFirstFocusableElement(container);

			CornerstoneTest.AreEqual(target4, firstFocusable);

			firstFocusable = (root.FocusManager as FocusManager)?.FindFirstFocusableElement();

			CornerstoneTest.AreEqual(target4, firstFocusable);
		}
	}

	[PresentationTestMethod]
	public void GetLastFocusableElementSkipsUnfocusableElements()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var skip3 = new Button { Focusable = false, Content = "s3" };
			var skip4 = new TextBlock { Text = "s4" };
			var skip5 = new StackPanel();
			var container = new StackPanel
			{
				Children =
				{
					target1,
					target2,
					skip3,
					skip4,
					skip5
				}
			};
			var root = new TestRoot
			{
				Child = container
			};

			var lastFocusable = FocusManager.FindLastFocusableElement(container);

			CornerstoneTest.AreEqual(target2, lastFocusable);

			lastFocusable = (root.FocusManager as FocusManager)?.FindLastFocusableElement();

			CornerstoneTest.AreEqual(target2, lastFocusable);
		}
	}

	[PresentationTestMethod]
	public void InvisibleControlsShouldNotReceiveFocus()
	{
		Button target;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = target = new Button { IsVisible = false }
			};

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());

			target.Focus();

			CornerstoneTest.IsFalse(target.IsFocused);
			CornerstoneTest.IsFalse(target.IsKeyboardFocusWithin);

			CornerstoneTest.IsNull(root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void PreviousWrapsToLastElementInCycleContainer()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target1 = new Button { Focusable = true, Content = "1" };
			var target2 = new Button { Focusable = true, Content = "2" };
			var target3 = new Button { Focusable = true, Content = "3" };
			var cycle = new StackPanel
			{
				[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
				Children =
				{
					target1,
					target2,
					target3
				}
			};
			var root = new TestRoot
			{
				Child = cycle
			};

			var focusManager = FocusManager.GetFocusManager(target1);
			CornerstoneTest.IsNotNull(focusManager);
			target1.Focus();

			// Wrapping backwards inside a Cycle scope must land on the last focusable
			// element, mirroring the forward wrap (last -> first). It used to take the
			// FIRST element - the focused element itself - making Previous a no-op.
			var previous = focusManager.FindNextElement(NavigationDirection.Previous);

			CornerstoneTest.AreEqual(target3, previous);
		}
	}

	[PresentationTestMethod]
	public void RemovingFocusScopeActivatesRootFocusScope()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		Button innerButton, outerButton;
		TestFocusScope innerScope;
		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					(innerScope = new TestFocusScope
					{
						Children =
						{
							(innerButton = new Button())
						}
					}),
					(outerButton = new Button())
				}
			}
		};

		// Focus a control in the top-level and inner focus scopes.
		outerButton.Focus();
		innerButton.Focus();

		// Remove the inner focus scope.
		((Panel) innerScope.Parent!).Children.Remove(innerScope);

		var focusManager = CornerstoneTest.IsType<FocusManager>(root.FocusManager);
		CornerstoneTest.Same(outerButton, focusManager.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void RemovingFocusedElementInsideFocusScopeActivatesRootFocusScope()
	{
		// Issue #13325
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		Button innerButton, intermediateButton, outerButton;
		TestFocusScope innerScope;
		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					// Intermediate focus scope to make sure that the root focus scope gets
					// activated, not this one.
					new TestFocusScope
					{
						Children =
						{
							(innerScope = new TestFocusScope
							{
								Children =
								{
									(innerButton = new Button())
								}
							}),
							(intermediateButton = new Button())
						}
					},
					(outerButton = new Button())
				}
			}
		};

		// Focus a control in each scope, ending with the innermost one.
		outerButton.Focus();
		intermediateButton.Focus();
		innerButton.Focus();

		// Remove the focused control from the tree.
		((Panel) innerButton.Parent!).Children.Remove(innerButton);

		var focusManager = CornerstoneTest.IsType<FocusManager>(root.FocusManager);
		CornerstoneTest.Same(outerButton, focusManager.GetFocusedElement());
		CornerstoneTest.IsNull(focusManager.GetFocusedElement(innerScope));
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/13134
	[PresentationTestMethod]
	public void SetFocusScopeOnNonFocusableScopeChangesScope()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);

		Button outerButton;
		TestFocusScope innerScope;
		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Focusable = false,
				Children =
				{
					(innerScope = new TestFocusScope()),
					(outerButton = new Button())
				}
			}
		};

		outerButton.Focus();

		var focusManager = CornerstoneTest.IsType<FocusManager>(root.FocusManager);
		CornerstoneTest.Same(outerButton, focusManager.GetFocusedElement());

		// Switch to a scope that has no previously focused element and isn't focusable itself.
		// TestFocusScope is a Panel (Focusable = false) + IFocusScope.
		focusManager.SetFocusScope(innerScope);

		// Focus must be cleared: the scope is not focusable and has no prior focused element.
		// Before the fix this was a no-op and outerButton would still be reported as focused.
		CornerstoneTest.IsNull(focusManager.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void SwitchingFocusScopeChangesFocus()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		Button innerButton, outerButton;
		TestFocusScope innerScope;
		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					(innerScope = new TestFocusScope
					{
						Children =
						{
							(innerButton = new Button())
						}
					}),
					(outerButton = new Button())
				}
			}
		};

		// Focus a control in the top-level and inner focus scopes.
		outerButton.Focus();
		innerButton.Focus();

		var focusManager = CornerstoneTest.IsType<FocusManager>(root.FocusManager);
		CornerstoneTest.Same(innerButton, focusManager.GetFocusedElement());

		focusManager.SetFocusScope(root);
		CornerstoneTest.Same(outerButton, focusManager.GetFocusedElement());

		focusManager.SetFocusScope(innerScope);
		CornerstoneTest.Same(innerButton, focusManager.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void TryingToFocusInvisibleControlShouldNotChangeFocus()
	{
		Button first;
		Button second;

		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var root = new TestRoot
			{
				Child = new StackPanel
				{
					Children =
					{
						(first = new Button()),
						(second = new Button { IsVisible = false })
					}
				}
			};

			first.Focus();

			CornerstoneTest.Same(first, root.FocusManager.GetFocusedElement());

			second.Focus();

			CornerstoneTest.Same(first, root.FocusManager.GetFocusedElement());
		}
	}

	#endregion

	#region Classes

	private class TestFocusScope : Panel, IFocusScope
	{
	}

	#endregion
}