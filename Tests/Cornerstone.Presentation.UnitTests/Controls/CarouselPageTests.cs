#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class CarouselPageTests
{
	#region Classes

	[TestClass]
	public class CarouselIsSwipeEnabledTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void GetTransitionAxisReturnsNullWhenNoTransition()
		{
			var carousel = new Carousel();
			CornerstoneTest.IsNull(carousel.GetTransitionAxis());
		}

		[PresentationTestMethod]
		public void GetTransitionAxisReturnsNullWhenNonPageSlideTransition()
		{
			var carousel = new Carousel { PageTransition = new CrossFade(TimeSpan.FromMilliseconds(200)) };
			CornerstoneTest.IsNull(carousel.GetTransitionAxis());
		}

		[PresentationTestMethod]
		public void GetTransitionAxisReturnsVerticalWhenPageSlideVertical()
		{
			var carousel = new Carousel
			{
				PageTransition = new PageSlide(TimeSpan.FromMilliseconds(200), PageSlide.SlideAxis.Vertical)
			};
			CornerstoneTest.AreEqual(PageSlide.SlideAxis.Vertical, carousel.GetTransitionAxis());
		}

		[PresentationTestMethod]
		public void GetTransitionAxisReturnsVerticalWhenRotate3DVertical()
		{
			var carousel = new Carousel
			{
				PageTransition = new Rotate3DTransition(TimeSpan.FromMilliseconds(200), PageSlide.SlideAxis.Vertical)
			};
			CornerstoneTest.AreEqual(PageSlide.SlideAxis.Vertical, carousel.GetTransitionAxis());
		}

		[PresentationTestMethod]
		public void IsSwipeEnabledDefaultIsFalse()
		{
			var carousel = new Carousel();
			CornerstoneTest.IsFalse(carousel.IsSwipeEnabled);
		}

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void IsSwipeEnabledRoundTrips(bool value)
		{
			var carousel = new Carousel { IsSwipeEnabled = value };
			CornerstoneTest.AreEqual(value, carousel.IsSwipeEnabled);
		}

		#endregion
	}

	[TestClass]
	public class CurrentPageChangedEvent : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CurrentPageChangedFiresOnSelectionChange()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });
			cp.SelectedIndex = 0;

			var count = 0;
			cp.CurrentPageChanged += (_, _) => count++;

			cp.SelectedIndex = 1;

			CornerstoneTest.AreEqual(1, count);
		}

		[PresentationTestMethod]
		public void CurrentPageChangedNotFiredWhenSamePageSelected()
		{
			var cp = new CarouselPage();
			var page = new ContentPage { Header = "A" };
			((OldPresentationList<Page>) cp.Pages!).Add(page);
			cp.SelectedIndex = 0;

			var count = 0;
			cp.CurrentPageChanged += (_, _) => count++;

			cp.SelectedIndex = 0;

			CornerstoneTest.AreEqual(0, count);
		}

		#endregion
	}

	[TestClass]
	public class DataTemplateTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ItemsSourceSelectedPageIsResolvedAfterLayout()
		{
			var cp = CreateTemplatedCarouselPage<CarouselPage>(
				new[] { new DataItem("First"), new DataItem("Second") });

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsNotNull(cp.SelectedPage);
			CornerstoneTest.AreEqual("First", cp.SelectedPage!.Header?.ToString());
			CornerstoneTest.AreEqual("Page 1 of 2: First", AutomationProperties.GetName(cp));
		}

		[PresentationTestMethod]
		public void ItemsSourceSelectionChangesFireLifecycleOnGeneratedPages()
		{
			var cp = CreateTemplatedCarouselPage<TestableCarouselPage>(
				new[] { new DataItem("First"), new DataItem("Second") },
				item => new TrackingPage
				{
					Header = item?.Name,
					Content = new Border { Width = 400, Height = 300 }
				});

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var firstPage = CornerstoneTest.IsType<TrackingPage>(cp.SelectedPage);
			CornerstoneTest.AreEqual(1, firstPage.NavigatedToCount);
			CornerstoneTest.AreEqual(0, firstPage.NavigatedFromCount);

			cp.SimulateKeyDown(Key.Right);
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var secondPage = CornerstoneTest.IsType<TrackingPage>(cp.SelectedPage);
			CornerstoneTest.AreEqual(1, firstPage.NavigatedFromCount);
			CornerstoneTest.AreEqual(1, secondPage.NavigatedToCount);
			CornerstoneTest.Same(secondPage, cp.CurrentPage);
		}

		[PresentationTestMethod]
		public void KeyboardNavigationUsesItemsSourceCount()
		{
			var cp = CreateTemplatedCarouselPage<TestableCarouselPage>(
				new[] { new DataItem("First"), new DataItem("Second") });

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			cp.SimulateKeyDown(Key.Right);
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
			CornerstoneTest.IsNotNull(cp.SelectedPage);
			CornerstoneTest.AreEqual("Second", cp.SelectedPage!.Header?.ToString());
			CornerstoneTest.AreEqual("Page 2 of 2: Second", AutomationProperties.GetName(cp));
		}

		[PresentationTestMethod]
		public void PageTemplateChangedAfterContainersRealizedUpdatesSelectedPage()
		{
			var cp = CreateTemplatedCarouselPage<CarouselPage>(
				new[] { new DataItem("First"), new DataItem("Second") },
				item => new ContentPage
				{
					Header = $"Detail {item?.Name}",
					Content = new Border { Width = 400, Height = 300 }
				});

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var originalPage = CornerstoneTest.IsType<ContentPage>(cp.SelectedPage);
			CornerstoneTest.AreEqual("Detail First", originalPage.Header);

			cp.PageTemplate = new FuncDataTemplate<DataItem>(
				(item, _) => new ContentPage
				{
					Header = $"Showcase {item?.Name}",
					Content = new Border { Width = 400, Height = 300 }
				},
				false);

			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var updatedPage = CornerstoneTest.IsType<ContentPage>(cp.SelectedPage);
			CornerstoneTest.NotSame(originalPage, updatedPage);
			CornerstoneTest.AreEqual("Showcase First", updatedPage.Header);
		}

		[PresentationTestMethod]
		public void WheelNavigationUsesItemsSourceCount()
		{
			var cp = CreateTemplatedCarouselPage<TestableCarouselPage>(
				new[] { new DataItem("First"), new DataItem("Second") });

			var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var handled = cp.SimulateWheelReturnsHandled(new Vector(0, -1));
			root.LayoutManager.ExecuteLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsTrue(handled);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
			CornerstoneTest.IsNotNull(cp.SelectedPage);
			CornerstoneTest.AreEqual("Second", cp.SelectedPage!.Header?.ToString());
		}

		private static IControlTemplate CarouselTemplate()
		{
			return new FuncControlTemplate((c, ns) =>
				new ScrollViewer
				{
					Name = "PART_ScrollViewer",
					Template = ScrollViewerTemplate(),
					HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
					VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
					Content = new ItemsPresenter
					{
						Name = "PART_ItemsPresenter",
						[~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty]
					}.RegisterInNameScope(ns)
				}.RegisterInNameScope(ns));
		}

		private static FuncControlTemplate<CarouselPage> CreateCarouselPageTemplate()
		{
			return new FuncControlTemplate<CarouselPage>((_, scope) =>
				new Carousel
				{
					Name = "PART_Carousel",
					Template = CarouselTemplate(),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}.RegisterInNameScope(scope));
		}

		private static T CreateTemplatedCarouselPage<T>(IEnumerable<DataItem> items)
			where T : CarouselPage, new()
		{
			return CreateTemplatedCarouselPage<T>(
				items,
				item => new ContentPage
				{
					Header = item?.Name,
					Content = new Border { Width = 400, Height = 300 }
				});
		}

		private static T CreateTemplatedCarouselPage<T>(IEnumerable<DataItem> items, Func<DataItem, Page> pageFactory)
			where T : CarouselPage, new()
		{
			return new T
			{
				Width = 400,
				Height = 300,
				ItemsSource = items,
				PageTemplate = new FuncDataTemplate<DataItem>(
					(item, _) => pageFactory(item),
					false),
				Template = CreateCarouselPageTemplate()
			};
		}

		private static FuncControlTemplate ScrollViewerTemplate()
		{
			return new FuncControlTemplate((_, ns) =>
				new ScrollContentPresenter
				{
					Name = "PART_ContentPresenter"
				}.RegisterInNameScope(ns));
		}

		#endregion

		#region Classes

		private sealed class TrackingPage : ContentPage
		{
			#region Properties

			public int NavigatedFromCount { get; private set; }
			public int NavigatedToCount { get; private set; }

			#endregion

			#region Methods

			protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
			{
				NavigatedFromCount++;
				base.OnNavigatedFrom(args);
			}

			protected override void OnNavigatedTo(NavigatedToEventArgs args)
			{
				NavigatedToCount++;
				base.OnNavigatedTo(args);
			}

			#endregion
		}

		#endregion

		#region Records

		private record DataItem(string Name);

		#endregion
	}

	[TestClass]
	public class InteractiveTransitionTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CrossFadeUpdateSetsOpacity()
		{
			var from = new Border();
			var to = new Border();

			var crossFade = new CrossFade(TimeSpan.FromMilliseconds(300));
			crossFade.Update(0.5, from, to, true, 0, Array.Empty<PageTransitionItem>());

			CornerstoneTest.AreEqual(0.5, from.Opacity, 2);
			CornerstoneTest.AreEqual(0.5, to.Opacity, 2);
			CornerstoneTest.IsTrue(to.IsVisible);
		}

		[PresentationTestMethod]
		public void PageSlideUpdateAppliesTranslateTransformToFrom()
		{
			var parent = new Canvas { Width = 400, Height = 300 };
			var from = new Border();
			var to = new Border();
			parent.Children.Add(from);
			parent.Children.Add(to);
			parent.Measure(new Size(400, 300));
			parent.Arrange(new Rect(0, 0, 400, 300));

			var slide = new PageSlide(TimeSpan.FromMilliseconds(300));
			slide.Update(0.5, from, to, true, 400, Array.Empty<PageTransitionItem>());

			CornerstoneTest.IsType<TranslateTransform>(from.RenderTransform);
			var ft = (TranslateTransform) from.RenderTransform!;
			CornerstoneTest.AreEqual(-200, ft.X);
		}

		[PresentationTestMethod]
		public void PageSlideUpdateAppliesTranslateTransformToTo()
		{
			var parent = new Canvas { Width = 400, Height = 300 };
			var from = new Border();
			var to = new Border();
			parent.Children.Add(from);
			parent.Children.Add(to);
			parent.Measure(new Size(400, 300));
			parent.Arrange(new Rect(0, 0, 400, 300));

			var slide = new PageSlide(TimeSpan.FromMilliseconds(300));
			slide.Update(0.5, from, to, true, 400, Array.Empty<PageTransitionItem>());

			CornerstoneTest.IsType<TranslateTransform>(to.RenderTransform);
			var tt = (TranslateTransform) to.RenderTransform!;
			CornerstoneTest.AreEqual(200, tt.X);
		}

		#endregion
	}

	[TestClass]
	public class KeyboardNavigationTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DownKeyNavigatesToNextPage()
		{
			var cp = MakeCarousel(3, 0);
			cp.SimulateKeyDown(Key.Down);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void EndKeyNavigatesToLastPage()
		{
			var cp = MakeCarousel(3, 0);
			cp.SimulateKeyDown(Key.End);
			CornerstoneTest.AreEqual(2, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void HomeKeyNavigatesToFirstPage()
		{
			var cp = MakeCarousel(3, 2);
			cp.SimulateKeyDown(Key.Home);
			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void KeyNavigationDisabledIgnoresArrowKey()
		{
			var cp = MakeCarousel(3, 0);
			cp.IsKeyboardNavigationEnabled = false;
			cp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void LeftKeyAtFirstPageDoesNotNavigate()
		{
			var cp = MakeCarousel(3, 0);
			cp.SimulateKeyDown(Key.Left);
			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void LeftKeyNavigatesToPreviousPage()
		{
			var cp = MakeCarousel(3, 2);
			cp.SimulateKeyDown(Key.Left);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RightKeyAtLastPageDoesNotMarkEventHandled()
		{
			var cp = MakeCarousel(3, 2);
			var handled = cp.SimulateKeyDownReturnsHandled(Key.Right);
			CornerstoneTest.IsFalse(handled);
		}

		[PresentationTestMethod]
		public void RightKeyAtLastPageDoesNotNavigate()
		{
			var cp = MakeCarousel(3, 2);
			cp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(2, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RightKeyMarksEventHandled()
		{
			var cp = MakeCarousel(3, 0);
			var handled = cp.SimulateKeyDownReturnsHandled(Key.Right);
			CornerstoneTest.IsTrue(handled);
		}

		[PresentationTestMethod]
		public void RightKeyNavigatesToNextPage()
		{
			var cp = MakeCarousel(3, 0);
			cp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RtlFlowDirectionLeftKeyAtLastPageDoesNotNavigate()
		{
			var cp = MakeCarousel(3, 2);
			cp.FlowDirection = FlowDirection.RightToLeft;
			cp.SimulateKeyDown(Key.Left);
			CornerstoneTest.AreEqual(2, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RtlFlowDirectionLeftKeyNavigatesToNextPage()
		{
			var cp = MakeCarousel(3, 0);
			cp.FlowDirection = FlowDirection.RightToLeft;
			cp.SimulateKeyDown(Key.Left);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RtlFlowDirectionRightKeyAtFirstPageDoesNotNavigate()
		{
			var cp = MakeCarousel(3, 0);
			cp.FlowDirection = FlowDirection.RightToLeft;
			cp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RtlFlowDirectionRightKeyNavigatesToPreviousPage()
		{
			var cp = MakeCarousel(3, 2);
			cp.FlowDirection = FlowDirection.RightToLeft;
			cp.SimulateKeyDown(Key.Right);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void UpKeyNavigatesToPreviousPage()
		{
			var cp = MakeCarousel(3, 2);
			cp.SimulateKeyDown(Key.Up);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		private static TestableCarouselPage MakeCarousel(int count, int selectedIndex)
		{
			var cp = new TestableCarouselPage();
			for (var i = 0; i < count; i++)
			{
				((OldPresentationList<Page>) cp.Pages!).Add(new ContentPage { Header = $"P{i}" });
			}
			cp.SelectedIndex = selectedIndex;
			return cp;
		}

		#endregion
	}

	[TestClass]
	public class LogicalChildrenTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PagesAddedBecomeLogicalChildren()
		{
			var cp = new CarouselPage();
			var page = new ContentPage { Header = "A" };

			((OldPresentationList<Page>) cp.Pages!).Add(page);

			CornerstoneTest.Contains(cp.GetLogicalChildren(), page);
		}

		[PresentationTestMethod]
		public void PagesClearRemovesAllLogicalChildren()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });

			((OldPresentationList<Page>) cp.Pages!).Clear();

			CornerstoneTest.DoesNotContain(cp.GetLogicalChildren(), page1);
			CornerstoneTest.DoesNotContain(cp.GetLogicalChildren(), page2);
		}

		[PresentationTestMethod]
		public void PagesRemovedRemovedFromLogicalChildren()
		{
			var cp = new CarouselPage();
			var page = new ContentPage { Header = "A" };
			((OldPresentationList<Page>) cp.Pages!).Add(page);

			((OldPresentationList<Page>) cp.Pages!).Remove(page);

			CornerstoneTest.DoesNotContain(cp.GetLogicalChildren(), page);
		}

		[PresentationTestMethod]
		public void PagesReplacedOldPagesRemovedNewPagesAdded()
		{
			var cp = new CarouselPage();
			var old1 = new ContentPage { Header = "Old1" };
			var old2 = new ContentPage { Header = "Old2" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { old1, old2 });

			var newPages = new OldPresentationList<Page> { new ContentPage { Header = "New1" } };
			cp.Pages = newPages;

			CornerstoneTest.DoesNotContain(cp.GetLogicalChildren(), old1);
			CornerstoneTest.DoesNotContain(cp.GetLogicalChildren(), old2);
			CornerstoneTest.Contains(cp.GetLogicalChildren(), newPages[0]);
		}

		[PresentationTestMethod]
		public void PagesSetNullClearsLogicalChildren()
		{
			var cp = new CarouselPage();
			var page = new ContentPage { Header = "A" };
			((OldPresentationList<Page>) cp.Pages!).Add(page);

			cp.Pages = null;

			CornerstoneTest.DoesNotContain(cp.GetLogicalChildren(), page);
		}

		#endregion
	}

	[TestClass]
	public class PageLifecycleEvents : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void NavigatedFromNavigationTypeIsReplace()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });

			NavigatedFromEventArgs args = null;
			page1.NavigatedFrom += (_, e) => args = e;

			cp.SelectedIndex = 1;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Replace, args!.NavigationType);
		}

		[PresentationTestMethod]
		public void NavigatedToFirstAutoSelectionNavigationTypeIsReplace()
		{
			var cp = new CarouselPage();
			var page = new ContentPage { Header = "A" };

			NavigatedToEventArgs args = null;
			page.NavigatedTo += (_, e) => args = e;

			((OldPresentationList<Page>) cp.Pages!).Add(page);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Replace, args!.NavigationType);
		}

		[PresentationTestMethod]
		public void NavigatedToNavigationTypeIsReplace()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });

			NavigatedToEventArgs args = null;
			page2.NavigatedTo += (_, e) => args = e;

			cp.SelectedIndex = 1;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Replace, args!.NavigationType);
		}

		[PresentationTestMethod]
		public void SelectionChangeFiresNavigatedFromOnPreviousPage()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });
			cp.SelectedIndex = 0;

			NavigatedFromEventArgs args = null;
			page1.NavigatedFrom += (_, e) => args = e;

			cp.SelectedIndex = 1;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(page2, args!.DestinationPage);
		}

		[PresentationTestMethod]
		public void SelectionChangeFiresNavigatedToOnNewPage()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });
			cp.SelectedIndex = 0;

			NavigatedToEventArgs args = null;
			page2.NavigatedTo += (_, e) => args = e;

			cp.SelectedIndex = 1;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(page1, args!.PreviousPage);
		}

		[PresentationTestMethod]
		public void SelectionChangeLifecycleOrderNavigatedFromThenNavigatedTo()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });
			cp.SelectedIndex = 0;

			var order = new List<string>();
			page1.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
			page2.NavigatedTo += (_, _) => order.Add("NavigatedTo");

			cp.SelectedIndex = 1;

			CornerstoneTest.AreEqual(new[] { "NavigatedFrom", "NavigatedTo" }, order);
		}

		[PresentationTestMethod]
		public void SelectionChangeSamePageNoLifecycleEvents()
		{
			var cp = new CarouselPage();
			var page = new ContentPage { Header = "A" };
			((OldPresentationList<Page>) cp.Pages!).Add(page);
			cp.SelectedIndex = 0;

			var events = new List<string>();
			page.NavigatedTo += (_, _) => events.Add("NavigatedTo");
			page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");

			cp.SelectedIndex = 0;

			CornerstoneTest.Empty(events);
		}

		#endregion
	}

	[TestClass]
	public class PagesChangedEvent : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PagesChangedFiresOnAdd()
		{
			var cp = new CarouselPage();
			NotifyCollectionChangedEventArgs received = null;
			cp.PagesChanged += (_, e) => received = e;

			((OldPresentationList<Page>) cp.Pages!).Add(new ContentPage());

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Add, received!.Action);
		}

		[PresentationTestMethod]
		public void PagesChangedFiresOnRemove()
		{
			var cp = new CarouselPage();
			var page = new ContentPage();
			((OldPresentationList<Page>) cp.Pages!).Add(page);

			NotifyCollectionChangedEventArgs received = null;
			cp.PagesChanged += (_, e) => received = e;

			((OldPresentationList<Page>) cp.Pages!).Remove(page);

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.AreEqual(NotifyCollectionChangedAction.Remove, received!.Action);
		}

		[PresentationTestMethod]
		public void PagesChangedNotFiredAfterPagesCollectionReplaced()
		{
			var cp = new CarouselPage();
			var oldPages = (OldPresentationList<Page>) cp.Pages!;
			cp.Pages = new OldPresentationList<Page>();

			cp.PagesChanged += (_, _) => throw new InvalidOperationException("Should not fire for old collection");

			oldPages.Add(new ContentPage());
		}

		#endregion
	}

	[TestClass]
	public class PropertyDefaults : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CurrentPageDefaultIsNull()
		{
			var cp = new CarouselPage();
			CornerstoneTest.IsNull(cp.CurrentPage);
		}

		[PresentationTestMethod]
		public void IsGestureEnabledDefaultIsTrue()
		{
			var cp = new CarouselPage();
			CornerstoneTest.IsTrue(cp.IsGestureEnabled);
		}

		[PresentationTestMethod]
		public void IsKeyboardNavigationEnabledDefaultIsTrue()
		{
			var cp = new CarouselPage();
			CornerstoneTest.IsTrue(cp.IsKeyboardNavigationEnabled);
		}

		[PresentationTestMethod]
		public void PageTransitionDefaultIsNull()
		{
			var cp = new CarouselPage();
			CornerstoneTest.IsNull(cp.PageTransition);
		}

		[PresentationTestMethod]
		public void SelectedIndexDefaultIsMinusOne()
		{
			var cp = new CarouselPage();
			CornerstoneTest.AreEqual(-1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void SelectedPageDefaultIsNull()
		{
			var cp = new CarouselPage();
			CornerstoneTest.IsNull(cp.SelectedPage);
		}

		#endregion
	}

	[TestClass]
	public class PropertyRoundTrips : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void IsGestureEnabledRoundTrips(bool value)
		{
			var cp = new CarouselPage { IsGestureEnabled = value };
			CornerstoneTest.AreEqual(value, cp.IsGestureEnabled);
		}

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void IsKeyboardNavigationEnabledRoundTrips(bool value)
		{
			var cp = new CarouselPage { IsKeyboardNavigationEnabled = value };
			CornerstoneTest.AreEqual(value, cp.IsKeyboardNavigationEnabled);
		}

		[PresentationTestMethod]
		public void ItemsPanelRoundTrip()
		{
			var template = new FuncTemplate<Panel>(() => new StackPanel());
			var cp = new CarouselPage { ItemsPanel = template };
			CornerstoneTest.Same(template, cp.ItemsPanel);
		}

		[PresentationTestMethod]
		public void PageTemplateCanBeSetToNull()
		{
			var cp = new CarouselPage { PageTemplate = null };
			CornerstoneTest.IsNull(cp.PageTemplate);
		}

		[PresentationTestMethod]
		public void PageTemplateRoundTrip()
		{
			var template = new FuncDataTemplate<Page>((_, _) => new ContentControl());
			var cp = new CarouselPage { PageTemplate = template };
			CornerstoneTest.Same(template, cp.PageTemplate);
		}

		[PresentationTestMethod]
		public void PageTransitionCanBeSetToNull()
		{
			var cp = new CarouselPage { PageTransition = new TestPageTransition() };
			cp.PageTransition = null;
			CornerstoneTest.IsNull(cp.PageTransition);
		}

		[PresentationTestMethod]
		public void PageTransitionRoundTrip()
		{
			var transition = new TestPageTransition();
			var cp = new CarouselPage { PageTransition = transition };
			CornerstoneTest.Same(transition, cp.PageTransition);
		}

		#endregion
	}

	[TestClass]
	public class SelectionBehavior : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PagesSetNullSelectedIndexRetainsLastValue()
		{
			var cp = new CarouselPage();
			((OldPresentationList<Page>) cp.Pages!).Add(new ContentPage { Header = "A" });
			CornerstoneTest.AreEqual(0, cp.SelectedIndex);

			cp.Pages = null;

			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void SelectedIndexAutoSelectsFirstWhenPagesSet()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });

			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
			CornerstoneTest.Same(page1, cp.SelectedPage);
		}

		[PresentationTestMethod]
		public void SelectedIndexInvalidWithPagesCoercesToFirstPage()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });

			cp.SelectedIndex = 999;

			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
			CornerstoneTest.Same(page1, cp.SelectedPage);
			CornerstoneTest.Same(page1, cp.CurrentPage);
		}

		[PresentationTestMethod]
		public void SelectedIndexSetBeforePagesIsAppliedWhenPagesSet()
		{
			var cp = new CarouselPage
			{
				SelectedIndex = 2
			};

			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			var page3 = new ContentPage { Header = "C" };

			cp.Pages = new OldPresentationList<Page> { page1, page2, page3 };

			CornerstoneTest.AreEqual(2, cp.SelectedIndex);
			CornerstoneTest.Same(page3, cp.SelectedPage);
			CornerstoneTest.Same(page3, cp.CurrentPage);
		}

		[PresentationTestMethod]
		public void SelectedIndexSetBeforePagesIsStored()
		{
			var cp = new CarouselPage();

			cp.SelectedIndex = 2;

			CornerstoneTest.AreEqual(2, cp.SelectedIndex);
			CornerstoneTest.IsNull(cp.SelectedPage);
			CornerstoneTest.IsNull(cp.CurrentPage);
		}

		[PresentationTestMethod]
		public void SelectedIndexSetUpdatesSelectedPageAndCurrentPage()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });
			cp.SelectedIndex = 0;

			cp.SelectedIndex = 1;

			CornerstoneTest.Same(page2, cp.SelectedPage);
			CornerstoneTest.Same(page2, cp.CurrentPage);
		}

		[PresentationTestMethod]
		public void SelectedPageSameReferenceAsCurrentPage()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });
			cp.SelectedIndex = 1;

			CornerstoneTest.Same(cp.SelectedPage, cp.CurrentPage);
		}

		[PresentationTestMethod]
		public void UpdateActivePageDoesNotOverrideExistingSelection()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });
			cp.SelectedIndex = 1;

			((OldPresentationList<Page>) cp.Pages!).Add(new ContentPage { Header = "C" });

			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void UpdateActivePageSelectsFirstWhenFirstPageAddedToEmptyCollection()
		{
			var cp = new CarouselPage();
			CornerstoneTest.AreEqual(-1, cp.SelectedIndex);

			var page1 = new ContentPage { Header = "A" };
			((OldPresentationList<Page>) cp.Pages!).Add(page1);

			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
			CornerstoneTest.Same(page1, cp.SelectedPage);
		}

		#endregion
	}

	[TestClass]
	public class SelectionChangedEvent : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void SelectionChangedFiresWhenSelectionChanges()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };
			var page2 = new ContentPage { Header = "B" };
			((OldPresentationList<Page>) cp.Pages!).AddRange(new[] { page1, page2 });
			cp.SelectedIndex = 0;

			PageSelectionChangedEventArgs received = null;
			cp.SelectionChanged += (_, e) => received = e;

			cp.SelectedIndex = 1;

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.Same(page1, received!.PreviousPage);
			CornerstoneTest.Same(page2, received.CurrentPage);
		}

		[PresentationTestMethod]
		public void SelectionChangedNotFiredWhenSamePageSelected()
		{
			var cp = new CarouselPage();
			var page = new ContentPage { Header = "A" };
			((OldPresentationList<Page>) cp.Pages!).Add(page);
			cp.SelectedIndex = 0;

			var count = 0;
			cp.SelectionChanged += (_, _) => count++;

			cp.SelectedIndex = 0;

			CornerstoneTest.AreEqual(0, count);
		}

		[PresentationTestMethod]
		public void SelectionChangedPreviousPageIsNullOnFirstAutoSelection()
		{
			var cp = new CarouselPage();
			var page1 = new ContentPage { Header = "A" };

			PageSelectionChangedEventArgs received = null;
			cp.SelectionChanged += (_, e) => received = e;

			((OldPresentationList<Page>) cp.Pages!).Add(page1);

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.IsNull(received!.PreviousPage);
			CornerstoneTest.Same(page1, received.CurrentPage);
		}

		[PresentationTestMethod]
		public void SelectionChangedTracksSequentialSelections()
		{
			var cp = new CarouselPage();
			var pages = new[]
			{
				new ContentPage { Header = "A" },
				new ContentPage { Header = "B" },
				new ContentPage { Header = "C" }
			};
			((OldPresentationList<Page>) cp.Pages!).AddRange(pages);
			cp.SelectedIndex = 0;

			var events = new List<(Page prev, Page curr)>();
			cp.SelectionChanged += (_, e) => events.Add((e.PreviousPage, e.CurrentPage));

			cp.SelectedIndex = 1;
			cp.SelectedIndex = 2;
			cp.SelectedIndex = 0;

			CornerstoneTest.AreEqual(3, events.Count);
			CornerstoneTest.Same(pages[0], events[0].prev);
			CornerstoneTest.Same(pages[1], events[0].curr);
			CornerstoneTest.Same(pages[1], events[1].prev);
			CornerstoneTest.Same(pages[2], events[1].curr);
			CornerstoneTest.Same(pages[2], events[2].prev);
			CornerstoneTest.Same(pages[0], events[2].curr);
		}

		#endregion
	}

	[TestClass]
	public class SwipeGestureTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void MouseSwipeAdvancesPage()
		{
			var clock = new MockGlobalClock();
			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var (cp, carousel, panel) = CreateSwipeReadyCarouselPage();
			var mouse = new MouseTestHelper();

			mouse.Down(panel, position: new Point(200, 100));
			mouse.Move(panel, new Point(40, 100));
			mouse.Up(panel, position: new Point(40, 100));
			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
			CornerstoneTest.Same(((OldPresentationList<Page>) cp.Pages!)[1], cp.CurrentPage);
		}

		[PresentationTestMethod]
		public void TouchSwipeAdvancesPage()
		{
			var clock = new MockGlobalClock();
			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var (cp, carousel, panel) = CreateSwipeReadyCarouselPage();
			var touch = new TouchTestHelper();

			touch.Down(panel, new Point(200, 100));
			touch.Move(panel, new Point(40, 100));
			touch.Up(panel, new Point(40, 100));
			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
			CornerstoneTest.Same(((OldPresentationList<Page>) cp.Pages!)[1], cp.CurrentPage);
		}

		private static IControlTemplate CarouselTemplate()
		{
			return new FuncControlTemplate((c, ns) =>
				new ScrollViewer
				{
					Name = "PART_ScrollViewer",
					Template = ScrollViewerTemplate(),
					HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
					VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
					Content = new ItemsPresenter
					{
						Name = "PART_ItemsPresenter",
						[~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty]
					}.RegisterInNameScope(ns)
				}.RegisterInNameScope(ns));
		}

		private static (CarouselPage Page, Carousel Carousel, VirtualizingCarouselPanel Panel) CreateSwipeReadyCarouselPage()
		{
			var cp = new CarouselPage
			{
				Width = 400,
				Height = 300,
				IsGestureEnabled = true,
				PageTransition = new PageSlide(TimeSpan.FromMilliseconds(1)),
				Pages = new OldPresentationList<Page>
				{
					new ContentPage { Header = "A", Content = new Border { Width = 400, Height = 300 } },
					new ContentPage { Header = "B", Content = new Border { Width = 400, Height = 300 } },
					new ContentPage { Header = "C", Content = new Border { Width = 400, Height = 300 } }
				},
				Template = new FuncControlTemplate<CarouselPage>((parent, scope) =>
					new Carousel
					{
						Name = "PART_Carousel",
						Template = CarouselTemplate(),
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch,
						[~ItemsControl.ItemsSourceProperty] = parent[~CarouselPage.PagesProperty],
						[~ItemsControl.ItemTemplateProperty] = parent[~CarouselPage.PageTemplateProperty],
						[~ItemsControl.ItemsPanelProperty] = parent[~CarouselPage.ItemsPanelProperty],
						[~Carousel.PageTransitionProperty] = parent[~CarouselPage.PageTransitionProperty]
					}.RegisterInNameScope(scope))
			};

			var root = new TestRoot
			{
				ClientSize = new Size(400, 300),
				Child = cp
			};
			root.ExecuteInitialLayoutPass();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var carousel = cp.GetVisualDescendants().OfType<Carousel>().Single();
			var panel = CornerstoneTest.IsType<VirtualizingCarouselPanel>(carousel.Presenter!.Panel!);
			CornerstoneTest.IsTrue(carousel.IsSwipeEnabled);
			var recognizer = CornerstoneTest.Single(panel.GestureRecognizers.OfType<SwipeGestureRecognizer>());
			CornerstoneTest.IsTrue(recognizer.IsEnabled);
			CornerstoneTest.IsTrue(recognizer.CanHorizontallySwipe);
			recognizer.IsMouseEnabled = true;
			return (cp, carousel, panel);
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
						}.RegisterInNameScope(scope)
					}
				});
		}

		#endregion
	}

	[TestClass]
	public class SystemBackButtonTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void BackEventIsForwardedToContent()
		{
			var cp = new CarouselPage();
			var page = new ContentPage();
			var isRaised = false;
			page.PageNavigationSystemBackButtonPressed += (s, e) => { isRaised = true; };
			var root = new TestRoot { Child = cp };
			((OldPresentationList<Page>) cp.Pages!).Add(page);

			var args = RaiseBackButton(cp);

			CornerstoneTest.IsTrue(isRaised);
			CornerstoneTest.IsFalse(args.Handled);
		}

		private static RoutedEventArgs RaiseBackButton(Page dp)
		{
			var args = new RoutedEventArgs(Page.PageNavigationSystemBackButtonPressedEvent);
			dp.RaiseEvent(args);
			return args;
		}

		#endregion
	}

	[TestClass]
	public class VisualTreeLifecycleTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DetachAndReattachCollectionChangedStillUpdatesSelection()
		{
			var pages = new OldPresentationList<Page>();
			var cp = new CarouselPage { Pages = pages };
			var root = new TestRoot { Child = cp };

			var page1 = new ContentPage { Header = "A" };
			pages.Add(page1);
			CornerstoneTest.Same(page1, cp.SelectedPage);

			root.Child = null;
			root.Child = cp;

			var page2 = new ContentPage { Header = "B" };
			pages.Add(page2);

			pages.Remove(page1);
			CornerstoneTest.Same(page2, cp.SelectedPage);
		}

		#endregion
	}

	[TestClass]
	public class WheelBehavior : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void WheelDownAtLastPageDoesNotHandleEvent()
		{
			var cp = MakeCarousel(3, 2);
			var handled = cp.SimulateWheelReturnsHandled(new Vector(0, -1));
			CornerstoneTest.AreEqual(2, cp.SelectedIndex);
			CornerstoneTest.IsFalse(handled);
		}

		[PresentationTestMethod]
		public void WheelDownNavigatesForward()
		{
			var cp = MakeCarousel(3, 0);
			cp.SimulateWheel(new Vector(0, -1));
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void WheelHandledByChildDoesNotNavigate()
		{
			var cp = new CarouselPage
			{
				Width = 400,
				Height = 300,
				IsGestureEnabled = true,
				Template = CreateCarouselPageTemplate()
			};

			var child = new Border();
			child.AddHandler(InputElement.PointerWheelChangedEvent, (_, e) => e.Handled = true);

			var page0 = new ContentPage { Header = "P0", Content = child };
			var page1 = new ContentPage { Header = "P1" };
			var page2 = new ContentPage { Header = "P2" };
			((OldPresentationList<Page>) cp.Pages!).Add(page0);
			((OldPresentationList<Page>) cp.Pages!).Add(page1);
			((OldPresentationList<Page>) cp.Pages!).Add(page2);

			var root = new TestRoot(cp) { ClientSize = new Size(400, 300) };
			root.LayoutManager.ExecuteInitialLayoutPass();

			var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
			var wheelArgs = new PointerWheelEventArgs(
				child,
				pointer,
				root,
				default,
				0,
				new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
				KeyModifiers.None,
				new Vector(0, -1))
			{
				RoutedEvent = InputElement.PointerWheelChangedEvent
			};
			child.RaiseEvent(wheelArgs);

			CornerstoneTest.IsTrue(wheelArgs.Handled);
			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void WheelUpAtFirstPageDoesNotHandleEvent()
		{
			var cp = MakeCarousel(3, 0);
			var handled = cp.SimulateWheelReturnsHandled(new Vector(0, 1));
			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
			CornerstoneTest.IsFalse(handled);
		}

		[PresentationTestMethod]
		public void WheelUpNavigatesBackward()
		{
			var cp = MakeCarousel(3, 2);
			cp.SimulateWheel(new Vector(0, 1));
			CornerstoneTest.AreEqual(1, cp.SelectedIndex);
		}

		[PresentationTestMethod]
		public void WheelWhenGestureDisabledDoesNotHandleEvent()
		{
			var cp = MakeCarousel(3, 0);
			cp.IsGestureEnabled = false;
			var handled = cp.SimulateWheelReturnsHandled(new Vector(0, -1));
			CornerstoneTest.AreEqual(0, cp.SelectedIndex);
			CornerstoneTest.IsFalse(handled);
		}

		private static FuncControlTemplate<CarouselPage> CreateCarouselPageTemplate()
		{
			return new FuncControlTemplate<CarouselPage>((_, scope) =>
				new Carousel
				{
					Name = "PART_Carousel",
					Template = new FuncControlTemplate((c, ns) =>
						new ScrollViewer
						{
							Name = "PART_ScrollViewer",
							HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
							VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
							Content = new ItemsPresenter
							{
								Name = "PART_ItemsPresenter",
								[~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty]
							}.RegisterInNameScope(ns)
						}.RegisterInNameScope(ns)),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}.RegisterInNameScope(scope));
		}

		private static TestableCarouselPage MakeCarousel(int count, int selectedIndex)
		{
			var cp = new TestableCarouselPage();
			for (var i = 0; i < count; i++)
			{
				((OldPresentationList<Page>) cp.Pages!).Add(new ContentPage { Header = $"P{i}" });
			}
			cp.SelectedIndex = selectedIndex;
			_ = new TestRoot { Child = cp };
			return cp;
		}

		#endregion
	}

	private sealed class FakePointer : IPointer
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

	private sealed class TestPageTransition : IPageTransition
	{
		#region Methods

		public Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		#endregion
	}

	private sealed class TestableCarouselPage : CarouselPage
	{
		#region Methods

		public void SimulateKeyDown(Key key)
		{
			var e = new KeyEventArgs { RoutedEvent = KeyDownEvent, Key = key };
			OnKeyDown(e);
		}

		public bool SimulateKeyDownReturnsHandled(Key key)
		{
			var e = new KeyEventArgs { RoutedEvent = KeyDownEvent, Key = key };
			OnKeyDown(e);
			return e.Handled;
		}

		public void SimulateWheel(Vector delta)
		{
			SimulateWheelReturnsHandled(delta);
		}

		public bool SimulateWheelReturnsHandled(Vector delta)
		{
			var pointer = new FakePointer();
			var e = new PointerWheelEventArgs(this, pointer, this, default, 0,
				new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
				KeyModifiers.None, delta)
			{
				RoutedEvent = PointerWheelChangedEvent
			};
			RaiseEvent(e);
			return e.Handled;
		}

		#endregion
	}

	#endregion
}