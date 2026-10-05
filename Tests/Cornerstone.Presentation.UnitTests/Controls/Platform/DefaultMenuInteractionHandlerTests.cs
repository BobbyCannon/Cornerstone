#region References

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Platform;

[TestClass]
public class DefaultMenuInteractionHandlerTests : ScopedTestBase
{
	#region Methods

	private static StubMenu CreateMockMainMenu()
	{
		return new StubMenu();
	}

	private static StubMenuItem CreateMockMenuItem(
		bool isTopLevel = false,
		bool hasSubMenu = false,
		bool isSubMenuOpen = false,
		IMenuElement parent = null)
	{
		var item = new StubMenuItem();
		item.IsTopLevel = isTopLevel;
		item.HasSubMenu = hasSubMenu;
		item.IsSubMenuOpen = isSubMenuOpen;
		item.MenuParent = parent;
		return item;
	}

	private static PointerPressedEventArgs CreatePressed(object source)
	{
		return new(source,
			new FakePointer(), (Visual) source, default, 0, new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
			default);
	}

	private static PointerReleasedEventArgs CreateReleased(object source)
	{
		return new(source,
			new FakePointer(), (Visual) source, default, 0,
			new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
			default, MouseButton.Left);
	}

	#endregion

	#region Classes

	[TestClass]
	public class ContextMenu : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DownSelectsSelectsFirstMenuItemWhenNoSelection()
		{
			var target = new DefaultMenuInteractionHandler(true);
			var contextMenu = new StubMenu();
			contextMenu.MoveSelectionResult = true;
			var e = new KeyEventArgs { Key = Key.Down, Source = contextMenu };

			target.AttachCore(contextMenu);
			target.KeyDown(contextMenu, e);

			contextMenu.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.Down, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		#endregion
	}

	[TestClass]
	public class NonTopLevel : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DownSelectsNextMenuItem()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var parentItem = CreateMockMenuItem(true, true);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = new KeyEventArgs { Key = Key.Down, Source = item };

			target.KeyDown(item, e);

			CornerstoneTest.IsTrue(parentItem.Calls.WasCalled("MoveSelection", NavigationDirection.Down, true));
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void EnterOnItemWithNoSubMenuCausesClick()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = new KeyEventArgs { Key = Key.Enter, Source = item };

			target.KeyDown(item, e);

			item.Calls.VerifyCalled("RaiseClick");
			menu.Calls.VerifyCalled("Close");
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void EnterOnItemWithSubMenuOpensSubMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var parentItem = CreateMockMenuItem(true, true);
			var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem);
			var e = new KeyEventArgs { Key = Key.Enter, Source = item };

			target.KeyDown(item, e);

			item.Calls.VerifyCalled("Open");
			item.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.First, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void EscapeClosesParentMenuItem()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var parentItem = CreateMockMenuItem(true, true);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = new KeyEventArgs { Key = Key.Escape, Source = item };

			target.KeyDown(item, e);

			parentItem.Calls.VerifyCalled("Close");
			parentItem.Calls.VerifyCalled("Focus");
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void LeftClosesParentSubMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var parentItem = CreateMockMenuItem(hasSubMenu: true, isSubMenuOpen: true);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = new KeyEventArgs { Key = Key.Left, Source = item };

			target.KeyDown(item, e);

			parentItem.Calls.VerifyCalled("Close");
			parentItem.Calls.VerifyCalled("Focus");
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerEnteredClosesSiblingSubmenuAfterDelay()
		{
			var timer = new TestTimer();
			var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(parent: parentItem);
			var sibling = CreateMockMenuItem(hasSubMenu: true, isSubMenuOpen: true, parent: parentItem);
			var e = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, item);

			parentItem.SubItems = new[] { item, sibling };

			target.PointerEntered(item, e);
			sibling.Calls.VerifyNotCalled("Close");

			timer.Pulse();
			sibling.Calls.VerifyCalled("Close");

			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerEnteredOpensSubmenuAfterDelay()
		{
			var timer = new TestTimer();
			var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem);
			var e = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, item);

			target.PointerEntered(item, e);
			item.Calls.VerifyNotCalled("Open");

			timer.Pulse();
			item.Calls.VerifyCalled("Open");

			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerEnteredSelectsItem()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, item);

			target.PointerEntered(item, e);

			CornerstoneTest.Same(item, parentItem.SelectedItem);
			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerExitedDeselectsItem()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item);

			parentItem.SelectedItem = item;
			target.PointerExited(item, e);

			CornerstoneTest.IsNull(parentItem.SelectedItem);
			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerExitedDoesntDeselectItemIfPointerOverSubmenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem);
			var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item);

			item.IsPointerOverSubMenu = true;
			target.PointerExited(item, e);

			parentItem.Calls.VerifyNotCalled("set_SelectedItem");
			CornerstoneTest.IsNull(parentItem.SelectedItem);
			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerExitedDoesntDeselectSibling()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(parent: parentItem);
			var sibling = CreateMockMenuItem(parent: parentItem);
			var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item);

			parentItem.SelectedItem = sibling;
			target.PointerExited(item, e);

			CornerstoneTest.Same(sibling, parentItem.SelectedItem);
			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerPressedOnDisabledItemDoesntCloseSubMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, true, menu);
			var popup = new Popup();
			var e = CreatePressed(popup);

			((ISetLogicalParent) popup).SetParent(parentItem);
			target.PointerPressed(parentItem, e);

			parentItem.Calls.VerifyNotCalled("Close");
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerPressedOnItemWithSubMenuCausesOpensSubmenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem);
			var e = CreatePressed(item);

			target.PointerPressed(item, e);

			item.Calls.VerifyCalled("Open");
			item.Calls.VerifyNotCalled("MoveSelection");
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerReleasedOnItemWithNoSubMenuCausesClick()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = CreateReleased(item);

			target.PointerReleased(item, e);

			item.Calls.VerifyCalled("RaiseClick");
			menu.Calls.VerifyCalled("Close");
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void RightOnTopLevelChildNavigatesTopLevelSelection()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, true, menu);
			var nextItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = new KeyEventArgs { Key = Key.Right, Source = item };

			menu.MoveSelectionHandler = (_, _) => menu.SelectedItem = nextItem;

			target.KeyDown(item, e);

			menu.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.Right, true);
			parentItem.Calls.VerifyCalled("Close");
			nextItem.Calls.VerifyCalled("Open");
			nextItem.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.First, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void RightWithSubMenuItemsOpensSubMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var parentItem = CreateMockMenuItem(true, true);
			var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem);
			var e = new KeyEventArgs { Key = Key.Right, Source = item };

			target.KeyDown(item, e);

			item.Calls.VerifyCalled("Open");
			item.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.First, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void SelectionIsCorrectWhenPointerTemporarilyExitsItemToSelectSubItem()
		{
			var timer = new TestTimer();
			var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
			var menu = new StubMenu();
			var parentItem = CreateMockMenuItem(true, true, parent: menu);
			var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem);
			var childItem = CreateMockMenuItem(parent: item);
			var enter = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, item);
			var leave = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item);

			// Pointer enters item; item is selected.
			target.PointerEntered(item, enter);
			CornerstoneTest.IsTrue(timer.ActionIsQueued);
			CornerstoneTest.Same(item, parentItem.SelectedItem);
			parentItem.Calls.Clear();

			// SubMenu shown after a delay.
			timer.Pulse();
			item.Calls.VerifyCalled("Open");
			item.IsSubMenuOpen = true;
			item.Calls.Clear();

			// Pointer briefly exits item, but submenu remains open.
			target.PointerExited(item, leave);
			item.Calls.VerifyNotCalled("Close");
			item.Calls.Clear();

			// Pointer enters child item; is selected.
			enter.Source = childItem;
			target.PointerEntered(childItem, enter);
			CornerstoneTest.Same(childItem, item.SelectedItem);
			CornerstoneTest.Same(item, parentItem.SelectedItem);
		}

		[PresentationTestMethod]
		public void UpSelectsPreviousMenuItem()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var parentItem = CreateMockMenuItem(true, true);
			var item = CreateMockMenuItem(parent: parentItem);
			var e = new KeyEventArgs { Key = Key.Up, Source = item };

			target.KeyDown(item, e);

			CornerstoneTest.IsTrue(parentItem.Calls.WasCalled("MoveSelection", NavigationDirection.Up, true));
			CornerstoneTest.IsTrue(e.Handled);
		}

		#endregion
	}

	[TestClass]
	public class TopLevel : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ClickOnOpenTopLevelMenuClosesMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var item = CreateMockMenuItem(true, true, true, menu);

			var e = CreatePressed(item);

			target.PointerPressed(item, e);
			menu.Calls.VerifyCalled("Close");
		}

		[PresentationTestMethod]
		public void ClickOnTopLevelCallsMainMenuOpen()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = CreateMockMainMenu();
			var item = CreateMockMenuItem(true, true, parent: menu);

			var e = CreatePressed(item);

			target.PointerPressed(item, e);
			menu.Calls.VerifyCalled("Open");
		}

		[PresentationTestMethod]
		public void DoesntReplaceIsCheckedBinding()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new Menu();
			var vm = new MenuItemVM();

			var item = new MenuItem
			{
				DataContext = vm,
				[!MenuItem.IsCheckedProperty] = new Binding(nameof(MenuItemVM.IsChecked)) { Priority = BindingPriority.Style, Mode = BindingMode.TwoWay },
				ToggleType = MenuItemToggleType.CheckBox
			};
			menu.Items.Add(item);

			target.KeyDown(item, new KeyEventArgs { Key = Key.Enter, Source = menu });

			CornerstoneTest.IsTrue(item.IsChecked);

			vm.IsChecked = false;

			CornerstoneTest.IsFalse(item.IsChecked);
		}

		[PresentationTestMethod]
		public void DoesntReplaceIsSubMenuOpenBinding()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new Menu();
			var vm = new MenuItemVM();

			var item = new MenuItem
			{
				DataContext = vm,
				[!MenuItem.IsSubMenuOpenProperty] = new Binding(nameof(MenuItemVM.IsSubMenuOpen)) { Priority = BindingPriority.Style, Mode = BindingMode.TwoWay },
				Items = { new MenuItem() }
			};

			target.KeyDown(item, new KeyEventArgs { Key = Key.Enter, Source = menu });

			CornerstoneTest.IsTrue(item.IsSubMenuOpen);

			vm.IsSubMenuOpen = false;

			CornerstoneTest.IsFalse(item.IsSubMenuOpen);
		}

		[PresentationTestMethod]
		public void DoesntThrowOnMenuKeypress()
		{
			// Issue #3459
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var item = CreateMockMenuItem(true, parent: menu);
			var e = new KeyEventArgs { Key = Key.Tab, Source = menu };

			target.KeyDown(menu, e);
		}

		[PresentationTestMethod]
		public void DownOpensMenuItemWithSubMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var item = CreateMockMenuItem(true, true);
			var e = new KeyEventArgs { Key = Key.Down, Source = item };

			target.KeyDown(item, e);

			item.Calls.VerifyCalled("Open");
			item.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.First, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void DownSelectsFirstItemOfAlreadyOpenedSubmenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var item = CreateMockMenuItem(true, true, true);
			var e = new KeyEventArgs { Key = Key.Down, Source = item };

			target.KeyDown(item, e);

			item.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.First, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void EnterOnItemWithNoSubMenuCausesClick()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var item = CreateMockMenuItem(true, parent: menu);
			var e = new KeyEventArgs { Key = Key.Enter, Source = item };

			target.KeyDown(item, e);

			item.Calls.VerifyCalled("RaiseClick");
			menu.Calls.VerifyCalled("Close");
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void EnterOnItemWithSubMenuOpensSubMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var item = CreateMockMenuItem(true, true, parent: menu);
			var e = new KeyEventArgs { Key = Key.Enter, Source = item };

			target.KeyDown(item, e);

			item.Calls.VerifyCalled("Open");
			item.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.First, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void EscapeClosesParentMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var item = CreateMockMenuItem(true, parent: menu);
			var e = new KeyEventArgs { Key = Key.Escape, Source = item };

			target.KeyDown(item, e);

			menu.Calls.VerifyCalled("Close");
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void LeftSelectsPreviousMenuItem()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			menu.MoveSelectionResult = true;
			var item = CreateMockMenuItem(true, parent: menu);
			var e = new KeyEventArgs { Key = Key.Left, Source = item };

			target.KeyDown(item, e);

			menu.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.Left, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerEnteredOpensItemWhenOldItemIsOpen()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var item = CreateMockMenuItem(true, true, true, menu);
			var nextItem = CreateMockMenuItem(true, true, parent: menu);
			var e = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, nextItem);

			menu.SelectedItem = item;

			target.PointerEntered(nextItem, e);

			item.Calls.VerifyCalled("Close");
			CornerstoneTest.Same(nextItem, menu.SelectedItem);
			nextItem.Calls.VerifyCalled("Open");
			nextItem.Calls.VerifyNotCalled("MoveSelection");
			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerExitedDeselectsItemWhenMenuNotOpen()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var item = CreateMockMenuItem(true, parent: menu);
			var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item);

			menu.SelectedItem = item;
			target.PointerExited(item, e);

			CornerstoneTest.IsNull(menu.SelectedItem);
			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void PointerExitedDoesntDeselectItemWhenMenuOpen()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			var item = CreateMockMenuItem(true, parent: menu);
			var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item);

			menu.IsOpen = true;
			menu.SelectedItem = item;
			target.PointerExited(item, e);

			CornerstoneTest.Same(item, menu.SelectedItem);
			CornerstoneTest.IsFalse(e.Handled);
		}

		[PresentationTestMethod]
		public void RightSelectsNextMenuItem()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var menu = new StubMenu();
			menu.MoveSelectionResult = true;
			var item = CreateMockMenuItem(true, parent: menu);
			var e = new KeyEventArgs { Key = Key.Right, Source = item };

			target.KeyDown(item, e);

			menu.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.Right, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		[PresentationTestMethod]
		public void UpOpensMenuItemWithSubMenu()
		{
			var target = new DefaultMenuInteractionHandler(false);
			var item = CreateMockMenuItem(true, true);
			var e = new KeyEventArgs { Key = Key.Up, Source = item };

			target.KeyDown(item, e);

			item.Calls.VerifyCalled("Open");
			item.Calls.VerifyLastPrefix("MoveSelection", NavigationDirection.First, true);
			CornerstoneTest.IsTrue(e.Handled);
		}

		#endregion

		#region Classes

		private class MenuItemVM : INotifyPropertyChanged
		{
			#region Properties

			public bool IsChecked
			{
				get;
				set
				{
					field = value;
					OnPropertyChanged();
				}
			}

			public bool IsSubMenuOpen
			{
				get;
				set
				{
					field = value;
					OnPropertyChanged();
				}
			}

			#endregion

			#region Methods

			protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
			{
				PropertyChanged?.Invoke(this, new(propertyName));
			}

			#endregion

			#region Events

			public event PropertyChangedEventHandler PropertyChanged;

			#endregion
		}

		#endregion
	}

	private class FakePointer : IPointer
	{
		#region Properties

		public IInputElement Captured { get; set; }
		public int Id { get; } = Pointer.GetNextFreeId();
		public bool IsPrimary => true;
		public PointerType Type => PointerType.Mouse;

		#endregion

		#region Methods

		public void Capture(IInputElement control)
		{
			Captured = control;
		}

		#endregion
	}

	private class TestTimer
	{
		#region Fields

		private Action _action;

		#endregion

		#region Properties

		public bool ActionIsQueued => _action != null;

		#endregion

		#region Methods

		public void Pulse()
		{
			CornerstoneTest.IsNotNull(_action);
			_action();
			_action = null;
		}

		public void RunOnce(Action action, TimeSpan timeSpan)
		{
			if (_action != null)
			{
				throw new NotSupportedException("Action already set.");
			}

			_action = action;
		}

		#endregion
	}

	#endregion
}