#region References

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ListBoxTestsSingle : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _mouse = new();
	private readonly MouseTestHelper _pen = new(PointerType.Pen);

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ClickingAnotherItemShouldSelectItWhenSelectionModeToggle()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " },
				SelectionMode = SelectionMode.Single | SelectionMode.Toggle
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);
			target.SelectedIndex = 1;

			_mouse.Click(target.Presenter!.Panel!.Children[0]);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void ClickingItemShouldSelectIt()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);
			_mouse.Click(target.Presenter!.Panel!.Children[0]);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void ClickingItemShouldSelectItWhenSelectionModeToggle()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " },
				SelectionMode = SelectionMode.Single | SelectionMode.Toggle
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);

			_mouse.Click(target.Presenter!.Panel!.Children[0]);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void ClickingSelectedItemShouldDeselectItWhenSelectionModeToggle()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " },
				SelectionMode = SelectionMode.Toggle
			};

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);
			target.SelectedIndex = 0;

			_mouse.Click(target.Presenter!.Panel!.Children[0]);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void ClickingSelectedItemShouldNotDeselectIt()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);
			target.SelectedIndex = 0;

			_mouse.Click(target.Presenter!.Panel!.Children[0]);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void ClickingSelectedItemShouldNotDeselectItWhenSelectionModeToggleAlwaysSelected()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " },
				SelectionMode = SelectionMode.Toggle | SelectionMode.AlwaysSelected
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);
			target.SelectedIndex = 0;

			_mouse.Click(target.Presenter!.Panel!.Children[0]);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void FocusingItemWithTabShouldNotSelectIt()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};

			Prepare(target);

			target.Presenter!.Panel!.Children[0].RaiseEvent(new FocusChangedEventArgs(InputElement.GotFocusEvent)
			{
				NavigationMethod = NavigationMethod.Tab
			});

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void PenLeftPressItemShouldNotSelectIt()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};
			Prepare(target);
			_pen.Down(target.Presenter!.Panel!.Children[0]);

			CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void PenRightPressItemShouldSelectIt()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};
			Prepare(target);
			_pen.Down(target.Presenter!.Panel!.Children[0], MouseButton.Right);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(PointerType.Mouse)]
	[DataRow(PointerType.Pen)]
	public void PointerRightClickShouldSelectItemAndOpenContext(PointerType type)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " },
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new Border { Height = 10 })
			};
			target.GestureRecognizers.Add(new ScrollGestureRecognizer
			{
				CanVerticallyScroll = true, ScrollStartDistance = 50
			});
			Prepare(target);

			var contextRaised = false;
			target.AddHandler(InputElement.ContextRequestedEvent, (sender, args) =>
			{
				contextRaised = true;
				args.Handled = true;
			});

			var pointer = type == PointerType.Mouse ? _mouse : _pen;
			pointer.Click(target.Presenter!.Panel!.Children[0], MouseButton.Right, new Point(5, 5));

			CornerstoneTest.IsTrue(contextRaised);
			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void PressingSpaceOnFocusedItemWithCtrlPressedShouldSelectIt()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};
			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			Prepare(target);

			target.Presenter!.Panel!.Children[0].RaiseEvent(new FocusChangedEventArgs(InputElement.GotFocusEvent)
			{
				NavigationMethod = NavigationMethod.Directional,
				KeyModifiers = KeyModifiers.Control
			});

			target.Presenter.Panel.Children[0].RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Space,
				KeyModifiers = KeyModifiers.Control
			});

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void SelectedItemShouldNotCauseStackOverflow()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var viewModel = new TestStackOverflowViewModel { Items = ["foo", "bar", "baz"] };

			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				DataContext = viewModel,
				ItemsSource = viewModel.Items
			};

			target.Bind(ListBox.SelectedItemProperty,
				new Binding("SelectedItem") { Mode = BindingMode.TwoWay });

			CornerstoneTest.AreEqual(0, viewModel.SetterInvokedCount);

			// In Issue #855, a Stackoverflow occurred here.
			target.SelectedItem = viewModel.Items[2];

			CornerstoneTest.AreEqual(viewModel.Items[1], target.SelectedItem);
			CornerstoneTest.AreEqual(1, viewModel.SetterInvokedCount);
		}
	}

	[PresentationTestMethod]
	public void SettingItemIsSelectedSetsListBoxSelection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};

			Prepare(target);

			((ListBoxItem) target.GetLogicalChildren().ElementAt(1)).IsSelected = true;

			CornerstoneTest.AreEqual("Bar", target.SelectedItem);
			CornerstoneTest.AreEqual(1, target.SelectedIndex);
		}
	}

	private Control CreateListBoxTemplate(TemplatedControl parent, INameScope scope)
	{
		return new ScrollViewer
		{
			Template = new FuncControlTemplate(CreateScrollViewerTemplate),
			Content = new ItemsPresenter
			{
				Name = "PART_ItemsPresenter"
			}.RegisterInNameScope(scope)
		};
	}

	private Control CreateScrollViewerTemplate(TemplatedControl parent, INameScope scope)
	{
		return new ScrollContentPresenter
		{
			Name = "PART_ContentPresenter",
			[~ContentPresenter.ContentProperty] =
				parent.GetObservable(ContentControl.ContentProperty).ToBinding()
		}.RegisterInNameScope(scope);
	}

	private static void Prepare(ListBox target)
	{
		target.Width = target.Height = 100;
		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
	}

	#endregion

	#region Classes

	private class TestStackOverflowViewModel : INotifyPropertyChanged
	{
		#region Constants

		public const int MaxInvokedCount = 1000;

		#endregion

		#region Fields

		private string _selectedItem;

		#endregion

		#region Properties

		public List<string> Items { get; set; } = [];

		public string SelectedItem
		{
			get => _selectedItem;
			set
			{
				if (_selectedItem != value)
				{
					SetterInvokedCount++;

					var index = Items.IndexOf(value);

					if ((MaxInvokedCount > SetterInvokedCount) && (index > 0))
					{
						_selectedItem = Items[index - 1];
					}
					else
					{
						_selectedItem = value;
					}

					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
				}
			}
		}

		public int SetterInvokedCount { get; private set; }

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	#endregion
}