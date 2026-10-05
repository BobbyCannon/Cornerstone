#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class KeyboardNavigationTestsCustom
{
	#region Methods

	[PresentationTestMethod]
	public void RightShouldCustomNavigateWithinChildren()
	{
		Button current;
		Button next;
		var target = new CustomNavigatingStackPanel
		{
			Children =
			{
				(current = new Button { Content = "Button 1" }),
				new Button { Content = "Button 2" },
				(next = new Button { Content = "Button 3" })
			},
			NextControl = next
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Right);

		CornerstoneTest.Same(next, result);
	}

	[PresentationTestMethod]
	public void ShiftTabShouldCustomNavigateFromOutside()
	{
		Button current;
		Button next;
		var target = new CustomNavigatingStackPanel
		{
			Children =
			{
				new Button { Content = "Button 1" },
				new Button { Content = "Button 2" },
				(next = new Button { Content = "Button 3" })
			},
			NextControl = next
		};

		var root = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				(current = new Button { Content = "Outside" }),
				target
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.Same(next, result);
	}

	[PresentationTestMethod]
	public void ShiftTabShouldNavigateOutsideWhenNullReturnedAsNext()
	{
		Button current;
		Button next;
		var target = new CustomNavigatingStackPanel
		{
			Children =
			{
				new Button { Content = "Button 1" },
				(current = new Button { Content = "Button 2" }),
				new Button { Content = "Button 3" }
			}
		};

		var root = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				target,
				(next = new Button { Content = "Outside" })
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.Same(next, result);
	}

	[PresentationTestMethod]
	public void TabShouldCustomNavigateFromOutside()
	{
		Button current;
		Button next;
		var target = new CustomNavigatingStackPanel
		{
			Children =
			{
				new Button { Content = "Button 1" },
				new Button { Content = "Button 2" },
				(next = new Button { Content = "Button 3" })
			},
			NextControl = next
		};

		var root = new StackPanel
		{
			Children =
			{
				(current = new Button { Content = "Outside" }),
				target
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.Same(next, result);
	}

	[PresentationTestMethod]
	public void TabShouldCustomNavigateFromOutsideWhenWrapping()
	{
		Button current;
		Button next;
		var target = new CustomNavigatingStackPanel
		{
			Children =
			{
				new Button { Content = "Button 1" },
				new Button { Content = "Button 2" },
				(next = new Button { Content = "Button 3" })
			},
			NextControl = next
		};

		var root = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				target,
				(current = new Button { Content = "Outside" })
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.Same(next, result);
	}

	[PresentationTestMethod]
	public void TabShouldCustomNavigateWithinChildren()
	{
		Button current;
		Button next;
		var target = new CustomNavigatingStackPanel
		{
			Children =
			{
				(current = new Button { Content = "Button 1" }),
				new Button { Content = "Button 2" },
				(next = new Button { Content = "Button 3" })
			},
			NextControl = next
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.Same(next, result);
	}

	[PresentationTestMethod]
	public void TabShouldNavigateOutsideWhenNullReturnedAsNext()
	{
		Button current;
		Button next;
		var target = new CustomNavigatingStackPanel
		{
			Children =
			{
				new Button { Content = "Button 1" },
				(current = new Button { Content = "Button 2" }),
				new Button { Content = "Button 3" }
			}
		};

		var root = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				target,
				(next = new Button { Content = "Outside" })
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.Same(next, result);
	}

	#endregion

	#region Classes

	private class CustomNavigatingStackPanel : StackPanel, ICustomKeyboardNavigation
	{
		#region Properties

		public bool CustomNavigates { get; } = true;
		public IInputElement NextControl { get; set; }

		#endregion

		#region Methods

		public (bool handled, IInputElement next) GetNext(IInputElement element, NavigationDirection direction)
		{
			return (CustomNavigates, NextControl);
		}

		#endregion
	}

	#endregion
}