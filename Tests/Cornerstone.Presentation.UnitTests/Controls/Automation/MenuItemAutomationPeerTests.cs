#region References

using System;
using System.Collections.Generic;
using System.Windows.Input;
using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class MenuItemAutomationPeerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ToggleChecksButDoesNotUncheckRadioItem()
	{
		var menuItem = new MenuItem { ToggleType = MenuItemToggleType.Radio };
		var provider = GetProvider(menuItem);

		provider.Toggle();
		CornerstoneTest.IsTrue(menuItem.IsChecked);

		provider.Toggle();
		CornerstoneTest.IsTrue(menuItem.IsChecked);
	}

	[PresentationTestMethod]
	public void ToggleFlipsCheckBoxItem()
	{
		var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
		var provider = GetProvider(menuItem);

		provider.Toggle();
		CornerstoneTest.IsTrue(menuItem.IsChecked);

		provider.Toggle();
		CornerstoneTest.IsFalse(menuItem.IsChecked);
	}

	[PresentationTestMethod]
	[DataRow(MenuItemToggleType.CheckBox)]
	[DataRow(MenuItemToggleType.Radio)]
	public void ToggleProviderIsExposedForCheckableItems(MenuItemToggleType toggleType)
	{
		var peer = ControlAutomationPeer.CreatePeerForElement(new MenuItem { ToggleType = toggleType });

		CornerstoneTest.IsNotNull(peer.GetProvider<IToggleProvider>());
	}

	[PresentationTestMethod]
	public void ToggleProviderIsNotExposedWhenToggleTypeNone()
	{
		var peer = ControlAutomationPeer.CreatePeerForElement(new MenuItem());

		CornerstoneTest.IsNull(peer.GetProvider<IToggleProvider>());
	}

	[PresentationTestMethod]
	public void ToggleRaisesToggleStatePropertyChanged()
	{
		var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
		var peer = ControlAutomationPeer.CreatePeerForElement(menuItem);
		var provider = GetProvider(menuItem);

		var raised = 0;
		peer.PropertyChanged += (_, e) =>
		{
			if (e.Property == TogglePatternIdentifiers.ToggleStateProperty)
			{
				CornerstoneTest.AreEqual(ToggleState.Off, e.OldValue);
				CornerstoneTest.AreEqual(ToggleState.On, e.NewValue);
				raised++;
			}
		};

		provider.Toggle();

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ToggleStateReflectsIsChecked()
	{
		var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
		var provider = GetProvider(menuItem);

		CornerstoneTest.AreEqual(ToggleState.Off, provider.ToggleState);
		menuItem.IsChecked = true;
		CornerstoneTest.AreEqual(ToggleState.On, provider.ToggleState);
	}

	[PresentationTestMethod]
	public void Leaf_Item_Exposes_Invoke_But_Not_ExpandCollapse()
	{
		var peer = ControlAutomationPeer.CreatePeerForElement(new MenuItem());

		CornerstoneTest.IsNotNull(peer.GetProvider<IInvokeProvider>());
		CornerstoneTest.IsNull(peer.GetProvider<IExpandCollapseProvider>());
	}

	[PresentationTestMethod]
	public void Submenu_Item_Exposes_ExpandCollapse_But_Not_Invoke()
	{
		var peer = ControlAutomationPeer.CreatePeerForElement(CreateSubmenuItem());

		CornerstoneTest.IsNotNull(peer.GetProvider<IExpandCollapseProvider>());
		CornerstoneTest.IsNull(peer.GetProvider<IInvokeProvider>());
	}

	[PresentationTestMethod]
	public void Providers_Follow_Items_Being_Added_And_Removed()
	{
		var menuItem = new MenuItem();
		var peer = ControlAutomationPeer.CreatePeerForElement(menuItem);

		menuItem.Items.Add(new MenuItem());

		CornerstoneTest.IsNotNull(peer.GetProvider<IExpandCollapseProvider>());
		CornerstoneTest.IsNull(peer.GetProvider<IInvokeProvider>());

		menuItem.Items.Clear();

		CornerstoneTest.IsNotNull(peer.GetProvider<IInvokeProvider>());
		CornerstoneTest.IsNull(peer.GetProvider<IExpandCollapseProvider>());
	}

	[PresentationTestMethod]
	public void ExpandCollapseState_Is_LeafNode_For_Leaf_Item()
	{
		var peer = (IExpandCollapseProvider) ControlAutomationPeer.CreatePeerForElement(new MenuItem());

		CornerstoneTest.AreEqual(ExpandCollapseState.LeafNode, peer.ExpandCollapseState);
		CornerstoneTest.IsFalse(peer.ShowsMenu);
	}

	[PresentationTestMethod]
	public void ExpandCollapseState_Reflects_IsSubMenuOpen()
	{
		var menuItem = CreateSubmenuItem();
		var provider = GetExpandCollapseProvider(menuItem);

		CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
		CornerstoneTest.IsTrue(provider.ShowsMenu);

		menuItem.IsSubMenuOpen = true;
		CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, provider.ExpandCollapseState);

		menuItem.IsSubMenuOpen = false;
		CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
	}

	[PresentationTestMethod]
	public void Expand_And_Collapse_Set_IsSubMenuOpen()
	{
		var menuItem = CreateSubmenuItem();
		var provider = GetExpandCollapseProvider(menuItem);

		provider.Expand();
		CornerstoneTest.IsTrue(menuItem.IsSubMenuOpen);

		provider.Collapse();
		CornerstoneTest.IsFalse(menuItem.IsSubMenuOpen);
	}

	[PresentationTestMethod]
	public void ExpandCollapse_Raises_PropertyChanged()
	{
		var menuItem = CreateSubmenuItem();
		var peer = ControlAutomationPeer.CreatePeerForElement(menuItem);
		var provider = GetExpandCollapseProvider(menuItem);
		var raised = new List<AutomationPropertyChangedEventArgs>();

		peer.PropertyChanged += (_, e) =>
		{
			if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
				raised.Add(e);
		};

		provider.Expand();
		provider.Collapse();

		CornerstoneTest.AreEqual(2, raised.Count);
		CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, raised[0].OldValue);
		CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, raised[0].NewValue);
		CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, raised[1].OldValue);
		CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, raised[1].NewValue);
	}

	[PresentationTestMethod]
	public void Expand_Throws_For_Leaf_Item()
	{
		var peer = (IExpandCollapseProvider) ControlAutomationPeer.CreatePeerForElement(new MenuItem());

		Assert.Throws<InvalidOperationException>(() => peer.Expand());
	}

	[PresentationTestMethod]
	public void Expand_Throws_When_Disabled()
	{
		var menuItem = CreateSubmenuItem();
		menuItem.IsEnabled = false;
		var provider = GetExpandCollapseProvider(menuItem);

		Assert.Throws<ElementNotEnabledException>(() => provider.Expand());
		CornerstoneTest.IsFalse(menuItem.IsSubMenuOpen);
	}

	[PresentationTestMethod]
	public void Collapse_Throws_For_Leaf_Item()
	{
		var peer = (IExpandCollapseProvider) ControlAutomationPeer.CreatePeerForElement(new MenuItem());

		Assert.Throws<InvalidOperationException>(() => peer.Collapse());
	}

	[PresentationTestMethod]
	public void Collapse_Throws_When_Disabled()
	{
		var menuItem = CreateSubmenuItem();
		menuItem.IsEnabled = false;
		menuItem.IsSubMenuOpen = true;
		var provider = GetExpandCollapseProvider(menuItem);

		Assert.Throws<ElementNotEnabledException>(() => provider.Collapse());
		CornerstoneTest.IsTrue(menuItem.IsSubMenuOpen);
	}

	[PresentationTestMethod]
	public void Invoke_Raises_Click()
	{
		var menuItem = new MenuItem();
		var provider = GetInvokeProvider(menuItem);
		var clicked = 0;

		menuItem.Click += (_, _) => clicked++;
		provider.Invoke();

		CornerstoneTest.AreEqual(1, clicked);
	}

	[PresentationTestMethod]
	public void Invoke_Executes_Command()
	{
		var executed = 0;
		var menuItem = new MenuItem
		{
			Command = new TestCommand(p => executed += (int) p),
			CommandParameter = 5,
		};
		var provider = GetInvokeProvider(menuItem);

		provider.Invoke();

		CornerstoneTest.AreEqual(5, executed);
	}

	[PresentationTestMethod]
	public void Invoke_Throws_When_Disabled()
	{
		var menuItem = new MenuItem { IsEnabled = false };
		var provider = GetInvokeProvider(menuItem);
		var clicked = 0;

		menuItem.Click += (_, _) => clicked++;

		Assert.Throws<ElementNotEnabledException>(() => provider.Invoke());
		CornerstoneTest.AreEqual(0, clicked);
	}

	[PresentationTestMethod]
	public void Invoke_Throws_When_Command_Cannot_Execute()
	{
		var executed = 0;
		var menuItem = new MenuItem { Command = new TestCommand(_ => executed++, false) };
		var provider = GetInvokeProvider(menuItem);

		Assert.Throws<ElementNotEnabledException>(() => provider.Invoke());
		CornerstoneTest.AreEqual(0, executed);
	}

	[PresentationTestMethod]
	public void Expand_Opens_Top_Level_Menu_Item()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var child = new MenuItem { Header = "Child" };
		var topLevel = new MenuItem { Header = "Top", Items = { child } };
		var menu = new Menu { Items = { topLevel } };
		CreateWindow(menu);

		GetExpandCollapseProvider(topLevel).Expand();

		CornerstoneTest.IsTrue(menu.IsOpen);
		CornerstoneTest.IsTrue(topLevel.IsSubMenuOpen);
		CornerstoneTest.IsTrue(child.IsAttachedToVisualTree);
	}

	[PresentationTestMethod]
	public void Collapse_Closes_Menu_For_Top_Level_Menu_Item()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var topLevel = new MenuItem { Header = "Top", Items = { new MenuItem { Header = "Child" } } };
		var menu = new Menu { Items = { topLevel } };
		CreateWindow(menu);
		var provider = GetExpandCollapseProvider(topLevel);

		provider.Expand();
		provider.Collapse();

		CornerstoneTest.IsFalse(topLevel.IsSubMenuOpen);
		CornerstoneTest.IsFalse(menu.IsOpen);
	}

	[PresentationTestMethod]
	public void Collapse_Does_Nothing_For_Collapsed_Top_Level_Menu_Item()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var first = new MenuItem { Header = "First", Items = { new MenuItem { Header = "Child" } } };
		var second = new MenuItem { Header = "Second", Items = { new MenuItem { Header = "Child" } } };
		var menu = new Menu { Items = { first, second } };
		CreateWindow(menu);

		GetExpandCollapseProvider(second).Expand();
		GetExpandCollapseProvider(first).Collapse();

		CornerstoneTest.IsTrue(second.IsSubMenuOpen);
		CornerstoneTest.IsTrue(menu.IsOpen);
	}

	[PresentationTestMethod]
	public void Collapse_Leaves_Menu_Open_For_Nested_Menu_Item()
	{
		// The nested submenu opens a popup from within a popup.
		using var app = UnitTestApplication.Start(TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(popupImpl: CreateNestablePopup)));

		var nested = new MenuItem { Header = "Nested", Items = { new MenuItem { Header = "Child" } } };
		var topLevel = new MenuItem { Header = "Top", Items = { nested } };
		var menu = new Menu { Items = { topLevel } };
		CreateWindow(menu);
		var nestedProvider = GetExpandCollapseProvider(nested);

		GetExpandCollapseProvider(topLevel).Expand();
		nestedProvider.Expand();
		nestedProvider.Collapse();

		CornerstoneTest.IsFalse(nested.IsSubMenuOpen);
		CornerstoneTest.IsTrue(topLevel.IsSubMenuOpen);
		CornerstoneTest.IsTrue(menu.IsOpen);
	}

	[PresentationTestMethod]
	public void Invoke_In_Menu_Bubbles_Click_To_Menu()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var child = new MenuItem { Header = "Child" };
		var topLevel = new MenuItem { Header = "Top", Items = { child } };
		var menu = new Menu { Items = { topLevel } };
		CreateWindow(menu);
		var clicked = new List<object>();

		menu.AddHandler(MenuItem.ClickEvent, (_, e) => clicked.Add(e.Source));
		GetExpandCollapseProvider(topLevel).Expand();
		GetInvokeProvider(child).Invoke();

		CornerstoneTest.AreEqual(1, clicked.Count);
		CornerstoneTest.IsTrue(ReferenceEquals(child, clicked[0]));
	}

	[PresentationTestMethod]
	public void Invoke_In_Menu_Closes_Menu()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var child = new MenuItem { Header = "Child" };
		var topLevel = new MenuItem { Header = "Top", Items = { child } };
		var menu = new Menu { Items = { topLevel } };
		CreateWindow(menu);

		GetExpandCollapseProvider(topLevel).Expand();
		CornerstoneTest.IsTrue(menu.IsOpen);

		GetInvokeProvider(child).Invoke();

		CornerstoneTest.IsFalse(topLevel.IsSubMenuOpen);
		CornerstoneTest.IsFalse(menu.IsOpen);
	}

	[PresentationTestMethod]
	public void Invoke_In_Menu_Respects_StaysOpenOnClick()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var child = new MenuItem { Header = "Child", StaysOpenOnClick = true };
		var topLevel = new MenuItem { Header = "Top", Items = { child } };
		var menu = new Menu { Items = { topLevel } };
		CreateWindow(menu);

		GetExpandCollapseProvider(topLevel).Expand();
		GetInvokeProvider(child).Invoke();

		CornerstoneTest.IsTrue(topLevel.IsSubMenuOpen);
		CornerstoneTest.IsTrue(menu.IsOpen);
	}

	[PresentationTestMethod]
	public void Invoke_In_Menu_Toggles_CheckBox_Item()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var child = new MenuItem { Header = "Child", ToggleType = MenuItemToggleType.CheckBox };
		var topLevel = new MenuItem { Header = "Top", Items = { child } };
		CreateWindow(new Menu { Items = { topLevel } });

		GetExpandCollapseProvider(topLevel).Expand();
		GetInvokeProvider(child).Invoke();

		CornerstoneTest.IsTrue(child.IsChecked);
	}

	[PresentationTestMethod]
	public void Invoke_In_ContextMenu_Closes_ContextMenu()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var child = new MenuItem { Header = "Child" };
		var contextMenu = new ContextMenu { Items = { child } };
		var window = new Window { ContextMenu = contextMenu };

		window.Show();
		contextMenu.Open();
		CornerstoneTest.IsTrue(contextMenu.IsOpen);

		GetInvokeProvider(child).Invoke();

		CornerstoneTest.IsFalse(contextMenu.IsOpen);
	}

	[PresentationTestMethod]
	public void Invoke_In_MenuFlyout_Closes_Flyout()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var child = new MenuItem { Header = "Child" };
		var flyout = new MenuFlyout { Items = { child } };
		var button = new Button { ContextFlyout = flyout };
		var window = new Window { Content = button };

		window.Show();
		flyout.ShowAt(button);
		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
		CornerstoneTest.IsTrue(flyout.IsOpen);

		GetInvokeProvider(child).Invoke();

		CornerstoneTest.IsFalse(flyout.IsOpen);
	}

	private static MenuItem CreateSubmenuItem()
	{
		return new MenuItem { Items = { new MenuItem() } };
	}

	private static IPopupImpl CreateNestablePopup(IWindowBaseImpl parent)
	{
		var popup = MockWindowingPlatform.CreatePopupMock(parent);
		popup.CreatePopupHandler = () => CreateNestablePopup(popup);
		return popup;
	}

	private static Window CreateWindow(Menu menu)
	{
		var window = new Window { Content = menu };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();
		return window;
	}

	private static IExpandCollapseProvider GetExpandCollapseProvider(MenuItem menuItem)
	{
		var provider = ControlAutomationPeer.CreatePeerForElement(menuItem).GetProvider<IExpandCollapseProvider>();
		CornerstoneTest.IsNotNull(provider);
		return provider;
	}

	private static IInvokeProvider GetInvokeProvider(MenuItem menuItem)
	{
		var provider = ControlAutomationPeer.CreatePeerForElement(menuItem).GetProvider<IInvokeProvider>();
		CornerstoneTest.IsNotNull(provider);
		return provider;
	}

	private static IToggleProvider GetProvider(MenuItem menuItem)
	{
		var provider = ControlAutomationPeer.CreatePeerForElement(menuItem).GetProvider<IToggleProvider>();
		CornerstoneTest.IsNotNull(provider);
		return provider;
	}

	private sealed class TestCommand : ICommand
	{
		private readonly Action<object> _execute;
		private readonly bool _canExecute;

		public TestCommand(Action<object> execute)
			: this(execute, true)
		{
		}

		public TestCommand(Action<object> execute, bool canExecute)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public event EventHandler CanExecuteChanged
		{
			add { }
			remove { }
		}

		public bool CanExecute(object parameter) => _canExecute;

		public void Execute(object parameter) => _execute(parameter);
	}

	#endregion
}