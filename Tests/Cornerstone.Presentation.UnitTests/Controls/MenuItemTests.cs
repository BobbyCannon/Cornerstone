#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Controls.Utils;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class MenuItemTests : ScopedTestBase
{
	#region Fields

	private StubWindowImpl _popupImpl;

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void HeaderAndItemsSourceCanBeBoundInItemContainerTheme()
	{
		using var app = Application();
		var items = new[]
		{
			new MenuViewModel("Foo")
			{
				Children = new[]
				{
					new MenuViewModel("FooChild")
				}
			},
			new MenuViewModel("Bar")
		};

		var target = new Menu
		{
			ItemsSource = items,
			ItemContainerTheme = new ControlTheme(typeof(MenuItem))
			{
				Setters =
				{
					new Setter(MenuItem.HeaderProperty, new Binding("Header")),
					new Setter(MenuItem.ItemsSourceProperty, new Binding("Children"))
				}
			}
		};

		var root = new TestRoot(true, target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		var children = target.GetRealizedContainers().Cast<MenuItem>().ToList();
		CornerstoneTest.AreEqual(2, children.Count);
		CornerstoneTest.AreEqual("Foo", children[0].Header);
		CornerstoneTest.AreEqual("Bar", children[1].Header);
		CornerstoneTest.Same(items[0].Children, children[0].ItemsSource);
	}

	[PresentationTestMethod]
	public void HeaderAndItemsSourceCanBeBoundInStyle()
	{
		using var app = Application();
		var items = new[]
		{
			new MenuViewModel("Foo")
			{
				Children = new[]
				{
					new MenuViewModel("FooChild")
				}
			},
			new MenuViewModel("Bar")
		};

		var target = new Menu
		{
			ItemsSource = items,
			Styles =
			{
				new Style(x => x.OfType<MenuItem>())
				{
					Setters =
					{
						new Setter(MenuItem.HeaderProperty, new Binding("Header")),
						new Setter(MenuItem.ItemsSourceProperty, new Binding("Children"))
					}
				}
			}
		};

		var root = new TestRoot(true, target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		var children = target.GetRealizedContainers().Cast<MenuItem>().ToList();
		CornerstoneTest.AreEqual(2, children.Count);
		CornerstoneTest.AreEqual("Foo", children[0].Header);
		CornerstoneTest.AreEqual("Bar", children[1].Header);
		CornerstoneTest.Same(items[0].Children, children[0].ItemsSource);
	}

	[PresentationTestMethod]
	public void HeaderOfMinusShouldApplySeparatorPseudoclass()
	{
		var target = new MenuItem { Header = "-" };

		CornerstoneTest.IsTrue(target.Classes.Contains(":separator"));
	}

	[PresentationTestMethod]
	public void MenuItemCommandParameterDoesNotChangeWhileExecution()
	{
		var target = new MenuItem();
		object lastParamenter = "A";
		var generator = new Random();
		var onlyOnce = false;
		var command = new TestCommand(parameter =>
			{
				if (!onlyOnce)
				{
					onlyOnce = true;
					target.CommandParameter = generator.Next();
				}
				lastParamenter = parameter;
				return true;
			},
			parameter => { CornerstoneTest.AreEqual(lastParamenter, parameter); });
		target.CommandParameter = lastParamenter;
		target.Command = command;
		var root = new TestRoot { Child = target };

		(target as IClickableControl).RaiseClick();
	}

	[PresentationTestMethod]
	public void MenuItemDoesNotInvokeCanExecuteWhenContextMenuClosed()
	{
		using (Application())
		{
			var canExecuteCallCount = 0;
			var command = new TestCommand(_ =>
			{
				canExecuteCallCount++;
				return true;
			});
			var target = new MenuItem();
			var contextMenu = new ContextMenu { Items = { target } };
			var window = new Window { Content = new Panel { ContextMenu = contextMenu } };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

			CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);
			target.Command = command;
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			target.CommandParameter = false;
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			command.RaiseCanExecuteChanged();
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			contextMenu.Open();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);
			CornerstoneTest.AreEqual(3, canExecuteCallCount); // 3 because popup is changing logical child and moreover we need to invalidate again after the item is attached to the visual tree

			command.RaiseCanExecuteChanged();
			CornerstoneTest.AreEqual(4, canExecuteCallCount);

			target.CommandParameter = true;
			CornerstoneTest.AreEqual(5, canExecuteCallCount);
		}
	}

	[PresentationTestMethod]
	public void MenuItemDoesNotInvokeCanExecuteWhenMenuFlyoutClosed()
	{
		using (Application())
		{
			var canExecuteCallCount = 0;
			var command = new TestCommand(_ =>
			{
				canExecuteCallCount++;
				return true;
			});
			var target = new MenuItem();
			var flyout = new MenuFlyout { Items = { target } };
			var button = new Button { Flyout = flyout };
			var window = new Window { Content = button };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);
			CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);
			target.Command = command;
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			target.CommandParameter = false;
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			command.RaiseCanExecuteChanged();
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			flyout.ShowAt(button);
			Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);
			CornerstoneTest.AreEqual(2, canExecuteCallCount); // 2 because we need to invalidate after the item is attached to the visual tree

			command.RaiseCanExecuteChanged();
			CornerstoneTest.AreEqual(3, canExecuteCallCount);

			target.CommandParameter = true;
			CornerstoneTest.AreEqual(4, canExecuteCallCount);
		}
	}

	[PresentationTestMethod]
	public void MenuItemDoesNotInvokeCanExecuteWhenParentMenuItemClosed()
	{
		using (Application())
		{
			var canExecuteCallCount = 0;
			var command = new TestCommand(_ =>
			{
				canExecuteCallCount++;
				return true;
			});
			var target = new MenuItem();
			var parentMenuItem = new MenuItem { Items = { target } };
			var contextMenu = new ContextMenu { Items = { parentMenuItem } };
			var window = new Window { Content = new Panel { ContextMenu = contextMenu } };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			contextMenu.Open();

			Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

			CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);
			target.Command = command;
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			target.CommandParameter = false;
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			command.RaiseCanExecuteChanged();
			CornerstoneTest.AreEqual(0, canExecuteCallCount);

			try
			{
				parentMenuItem.IsSubMenuOpen = true;
			}
			catch (InvalidOperationException)
			{
				//popup host creation failed exception
			}
			CornerstoneTest.AreEqual(2, canExecuteCallCount);

			command.RaiseCanExecuteChanged();
			CornerstoneTest.AreEqual(3, canExecuteCallCount);

			target.CommandParameter = true;
			CornerstoneTest.AreEqual(4, canExecuteCallCount);
		}
	}

	[PresentationTestMethod]
	public void MenuItemDoesNotSubscribeToCommandCanExecuteChangedUntilAddedToLogicalTree()
	{
		var command = new TestCommand();
		var target = new MenuItem
		{
			Command = command
		};

		CornerstoneTest.AreEqual(0, command.SubscriptionCount);
	}

	[PresentationTestMethod]
	public void MenuItemInvokesCanExecuteWhenAddedToLogicalTreeAndCommandParameterChanged()
	{
		var command = new TestCommand(p => p is bool value && value);
		var target = new MenuItem { Command = command };
		var root = new TestRoot { Child = target };

		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		target.CommandParameter = true;
		CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);

		target.CommandParameter = false;
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void MenuItemIsDisabledWhenBoundCommandDoesntExist()
	{
		var target = new MenuItem
		{
			[!MenuItem.CommandProperty] = new Binding("Command")
		};

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void MenuItemIsDisabledWhenBoundCommandIsRemoved()
	{
		var viewModel = new
		{
			Command = new TestCommand(true)
		};

		var target = new MenuItem
		{
			DataContext = viewModel,
			[!MenuItem.CommandProperty] = new Binding("Command")
		};

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);

		target.DataContext = null;

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void MenuItemIsDisabledWhenCommandIsEnabledButIsEnabledIsFalse()
	{
		var command = new TestCommand(true);
		var target = new MenuItem
		{
			IsEnabled = false,
			Command = command
		};

		var root = new TestRoot { Child = target };

		CornerstoneTest.IsFalse(((IInputElement) target).IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void MenuItemIsDisabledWhenDisabledBoundCommandIsAdded()
	{
		var viewModel = new
		{
			Command = new TestCommand(false)
		};

		var target = new MenuItem
		{
			DataContext = new object(),
			[!MenuItem.CommandProperty] = new Binding("Command")
		};

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);

		target.DataContext = viewModel;

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void MenuItemIsEnabledWhenAddedToLogicalTreeAndBoundCommandIsAdded()
	{
		var viewModel = new
		{
			Command = new TestCommand(true)
		};

		var target = new MenuItem
		{
			DataContext = new object(),
			[!MenuItem.CommandProperty] = new Binding("Command")
		};
		var root = new TestRoot { Child = target };

		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);

		target.DataContext = viewModel;

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void MenuItemSubscribesToCommandCanExecuteChangedWhenAddedToLogicalTree()
	{
		var command = new TestCommand();
		var target = new MenuItem { Command = command };
		var root = new TestRoot { Child = target };

		CornerstoneTest.AreEqual(1, command.SubscriptionCount);
	}

	[PresentationTestMethod]
	public void MenuItemTemplateShouldBeAppliedToTopLevelMenuItemHeader()
	{
		using var app = Application();

		var items = new[]
		{
			new MenuViewModel("Foo"),
			new MenuViewModel("Bar")
		};

		var itemTemplate = new FuncDataTemplate<MenuViewModel>((x, _) =>
			new TextBlock { Text = x.Header });

		var menu = new Menu
		{
			ItemTemplate = itemTemplate,
			ItemsSource = items
		};

		var window = new Window { Content = menu };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		var panel = CornerstoneTest.IsType<StackPanel>(menu.Presenter!.Panel);
		CornerstoneTest.AreEqual(2, panel.Children.Count);

		for (var i = 0; i < panel.Children.Count; i++)
		{
			var menuItem = CornerstoneTest.IsType<MenuItem>(panel.Children[i]);

			CornerstoneTest.AreEqual(items[i], menuItem.Header);
			CornerstoneTest.Same(itemTemplate, menuItem.HeaderTemplate);

			var headerPresenter = CornerstoneTest.IsType<ContentPresenter>(menuItem.HeaderPresenter);
			CornerstoneTest.Same(itemTemplate, headerPresenter.ContentTemplate);

			var headerControl = CornerstoneTest.IsType<TextBlock>(headerPresenter.Child);
			CornerstoneTest.AreEqual(items[i].Header, headerControl.Text);
		}
	}

	[PresentationTestMethod]
	public void MenuItemUnsubscribesFromCommandCanExecuteChangedWhenRemovedFromLogicalTree()
	{
		var command = new TestCommand();
		var target = new MenuItem { Command = command };
		var root = new TestRoot { Child = target };

		root.Child = null;
		CornerstoneTest.AreEqual(0, command.SubscriptionCount);
	}

	[PresentationTestMethod]
	public void MenuItemWithStyledCommandBindingShouldBeEnabledWithChildMissingCommand()
	{
		using var app = Application();

		var viewModel = new MenuViewModel("Parent")
		{
			Children = [new MenuViewModel("Child")]
		};

		var contextMenu = new ContextMenu
		{
			ItemsSource = new[] { viewModel },
			Styles =
			{
				new Style(x => x.OfType<MenuItem>())
				{
					Setters =
					{
						new Setter(MenuItem.HeaderProperty, new Binding("Header")),
						new Setter(MenuItem.ItemsSourceProperty, new Binding("Children")),
						new Setter(MenuItem.CommandProperty, new Binding("Command"))
					}
				}
			}
		};

		var window = new Window { ContextMenu = contextMenu };
		window.Show();
		contextMenu.Open();

		var parentMenuItem = CornerstoneTest.IsType<MenuItem>(contextMenu.ContainerFromIndex(0));

		CornerstoneTest.Same(parentMenuItem.DataContext, viewModel);
		CornerstoneTest.Same(parentMenuItem.ItemsSource, viewModel.Children);
		CornerstoneTest.IsTrue(parentMenuItem.IsEnabled);
		CornerstoneTest.IsTrue(parentMenuItem.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void RadioMenuGroupCanBeChangedInRuntime()
	{
		using var app = Application();

		MenuItem menuItem1, menuItem2, menuItem3;

		var menu = new Menu
		{
			Items =
			{
				(menuItem1 = new MenuItem
				{
					GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem2 = new MenuItem
				{
					GroupName = "A", IsChecked = true, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem3 = new MenuItem
				{
					GroupName = null, IsChecked = false, ToggleType = MenuItemToggleType.Radio
				})
			}
		};

		var window = new Window { Content = menu };
		window.Show();

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsTrue(menuItem2.IsChecked);
		CornerstoneTest.IsFalse(menuItem3.IsChecked);

		menuItem3.GroupName = "A";
		menuItem3.IsChecked = true;

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsTrue(menuItem3.IsChecked);

		menuItem3.GroupName = null;
		menuItem1.IsChecked = true;

		CornerstoneTest.IsTrue(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsTrue(menuItem3.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioMenuItemEmptyGroupNameNotInfluenceOtherGroups()
	{
		using var app = Application();

		MenuItem menuItem1, menuItem2, menuItem3, menuItem4;

		var menu = new Menu
		{
			Items =
			{
				(menuItem1 = new MenuItem
				{
					GroupName = "A", IsChecked = true, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem2 = new MenuItem
				{
					GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem3 = new MenuItem
				{
					GroupName = null, IsChecked = false, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem4 = new MenuItem
				{
					GroupName = null, IsChecked = true, ToggleType = MenuItemToggleType.Radio
				})
			}
		};

		var window = new Window { Content = menu };
		window.Show();

		CornerstoneTest.IsTrue(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsFalse(menuItem3.IsChecked);
		CornerstoneTest.IsTrue(menuItem4.IsChecked);

		menuItem3.IsChecked = true;

		CornerstoneTest.IsTrue(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsTrue(menuItem3.IsChecked);
		CornerstoneTest.IsFalse(menuItem4.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioMenuItemInSameGroupButSubmenuIsChecked()
	{
		using var app = Application();

		MenuItem menuItem1, menuItem2, menuItem3, menuItem4;

		var menu = new Menu
		{
			Items =
			{
				(menuItem1 = new MenuItem
				{
					GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem2 = new MenuItem
				{
					GroupName = "A", IsChecked = true, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem3 = new MenuItem
				{
					GroupName = "A",
					IsChecked = false,
					ToggleType = MenuItemToggleType.Radio,
					Items =
					{
						(menuItem4 = new MenuItem
						{
							GroupName = "A",
							IsChecked = false,
							ToggleType = MenuItemToggleType.Radio
						})
					}
				})
			}
		};

		var window = new Window { Content = menu };
		window.Show();

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsTrue(menuItem2.IsChecked);
		CornerstoneTest.IsFalse(menuItem3.IsChecked);
		CornerstoneTest.IsFalse(menuItem4.IsChecked);

		menuItem4.IsChecked = true;

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsTrue(menuItem3.IsChecked);
		CornerstoneTest.IsTrue(menuItem4.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioMenuItemInSameGroupButSubmenuIsUnchecked()
	{
		using var app = Application();

		MenuItem menuItem1, menuItem2, menuItem3, menuItem4;

		var menu = new Menu
		{
			Items =
			{
				(menuItem1 = new MenuItem
				{
					GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem2 = new MenuItem
				{
					GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem3 = new MenuItem
				{
					GroupName = "A",
					IsChecked = true,
					ToggleType = MenuItemToggleType.Radio,
					Items =
					{
						(menuItem4 = new MenuItem
						{
							GroupName = "A",
							IsChecked = true,
							ToggleType = MenuItemToggleType.Radio
						})
					}
				})
			}
		};

		var window = new Window { Content = menu };
		window.Show();

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsTrue(menuItem3.IsChecked);
		CornerstoneTest.IsTrue(menuItem4.IsChecked);

		menuItem2.IsChecked = true;

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsTrue(menuItem2.IsChecked);
		CornerstoneTest.IsFalse(menuItem3.IsChecked);
		CornerstoneTest.IsFalse(menuItem4.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioMenuItemInSameGroupInMenuFlyoutIsUnchecked()
	{
		using var app = Application();

		var menuItem1 = new MenuItem
		{
			GroupName = "A",
			IsChecked = true,
			StaysOpenOnClick = true,
			ToggleType = MenuItemToggleType.Radio
		};
		var menuItem2 = new MenuItem
		{
			GroupName = "A",
			IsChecked = false,
			StaysOpenOnClick = true,
			ToggleType = MenuItemToggleType.Radio
		};

		var flyout = new MenuFlyout
		{
			Items =
			{
				menuItem1,
				menuItem2
			}
		};
		var button = new Button { ContextFlyout = flyout };
		var window = new Window { Content = button };

		window.Show();
		flyout.ShowAt(button);
		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		menuItem2.IsChecked = true;

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsTrue(menuItem2.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioMenuItemInSameGroupIsUnchecked()
	{
		using var app = Application();

		MenuItem menuItem1, menuItem2, menuItem3;

		var menu = new Menu
		{
			Items =
			{
				(menuItem1 = new MenuItem
				{
					GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem2 = new MenuItem
				{
					GroupName = "A", IsChecked = true, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem3 = new MenuItem
				{
					GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
				})
			}
		};

		var window = new Window { Content = menu };
		window.Show();

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsTrue(menuItem2.IsChecked);
		CornerstoneTest.IsFalse(menuItem3.IsChecked);

		menuItem3.IsChecked = true;

		CornerstoneTest.IsFalse(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsTrue(menuItem3.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioMenusWithEmptyGroupOnDifferentLevelsCanBeCheckedSimultaneously()
	{
		using var app = Application();

		MenuItem menuItem1, menuItem2, menuItem3, menuItem4;

		var menu = new Menu
		{
			Items =
			{
				(menuItem1 = new MenuItem
				{
					GroupName = null, IsChecked = true, ToggleType = MenuItemToggleType.Radio
				}),
				(menuItem2 = new MenuItem
				{
					GroupName = null,
					IsChecked = false,
					ToggleType = MenuItemToggleType.Radio,
					Items =
					{
						(menuItem3 = new MenuItem
						{
							GroupName = null,
							IsChecked = false,
							ToggleType = MenuItemToggleType.Radio
						}),
						(menuItem4 = new MenuItem
						{
							GroupName = null,
							IsChecked = false,
							ToggleType = MenuItemToggleType.Radio
						})
					}
				})
			}
		};

		var window = new Window { Content = menu };
		window.Show();

		CornerstoneTest.IsTrue(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsFalse(menuItem3.IsChecked);
		CornerstoneTest.IsFalse(menuItem4.IsChecked);

		menuItem3.IsChecked = true;

		CornerstoneTest.IsTrue(menuItem1.IsChecked);
		CornerstoneTest.IsFalse(menuItem2.IsChecked);
		CornerstoneTest.IsTrue(menuItem3.IsChecked);
		CornerstoneTest.IsFalse(menuItem4.IsChecked);
	}

	[PresentationTestMethod]
	public void SeparatorItemShouldSetFocusableFalse()
	{
		var target = new MenuItem { Header = "-" };

		CornerstoneTest.IsFalse(target.Focusable);
	}

	[PresentationTestMethod]
	public void TemplatedParentShouldNotBeAppliedToSubmenus()
	{
		using (Application())
		{
			MenuItem topLevelMenu;
			MenuItem childMenu1;
			MenuItem childMenu2;
			var menu = new Menu
			{
				Items =
				{
					(topLevelMenu = new MenuItem
					{
						Header = "Foo",
						Items =
						{
							(childMenu1 = new MenuItem { Header = "Bar" }),
							(childMenu2 = new MenuItem { Header = "Baz" })
						}
					})
				}
			};

			var window = new Window { Content = menu };
			window.Show();
			window.LayoutManager.ExecuteInitialLayoutPass();

			topLevelMenu.IsSubMenuOpen = true;

			CornerstoneTest.IsTrue(childMenu1.IsAttachedToVisualTree);
			CornerstoneTest.IsNull(childMenu1.TemplatedParent);
			CornerstoneTest.IsNull(childMenu2.TemplatedParent);

			topLevelMenu.IsSubMenuOpen = false;
			topLevelMenu.IsSubMenuOpen = true;

			CornerstoneTest.IsNull(childMenu1.TemplatedParent);
			CornerstoneTest.IsNull(childMenu2.TemplatedParent);
		}
	}

	private IDisposable Application()
	{
		var screen = new PixelRect(new PixelPoint(), new PixelSize(100, 100));
		var screenImpl = new StubScreenImpl(new MockScreen(1, screen, screen, true));

		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		_popupImpl = MockWindowingPlatform.CreatePopupMock(windowImpl);
		windowImpl.CreatePopupHandler = () => _popupImpl;
		windowImpl.Screens = screenImpl;

		var services = TestServices.StyledWindow.With(
			inputManager: new InputManager(),
			windowImpl: windowImpl,
			windowingPlatform: new MockWindowingPlatform(() => windowImpl, x => _popupImpl));

		return UnitTestApplication.Start(services);
	}

	#endregion

	#region Records

	private record MenuViewModel(string Header)
	{
		#region Properties

		public IList<MenuViewModel> Children { get; set; } = [];

		#endregion
	}

	#endregion
}