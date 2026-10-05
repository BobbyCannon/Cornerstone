#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Selection;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Controls.Utils;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TabControlTests : ScopedTestBase
{
	#region Constructors

	static TabControlTests()
	{
		RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void CanHaveEmptyTabControl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.Markup.Xaml.UnitTests.Xaml;assembly=Cornerstone.Presentation.Markup.Xaml.UnitTests'>
    <TabControl Name='tabs' ItemsSource='{Binding Tabs}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var tabControl = window.GetControl<TabControl>("tabs");

			tabControl.DataContext = new { Tabs = new List<string>() };
			window.ApplyTemplate();

			CornerstoneTest.AreEqual(0, tabControl.ItemsSource.Count());
		}
	}

	[PresentationTestMethod]
	public void ContentShouldNotTemporarilyGetWrongDataContextWhenSwitchingTabs()
	{
		// When ContentPart.Content is set, ContentPresenter.UpdateChild clears its
		// DataContext before we can set it to the container's DataContext. This causes
		// the content to briefly inherit TabControl's DataContext instead of TabItem's.
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var viewModel = new MainViewModel();

		var tab1View = new UserControl();
		var tab2View = new UserControl();

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = viewModel,
			Items =
			{
				new TabItem
				{
					Header = "Tab1",
					[~TabItem.DataContextProperty] = new Binding("Tab1"),
					Content = tab1View
				},
				new TabItem
				{
					Header = "Tab2",
					[~TabItem.DataContextProperty] = new Binding("Tab2"),
					Content = tab2View
				}
			}
		};

		var root = new TestRoot(target);
		Prepare(target);

		CornerstoneTest.Same(viewModel.Tab1, tab1View.DataContext);

		// Track all DataContext values the new content receives during the switch.
		var dataContexts = new List<object>();
		tab2View.PropertyChanged += (s, e) =>
		{
			if (e.Property == StyledElement.DataContextProperty)
			{
				dataContexts.Add(e.NewValue);
			}
		};

		target.SelectedIndex = 1;

		// tab2View should only have received the correct DataContext (Tab2ViewModel).
		// It should NOT have temporarily received the TabControl's DataContext (MainViewModel).
		CornerstoneTest.All(dataContexts, dc => CornerstoneTest.Same(viewModel.Tab2, dc));
		CornerstoneTest.Same(viewModel.Tab2, tab2View.DataContext);
	}

	[PresentationTestMethod]
	public void ContentTemplateWithControlContentShouldSetDataContextToContent()
	{
		// When a TabItem has a ContentTemplate and its Content is a Control, the
		// ContentPresenter should set DataContext = content (so the template can bind
		// to the control's properties), not the TabItem's DataContext.
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var viewModel = new MainViewModel();
		var userControl = new UserControl { Tag = "my-content" };

		TextBlock templateChild = null;
		var contentTemplate = new FuncDataTemplate<UserControl>((x, _) =>
		{
			templateChild = new TextBlock();
			templateChild.Bind(TextBlock.TextProperty, new Binding("Tag"));
			return templateChild;
		});

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = viewModel,
			Items =
			{
				new TabItem
				{
					Header = "Tab1",
					[~TabItem.DataContextProperty] = new Binding("Tab1"),
					ContentTemplate = contentTemplate,
					Content = userControl
				}
			}
		};

		var root = new TestRoot(target);
		Prepare(target);

		// The ContentPresenter's DataContext should be the content (UserControl),
		// not the TabItem's DataContext (Tab1ViewModel), because ContentTemplate is set.
		CornerstoneTest.Same(userControl, target.ContentPart!.DataContext);
		CornerstoneTest.IsNotNull(templateChild);
		CornerstoneTest.AreEqual("my-content", templateChild!.Text);
	}

	[PresentationTestMethod]
	public void ContentTemplateWithControlContentShouldSetDataContextToContentAfterTabSwitch()
	{
		// Same as above but verifies the behavior after switching tabs.
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var viewModel = new MainViewModel();
		var userControl = new UserControl { Tag = "my-content" };

		TextBlock templateChild = null;
		var contentTemplate = new FuncDataTemplate<UserControl>((x, _) =>
		{
			templateChild = new TextBlock();
			templateChild.Bind(TextBlock.TextProperty, new Binding("Tag"));
			return templateChild;
		});

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = viewModel,
			Items =
			{
				new TabItem
				{
					Header = "Tab1",
					[~TabItem.DataContextProperty] = new Binding("Tab1"),
					ContentTemplate = contentTemplate,
					Content = userControl
				},
				new TabItem
				{
					Header = "Tab2",
					Content = "Other content"
				}
			}
		};

		var root = new TestRoot(target);
		Prepare(target);

		CornerstoneTest.Same(userControl, target.ContentPart!.DataContext);

		// Switch away and back.
		target.SelectedIndex = 1;
		target.SelectedIndex = 0;

		// DataContext should still be the content, not the TabItem's DataContext.
		CornerstoneTest.Same(userControl, target.ContentPart!.DataContext);
		CornerstoneTest.IsNotNull(templateChild);
		CornerstoneTest.AreEqual("my-content", templateChild!.Text);
	}

	[PresentationTestMethod]
	public void DataContextsShouldBeCorrectlySet()
	{
		using var app = Start();
		var items = new object[]
		{
			"Foo",
			new Item("Bar"),
			new TextBlock { Text = "Baz" },
			new TabItem { Content = "Qux" },
			new TabItem { Content = new TextBlock { Text = "Bob" } },
			new TabItem { DataContext = "Rob", Content = new TextBlock { Text = "Bob" } }
		};

		var target = new TabControl
		{
			DataContext = "Base",
			ItemsSource = items
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		var dataContext = ((TextBlock) target.ContentPart!.Child!).DataContext;
		CornerstoneTest.AreEqual(items[0], dataContext);

		target.SelectedIndex = 1;
		dataContext = ((Button) target.ContentPart.Child).DataContext;
		CornerstoneTest.AreEqual(items[1], dataContext);

		target.SelectedIndex = 2;
		dataContext = ((TextBlock) target.ContentPart.Child).DataContext;
		CornerstoneTest.AreEqual("Base", dataContext);

		target.SelectedIndex = 3;
		dataContext = ((TextBlock) target.ContentPart.Child).DataContext;
		CornerstoneTest.AreEqual("Qux", dataContext);

		target.SelectedIndex = 4;
		dataContext = target.ContentPart.DataContext;
		CornerstoneTest.AreEqual("Base", dataContext);

		target.SelectedIndex = 5;
		dataContext = target.ContentPart.Child.DataContext;
		CornerstoneTest.AreEqual("Rob", dataContext);
	}

	[PresentationTestMethod]
	public void DataTemplateCreatedContentShouldBeLogicalChildAfterApplyTemplate()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			ContentTemplate = new FuncDataTemplate<string>((x, _) =>
				new TextBlock { Tag = "bar", Text = x }),
			ItemsSource = new[] { "Foo" }
		};
		var root = new TestRoot(target);

		ApplyTemplate(target);
		target.ContentPart!.UpdateChild();

		var content = CornerstoneTest.IsType<TextBlock>(target.ContentPart.Child);
		CornerstoneTest.AreEqual("bar", content.Tag);
		CornerstoneTest.Same(target, content.GetLogicalParent());
		CornerstoneTest.Single(target.GetLogicalChildren(), content);
	}

	[PresentationTestMethod]
	public void FirstTabShouldBeSelectedByDefault()
	{
		TabItem selected;
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				(selected = new TabItem
				{
					Name = "first",
					Content = "foo"
				}),
				new TabItem
				{
					Name = "second",
					Content = "bar"
				}
			}
		};

		target.ApplyTemplate();

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual(selected, target.SelectedItem);
	}

	[PresentationTestMethod]
	public void InterruptedPageTransitionCanSelectOriginalControlBeforePreviousTransitionCompletes()
	{
		using var app = Start();

		var firstPage = new ContentPage { Content = "Alpha" };
		var secondPage = new ContentPage { Content = "Beta" };
		var starts = new List<(object FromContent, object ToContent, bool Forward)>();
		var transitionGate = new TaskCompletionSource();
		var transition = new StubPageTransition();
		transition.StartHandler = (from, to, forward, _) =>
		{
			starts.Add((
				(from as ContentPresenter)?.Content,
				(to as ContentPresenter)?.Content,
				forward));
			return transitionGate.Task;
		};

		var target = new TabControl
		{
			PageTransition = transition,
			Items =
			{
				new TabItem { Name = "first", Content = firstPage },
				new TabItem { Name = "second", Content = secondPage }
			}
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		target.SelectedIndex = 1;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.Single(starts);
		CornerstoneTest.Same(firstPage, starts[0].FromContent);
		CornerstoneTest.Same(secondPage, starts[0].ToContent);
		CornerstoneTest.IsTrue(starts[0].Forward);

		var exception = Record.Exception(() => target.SelectedIndex = 0);

		CornerstoneTest.IsNull(exception);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(2, starts.Count);
		CornerstoneTest.IsNull(starts[1].FromContent);
		CornerstoneTest.Same(firstPage, starts[1].ToContent);
		CornerstoneTest.IsFalse(starts[1].Forward);
		CornerstoneTest.Same(firstPage, target.SelectedContent);
	}

	[PresentationTestMethod]
	public void InterruptedPageTransitionClearsReusedControlFromOwningSelectedContentHost()
	{
		using var app = Start();

		var firstPage = new ContentPage { Content = "Alpha" };
		var secondPage = new ContentPage { Content = "Beta" };
		var transition = new StubPageTransition();

		var target = new TabControl
		{
			Items =
			{
				new TabItem { Name = "first", Content = firstPage },
				new TabItem { Name = "second", Content = secondPage }
			}
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		target.SelectedIndex = 1;
		root.LayoutManager.ExecuteLayoutPass();

		var primary = target.GetVisualDescendants()
			.OfType<ContentPresenter>()
			.Single(x => x.Name == "PART_SelectedContentHost");
		var secondary = target.GetVisualDescendants()
			.OfType<ContentPresenter>()
			.Single(x => x.Name == "PART_SelectedContentHost2");

		// Simulate the stale presenter ownership that can happen when tab changes
		// interrupt a transition: the page is still parented by the named content
		// host, but the active field no longer points at that host.
		primary.SetContentWithDataContext(firstPage, null);
		secondary.IsVisible = false;
		SetPrivateField(target, "_contentPart", secondary);
		SetPrivateField(target, "_contentPresenter2", secondary);

		target.PageTransition = transition;
		var exception = Record.Exception(() => target.SelectedIndex = 0);

		CornerstoneTest.IsNull(exception);
		CornerstoneTest.Same(firstPage, target.SelectedContent);
		CornerstoneTest.IsNull(primary.Content);
		CornerstoneTest.Same(firstPage, secondary.Content);
	}

	[PresentationTestMethod]
	public void LogicalChildrenShouldBeTabItems()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem
				{
					Content = "foo"
				},
				new TabItem
				{
					Content = "bar"
				}
			}
		};

		CornerstoneTest.AreEqual(target.Items, target.GetLogicalChildren().ToList());
		target.ApplyTemplate();
		CornerstoneTest.AreEqual(target.Items, target.GetLogicalChildren().ToList());
	}

	/// <summary>
	/// Non-headered control items should result in TabItems with empty header.
	/// </summary>
	/// <remarks>
	/// If a TabControl is created with non IHeadered controls as its items, don't try to
	/// display the control in the header: if the control is part of the header then
	/// *that* control would also end up in the content region, resulting in dual-parentage
	/// breakage.
	/// </remarks>
	[PresentationTestMethod]
	public void NonIHeaderedControlItemsShouldBeIgnored()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TextBlock { Text = "foo" },
				new TextBlock { Text = "bar" }
			}
		};

		ApplyTemplate(target);

		var logicalChildren = target.GetLogicalChildren();

		var result = logicalChildren
			.OfType<TabItem>()
			.Select(x => x.Header)
			.ToList();

		CornerstoneTest.AreEqual(new object[] { null, null }, result);
	}

	[PresentationTestMethod]
	public void OnlyFirstEnabledTabShouldBeSelectedByDefault()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem { Header = "disabled", IsEnabled = false },
				new TabItem { Header = "enabled" }
			}
		};

		ApplyTemplate(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void OnlyFirstVisibleAndEnabledTabShouldBeSelectedByDefault()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem { Header = "hidden", IsVisible = false },
				new TabItem { Header = "visible" }
			}
		};

		ApplyTemplate(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void PageTransitionForwardIsFalseWhenSwitchingToEarlierTab()
	{
		using var app = Start();

		var transition = new StubPageTransition();

		var target = new TabControl
		{
			PageTransition = transition,
			Items =
			{
				new TabItem { Name = "first", Content = "Alpha" },
				new TabItem { Name = "second", Content = "Beta" },
				new TabItem { Name = "third", Content = "Gamma" }
			}
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		// Go forward to tab 2
		target.SelectedIndex = 2;
		root.LayoutManager.ExecuteLayoutPass();

		// Now go backward to tab 0
		target.SelectedIndex = 0;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(transition.Calls.WasCalled("Start"));
		CornerstoneTest.IsFalse((bool) transition.Calls.Arguments("Start")[^1][2]);
	}

	[PresentationTestMethod]
	public void PageTransitionIsNullByDefault()
	{
		var target = new TabControl { Template = TabControlTemplate() };
		CornerstoneTest.IsNull(target.PageTransition);
	}

	[PresentationTestMethod]
	public void PageTransitionRoundTrips()
	{
		var transition = new CrossFade(TimeSpan.FromMilliseconds(100));
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			PageTransition = transition
		};
		CornerstoneTest.Same(transition, target.PageTransition);
	}

	[PresentationTestMethod]
	public void PageTransitionStartIsCalledWhenTabSwitches()
	{
		using var app = Start();

		var transition = new StubPageTransition();

		var target = new TabControl
		{
			PageTransition = transition,
			Items =
			{
				new TabItem { Name = "first", Content = "Alpha" },
				new TabItem { Name = "second", Content = "Beta" }
			}
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		// Switch tab — triggers shouldTransition = true and InvalidateArrange
		target.SelectedIndex = 1;

		// Execute layout pass to invoke ArrangeOverride, which fires the transition
		root.LayoutManager.ExecuteLayoutPass();

		transition.Calls.VerifyCalled("Start", 1);
		CornerstoneTest.IsTrue((bool) transition.Calls.Arguments("Start")[0][2]);
	}

	[PresentationTestMethod]
	public void PendingPageTransitionCanSelectOriginalControlBeforeTransitionStarts()
	{
		using var app = Start();

		var firstPage = new ContentPage { Content = "Alpha" };
		var secondPage = new ContentPage { Content = "Beta" };
		var starts = new List<(object FromContent, object ToContent, bool Forward)>();
		var transition = new StubPageTransition();
		transition.StartHandler = (from, to, forward, _) =>
		{
			starts.Add((
				(from as ContentPresenter)?.Content,
				(to as ContentPresenter)?.Content,
				forward));
			return Task.CompletedTask;
		};

		var target = new TabControl
		{
			PageTransition = transition,
			Items =
			{
				new TabItem { Name = "first", Content = firstPage },
				new TabItem { Name = "second", Content = secondPage }
			}
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		target.SelectedIndex = 1;
		var exception = Record.Exception(() => target.SelectedIndex = 0);

		CornerstoneTest.IsNull(exception);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.Single(starts);
		CornerstoneTest.IsNull(starts[0].FromContent);
		CornerstoneTest.Same(firstPage, starts[0].ToContent);
		CornerstoneTest.IsFalse(starts[0].Forward);
		CornerstoneTest.Same(firstPage, target.SelectedContent);
	}

	[PresentationTestMethod]
	public void PreSelectingTabItemShouldSetSelectedContentAfterItWasAdded()
	{
		const string secondContent = "Second";
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem { Header = "First" },
				new TabItem { Header = "Second", Content = secondContent, IsSelected = true }
			}
		};

		ApplyTemplate(target);

		CornerstoneTest.AreEqual(secondContent, target.SelectedContent);
	}

	[PresentationTestMethod]
	public void PreviousContentTemplateIsNotReusedWhenTabItemChanges()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var templatesBuilt = 0;

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				TabItemFactory("First tab content"),
				TabItemFactory("Second tab content")
			}
		};

		var root = new TestRoot(target);
		ApplyTemplate(target);

		target.SelectedIndex = 0;
		target.SelectedIndex = 1;

		CornerstoneTest.AreEqual(2, templatesBuilt);

		TabItem TabItemFactory(object content)
		{
			return new()
			{
				Content = content,
				ContentTemplate = new FuncDataTemplate<object>((actual, ns) =>
				{
					CornerstoneTest.AreEqual(content, actual);
					templatesBuilt++;
					return new Border();
				})
			};
		}
	}

	[PresentationTestMethod]
	public void RemovalShouldSetFirstTab()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem
				{
					Name = "first",
					Content = "foo"
				},
				new TabItem
				{
					Name = "second",
					Content = "bar"
				},
				new TabItem
				{
					Name = "3rd",
					Content = "barf"
				}
			}
		};

		Prepare(target);
		target.SelectedItem = target.Items[1];

		var item = CornerstoneTest.IsType<TabItem>(target.Items[1]);
		CornerstoneTest.Same(item, target.SelectedItem);
		CornerstoneTest.AreEqual(item.Content, target.SelectedContent);

		target.Items.RemoveAt(1);

		item = CornerstoneTest.IsType<TabItem>(target.Items[0]);
		CornerstoneTest.Same(item, target.SelectedItem);
		CornerstoneTest.AreEqual(item.Content, target.SelectedContent);
	}

	[PresentationTestMethod]
	public void RemovalShouldSetNewItem0WhenItem0Selected()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem
				{
					Name = "first",
					Content = "foo"
				},
				new TabItem
				{
					Name = "second",
					Content = "bar"
				},
				new TabItem
				{
					Name = "3rd",
					Content = "barf"
				}
			}
		};

		Prepare(target);
		target.SelectedItem = target.Items[0];

		var item = CornerstoneTest.IsType<TabItem>(target.Items[0]);
		CornerstoneTest.Same(item, target.SelectedItem);
		CornerstoneTest.AreEqual(item.Content, target.SelectedContent);

		target.Items.RemoveAt(0);

		item = CornerstoneTest.IsType<TabItem>(target.Items[0]);
		CornerstoneTest.Same(item, target.SelectedItem);
		CornerstoneTest.AreEqual(item.Content, target.SelectedContent);
	}

	[PresentationTestMethod]
	public void RemovalShouldSetNewItem0WhenItem0SelectedWithDataTemplate()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var collection = new ObservableCollection<Item>
		{
			new("first"),
			new("second"),
			new("3rd")
		};

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			ItemsSource = collection
		};

		Prepare(target);
		target.SelectedItem = collection[0];

		CornerstoneTest.Same(collection[0], target.SelectedItem);
		CornerstoneTest.AreEqual(collection[0], target.SelectedContent);

		collection.RemoveAt(0);

		CornerstoneTest.Same(collection[0], target.SelectedItem);
		CornerstoneTest.AreEqual(collection[0], target.SelectedContent);
	}

	[PresentationTestMethod]
	public void SelectedContentTemplateUpdatesAfterNewContentTemplate()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			ItemsSource = new[] { "Foo" }
		};
		var root = new TestRoot(target);

		ApplyTemplate(target);
		target.ContentPart!.UpdateChild();

		CornerstoneTest.AreEqual(null, CornerstoneTest.IsType<TextBlock>(target.ContentPart.Child).Tag);

		target.ContentTemplate = new FuncDataTemplate<string>((x, _) =>
			new TextBlock { Tag = "bar", Text = x });

		CornerstoneTest.AreEqual("bar", CornerstoneTest.IsType<TextBlock>(target.ContentPart.Child).Tag);
	}

	[PresentationTestMethod]
	public void ShouldHandleChangingToTabItemWithNullContent()
	{
		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem { Header = "Foo" },
				new TabItem { Header = "Foo", Content = new Decorator() },
				new TabItem { Header = "Baz" }
			}
		};

		ApplyTemplate(target);

		target.SelectedIndex = 2;

		var page = CornerstoneTest.IsType<TabItem>(target.SelectedItem);

		CornerstoneTest.IsNull(page.Content);
	}

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void ShouldHaveInitialSelectedValue()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var xaml = @"
        <TabControl
            xmlns='https://github.com/BobbyCannon/Cornerstone'
            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            xmlns:local='clr-namespace:Cornerstone.Presentation.Markup.Xaml.UnitTests.Xaml;assembly=Cornerstone.Presentation.Markup.Xaml.UnitTests'
            x:DataType='TabItem'
            x:Name='tabs'
            Tag='World' 
            SelectedValue='{Binding $self.Tag}' 
            SelectedValueBinding='{Binding Header}'>
            <TabItem Header='Hello'/>
            <TabItem Header='World'/>
        </TabControl>";

		var tabControl = (TabControl) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual("World", tabControl.SelectedValue);
		CornerstoneTest.AreEqual(1, tabControl.SelectedIndex);
	}

	[PresentationTestMethod]
	public void ShouldNotPropagateDataContextToTabItemContent()
	{
		var dataContext = "DataContext";

		var tabItem = new TabItem();

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = dataContext,
			Items = { tabItem }
		};

		ApplyTemplate(target);

		CornerstoneTest.AreNotEqual(dataContext, tabItem.Content);
	}

	[PresentationTestMethod]
	[DataRow(Key.A, "a", 1)]
	[DataRow(Key.L, "l", 2)]
	[DataRow(Key.D, "d", 0)]
	public void ShouldTabControlRecognizesAccessKey(Key accessKey, string accessKeySymbol, int selectedTabIndex)
	{
		var kd = new KeyboardDevice();
		using (UnitTestApplication.Start(TestServices.StyledWindow
					.With(
						accessKeyHandler: () => new AccessKeyHandler(),
						keyboardDevice: () => kd)
				))
		{
			var impl = CreateMockTopLevelImpl();

			var tabControl = new TabControl
			{
				Template = TabControlTemplate(),
				Items =
				{
					new TabItem
					{
						Header = "General"
					},
					new TabItem { Header = "_Arch" },
					new TabItem { Header = "_Leaf" },
					new TabItem { Header = "_Disabled", IsEnabled = false }
				}
			};
			kd.SetFocusedElement((TabItem) tabControl.Items[selectedTabIndex], NavigationMethod.Unspecified, KeyModifiers.None);

			var root = new TestTopLevel(impl)
			{
				Template = CreateTemplate(),
				Content = tabControl
			};

			root.ApplyTemplate();
			root.Presenter!.UpdateChild();
			ApplyTemplate(tabControl);

			KeyDown(root, Key.LeftAlt);
			KeyDown(root, accessKey, accessKeySymbol, KeyModifiers.Alt);
			KeyUp(root, accessKey, accessKeySymbol, KeyModifiers.Alt);
			KeyUp(root, Key.LeftAlt);

			CornerstoneTest.AreEqual(selectedTabIndex, tabControl.SelectedIndex);
		}

		static FuncControlTemplate<TestTopLevel> CreateTemplate()
		{
			return new FuncControlTemplate<TestTopLevel>((x, scope) =>
				new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty),
					[~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(ContentControl.ContentTemplateProperty)
				}.RegisterInNameScope(scope));
		}

		static StubWindowImpl CreateMockTopLevelImpl(bool setupProperties = false)
		{
			var topLevel = new StubWindowImpl();
			if (setupProperties)
			{
			}
			return topLevel;
		}

		static void KeyDown(IInputElement target, Key key, string keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
		{
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = key,
				KeySymbol = keySymbol,
				KeyModifiers = modifiers
			});
		}

		static void KeyUp(IInputElement target, Key key, string keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
		{
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyUpEvent,
				Key = key,
				KeySymbol = keySymbol,
				KeyModifiers = modifiers
			});
		}
	}

	[PresentationTestMethod]
	public void SwitchingTabShouldPreserveDataContextBindingOnUserControlContent()
	{
		// Issue #18280: When switching tabs, a UserControl inside a TabItem has its
		// DataContext set to null, causing two-way bindings on child controls (like
		// DataGrid.SelectedItem) to propagate null back to the view model.
		// Verify that after switching away and back, the DataContext binding still
		// resolves correctly.
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var viewModel = new TabDataContextViewModel { SelectedItem = "Item1" };

		// Create a UserControl with an explicit DataContext binding,
		// matching the issue scenario.
		var userControl = new UserControl
		{
			[~UserControl.DataContextProperty] = new Binding("SelectedItem")
		};

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = viewModel,
			Items =
			{
				new TabItem
				{
					Header = "Tab1",
					Content = userControl
				},
				new TabItem
				{
					Header = "Tab2",
					Content = "Other content"
				}
			}
		};

		var root = new TestRoot(target);
		Prepare(target);

		// Verify initial state
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("Item1", userControl.DataContext);

		// Switch to second tab and back
		target.SelectedIndex = 1;
		target.SelectedIndex = 0;

		// The UserControl's DataContext binding should still resolve correctly.
		CornerstoneTest.AreEqual("Item1", userControl.DataContext);

		// Verify the binding is still live by changing the source property.
		viewModel.SelectedItem = "Item2";
		CornerstoneTest.AreEqual("Item2", userControl.DataContext);
	}

	[PresentationTestMethod]
	public void SwitchingTabsShouldNotNullOutDataContextBoundProperties()
	{
		// Issue #20845: DataContext binding should survive tab switches.
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var viewModel = new MainViewModel();

		var tab1View = new UserControl();
		tab1View.Bind(UserControl.DataContextProperty, new Binding("Tab1"));
		var textBlock = new TextBlock();
		textBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
		tab1View.Content = textBlock;

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = viewModel,
			Items =
			{
				new TabItem
				{
					Header = "Tab1",
					Content = tab1View
				},
				new TabItem
				{
					Header = "Tab2",
					Content = "Other content"
				}
			}
		};

		var root = new TestRoot(target);
		Prepare(target);

		CornerstoneTest.Same(viewModel.Tab1, tab1View.DataContext);
		CornerstoneTest.AreEqual("Tab 1 message here", textBlock.Text);

		// Switch to tab 2 and back
		target.SelectedIndex = 1;
		target.SelectedIndex = 0;

		// DataContext binding should still be resolved correctly.
		CornerstoneTest.Same(viewModel.Tab1, tab1View.DataContext);
		CornerstoneTest.AreEqual("Tab 1 message here", textBlock.Text);
	}

	[PresentationTestMethod]
	public void TabControlIndicatorTemplateCanBeSetToNull()
	{
		var template = new FuncDataTemplate<object>((_, _) => new Border());
		var tc = new TabControl { IndicatorTemplate = template };
		tc.IndicatorTemplate = null;
		CornerstoneTest.IsNull(tc.IndicatorTemplate);
	}

	[PresentationTestMethod]
	public void TabControlIndicatorTemplateDefaultIsNull()
	{
		var tc = new TabControl();
		CornerstoneTest.IsNull(tc.IndicatorTemplate);
	}

	[PresentationTestMethod]
	public void TabControlIndicatorTemplateDoesNotOverwriteUserSetTabItemIndicatorTemplate()
	{
		var tabItems = new[]
		{
			new TabItem { Header = "A" },
			new TabItem { Header = "B" }
		};
		var userTemplate = new FuncDataTemplate<object>((_, _) => new Border());
		tabItems[0].IndicatorTemplate = userTemplate;

		var tabControlTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock());
		var tc = new TabControl
		{
			ItemsSource = tabItems,
			IndicatorTemplate = tabControlTemplate,
			Template = new FuncControlTemplate<TabControl>((_, scope) =>
			{
				var ip = new ItemsPresenter { Name = "PART_ItemsPresenter" };
				scope.Register("PART_ItemsPresenter", ip);
				var cp = new ContentPresenter { Name = "PART_SelectedContentHost" };
				scope.Register("PART_SelectedContentHost", cp);
				return new Panel { Children = { ip, cp } };
			})
		};

		var root = new TestRoot { Child = tc };
		tc.ApplyTemplate();
		tc.Presenter?.ApplyTemplate();

		// TabItem with a local value must keep it
		CornerstoneTest.Same(userTemplate, tabItems[0].IndicatorTemplate);

		// TabItem without a local value gets the TabControl template
		CornerstoneTest.Same(tabControlTemplate, tabItems[1].IndicatorTemplate);
	}

	[PresentationTestMethod]
	public void TabControlIndicatorTemplateRoundTrips()
	{
		var template = new FuncDataTemplate<object>((_, _) => new Border());
		var tc = new TabControl { IndicatorTemplate = template };
		CornerstoneTest.Same(template, tc.IndicatorTemplate);
	}

	[PresentationTestMethod]
	public void TabItemChildDataContextBindingShouldWork()
	{
		// Issue #20845: When a DataContext binding is placed on the child of a TabItem,
		// the DataContext is null. The binding hasn't resolved when the content's
		// DataContext is captured in UpdateSelectedContent, so the captured value is null.
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var viewModel = new MainViewModel();

		var tab1View = new UserControl();
		tab1View.Bind(UserControl.DataContextProperty, new Binding("Tab1"));

		// Add a child TextBlock that binds to a property on Tab1ViewModel.
		var textBlock = new TextBlock();
		textBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
		tab1View.Content = textBlock;

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = viewModel,
			Items =
			{
				new TabItem
				{
					Header = "Tab1",
					Content = tab1View
				}
			}
		};

		var root = new TestRoot(target);
		Prepare(target);

		// The UserControl's DataContext should be the Tab1ViewModel.
		CornerstoneTest.Same(viewModel.Tab1, tab1View.DataContext);

		// The TextBlock should display the Name from Tab1ViewModel.
		CornerstoneTest.AreEqual("Tab 1 message here", textBlock.Text);
	}

	[PresentationTestMethod]
	public void TabItemChildWithDataContextBindingShouldPropagateToChildren()
	{
		// Issue #20845 (comment): Putting the DataContext binding on the TabItem itself
		// is also broken. The child should inherit the TabItem's DataContext.
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var viewModel = new MainViewModel();

		var textBlock = new TextBlock();
		textBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
		var tab1View = new UserControl { Content = textBlock };

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = viewModel,
			Items =
			{
				new TabItem
				{
					Header = "Tab1",
					[~TabItem.DataContextProperty] = new Binding("Tab1"),
					Content = tab1View
				}
			}
		};

		var root = new TestRoot(target);
		Prepare(target);

		// The TabItem's DataContext should be the Tab1ViewModel.
		var tabItem = (TabItem) target.Items[0]!;
		CornerstoneTest.Same(viewModel.Tab1, tabItem.DataContext);

		// The UserControl should inherit the TabItem's DataContext.
		CornerstoneTest.Same(viewModel.Tab1, tab1View.DataContext);

		// The TextBlock should display the Name from Tab1ViewModel.
		CornerstoneTest.AreEqual("Tab 1 message here", textBlock.Text);
	}

	[PresentationTestMethod]
	public void TabItemHeaderShouldBeSettableByStyleWhenDataContextIsSet()
	{
		var tabItem = new TabItem
		{
			DataContext = "Some DataContext"
		};

		_ = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<TabItem>())
				{
					Setters =
					{
						new Setter(HeaderedContentControl.HeaderProperty, "Header from style")
					}
				}
			},
			Child = tabItem
		};

		CornerstoneTest.AreEqual("Header from style", tabItem.Header);
	}

	[PresentationTestMethod]
	public void TabItemIconChangeUpdatesPresenterContent()
	{
		var tabItem = new TabItem
		{
			Icon = "first",
			Template = TabItemWithIconTemplate()
		};

		var root = new TestRoot { Child = tabItem };
		tabItem.ApplyTemplate();
		tabItem.Presenter!.UpdateChild();

		var iconPresenter = tabItem.GetTemplateDescendants().OfType<ContentPresenter>().First(x => x.Name == "PART_IconPresenter");
		CornerstoneTest.AreEqual("first", iconPresenter!.Content);

		tabItem.Icon = "second";
		CornerstoneTest.AreEqual("second", iconPresenter.Content);
	}

	[PresentationTestMethod]
	public void TabItemIconTemplateCreatesContentFromNonControlIcon()
	{
		var tabItem = new TabItem
		{
			Icon = "home",
			IconTemplate = new FuncDataTemplate<object>((val, _) =>
				new TextBlock { Text = (string) val }),
			Template = TabItemWithIconTemplate()
		};

		var root = new TestRoot { Child = tabItem };
		tabItem.ApplyTemplate();
		tabItem.Presenter!.UpdateChild();

		var iconPresenter = tabItem.GetTemplateDescendants().OfType<ContentPresenter>().First(x => x.Name == "PART_IconPresenter");
		CornerstoneTest.IsNotNull(iconPresenter);
		CornerstoneTest.AreEqual("home", iconPresenter!.Content);
		CornerstoneTest.IsNotNull(iconPresenter.ContentTemplate);

		iconPresenter.UpdateChild();
		var textBlock = iconPresenter.Child as TextBlock;
		CornerstoneTest.IsNotNull(textBlock);
		CornerstoneTest.AreEqual("home", textBlock!.Text);
	}

	[PresentationTestMethod]
	public void TabItemIconWithoutTemplateRendersControlDirectly()
	{
		var icon = new Path
		{
			Data = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) }
		};
		var tabItem = new TabItem
		{
			Icon = icon,
			Template = TabItemWithIconTemplate()
		};

		var root = new TestRoot { Child = tabItem };
		tabItem.ApplyTemplate();
		tabItem.Presenter!.UpdateChild();

		var iconPresenter = tabItem.GetTemplateDescendants().OfType<ContentPresenter>().First(x => x.Name == "PART_IconPresenter");
		CornerstoneTest.IsNotNull(iconPresenter);
		CornerstoneTest.Same(icon, iconPresenter!.Content);
		CornerstoneTest.IsNull(iconPresenter.ContentTemplate);
	}

	[PresentationTestMethod]
	public void TabItemIndicatorTemplateCanBeSetToNull()
	{
		var template = new FuncDataTemplate<object>((_, _) => new Border());
		var tabItem = new TabItem { IndicatorTemplate = template };
		tabItem.IndicatorTemplate = null;
		CornerstoneTest.IsNull(tabItem.IndicatorTemplate);
	}

	[PresentationTestMethod]
	public void TabItemIndicatorTemplateDefaultIsNull()
	{
		var tabItem = new TabItem();
		CornerstoneTest.IsNull(tabItem.IndicatorTemplate);
	}

	[PresentationTestMethod]
	public void TabItemIndicatorTemplateRoundTrips()
	{
		var template = new FuncDataTemplate<object>((_, _) => new Border());
		var tabItem = new TabItem { IndicatorTemplate = template };
		CornerstoneTest.Same(template, tabItem.IndicatorTemplate);
	}

	[PresentationTestMethod]
	public void TabItemTabStripPlacementShouldBeCorrectlySet()
	{
		var items = new object[]
		{
			"Foo",
			new TabItem { Content = new TextBlock { Text = "Baz" } }
		};

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = "Base",
			ItemsSource = items
		};

		ApplyTemplate(target);

		var result = target.GetLogicalChildren()
			.OfType<TabItem>()
			.ToList();
		CornerstoneTest.Collection(result, x => CornerstoneTest.AreEqual(Dock.Top, x.TabStripPlacement), x => CornerstoneTest.AreEqual(Dock.Top, x.TabStripPlacement));

		target.TabStripPlacement = Dock.Right;
		result = target.GetLogicalChildren()
			.OfType<TabItem>()
			.ToList();
		CornerstoneTest.Collection(result, x => CornerstoneTest.AreEqual(Dock.Right, x.TabStripPlacement), x => CornerstoneTest.AreEqual(Dock.Right, x.TabStripPlacement));
	}

	[PresentationTestMethod]
	public void TabItemTabStripPlacementShouldBeCorrectlySetForNewItems()
	{
		var items = new object[]
		{
			"Foo",
			new TabItem { Content = new TextBlock { Text = "Baz" } }
		};

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			DataContext = "Base"
		};

		ApplyTemplate(target);

		target.ItemsSource = items;

		var result = target.GetLogicalChildren()
			.OfType<TabItem>()
			.ToList();
		CornerstoneTest.Collection(result, x => CornerstoneTest.AreEqual(Dock.Top, x.TabStripPlacement), x => CornerstoneTest.AreEqual(Dock.Top, x.TabStripPlacement));

		target.TabStripPlacement = Dock.Right;
		result = target.GetLogicalChildren()
			.OfType<TabItem>()
			.ToList();
		CornerstoneTest.Collection(result, x => CornerstoneTest.AreEqual(Dock.Right, x.TabStripPlacement), x => CornerstoneTest.AreEqual(Dock.Right, x.TabStripPlacement));
	}

	[PresentationTestMethod]
	public void TabItemTemplatesShouldBeSetBeforeTabItemApplyTemplate()
	{
		var template = new FuncControlTemplate<TabItem>((x, __) => new Decorator());
		TabControl target;
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<TabItem>())
				{
					Setters =
					{
						new Setter(TemplatedControl.TemplateProperty, template)
					}
				}
			},
			Child = target = new TabControl
			{
				Template = TabControlTemplate(),
				Items =
				{
					new TabItem
					{
						Name = "first",
						Content = "foo"
					},
					new TabItem
					{
						Name = "second",
						Content = "bar"
					},
					new TabItem
					{
						Name = "3rd",
						Content = "barf"
					}
				}
			}
		};

		var collection = target.Items.Cast<TabItem>().ToList();
		CornerstoneTest.Same(collection[0].Template, template);
		CornerstoneTest.Same(collection[1].Template, template);
		CornerstoneTest.Same(collection[2].Template, template);
	}

	[PresentationTestMethod]
	public void TabNavigationShouldMoveToAnchorTabItem()
	{
		var services = TestServices.StyledWindow.With(
			keyboardDevice: () => new KeyboardDevice());
		using var app = UnitTestApplication.Start(services);

		var target = new TestTabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem { Header = "foo" },
				new TabItem { Header = "bar" },
				new TabItem { Header = "baz" }
			}
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
	public void TabNavigationShouldMoveToFirstTabItemWhenNoAnchorElementSelected()
	{
		var services = TestServices.StyledWindow.With(
			keyboardDevice: () => new KeyboardDevice());
		using var app = UnitTestApplication.Start(services);

		var target = new TabControl
		{
			Template = TabControlTemplate(),
			Items =
			{
				new TabItem { Header = "foo" },
				new TabItem { Header = "bar" },
				new TabItem { Header = "baz" }
			}
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
	public void TransitionShouldNotApplyNewDataContextToOldContent()
	{
		// When a PageTransition is set, the old content stays in ContentPart while the
		// new content goes into _contentPresenter2. The DataContext subscription for the
		// new container should not update ContentPart's DataContext (which still holds
		// the old content).
		using var app = Start();

		var viewModel = new MainViewModel();

		var tab1View = new UserControl();
		var tab2View = new UserControl();

		var transition = new StubPageTransition();

		var target = new TabControl
		{
			PageTransition = transition,
			DataContext = viewModel,
			Items =
			{
				new TabItem
				{
					Header = "Tab1",
					[~TabItem.DataContextProperty] = new Binding("Tab1"),
					Content = tab1View
				},
				new TabItem
				{
					Header = "Tab2",
					[~TabItem.DataContextProperty] = new Binding("Tab2"),
					Content = tab2View
				}
			}
		};

		var root = CreateRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.Same(viewModel.Tab1, tab1View.DataContext);

		// Track all DataContext values the OLD content receives during the transition.
		var oldContentDataContexts = new List<object>();
		tab1View.PropertyChanged += (s, e) =>
		{
			if (e.Property == StyledElement.DataContextProperty)
			{
				oldContentDataContexts.Add(e.NewValue);
			}
		};

		// Switch tab — triggers transition
		target.SelectedIndex = 1;
		root.LayoutManager.ExecuteLayoutPass();

		// The old content (tab1View) should NOT have received Tab2's DataContext.
		CornerstoneTest.DoesNotContain(oldContentDataContexts, viewModel.Tab2);
	}

	private static void ApplyTemplate(TabControl target)
	{
		target.ApplyTemplate();

		target.Presenter!.ApplyTemplate();

		foreach (var tabItem in target.GetLogicalChildren().OfType<TabItem>())
		{
			tabItem.Template = TabItemTemplate();

			tabItem.ApplyTemplate();

			tabItem.Presenter!.UpdateChild();
		}

		target.ContentPart!.ApplyTemplate();
	}

	private static TestRoot CreateRoot(Control child)
	{
		return new TestRoot
		{
			Resources =
			{
				{ typeof(TabControl), CreateTabControlControlTheme() },
				{ typeof(TabItem), CreateTabItemControlTheme() }
			},
			DataTemplates =
			{
				new FuncDataTemplate<Item>((x, _) => new Button { Content = x.Value })
			},
			Child = child
		};
	}

	private static ControlTheme CreateTabControlControlTheme()
	{
		return new ControlTheme(typeof(TabControl))
		{
			Setters =
			{
				new Setter(TabControl.TemplateProperty, TabControlTemplate())
			}
		};
	}

	private static ControlTheme CreateTabItemControlTheme()
	{
		return new ControlTheme(typeof(TabItem))
		{
			Setters =
			{
				new Setter(TabItem.TemplateProperty, TabItemTemplate())
			}
		};
	}

	private static void Prepare(TabControl target)
	{
		ApplyTemplate(target);
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));
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

	private static void SetPrivateField<T>(TabControl target, string name, T value)
	{
		var field = typeof(TabControl).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		CornerstoneTest.IsNotNull(field);
		field.SetValue(target, value);
	}

	private IDisposable Start()
	{
		return UnitTestApplication.Start(
			TestServices.MockThreadingInterface.With(
				fontManagerImpl: new HeadlessFontManagerStub(),
				keyboardDevice: () => new KeyboardDevice(),
				keyboardNavigation: () => new KeyboardNavigationHandler(),
				inputManager: new InputManager(),
				renderInterface: new HeadlessPlatformRenderInterface(),
				textShaperImpl: new HarfBuzzTextShaper(),
				assetLoader: new StandardAssetLoader()));
	}

	private static IControlTemplate TabControlTemplate()
	{
		return new FuncControlTemplate<TabControl>((parent, scope) =>
			new StackPanel
			{
				Children =
				{
					new ItemsPresenter
					{
						Name = "PART_ItemsPresenter"
					}.RegisterInNameScope(scope),
					new Panel
					{
						Children =
						{
							new ContentPresenter
							{
								Name = "PART_SelectedContentHost2",
								IsVisible = false
							}.RegisterInNameScope(scope),
							new ContentPresenter
							{
								Name = "PART_SelectedContentHost"
							}.RegisterInNameScope(scope)
						}
					}
				}
			});
	}

	private static IControlTemplate TabItemTemplate()
	{
		return new FuncControlTemplate<TabItem>((parent, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[~ContentPresenter.ContentProperty] = new TemplateBinding(TabItem.HeaderProperty),
				[~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(TabItem.HeaderTemplateProperty),
				RecognizesAccessKey = true
			}.RegisterInNameScope(scope));
	}

	private static IControlTemplate TabItemWithIconTemplate()
	{
		return new FuncControlTemplate<TabItem>((parent, scope) =>
			new StackPanel
			{
				Children =
				{
					new ContentPresenter
					{
						Name = "PART_IconPresenter",
						[~ContentPresenter.ContentProperty] = new TemplateBinding(TabItem.IconProperty),
						[~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(TabItem.IconTemplateProperty)
					}.RegisterInNameScope(scope),
					new ContentPresenter
					{
						Name = "PART_ContentPresenter",
						[~ContentPresenter.ContentProperty] = new TemplateBinding(TabItem.HeaderProperty),
						[~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(TabItem.HeaderTemplateProperty),
						RecognizesAccessKey = true
					}.RegisterInNameScope(scope)
				}
			});
	}

	#endregion

	#region Classes

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

	private class MainViewModel
	{
		#region Properties

		public Tab1ViewModel Tab1 { get; } = new();
		public Tab2ViewModel Tab2 { get; } = new();

		#endregion
	}

	private class Tab1ViewModel
	{
		#region Properties

		public string Name { get; set; } = "Tab 1 message here";

		#endregion
	}

	private class Tab2ViewModel
	{
		#region Properties

		public string Name { get; set; } = "Tab 2 message here";

		#endregion
	}

	private class TabDataContextViewModel : NotifyingBase
	{
		#region Fields

		private string _selectedItem;

		#endregion

		#region Properties

		public string SelectedItem
		{
			get => _selectedItem;
			set => SetField(ref _selectedItem, value);
		}

		#endregion
	}

	private class TestTabControl : TabControl
	{
		#region Properties

		public new ISelectionModel Selection => base.Selection;
		protected override Type StyleKeyOverride => typeof(TabControl);

		#endregion
	}

	private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
	{
	}

	#endregion
}