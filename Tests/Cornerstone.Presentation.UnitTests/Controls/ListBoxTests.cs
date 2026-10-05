#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pointer = Cornerstone.Presentation.Input.Pointer;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ListBoxTests : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _mouse = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AddingAndSelectingItemWithAutoScrollToSelectedItemShouldNotHideFirstItem()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = new OldPresentationList<string>();

			var wnd = new Window { Width = 100, Height = 100, IsVisible = true };

			var target = new ListBox
			{
				VerticalAlignment = VerticalAlignment.Top,
				AutoScrollToSelectedItem = true,
				Width = 50,
				ItemTemplate = new FuncDataTemplate<object>((c, _) => new Border { Height = 10 }),
				ItemsSource = items
			};
			wnd.Content = target;

			var lm = wnd.LayoutManager;

			lm.ExecuteInitialLayoutPass();

			var panel = target.Presenter!.Panel!;

			items.Add("Item 1");
			target.Selection.Select(0);
			lm.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(1, panel.Children.Count);

			items.Add("Item 2");
			target.Selection.Select(1);
			lm.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(2, panel.Children.Count);

			//make sure we have enough space to show all items
			CornerstoneTest.IsTrue(panel.Bounds.Height >= panel.Children.Sum(c => c.Bounds.Height));

			//make sure we show items and they completelly visible, not only partially
			CornerstoneTest.IsTrue((panel.Children[0].Bounds.Top >= 0) && (panel.Children[0].Bounds.Bottom <= panel.Bounds.Height), "first item is not completelly visible!");
			CornerstoneTest.IsTrue((panel.Children[1].Bounds.Top >= 0) && (panel.Children[1].Bounds.Bottom <= panel.Bounds.Height), "second item is not completelly visible!");
		}
	}

	[PresentationTestMethod]
	public void ArrowKeysShouldFocusSelection()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = items,
			ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
			SelectedIndex = 0
		};

		Prepare(target);

		RaiseKeyEvent(target, Key.Down);
		CornerstoneTest.IsTrue(target.ContainerFromIndex(1)!.IsFocused);

		RaiseKeyEvent(target, Key.Down);
		CornerstoneTest.IsTrue(target.ContainerFromIndex(2)!.IsFocused);

		RaiseKeyEvent(target, Key.Up);
		CornerstoneTest.IsTrue(target.ContainerFromIndex(1)!.IsFocused);
	}

	[PresentationTestMethod]
	public void ArrowKeysShouldMoveSelectionHorizontal()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = items,
			ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel
			{
				Orientation = Orientation.Horizontal
			}),
			ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
			SelectedIndex = 0
		};

		Prepare(target);

		RaiseKeyEvent(target, Key.Right);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		RaiseKeyEvent(target, Key.Right);
		CornerstoneTest.AreEqual(2, target.SelectedIndex);

		RaiseKeyEvent(target, Key.Left);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void ArrowKeysShouldMoveSelectionVertical()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = items,
			ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
			SelectedIndex = 0
		};

		Prepare(target);

		RaiseKeyEvent(target, Key.Down);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		RaiseKeyEvent(target, Key.Down);
		CornerstoneTest.AreEqual(2, target.SelectedIndex);

		RaiseKeyEvent(target, Key.Up);
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void CanDecreaseNumberOfMaterializedItemsByRemovingFromSourceCollection()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = new OldPresentationList<string>(Enumerable.Range(0, 20).Select(x => $"Item {x}"));
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
			};

			Prepare(target);
			target.Scroll!.Offset = new Vector(0, 1);

			items.RemoveRange(0, 11);
		}
	}

	[PresentationTestMethod]
	public void ClickingItemShouldRaiseBringIntoViewForCorrectControl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			// Issue #3934
			var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
				SelectionMode = SelectionMode.AlwaysSelected
			};

			Prepare(target);

			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			// First an item that is not index 0 must be selected.
			_mouse.Click(target.Presenter!.Panel!.Children[1]);

			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(1, target.Selection.AnchorIndex);

			// We're going to be clicking on item 9.
			var item = (ListBoxItem) target.Presenter.Panel.Children[9];
			var raised = 0;

			// Make sure a RequestBringIntoView event is raised for item 9. It won't be handled
			// by the ScrollContentPresenter as the item is already visible, so we don't need
			// handledEventsToo: true. Issue #3934 failed here because item 0 was being scrolled
			// into view due to SelectionMode.AlwaysSelected.
			target.AddHandler(Control.RequestBringIntoViewEvent, (s, e) =>
			{
				CornerstoneTest.Same(item, e.TargetObject);
				++raised;
			});

			// Click item 9.
			_mouse.Click(item);

			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void ContainerClearingIsRaisedWhenItemRemoved()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var data = new OldPresentationList<string> { "Foo", "Bar", "Baz" };
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = data
		};

		Prepare(target);

		var expected = target.ContainerFromIndex(1);
		var raised = 0;

		target.ContainerClearing += (s, e) =>
		{
			CornerstoneTest.Same(expected, e.Container);
			++raised;
		};

		data.RemoveAt(1);
		Layout(target);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ContainerIndexChangedIsRaisedWhenItemAdded()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var data = new OldPresentationList<string> { "Foo", "Bar", "Baz" };
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = data
		};

		Prepare(target);

		var result = new List<Control>();
		var index = 1;

		target.ContainerIndexChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(index++, e.OldIndex);
			CornerstoneTest.AreEqual(index, e.NewIndex);
			result.Add(e.Container);
		};

		data.Insert(1, "Qux");
		Layout(target);

		CornerstoneTest.AreEqual(2, result.Count);
		CornerstoneTest.AreEqual(target.GetRealizedContainers().Skip(2), result);
	}

	[PresentationTestMethod]
	public void ContainerPreparedIsRaisedForAddedItem()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var data = new OldPresentationList<string> { "Foo", "Bar", "Baz" };
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = data
		};

		Prepare(target);

		var result = new List<Control>();

		target.ContainerPrepared += (s, e) =>
		{
			CornerstoneTest.AreEqual(3, e.Index);
			result.Add(e.Container);
		};

		data.Add("Qux");
		Layout(target);

		CornerstoneTest.AreEqual(1, result.Count);
	}

	[PresentationTestMethod]
	public void ContainerPreparedIsRaisedForEachItemContainerOnLayout()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			Items = { "Foo", "Bar", "Baz" }
		};

		var result = new List<Control>();
		var index = 0;

		target.ContainerPrepared += (s, e) =>
		{
			CornerstoneTest.AreEqual(index++, e.Index);
			result.Add(e.Container);
		};

		Prepare(target);

		CornerstoneTest.AreEqual(3, result.Count);
		CornerstoneTest.AreEqual(target.GetRealizedContainers(), result);
	}

	[PresentationTestMethod]
	public void ContainerPreparedIsRaisedForEachItemsSourceContainerOnLayout()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" }
		};

		var result = new List<Control>();
		var index = 0;

		target.ContainerPrepared += (s, e) =>
		{
			CornerstoneTest.AreEqual(index++, e.Index);
			result.Add(e.Container);
		};

		Prepare(target);

		CornerstoneTest.AreEqual(3, result.Count);
		CornerstoneTest.AreEqual(target.GetRealizedContainers(), result);
	}

	[PresentationTestMethod]
	public void ContainerShouldHaveThemeSetToItemContainerTheme()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = new[] { "Foo", "Bar", "Baz " };
			var theme = new ControlTheme(typeof(ListBoxItem));
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items,
				ItemContainerTheme = theme
			};

			Prepare(target);

			var container = (ListBoxItem) target.Presenter!.Panel!.Children[0];

			CornerstoneTest.Same(container.Theme, theme);
		}
	}

	[PresentationTestMethod]
	public void ContainersCorrectAfterClearAddRemove()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			// Issue #1936
			var items = new OldPresentationList<string>(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				SelectedIndex = 0
			};

			Prepare(target);

			items.Clear();
			items.AddRange(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
			Layout(target);

			items.Remove("Item 2");
			Layout(target);

			var actual = target.GetRealizedContainers().Cast<ListBoxItem>().Select(x => (string) x.Content).ToList();
			CornerstoneTest.AreEqual(items.OrderBy(x => x), actual.OrderBy(x => x));
		}
	}

	[PresentationTestMethod]
	public void ContentCanBeBoundInItemContainerTheme()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = new[] { new ItemViewModel("Foo"), new ItemViewModel("Bar") };
			var theme = new ControlTheme(typeof(ListBoxItem))
			{
				Setters =
				{
					new Setter(ListBoxItem.ContentProperty, new Binding("Caption"))
				}
			};

			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items,
				ItemContainerTheme = theme
			};

			Prepare(target);

			var containers = target.GetRealizedContainers().Cast<ListBoxItem>().ToList();
			CornerstoneTest.AreEqual(2, containers.Count);
			CornerstoneTest.AreEqual("Foo", containers[0].Content);
			CornerstoneTest.AreEqual("Bar", containers[1].Content);
		}
	}

	[PresentationTestMethod]
	public void CtrlDownKeyMovesFocusButNotSelection()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			Width = 100,
			Height = 100,
			SelectedIndex = 0
		};

		Prepare(target);

		target.ContainerFromIndex(0)!.Focus();
		RaiseKeyEvent(target, Key.Down, KeyModifiers.Control);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.IsTrue(target.ContainerFromIndex(1)!.IsFocused);
	}

	[PresentationTestMethod]
	public void DataContextsShouldBeCorrectlySet()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = new object[]
			{
				"Foo",
				new Item("Bar"),
				new TextBlock { Text = "Baz" },
				new ListBoxItem { Content = "Qux" }
			};

			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				DataContext = "Base",
				ItemTemplate = new FuncDataTemplate<Item>((x, _) => new Button { Content = x }),
				ItemsSource = items
			};

			Prepare(target);

			var dataContexts = target.Presenter!.Panel!.Children
				.Select(x => x.DataContext)
				.ToList();

			CornerstoneTest.AreEqual(new[] { items[0], items[1], "Base", "Base" }, dataContexts);
		}
	}

	[PresentationTestMethod]
	public void DownKeyBringsUnrealizedSelectionIntoView()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var items = Enumerable.Range(0, 100).Select(x => $"Item {x}").ToArray();
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = items,
			ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
			Width = 100,
			Height = 100,
			SelectedIndex = 0
		};

		Prepare(target);

		target.ContainerFromIndex(0)!.Focus();
		target.Scroll!.Offset = new Vector(0, 100);
		Layout(target);

		var panel = (VirtualizingStackPanel) target.ItemsPanelRoot!;
		CornerstoneTest.AreEqual(10, panel.FirstRealizedIndex);

		RaiseKeyEvent(target, Key.Down);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.IsTrue(target.ContainerFromIndex(1)!.IsFocused);
		CornerstoneTest.AreEqual(new Vector(0, 10), target.Scroll.Offset);
	}

	[PresentationTestMethod]
	public void DownKeySelectingFromNoSelectionAndNoFocusSelectsFromStart()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			Width = 100,
			Height = 100
		};

		Prepare(target);

		RaiseKeyEvent(target, Key.Down);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void DownKeySelectingFromNoSelectionSelectsFromFocus()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			Width = 100,
			Height = 100
		};

		Prepare(target);

		target.ContainerFromIndex(1)!.Focus();
		RaiseKeyEvent(target, Key.Down);

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void HandlesResettingItems()
	{
		var items = new ResettingCollection(100);
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = items,
			ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas { Height = 10 })
		};

		Prepare(target);

		var realized = target.GetRealizedContainers()
			.Cast<ListBoxItem>()
			.Select(x => (string) x.DataContext)
			.ToList();

		CornerstoneTest.AreEqual(Enumerable.Range(0, 10).Select(x => $"Item{x}"), realized);

		items.Reverse();
		Layout(target);

		realized = target.GetRealizedContainers()
			.Cast<ListBoxItem>()
			.Select(x => (string) x.DataContext)
			.ToList();

		CornerstoneTest.AreEqual(Enumerable.Range(0, 10).Select(x => $"Item{99 - x}"), realized);
	}

	[PresentationTestMethod]
	public void HandlesResettingItemsWithExistingSelectionAndAutoScrollToSelectedItem()
	{
		var items = new ResettingCollection(100);
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = items,
			ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas { Height = 10 }),
			AutoScrollToSelectedItem = true,
			SelectedIndex = 1
		};

		Prepare(target);

		var realized = target.GetRealizedContainers()
			.Cast<ListBoxItem>()
			.Select(x => (string) x.DataContext)
			.ToList();

		CornerstoneTest.AreEqual(Enumerable.Range(0, 10).Select(x => $"Item{x}"), realized);

		items.Reverse();
		Layout(target);

		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		realized = target.GetRealizedContainers()
			.Cast<ListBoxItem>()
			.Select(x => (string) x.DataContext)
			.ToList();

		// "Item1" should remain selected, and now be at the bottom of the viewport.
		CornerstoneTest.AreEqual(Enumerable.Range(0, 10).Select(x => $"Item{10 - x}"), realized);
	}

	[PresentationTestMethod]
	public void InitialBindingOfSelectedItemsShouldNotCauseWriteToSelectedItems()
	{
		var target = new ListBox
		{
			[!ListBox.ItemsSourceProperty] = new Binding("Items"),
			[!ListBox.SelectedItemsProperty] = new Binding("SelectedItems")
		};

		var viewModel = new
		{
			Items = new[] { "Foo", "Bar", "Baz " },
			SelectedItems = new ObservableCollection<string> { "Bar" }
		};

		var raised = 0;

		viewModel.SelectedItems.CollectionChanged += (s, e) => ++raised;

		target.DataContext = viewModel;

		CornerstoneTest.AreEqual(0, raised);
		CornerstoneTest.AreEqual(new[] { "Bar" }, viewModel.SelectedItems);
		CornerstoneTest.AreEqual(new[] { "Bar" }, target.SelectedItems);
		CornerstoneTest.AreEqual(new[] { "Bar" }, target.Selection.SelectedItems);
	}

	[PresentationTestMethod]
	public void InlineItemShouldHaveThemeSetToItemContainerTheme()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var theme = new ControlTheme(typeof(ListBoxItem));
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				Items = { new ListBoxItem() },
				ItemContainerTheme = theme
			};

			Prepare(target);

			var container = (ListBoxItem) target.Presenter!.Panel!.Children[0];

			CornerstoneTest.Same(container.Theme, theme);
		}
	}

	[PresentationTestMethod]
	public void LayoutManagerShouldMeasureArrangeAll()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = new OldPresentationList<string>(Enumerable.Range(1, 7).Select(v => v.ToString()));

			var wnd = new Window { SizeToContent = SizeToContent.WidthAndHeight };

			wnd.IsVisible = true;

			var target = new ListBox();

			wnd.Content = target;

			var lm = wnd.LayoutManager;

			target.Height = 110;
			target.Width = 50;
			target.DataContext = items;

			target.ItemTemplate = new FuncDataTemplate<object>((c, _) =>
			{
				var tb = new TextBlock { Height = 10, Width = 30 };
				tb.Bind(TextBlock.TextProperty, new Binding());
				return tb;
			}, true);

			lm.ExecuteInitialLayoutPass();

			target.ItemsSource = items;

			lm.ExecuteLayoutPass();

			items.Insert(3, "3+");
			lm.ExecuteLayoutPass();

			items.Insert(4, "4+");
			lm.ExecuteLayoutPass();

			//RESET
			items.Clear();
			foreach (var i in Enumerable.Range(1, 7))
			{
				items.Add(i.ToString());
			}

			//working bit better with this line no outof memory or remaining to arrange/measure ???
			//lm.ExecuteLayoutPass();

			items.Insert(2, "2+");

			lm.ExecuteLayoutPass();

			//after few more layout cycles layoutmanager shouldn't hold any more visual for measure/arrange
			lm.ExecuteLayoutPass();
			lm.ExecuteLayoutPass();

			var flags = BindingFlags.Instance | BindingFlags.NonPublic;
			var toMeasure = lm.GetType().GetField("_toMeasure", flags)!.GetValue(lm) as IEnumerable<Layoutable>;
			var toArrange = lm.GetType().GetField("_toArrange", flags)!.GetValue(lm) as IEnumerable<Layoutable>;

			CornerstoneTest.IsNotNull(toMeasure);
			CornerstoneTest.AreEqual(0, toMeasure.Count());
			CornerstoneTest.IsNotNull(toArrange);
			CornerstoneTest.AreEqual(0, toArrange.Count());
		}
	}

	[PresentationTestMethod]
	public void ListBoxItemContainersShouldBeGenerated()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var items = new[] { "Foo", "Bar", "Baz " };
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items
			};

			Prepare(target);

			var text = target.Presenter!.Panel!.Children
				.OfType<ListBoxItem>()
				.Select(x => x.Presenter!.Child)
				.OfType<TextBlock>()
				.Select(x => x.Text)
				.ToList();

			CornerstoneTest.AreEqual(items, text);
		}
	}

	[PresentationTestMethod]
	public void ListBoxItemShouldNotBlockTappedEvents()
	{
		// #13474
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var _pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
			ulong nextStamp = 1;

			var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items,
				SelectionMode = SelectionMode.Toggle,
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
			};

			Prepare(target);

			var lbItems = target.GetLogicalChildren().OfType<ListBoxItem>().ToArray();

			var item = lbItems[0];

			var tappedCount = 0;
			target.Tapped += (s, e) => { tappedCount++; };

			_mouse.Click(item);
			CornerstoneTest.AreEqual(1, tappedCount);

			// Raise PointerPressed and PointerReleased events with the Left Button pressed.  TouchTestHelper 
			// assumes no button pressed, which prevents it from generating Tapped events, or I would use that.

			item.RaiseEvent(new PointerPressedEventArgs(item, _pointer, item, default, nextStamp++,
				new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));

			item.RaiseEvent(new PointerReleasedEventArgs(item, _pointer, item, default, nextStamp++,
				PointerPointProperties.None, KeyModifiers.None, MouseButton.Left));

			CornerstoneTest.AreEqual(2, tappedCount);
		}
	}

	[PresentationTestMethod]
	public void ListBoxShouldBeValidAfterRemoveOfItemInNonVisibleArea()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = new OldPresentationList<string>(Enumerable.Range(1, 30).Select(v => v.ToString()));

			var wnd = new Window { Width = 100, Height = 100, IsVisible = true };

			var target = new ListBox
			{
				AutoScrollToSelectedItem = true,
				Height = 100,
				Width = 50,
				ItemTemplate = new FuncDataTemplate<object>((c, _) => new Border { Height = 10 }),
				ItemsSource = items
			};
			wnd.Content = target;

			var lm = wnd.LayoutManager;

			lm.ExecuteInitialLayoutPass();

			//select last / scroll to last item
			target.SelectedItem = items.Last();

			lm.ExecuteLayoutPass();

			//remove the first item (in non realized area of the listbox)
			items.Remove("1");
			lm.ExecuteLayoutPass();

			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual("30", target.ContainerFromIndex(items.Count - 1)!.DataContext);
			CornerstoneTest.AreEqual("29", target.ContainerFromIndex(items.Count - 2)!.DataContext);
			CornerstoneTest.AreEqual("28", target.ContainerFromIndex(items.Count - 3)!.DataContext);
			CornerstoneTest.AreEqual("27", target.ContainerFromIndex(items.Count - 4)!.DataContext);
			CornerstoneTest.AreEqual("26", target.ContainerFromIndex(items.Count - 5)!.DataContext);
		}
	}

	[PresentationTestMethod]
	public void ListBoxShouldFindItemsPresenterInScrollViewer()
	{
		var target = new ListBox
		{
			Template = ListBoxTemplate()
		};

		Prepare(target);

		CornerstoneTest.IsType<ItemsPresenter>(target.Presenter);
	}

	[PresentationTestMethod]
	public void ListBoxShouldFindScrollviewerInTemplate()
	{
		var target = new ListBox
		{
			Template = ListBoxTemplate()
		};

		ScrollViewer viewer = null;

		target.TemplateApplied += (sender, e) => { viewer = target.Scroll as ScrollViewer; };

		Prepare(target);

		CornerstoneTest.IsNotNull(viewer);
	}

	[PresentationTestMethod]
	public void LogicalChildrenShouldBeSetForDataTemplateGeneratedItems()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = new[] { "Foo", "Bar", "Baz " }
			};

			Prepare(target);

			CornerstoneTest.AreEqual(3, target.GetLogicalChildren().Count());

			foreach (var child in target.GetLogicalChildren())
			{
				CornerstoneTest.IsType<ListBoxItem>(child);
			}
		}
	}

	[PresentationTestMethod]
	public void ReadsOnlyRealizedItemsFromItemsSource()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var data = new DataVirtualizingList();
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = data
		};

		Prepare(target);

		var panel = CornerstoneTest.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
		CornerstoneTest.AreEqual(0, panel.FirstRealizedIndex);
		CornerstoneTest.AreEqual(6, panel.LastRealizedIndex);

		CornerstoneTest.AreEqual(Enumerable.Range(0, 7).Select(x => $"Item{x}"), data.GetRealizedItems());
	}

	[PresentationTestMethod]
	public void ScrollViewerShouldHaveCorrectExtentAndViewport()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
				SelectedIndex = 0
			};

			Prepare(target);

			CornerstoneTest.AreEqual(new Size(100, 200), target.Scroll!.Extent);
			CornerstoneTest.AreEqual(new Size(100, 100), target.Scroll.Viewport);
		}
	}

	[PresentationTestMethod]
	public void SelectedItemValidation()
	{
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = new[] { "Foo" },
			ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
			SelectionMode = SelectionMode.AlwaysSelected
		};

		Prepare(target);

		var exception = new InvalidCastException("failed validation");
		var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
		target.Bind(ComboBox.SelectedItemProperty, textObservable);

		CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(target));
		CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(target));
	}

	[PresentationTestMethod]
	public void SelectionShouldBeClearedOnRecycledItems()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
				SelectedIndex = 0
			};

			Prepare(target);

			// Make sure we're virtualized and first item is selected.
			CornerstoneTest.AreEqual(10, target.Presenter!.Panel!.Children.Count);
			CornerstoneTest.IsTrue(((ListBoxItem) target.Presenter.Panel.Children[0]).IsSelected);

			// The selected item must not be the anchor, otherwise it won't get recycled.
			target.Selection.AnchorIndex = -1;

			// Scroll down a page.
			target.Scroll!.Offset = new Vector(0, 10);
			Layout(target);

			// Make sure recycled item isn't now selected.
			CornerstoneTest.IsFalse(((ListBoxItem) target.Presenter.Panel.Children[0]).IsSelected);
		}
	}

	[PresentationTestMethod]
	public void ShouldNotHandleSpaceWhenTextBoxInsideListBoxItem()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target = new TextBox
			{
				Focusable = true
			};
			var listbox = new ListBox
			{
				Template = ListBoxTemplate(),
				Items =
				{
					new ListBoxItem
					{
						Content = target
					}
				}
			};

			var nKeyDown = 0;

			var root = new TestRoot
			{
				Width = 1000,
				Height = 1000,
				Child = listbox
			};

			root.KeyDown += (s, e) => nKeyDown++;

			listbox.ApplyTemplate();
			root.LayoutManager.ExecuteInitialLayoutPass();

			target.Focus();

			RaiseKeyEvent(target, Key.Space, KeyModifiers.None);

			CornerstoneTest.AreEqual(1, nKeyDown);
		}
	}

	[PresentationTestMethod]
	public void ShouldUseItemTemplateToCreateItemContent()
	{
		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			ItemsSource = new[] { "Foo" },
			ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas())
		};

		Prepare(target);

		var container = (ListBoxItem) target.Presenter!.Panel!.Children[0];
		CornerstoneTest.IsType<Canvas>(container.Presenter!.Child);
	}

	[PresentationTestMethod]
	public void TabNavigationShouldMoveToAnchorElement()
	{
		var services = TestServices.StyledWindow.With(
			keyboardDevice: () => new KeyboardDevice());
		using var app = UnitTestApplication.Start(services);

		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			Items = { "Foo", "Bar", "Baz" }
		};

		var button = new Button
		{
			Content = "Button",
			[DockPanel.DockProperty] = Dock.Top
		};

		var root = new TestRoot
		{
			Width = 1000,
			Height = 1000,
			Child = new DockPanel
			{
				Children =
				{
					button,
					target
				}
			}
		};

		var navigation = new KeyboardNavigationHandler();
		navigation.SetOwner(root);

		root.LayoutManager.ExecuteInitialLayoutPass();

		button.Focus();
		target.Selection.AnchorIndex = 1;
		RaiseKeyEvent(button, Key.Tab);

		var item = target.ContainerFromIndex(1);
		CornerstoneTest.IsNotNull(item);
		CornerstoneTest.Same(item, root.FocusManager.GetFocusedElement());

		RaiseKeyEvent(item, Key.Tab);

		CornerstoneTest.Same(button, root.FocusManager.GetFocusedElement());

		target.Selection.AnchorIndex = 2;
		RaiseKeyEvent(button, Key.Tab);

		item = target.ContainerFromIndex(2);
		CornerstoneTest.Same(item, root.FocusManager.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void TabNavigationShouldMoveToFirstItemWhenNoAnchorElementSelected()
	{
		var services = TestServices.StyledWindow.With(
			keyboardDevice: () => new KeyboardDevice());
		using var app = UnitTestApplication.Start(services);

		var target = new ListBox
		{
			Template = ListBoxTemplate(),
			Items = { "Foo", "Bar", "Baz" }
		};

		var button = new Button
		{
			Content = "Button",
			[DockPanel.DockProperty] = Dock.Top
		};

		var root = new TestRoot
		{
			Child = new DockPanel
			{
				Children =
				{
					button,
					target
				}
			}
		};

		var navigation = new KeyboardNavigationHandler();
		navigation.SetOwner(root);

		root.LayoutManager.ExecuteInitialLayoutPass();

		button.Focus();
		RaiseKeyEvent(button, Key.Tab);

		var item = target.ContainerFromIndex(0);
		CornerstoneTest.Same(item, root.FocusManager.GetFocusedElement());
	}

	[PresentationTestMethod]
	public void ToggleSelectionShouldUpdateContainers()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items,
				SelectionMode = SelectionMode.Toggle,
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 })
			};

			Prepare(target);

			var lbItems = target.GetLogicalChildren().OfType<ListBoxItem>().ToArray();

			var item = lbItems[0];

			CornerstoneTest.AreEqual(false, item.IsSelected);

			RaisePressedEvent(item, MouseButton.Left);

			CornerstoneTest.AreEqual(true, item.IsSelected);

			RaisePressedEvent(item, MouseButton.Left);

			CornerstoneTest.AreEqual(false, item.IsSelected);
		}
	}

	[PresentationTestMethod]
	public void WrapSelectionShouldWrap()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var items = Enumerable.Range(0, 10).Select(x => $"Item {x}").ToArray();
			var target = new ListBox
			{
				Template = ListBoxTemplate(),
				ItemsSource = items,
				ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Height = 10 }),
				WrapSelection = true
			};

			Prepare(target);

			var lbItems = target.GetLogicalChildren().OfType<ListBoxItem>().ToArray();

			var first = lbItems.First();
			var beforeLast = lbItems[^2];
			var last = lbItems.Last();

			first.Focus();

			RaisePressedEvent(first, MouseButton.Left);
			CornerstoneTest.AreEqual(true, first.IsSelected);

			RaiseKeyEvent(target, Key.Up);
			CornerstoneTest.AreEqual(true, last.IsSelected);

			RaiseKeyEvent(target, Key.Up);
			CornerstoneTest.AreEqual(true, beforeLast.IsSelected);

			RaiseKeyEvent(target, Key.Down);
			CornerstoneTest.AreEqual(true, last.IsSelected);

			RaiseKeyEvent(target, Key.Down);
			CornerstoneTest.AreEqual(true, first.IsSelected);

			target.WrapSelection = false;
			RaiseKeyEvent(target, Key.Up);

			CornerstoneTest.AreEqual(true, first.IsSelected);
		}
	}

	private static void Layout(Control c)
	{
		c.GetLayoutManager()?.ExecuteLayoutPass();
	}

	private static FuncControlTemplate ListBoxTemplate()
	{
		return new FuncControlTemplate<ListBox>((parent, scope) =>
			new ScrollViewer
			{
				Name = "PART_ScrollViewer",
				Template = ScrollViewerTemplate(),
				Content = new ItemsPresenter
				{
					Name = "PART_ItemsPresenter",
					[~ItemsPresenter.ItemsPanelProperty] = parent.GetObservable(ItemsControl.ItemsPanelProperty).ToBinding()
				}.RegisterInNameScope(scope)
			}.RegisterInNameScope(scope));
	}

	private static void Prepare(ListBox target)
	{
		target.Width = target.Height = 100;
		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
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

	private void RaisePressedEvent(ListBoxItem item, MouseButton mouseButton)
	{
		_mouse.Click(item, item, mouseButton);
	}

	private static FuncControlTemplate ScrollViewerTemplate()
	{
		return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
			new Panel
			{
				Children =
				{
					new ScrollContentPresenter
					{
						Name = "PART_ContentPresenter"
					}.RegisterInNameScope(scope),
					new ScrollBar
					{
						Name = "verticalScrollBar",
						Orientation = Orientation.Vertical
					}
				}
			});
	}

	#endregion

	#region Classes

	private class DataVirtualizingList : IList
	{
		#region Fields

		private readonly List<string> _inner = new(Enumerable.Repeat<string>(null, 100));

		#endregion

		#region Properties

		public int Count => _inner.Count;

		public bool IsFixedSize => true;
		public bool IsReadOnly => true;
		public bool IsSynchronized => false;

		public object this[int index]
		{
			get => _inner[index] = $"Item{index}";
			set => throw new NotSupportedException();
		}

		public object SyncRoot => this;

		#endregion

		#region Methods

		public int Add(object value)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(object value)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public IEnumerator GetEnumerator()
		{
			return _inner.GetEnumerator();
		}

		public IEnumerable<string> GetRealizedItems()
		{
			return _inner.Where(x => x is not null)!;
		}

		public int IndexOf(object value)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, object value)
		{
			throw new NotSupportedException();
		}

		public void Remove(object value)
		{
			throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		#endregion
	}

	private class Item
	{
		#region Constructors

		public Item(string value)
		{
			Value = value;
		}

		#endregion

		#region Properties

		public string Value { get; }

		#endregion
	}

	private class ResettingCollection : List<string>, INotifyCollectionChanged
	{
		#region Constructors

		public ResettingCollection(int itemCount)
		{
			AddRange(Enumerable.Range(0, itemCount).Select(x => $"Item{x}"));
		}

		#endregion

		#region Methods

		public new void Reverse()
		{
			base.Reverse();
			CollectionChanged?.Invoke(
				this,
				new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		#endregion

		#region Events

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion
	}

	#endregion

	#region Records

	private record ItemViewModel(string Caption);

	#endregion
}