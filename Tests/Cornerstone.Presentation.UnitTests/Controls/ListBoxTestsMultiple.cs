#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ListBoxTestsMultiple : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _helper = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CanShiftSelectAllItemsWhenDuplicatesArePresent()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[0]);
			_helper.Click(panel.Children[5], modifiers: KeyModifiers.Shift);

			CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" }, target.SelectedItems);
			CornerstoneTest.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, SelectedContainers(target));
		}
	}

	[PresentationTestMethod]
	public void CtrlRightClickShouldNotSelectMultiple()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz" },
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());

			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[0]);
			_helper.Click(panel.Children[2], MouseButton.Right, modifiers: KeyModifiers.Control);

			CornerstoneTest.IsNotNull(target.SelectedItems);
			CornerstoneTest.AreEqual(1, target.SelectedItems.Count);
		}
	}

	[PresentationTestMethod]
	public void CtrlSelectingNonSelectedItemWithMultipleSelectionActiveLeavesSelectedItemTheSame()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[1]);
			_helper.Click(panel.Children[2], modifiers: KeyModifiers.Control);

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual("Bar", target.SelectedItem);

			_helper.Click(panel.Children[2], modifiers: KeyModifiers.Control);

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual("Bar", target.SelectedItem);
		}
	}

	[PresentationTestMethod]
	public void CtrlSelectingRaisesSelectionChangedEvents()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());

			SelectionChangedEventArgs receivedArgs = null;

			target.SelectionChanged += (_, args) => receivedArgs = args;

			void VerifyAdded(string selection)
			{
				CornerstoneTest.IsNotNull(receivedArgs);
				CornerstoneTest.AreEqual(new[] { selection }, receivedArgs.AddedItems);
				CornerstoneTest.Empty(receivedArgs.RemovedItems);
			}

			void VerifyRemoved(string selection)
			{
				CornerstoneTest.IsNotNull(receivedArgs);
				CornerstoneTest.AreEqual(new[] { selection }, receivedArgs.RemovedItems);
				CornerstoneTest.Empty(receivedArgs.AddedItems);
			}

			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[1]);

			VerifyAdded("Bar");

			receivedArgs = null;
			_helper.Click(panel.Children[2], modifiers: KeyModifiers.Control);

			VerifyAdded("Baz");

			receivedArgs = null;
			_helper.Click(panel.Children[3], modifiers: KeyModifiers.Control);

			VerifyAdded("Qux");

			receivedArgs = null;
			_helper.Click(panel.Children[1], modifiers: KeyModifiers.Control);

			VerifyRemoved("Bar");
		}
	}

	[PresentationTestMethod]
	public void CtrlSelectingSelectedItemWithMultipleSelectionActiveSetsSelectedItemToNextSelection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[1]);
			_helper.Click(panel.Children[2], modifiers: KeyModifiers.Control);
			_helper.Click(panel.Children[3], modifiers: KeyModifiers.Control);

			CornerstoneTest.AreEqual(1, target.SelectedIndex);
			CornerstoneTest.AreEqual("Bar", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "Bar", "Baz", "Qux" }, target.SelectedItems);

			_helper.Click(panel.Children[1], modifiers: KeyModifiers.Control);

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
			CornerstoneTest.AreEqual("Baz", target.SelectedItem);
			CornerstoneTest.AreEqual(new[] { "Baz", "Qux" }, target.SelectedItems);
		}
	}

	[PresentationTestMethod]
	public void DuplicateItemsAreAddedToSelectedItemsInOrder()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[0]);

			CornerstoneTest.AreEqual(new[] { "Foo" }, target.SelectedItems);

			_helper.Click(panel.Children[4], modifiers: KeyModifiers.Control);

			CornerstoneTest.AreEqual(new[] { "Foo", "Bar" }, target.SelectedItems);

			_helper.Click(panel.Children[3], modifiers: KeyModifiers.Control);

			CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Foo" }, target.SelectedItems);

			_helper.Click(panel.Children[1], modifiers: KeyModifiers.Control);

			CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Foo", "Bar" }, target.SelectedItems);
		}
	}

	[PresentationTestMethod]
	public void LeftClickOnSelectedItemShouldClearExistingSelection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz" },
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			target.SelectAll();

			CornerstoneTest.AreEqual(3, target.SelectedItems!.Count);

			_helper.Click(target.Presenter!.Panel!.Children[0]);

			CornerstoneTest.AreEqual(1, target.SelectedItems.Count);
			CornerstoneTest.AreEqual(new[] { "Foo" }, target.SelectedItems);
			CornerstoneTest.AreEqual(new[] { 0 }, SelectedContainers(target));
		}
	}

	[PresentationTestMethod]
	public void RightClickOnSelectedItemShouldNotClearExistingSelection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz" },
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			target.SelectAll();

			CornerstoneTest.AreEqual(3, target.SelectedItems!.Count);

			_helper.Click(target.Presenter!.Panel!.Children[0], MouseButton.Right);

			CornerstoneTest.AreEqual(3, target.SelectedItems.Count);
		}
	}

	[PresentationTestMethod]
	public void RightClickOnUnselectedItemShouldClearExistingSelection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz" },
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[0]);
			_helper.Click(panel.Children[1], modifiers: KeyModifiers.Shift);

			CornerstoneTest.IsNotNull(target.SelectedItems);
			CornerstoneTest.AreEqual(2, target.SelectedItems.Count);

			_helper.Click(panel.Children[2], MouseButton.Right);

			CornerstoneTest.IsNotNull(target.SelectedItems);
			CornerstoneTest.AreEqual(1, target.SelectedItems.Count);
		}
	}

	[PresentationTestMethod]
	public void SelectAllWorksFromNoSelectionWhenSelectedIndexIsBoundTwoWay()
	{
		// Issue #13676
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var target = new ListBox
		{
			Template = new FuncControlTemplate(CreateListBoxTemplate),
			ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
			SelectionMode = SelectionMode.Multiple,
			Width = 100,
			Height = 100
		};

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		target.Bind(ListBox.SelectedIndexProperty, new Binding("Tag")
		{
			Mode = BindingMode.TwoWay,
			RelativeSource = new RelativeSource(RelativeSourceMode.Self)
		});

		target.SelectAll();

		CornerstoneTest.AreEqual(new[] { 0, 1, 2, 3 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz", "Qux" }, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void SelectAllWorksFromNoSelectionWhenSelectedItemIsBoundTwoWay()
	{
		// Issue #13676
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var target = new ListBox
		{
			Template = new FuncControlTemplate(CreateListBoxTemplate),
			ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
			SelectionMode = SelectionMode.Multiple,
			Width = 100,
			Height = 100
		};

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		target.Bind(ListBox.SelectedItemProperty, new Binding("Tag")
		{
			Mode = BindingMode.TwoWay,
			RelativeSource = new RelativeSource(RelativeSourceMode.Self)
		});

		target.SelectAll();

		CornerstoneTest.AreEqual(new[] { 0, 1, 2, 3 }, target.Selection.SelectedIndexes);
		CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz", "Qux" }, target.SelectedItems);
	}

	[PresentationTestMethod]
	public void ShiftArrowKeySelectsRange()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var target = new ListBox
		{
			Template = new FuncControlTemplate(CreateListBoxTemplate),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			SelectionMode = SelectionMode.Multiple,
			Width = 100,
			Height = 100,
			SelectedIndex = 0
		};

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

		CornerstoneTest.AreEqual(new[] { "Foo", "Bar" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1 }, SelectedContainers(target));
		CornerstoneTest.IsTrue(target.ContainerFromIndex(1)!.IsFocused);

		RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

		CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, SelectedContainers(target));
		CornerstoneTest.IsTrue(target.ContainerFromIndex(2)!.IsFocused);

		RaiseKeyEvent(target, Key.Up, KeyModifiers.Shift);

		CornerstoneTest.AreEqual(new[] { "Foo", "Bar" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1 }, SelectedContainers(target));
		CornerstoneTest.IsTrue(target.ContainerFromIndex(1)!.IsFocused);
	}

	[PresentationTestMethod]
	public void ShiftDownKeySelectingSelectsRangeEndFromFocus()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var target = new ListBox
		{
			Template = new FuncControlTemplate(CreateListBoxTemplate),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			SelectionMode = SelectionMode.Multiple,
			Width = 100,
			Height = 100,
			SelectedIndex = 0
		};

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		target.ContainerFromIndex(1)!.Focus();
		RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

		CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, SelectedContainers(target));
		CornerstoneTest.IsTrue(target.ContainerFromIndex(2)!.IsFocused);
	}

	[PresentationTestMethod]
	public void ShiftDownKeySelectingSelectsRangeEndFromFocusMovedWithCtrlKey()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var target = new ListBox
		{
			Template = new FuncControlTemplate(CreateListBoxTemplate),
			ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
			SelectionMode = SelectionMode.Multiple,
			Width = 100,
			Height = 100,
			SelectedIndex = 0
		};

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

		CornerstoneTest.AreEqual(new[] { "Foo", "Bar" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1 }, SelectedContainers(target));
		CornerstoneTest.IsTrue(target.ContainerFromIndex(1)!.IsFocused);

		RaiseKeyEvent(target, Key.Down, KeyModifiers.Control);

		CornerstoneTest.AreEqual(new[] { "Foo", "Bar" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1 }, SelectedContainers(target));
		CornerstoneTest.IsTrue(target.ContainerFromIndex(2)!.IsFocused);

		RaiseKeyEvent(target, Key.Down, KeyModifiers.Shift);

		CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz", "Qux" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { 0, 1, 2, 3 }, SelectedContainers(target));
		CornerstoneTest.IsTrue(target.ContainerFromIndex(3)!.IsFocused);
	}

	[PresentationTestMethod]
	public void ShiftRightClickShouldNotSelectMultiple()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz" },
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());

			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[0]);
			_helper.Click(panel.Children[2], MouseButton.Right, modifiers: KeyModifiers.Shift);

			CornerstoneTest.IsNotNull(target.SelectedItems);
			CornerstoneTest.AreEqual(1, target.SelectedItems.Count);
		}
	}

	[PresentationTestMethod]
	public void ShiftSelectingFromNoSelectionSelectsFromStart()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			_helper.Click(target.Presenter!.Panel!.Children[2], modifiers: KeyModifiers.Shift);

			CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
			CornerstoneTest.AreEqual(new[] { 0, 1, 2 }, SelectedContainers(target));
		}
	}

	[PresentationTestMethod]
	public void ShiftSelectingRaisesSelectionChangedEvents()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Qux" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};
			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());

			SelectionChangedEventArgs receivedArgs = null;

			target.SelectionChanged += (_, args) => receivedArgs = args;

			void VerifyAdded(params string[] selection)
			{
				CornerstoneTest.IsNotNull(receivedArgs);
				CornerstoneTest.AreEqual(selection, receivedArgs.AddedItems);
				CornerstoneTest.Empty(receivedArgs.RemovedItems);
			}

			void VerifyRemoved(string selection)
			{
				CornerstoneTest.IsNotNull(receivedArgs);
				CornerstoneTest.AreEqual(new[] { selection }, receivedArgs.RemovedItems);
				CornerstoneTest.Empty(receivedArgs.AddedItems);
			}

			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[1]);

			VerifyAdded("Bar");

			receivedArgs = null;
			_helper.Click(panel.Children[3], modifiers: KeyModifiers.Shift);

			VerifyAdded("Baz", "Qux");

			receivedArgs = null;
			_helper.Click(panel.Children[2], modifiers: KeyModifiers.Shift);

			VerifyRemoved("Qux");
		}
	}

	[PresentationTestMethod]
	public void ShouldCtrlSelectCorrectItemWhenDuplicateItemsArePresent()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[3]);
			_helper.Click(panel.Children[4], modifiers: KeyModifiers.Control);

			CornerstoneTest.AreEqual(new[] { "Foo", "Bar" }, target.SelectedItems);
			CornerstoneTest.AreEqual(new[] { 3, 4 }, SelectedContainers(target));
		}
	}

	[PresentationTestMethod]
	public void ShouldShiftSelectCorrectItemWhenDuplicatesArePresent()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			var panel = target.Presenter!.Panel!;
			_helper.Click(panel.Children[3]);
			_helper.Click(panel.Children[5], modifiers: KeyModifiers.Shift);

			CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz" }, target.SelectedItems);
			CornerstoneTest.AreEqual(new[] { 3, 4, 5 }, SelectedContainers(target));
		}
	}

	[PresentationTestMethod]
	public void ToggleModifierAndRangeShouldSelectSecondRange()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = new FuncControlTemplate(CreateListBoxTemplate),
				ItemsSource = new[] { "Foo", "Bar", "Baz", "Gap", "Boo", "Far", "Faz" },
				SelectionMode = SelectionMode.Multiple,
				Width = 100,
				Height = 100
			};

			var root = new TestRoot(target);
			root.LayoutManager.ExecuteInitialLayoutPass();

			PresentationLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration());
			var panel = target.Presenter!.Panel!;

			// Select first range
			_helper.Click(panel.Children[0]);
			_helper.Click(panel.Children[2], modifiers: KeyModifiers.Shift);

			// Select second range
			_helper.Click(panel.Children[4], modifiers: KeyModifiers.Control);
			_helper.Click(panel.Children[6], modifiers: KeyModifiers.Control | KeyModifiers.Shift);

			CornerstoneTest.AreEqual(new[] { "Foo", "Bar", "Baz", "Boo", "Far", "Faz" }, target.SelectedItems);
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

	private static void RaiseKeyEvent(Control target, Key key, KeyModifiers inputModifiers = 0)
	{
		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			KeyModifiers = inputModifiers,
			Key = key
		});
	}

	private static IEnumerable<int> SelectedContainers(SelectingItemsControl target)
	{
		return target.Presenter!.Panel!.Children
			.Select(x => x.Classes.Contains(":selected") ? target.IndexFromContainer(x) : -1)
			.Where(x => x != -1);
	}

	#endregion
}