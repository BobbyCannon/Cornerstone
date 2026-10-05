#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class NavigationPageTests
{
	#region Methods

	private static NavigationPage CreateNavigationPage(IPageTransition transition = null)
	{
		var nav = new NavigationPage
		{
			PageTransition = transition,
			Template = CreateNavigationPageTemplate()
		};
		var root = new TestRoot { Child = nav };
		root.LayoutManager.ExecuteInitialLayoutPass();
		return nav;
	}

	private static IControlTemplate CreateNavigationPageTemplate()
	{
		return new FuncControlTemplate<NavigationPage>((_, ns) =>
		{
			var contentHost = new Panel
			{
				Name = "PART_ContentHost",
				Children =
				{
					new ContentPresenter { Name = "PART_PageBackPresenter" }.RegisterInNameScope(ns),
					new ContentPresenter { Name = "PART_PagePresenter" }.RegisterInNameScope(ns)
				}
			}.RegisterInNameScope(ns);

			return new Panel
			{
				Children =
				{
					new Border
					{
						Name = "PART_NavigationBar",
						Child = new Button { Name = "PART_BackButton" }.RegisterInNameScope(ns)
					}.RegisterInNameScope(ns),
					contentHost,
					new ContentPresenter { Name = "PART_TopCommandBar" }.RegisterInNameScope(ns),
					new ContentPresenter { Name = "PART_ModalBackPresenter" }.RegisterInNameScope(ns),
					new ContentPresenter { Name = "PART_ModalPresenter" }.RegisterInNameScope(ns)
				}
			};
		});
	}

	#endregion

	#region Classes

	[TestClass]
	public class AttachedPropertyTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task BarLayoutBehaviorDefaultAppliesNavBarInsetPseudoClass()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			CornerstoneTest.IsTrue(nav.Classes.Contains(":nav-bar-inset"));
		}

		[PresentationTestMethod]
		public async Task BarLayoutBehaviorOverlayRemovesNavBarInsetPseudoClass()
		{
			var nav = new NavigationPage();
			var page = new ContentPage();
			NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);
			await nav.PushAsync(page);
			CornerstoneTest.IsFalse(nav.Classes.Contains(":nav-bar-inset"));
		}

		[PresentationTestMethod]
		public async Task EffectiveBarHeightFallsBackToGlobalBarHeight()
		{
			var nav = new NavigationPage { BarHeight = 56.0 };
			await nav.PushAsync(new ContentPage());
			CornerstoneTest.AreEqual(56.0, nav.EffectiveBarHeight);
		}

		[PresentationTestMethod]
		public async Task EffectiveBarHeightUsesPageOverride()
		{
			var nav = new NavigationPage();
			var page = new ContentPage();
			NavigationPage.SetBarHeightOverride(page, 60.0);
			await nav.PushAsync(page);
			CornerstoneTest.AreEqual(60.0, nav.EffectiveBarHeight);
		}

		#endregion
	}

	[TestClass]
	public class BackButtonContentTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task CurrentPageBackButtonContentUpdatesRenderedPresenter()
		{
			var parts = new BackButtonParts();
			var nav = CreateNavigationPageWithBackButtonParts(parts);
			var root = new TestRoot { Child = nav };
			root.LayoutManager.ExecuteInitialLayoutPass();

			await nav.PushAsync(new ContentPage { Header = "Root" });
			var detail = new ContentPage { Header = "Detail" };
			await nav.PushAsync(detail);

			var customContent = "Custom";
			NavigationPage.SetBackButtonContent(detail, customContent);

			CornerstoneTest.IsNotNull(parts.DefaultIcon);
			CornerstoneTest.IsNotNull(parts.ContentPresenter);
			CornerstoneTest.IsFalse(parts.DefaultIcon!.IsVisible);
			CornerstoneTest.AreEqual(customContent, parts.ContentPresenter!.Content);

			NavigationPage.SetBackButtonContent(detail, null);

			CornerstoneTest.IsTrue(parts.DefaultIcon.IsVisible);
			CornerstoneTest.IsNull(parts.ContentPresenter.Content);
		}

		[PresentationTestMethod]
		public async Task DrawerBehaviorChangeDoesNotClearCustomPathIcon()
		{
			var parts = new BackButtonParts();
			var nav = CreateNavigationPageWithBackButtonParts(parts);
			var drawer = new DrawerPage();
			var root = new TestRoot { Child = nav };
			root.LayoutManager.ExecuteInitialLayoutPass();
			nav.SetDrawerPage(drawer);

			var customIcon = new PathIcon();
			var page = new ContentPage { Header = "Root" };
			NavigationPage.SetBackButtonContent(page, customIcon);

			await nav.PushAsync(page);
			drawer.DrawerBehavior = DrawerBehavior.Locked;
			nav.SetDrawerPage(drawer);

			CornerstoneTest.IsNotNull(parts.DefaultIcon);
			CornerstoneTest.IsNotNull(parts.ContentPresenter);
			CornerstoneTest.Same(customIcon, NavigationPage.GetBackButtonContent(page));
			CornerstoneTest.Same(customIcon, parts.ContentPresenter!.Content);
			CornerstoneTest.IsFalse(parts.DefaultIcon!.IsVisible);
		}

		[PresentationTestMethod]
		public async Task DrawerToggleUsesMenuIconWithoutMutatingPageBackButtonContent()
		{
			var parts = new BackButtonParts();
			var nav = CreateNavigationPageWithBackButtonParts(parts);
			nav.Resources.Add("NavigationPageMenuIcon", new StreamGeometry());
			var drawer = new DrawerPage();
			var root = new TestRoot { Child = nav };
			root.LayoutManager.ExecuteInitialLayoutPass();
			nav.SetDrawerPage(drawer);

			var page = new ContentPage { Header = "Root" };
			await nav.PushAsync(page);

			CornerstoneTest.IsNotNull(parts.DefaultIcon);
			CornerstoneTest.IsNotNull(parts.ContentPresenter);
			CornerstoneTest.IsNull(NavigationPage.GetBackButtonContent(page));
			CornerstoneTest.IsFalse(parts.DefaultIcon!.IsVisible);
			CornerstoneTest.IsType<PathIcon>(parts.ContentPresenter!.Content);
		}

		private static NavigationPage CreateNavigationPageWithBackButtonParts(BackButtonParts parts)
		{
			return new NavigationPage
			{
				Template = new FuncControlTemplate<NavigationPage>((parent, ns) =>
				{
					parts.DefaultIcon = new Path { Name = "PART_BackButtonDefaultIcon" }.RegisterInNameScope(ns);
					parts.ContentPresenter = new ContentPresenter { Name = "PART_BackButtonContentPresenter" }.RegisterInNameScope(ns);

					return new Panel
					{
						Children =
						{
							new Border
							{
								Name = "PART_NavigationBar",
								Child = new Button
								{
									Name = "PART_BackButton",
									Content = new Panel
									{
										Children =
										{
											parts.DefaultIcon,
											parts.ContentPresenter
										}
									}
								}.RegisterInNameScope(ns)
							}.RegisterInNameScope(ns),
							new Panel
							{
								Name = "PART_ContentHost",
								Children =
								{
									new ContentPresenter { Name = "PART_PageBackPresenter" }.RegisterInNameScope(ns),
									new ContentPresenter { Name = "PART_PagePresenter" }.RegisterInNameScope(ns)
								}
							}.RegisterInNameScope(ns),
							new ContentPresenter { Name = "PART_TopCommandBar" }.RegisterInNameScope(ns),
							new ContentPresenter { Name = "PART_ModalBackPresenter" }.RegisterInNameScope(ns),
							new ContentPresenter { Name = "PART_ModalPresenter" }.RegisterInNameScope(ns)
						}
					};
				})
			};
		}

		#endregion

		#region Classes

		private sealed class BackButtonParts
		{
			#region Properties

			public ContentPresenter ContentPresenter { get; set; }
			public Path DefaultIcon { get; set; }

			#endregion
		}

		#endregion
	}

	[TestClass]
	public class BackButtonVisibilityTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task BackButtonVisibleFalseWhenIsBackButtonVisibleIsFalse()
		{
			var nav = new NavigationPage { IsBackButtonVisible = false };
			await nav.PushAsync(new ContentPage());
			await nav.PushAsync(new ContentPage());
			CornerstoneTest.IsFalse(nav.IsBackButtonEffectivelyVisible);
		}

		[PresentationTestMethod]
		public async Task BackButtonVisibleFalseWhenPerPageIsBackButtonVisibleIsFalse()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var top = new ContentPage();
			NavigationPage.SetHasBackButton(top, false);
			await nav.PushAsync(top);
			CornerstoneTest.IsFalse(nav.IsBackButtonEffectivelyVisible);
		}

		[PresentationTestMethod]
		public async Task BackButtonVisibleFalseWhenStackDepthIsOne()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			CornerstoneTest.IsFalse(nav.IsBackButtonEffectivelyVisible);
		}

		[PresentationTestMethod]
		public async Task BackButtonVisibleTrueAfterRestoringGlobalVisibility()
		{
			var nav = new NavigationPage { IsBackButtonVisible = false };
			await nav.PushAsync(new ContentPage());
			await nav.PushAsync(new ContentPage());
			nav.IsBackButtonVisible = true;
			CornerstoneTest.IsTrue(nav.IsBackButtonEffectivelyVisible);
		}

		[PresentationTestMethod]
		public async Task BackButtonVisibleTrueWhenStackDepthIsTwo()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			await nav.PushAsync(new ContentPage());
			CornerstoneTest.IsTrue(nav.IsBackButtonEffectivelyVisible);
		}

		[PresentationTestMethod]
		public async Task BackButtonVisibleUpdatesWhenCurrentPageHasBackButtonChanges()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var top = new ContentPage();
			await nav.PushAsync(top);

			CornerstoneTest.IsTrue(nav.IsBackButtonEffectivelyVisible);

			NavigationPage.SetHasBackButton(top, false);
			CornerstoneTest.IsFalse(nav.IsBackButtonEffectivelyVisible);

			NavigationPage.SetHasBackButton(top, true);
			CornerstoneTest.IsTrue(nav.IsBackButtonEffectivelyVisible);
		}

		#endregion
	}

	[TestClass]
	public class ContentPageCoerceTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ContentPageContentSetToNonPageDoesNotThrow()
		{
			var page = new ContentPage();
			page.Content = "hello";
			CornerstoneTest.AreEqual("hello", page.Content);
		}

		[PresentationTestMethod]
		public void ContentPageContentSetToPageThrowsInvalidOperationException()
		{
			var page = new ContentPage();
			Assert.Throws<InvalidOperationException>(() => page.Content = new ContentPage());
		}

		#endregion
	}

	[TestClass]
	public class DrawerPageFirstPageTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DrawerPageContentReplacedResendsNavigatedToOnLoad()
		{
			var root = new TestRoot();
			var nav1 = new NavigationPage();
			var page1 = new ContentPage();
			var drawer = new DrawerPage { Content = nav1 };
			root.Child = drawer;
			root.LayoutManager.ExecuteInitialLayoutPass();

			NavigatedToEventArgs firstArgs = null;
			page1.NavigatedTo += (_, e) => firstArgs = e;
			nav1.Content = page1;

			CornerstoneTest.IsNotNull(firstArgs);

			var nav2 = new NavigationPage();
			var page2 = new ContentPage();

			NavigatedToEventArgs secondArgs = null;
			page2.NavigatedTo += (_, e) => secondArgs = e;

			drawer.Content = nav2;
			nav2.Content = page2;

			CornerstoneTest.IsNotNull(secondArgs);
		}

		#endregion
	}

	[TestClass]
	public class InitialContentTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task ContentSetAfterPushIsIgnored()
		{
			var nav = new NavigationPage();
			var first = new ContentPage { Header = "First" };
			await nav.PushAsync(first);

			// Setting Content when stack is already populated should not push again
			nav.Content = new ContentPage { Header = "Second" };
			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(first, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public void ContentSetBeforePushIsUsedAsInitialPage()
		{
			var page = new ContentPage { Header = "Initial" };
			var nav = new NavigationPage { Content = page };
			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(page, nav.CurrentPage);
		}

		#endregion
	}

	[TestClass]
	public class InsertRemoveTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task InsertPageAddsPageBeforeTarget()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			var middle = new ContentPage { Header = "Middle" };
			nav.InsertPage(middle, top);

			var stack = nav.NavigationStack;
			CornerstoneTest.AreEqual(3, stack.Count);
			CornerstoneTest.Same(root, stack[0]);
			CornerstoneTest.Same(middle, stack[1]);
			CornerstoneTest.Same(top, stack[2]);
		}

		[PresentationTestMethod]
		public async Task InsertPageBeforeNotInStackThrowsInvalidOperationException()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			var stranger = new ContentPage();
			Assert.Throws<InvalidOperationException>(() => nav.InsertPage(new ContentPage(), stranger));
		}

		[PresentationTestMethod]
		public async Task InsertPageDoesNotFireNavigatedToOnInsertedPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			NavigatedToEventArgs args = null;
			var inserted = new ContentPage();
			inserted.NavigatedTo += (_, e) => args = e;

			nav.InsertPage(inserted, top);

			CornerstoneTest.IsNull(args);
		}

		[PresentationTestMethod]
		public async Task InsertPageDuplicatePageThrowsInvalidOperationException()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			Assert.Throws<InvalidOperationException>(() => nav.InsertPage(root, top));
		}

		[PresentationTestMethod]
		public async Task InsertPageFiresNavigatedToWhenInsertedPageBecomesCurrent()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			var inserted = new ContentPage();
			var navigatedToCount = 0;
			NavigatedToEventArgs args = null;
			inserted.NavigatedTo += (_, e) =>
			{
				navigatedToCount++;
				args = e;
			};

			nav.InsertPage(inserted, top);
			CornerstoneTest.AreEqual(0, navigatedToCount);

			await nav.PopAsync();

			CornerstoneTest.AreEqual(1, navigatedToCount);
			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Pop, args!.NavigationType);
			CornerstoneTest.Same(top, args.PreviousPage);
			CornerstoneTest.Same(inserted, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task InsertPageFiresPageInsertedEvent()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			PageInsertedEventArgs args = null;
			nav.PageInserted += (_, e) => args = e;

			var inserted = new ContentPage();
			nav.InsertPage(inserted, top);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(inserted, args.Page);
		}

		[PresentationTestMethod]
		public async Task InsertPageNullBeforeThrowsArgumentNullException()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			Assert.Throws<ArgumentNullException>(() => nav.InsertPage(new ContentPage(), null!));
		}

		[PresentationTestMethod]
		public async Task InsertPageNullPageThrowsArgumentNullException()
		{
			var nav = new NavigationPage();
			var before = new ContentPage();
			await nav.PushAsync(before);
			Assert.Throws<ArgumentNullException>(() => nav.InsertPage(null!, before));
		}

		[PresentationTestMethod]
		public async Task InsertPagePageAlreadyPresentedModallyThrowsInvalidOperationException()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			var modal = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);
			await nav.PushModalAsync(modal);

			Assert.Throws<InvalidOperationException>(() => nav.InsertPage(modal, top));
		}

		[PresentationTestMethod]
		public async Task RemovePageFiresPageRemovedEvent()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var mid = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(mid);
			await nav.PushAsync(top);

			PageRemovedEventArgs args = null;
			nav.PageRemoved += (_, e) => args = e;

			nav.RemovePage(mid);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(mid, args.Page);
		}

		[PresentationTestMethod]
		public async Task RemovePageNullPageThrowsArgumentNullException()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			Assert.Throws<ArgumentNullException>(() => nav.RemovePage(null!));
		}

		[PresentationTestMethod]
		public async Task RemovePagePageNotInStackIsNoOp()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var stranger = new ContentPage();

			// Should not throw and should not change stack depth
			nav.RemovePage(stranger);
			CornerstoneTest.AreEqual(1, nav.StackDepth);
		}

		[PresentationTestMethod]
		public async Task RemovePageRemovesFromMiddleOfStack()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var middle = new ContentPage { Header = "Middle" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(middle);
			await nav.PushAsync(top);

			nav.RemovePage(middle);

			var stack = nav.NavigationStack;
			CornerstoneTest.AreEqual(2, stack.Count);
			CornerstoneTest.Same(root, stack[0]);
			CornerstoneTest.Same(top, stack[1]);
		}

		[PresentationTestMethod]
		public async Task RemovePageTopPageFiresNavigatedFromWithRemoveType()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			NavigatedFromEventArgs fromArgs = null;
			top.NavigatedFrom += (_, e) => fromArgs = e;

			nav.RemovePage(top);

			CornerstoneTest.IsNotNull(fromArgs);
			CornerstoneTest.AreEqual(NavigationType.Remove, fromArgs!.NavigationType);
			CornerstoneTest.Same(root, fromArgs.DestinationPage);
		}

		[PresentationTestMethod]
		public async Task RemovePageTopPageFiresNavigatedToOnNewCurrentPageWithRemoveType()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			NavigatedToEventArgs toArgs = null;
			root.NavigatedTo += (_, e) => toArgs = e;

			nav.RemovePage(top);

			CornerstoneTest.IsNotNull(toArgs);
			CornerstoneTest.AreEqual(NavigationType.Remove, toArgs!.NavigationType);
			CornerstoneTest.Same(top, toArgs.PreviousPage);
		}

		[PresentationTestMethod]
		public async Task RemovePageTopPageUpdatesCurrentPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			nav.RemovePage(top);

			CornerstoneTest.Same(root, nav.CurrentPage);
			CornerstoneTest.AreEqual(1, nav.StackDepth);
		}

		#endregion
	}

	[TestClass]
	public class IsNavigatingTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task IsNavigatingFalseAfterPushCompletes()
		{
			var tcs = new TaskCompletionSource<bool>();
			var nav = CreateNavigationPage(new ControllableTransition(tcs.Task));

			await nav.PushAsync(new ContentPage());
			var pushTask = nav.PushAsync(new ContentPage());
			tcs.SetResult(true);
			await pushTask;

			CornerstoneTest.IsFalse(nav.IsNavigating);
		}

		[PresentationTestMethod]
		public void IsNavigatingFalseByDefault()
		{
			var nav = new NavigationPage();
			CornerstoneTest.IsFalse(nav.IsNavigating);
		}

		[PresentationTestMethod]
		public async Task IsNavigatingTrueWhilePushInProgress()
		{
			var tcs = new TaskCompletionSource<bool>();
			var nav = CreateNavigationPage(new ControllableTransition(tcs.Task));

			await nav.PushAsync(new ContentPage());

			var duringPush = false;
			var pushTask = nav.PushAsync(new ContentPage());
			duringPush = nav.IsNavigating;
			tcs.SetResult(true);
			await pushTask;

			CornerstoneTest.IsTrue(duringPush);
		}

		#endregion
	}

	[TestClass]
	public class LifecycleAfterTransitionTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopAsyncLifecycleEventsFireAfterTransition()
		{
			var tcs = new TaskCompletionSource();
			var nav = CreateNavigationPage(null);

			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			nav.PageTransition = new ControllableTransition(tcs.Task);

			var navigatedFromDuringTransition = false;
			var navigatedToDuringTransition = false;
			var poppedDuringTransition = false;

			top.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
			root.NavigatedTo += (_, _) => navigatedToDuringTransition = !tcs.Task.IsCompleted;
			nav.Popped += (_, _) => poppedDuringTransition = !tcs.Task.IsCompleted;

			var popTask = nav.PopAsync();

			tcs.SetResult();
			await popTask;

			CornerstoneTest.IsFalse(navigatedFromDuringTransition);
			CornerstoneTest.IsFalse(navigatedToDuringTransition);
			CornerstoneTest.IsFalse(poppedDuringTransition);
		}

		[PresentationTestMethod]
		public async Task PopToRootAsyncLifecycleEventsFireAfterTransition()
		{
			var tcs = new TaskCompletionSource();
			var nav = CreateNavigationPage(null);

			var root = new ContentPage { Header = "Root" };
			var second = new ContentPage { Header = "Second" };
			var third = new ContentPage { Header = "Third" };
			await nav.PushAsync(root);
			await nav.PushAsync(second);
			await nav.PushAsync(third);

			nav.PageTransition = new ControllableTransition(tcs.Task);

			var navigatedFromDuringTransition = false;
			var navigatedToDuringTransition = false;
			var poppedToRootDuringTransition = false;

			second.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
			third.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
			root.NavigatedTo += (_, _) => navigatedToDuringTransition = !tcs.Task.IsCompleted;
			nav.PoppedToRoot += (_, _) => poppedToRootDuringTransition = !tcs.Task.IsCompleted;

			var popTask = nav.PopToRootAsync();

			tcs.SetResult();
			await popTask;

			CornerstoneTest.IsFalse(navigatedFromDuringTransition);
			CornerstoneTest.IsFalse(navigatedToDuringTransition);
			CornerstoneTest.IsFalse(poppedToRootDuringTransition);
		}

		[PresentationTestMethod]
		public async Task PushAsyncLifecycleEventsFireAfterTransition()
		{
			var tcs = new TaskCompletionSource();
			var transition = new ControllableTransition(tcs.Task);
			var nav = CreateNavigationPage(transition);

			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);

			var navigatedFromDuringTransition = false;
			var navigatedToDuringTransition = false;
			var pushedDuringTransition = false;

			var second = new ContentPage { Header = "Second" };
			root.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
			second.NavigatedTo += (_, _) => navigatedToDuringTransition = !tcs.Task.IsCompleted;
			nav.Pushed += (_, _) => pushedDuringTransition = !tcs.Task.IsCompleted;

			var pushTask = nav.PushAsync(second);

			tcs.SetResult();
			await pushTask;

			CornerstoneTest.IsFalse(navigatedFromDuringTransition);
			CornerstoneTest.IsFalse(navigatedToDuringTransition);
			CornerstoneTest.IsFalse(pushedDuringTransition);
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncLifecycleEventsFireAfterTransition()
		{
			var tcs = new TaskCompletionSource();
			var nav = CreateNavigationPage(null);

			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);

			nav.PageTransition = new ControllableTransition(tcs.Task);

			var navigatedFromDuringTransition = false;
			var navigatedToDuringTransition = false;

			var replacement = new ContentPage { Header = "Replacement" };
			root.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
			replacement.NavigatedTo += (_, _) => navigatedToDuringTransition = !tcs.Task.IsCompleted;

			var replaceTask = nav.ReplaceAsync(replacement);

			tcs.SetResult();
			await replaceTask;

			CornerstoneTest.IsFalse(navigatedFromDuringTransition);
			CornerstoneTest.IsFalse(navigatedToDuringTransition);
		}

		#endregion
	}

	[TestClass]
	public class LogicalChildrenTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopKeepsOutgoingPageInLogicalTreeUntilTransitionCompletes()
		{
			var gate = new TaskCompletionSource();
			var nav = CreateNavigationPage();

			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			nav.PageTransition = new ControllableTransition(gate.Task);

			var popTask = nav.PopAsync();

			// The outgoing page is still animating out, so it must remain in the logical tree.
			CornerstoneTest.Contains(nav.GetLogicalChildren(), top);

			gate.SetResult();
			await popTask;

			// Once the transition completes, the page is detached.
			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), top);
			CornerstoneTest.Contains(nav.GetLogicalChildren(), root);
		}

		[PresentationTestMethod]
		public async Task PopModalClearsNavigationAndIsInNavigationPage()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var modal = new ContentPage();
			await nav.PushModalAsync(modal);

			await nav.PopModalAsync();

			CornerstoneTest.IsNull(modal.Navigation);
			CornerstoneTest.IsFalse(modal.IsInNavigationPage);
		}

		[PresentationTestMethod]
		public async Task PopRemovesPageFromLogicalChildren()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var top = new ContentPage();
			await nav.PushAsync(top);

			await nav.PopAsync();

			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), top);
			CornerstoneTest.Contains(nav.GetLogicalChildren(), root);
		}

		[PresentationTestMethod]
		public async Task PopToRootAsyncRemovesIntermediatePagesFromLogicalChildren()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var middle = new ContentPage();
			await nav.PushAsync(middle);
			var top = new ContentPage();
			await nav.PushAsync(top);

			await nav.PopToRootAsync();

			CornerstoneTest.Contains(nav.GetLogicalChildren(), root);
			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), middle);
			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), top);
		}

		[PresentationTestMethod]
		public async Task PopToRootKeepsVisibleTopPageInLogicalTreeUntilTransitionCompletes()
		{
			var gate = new TaskCompletionSource();
			var nav = CreateNavigationPage();

			var root = new ContentPage();
			var middle = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(middle);
			await nav.PushAsync(top);

			nav.PageTransition = new ControllableTransition(gate.Task);

			var popTask = nav.PopToRootAsync();

			// The hidden intermediate page is detached eagerly, but the visible top page is
			// animating out and must stay attached until the transition completes.
			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), middle);
			CornerstoneTest.Contains(nav.GetLogicalChildren(), top);

			gate.SetResult();
			await popTask;

			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), top);
			CornerstoneTest.Contains(nav.GetLogicalChildren(), root);
		}

		[PresentationTestMethod]
		public async Task PushAsyncAddsPageToLogicalChildren()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			var second = new ContentPage();
			await nav.PushAsync(second);

			var children = new List<ILogical>(nav.GetLogicalChildren());
			CornerstoneTest.Contains(children, root);
			CornerstoneTest.Contains(children, second);
		}

		[PresentationTestMethod]
		public async Task PushModalModalPageIsNotInDirectLogicalChildren()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var modal = new ContentPage();
			await nav.PushModalAsync(modal);

			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), modal);
			CornerstoneTest.Same(nav, modal.Navigation);
			CornerstoneTest.IsTrue(modal.IsInNavigationPage);
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncSwapsLogicalChildren()
		{
			var nav = new NavigationPage();
			var original = new ContentPage();
			await nav.PushAsync(original);

			var replacement = new ContentPage();
			await nav.ReplaceAsync(replacement);

			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), original);
			CornerstoneTest.Contains(nav.GetLogicalChildren(), replacement);
		}

		[PresentationTestMethod]
		public async Task ReplaceKeepsOutgoingPageInLogicalTreeUntilTransitionCompletes()
		{
			var gate = new TaskCompletionSource();
			var nav = CreateNavigationPage();

			var original = new ContentPage();
			await nav.PushAsync(original);

			nav.PageTransition = new ControllableTransition(gate.Task);

			var replacement = new ContentPage();
			var replaceTask = nav.ReplaceAsync(replacement);

			// The replaced page is still animating out, so both pages are in the logical tree.
			CornerstoneTest.Contains(nav.GetLogicalChildren(), original);
			CornerstoneTest.Contains(nav.GetLogicalChildren(), replacement);

			gate.SetResult();
			await replaceTask;

			CornerstoneTest.DoesNotContain(nav.GetLogicalChildren(), original);
			CornerstoneTest.Contains(nav.GetLogicalChildren(), replacement);
		}

		#endregion
	}

	[TestClass]
	public class ModalTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task ModalStackIsOrderedBottomToTop()
		{
			// Index 0 = oldest (bottom-most); last index = topmost, consistent with NavigationStack.
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var m1 = new ContentPage { Header = "M1" };
			var m2 = new ContentPage { Header = "M2" };
			var m3 = new ContentPage { Header = "M3" };
			await nav.PushModalAsync(m1);
			await nav.PushModalAsync(m2);
			await nav.PushModalAsync(m3);

			CornerstoneTest.AreEqual(new[] { m1, m2, m3 }, nav.ModalStack);
		}

		[PresentationTestMethod]
		public async Task PopModalFiresModalPoppedEvent()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var modal = new ContentPage();
			await nav.PushModalAsync(modal);

			ModalPoppedEventArgs args = null;
			nav.ModalPopped += (_, e) => args = e;

			await nav.PopModalAsync();

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(modal, args.Modal);
		}

		[PresentationTestMethod]
		public async Task PopModalInvokesNavigatedFromOnPoppedModalWithPopModalType()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var modal = new ContentPage { Header = "Modal" };
			await nav.PushModalAsync(modal);

			NavigatedFromEventArgs args = null;
			modal.NavigatedFrom += (_, e) => args = e;

			await nav.PopModalAsync();

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.PopModal, args!.NavigationType);
		}

		[PresentationTestMethod]
		public async Task PopModalInvokesNavigatedToOnRevealedPageWithPopModalType()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);
			await nav.PushModalAsync(new ContentPage { Header = "Modal" });

			NavigatedToEventArgs args = null;
			root.NavigatedTo += (_, e) => args = e;

			await nav.PopModalAsync();

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.PopModal, args!.NavigationType);
		}

		[PresentationTestMethod]
		public async Task PopModalNavigatedFromDestinationPageIsRevealedPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var modal = new ContentPage { Header = "Modal" };
			await nav.PushAsync(root);
			await nav.PushModalAsync(modal);

			NavigatedFromEventArgs args = null;
			modal.NavigatedFrom += (_, e) => args = e;

			await nav.PopModalAsync();

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(root, args!.DestinationPage);
		}

		[PresentationTestMethod]
		public async Task PopModalNavigatedToPreviousPageIsPoppedModal()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var modal = new ContentPage { Header = "Modal" };
			await nav.PushAsync(root);
			await nav.PushModalAsync(modal);

			NavigatedToEventArgs args = null;
			root.NavigatedTo += (_, e) => args = e;

			await nav.PopModalAsync();

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(modal, args!.PreviousPage);
		}

		[PresentationTestMethod]
		public async Task PopModalOnEmptyStackReturnsNull()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var result = await nav.PopModalAsync();
			CornerstoneTest.IsNull(result);
		}

		[PresentationTestMethod]
		public async Task PopModalRemovesFromModalStack()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var modal = new ContentPage();
			await nav.PushModalAsync(modal);
			await nav.PopModalAsync();

			CornerstoneTest.AreEqual(0, nav.ModalStack.Count);
		}

		[PresentationTestMethod]
		public async Task PopModalWhenModalCancelsNavigatingDoesNotPopModal()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage { Header = "Root" });

			var modal = new ContentPage { Header = "Modal" };
			await nav.PushModalAsync(modal);

			var handlerInvoked = false;
			modal.Navigating += args =>
			{
				handlerInvoked = true;
				args.Cancel = true;
				return Task.CompletedTask;
			};

			var result = await nav.PopModalAsync();

			CornerstoneTest.IsTrue(handlerInvoked);
			CornerstoneTest.IsNull(result);
			CornerstoneTest.Single(nav.ModalStack);
			CornerstoneTest.Same(modal, nav.ModalStack[0]);
		}

		[PresentationTestMethod]
		public async Task PushModalAddsToModalStack()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var modal = new ContentPage { Header = "Modal" };
			await nav.PushModalAsync(modal);

			CornerstoneTest.AreEqual(1, nav.ModalStack.Count);
		}

		[PresentationTestMethod]
		public async Task PushModalDuplicateModalThrows()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var modal = new ContentPage { Header = "Modal" };
			await nav.PushModalAsync(modal);

			await Assert.ThrowsAsync<InvalidOperationException>(() => nav.PushModalAsync(modal));
		}

		[PresentationTestMethod]
		public async Task PushModalFiresModalPushedEvent()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			ModalPushedEventArgs args = null;
			nav.ModalPushed += (_, e) => args = e;

			var modal = new ContentPage();
			await nav.PushModalAsync(modal);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(modal, args.Modal);
		}

		[PresentationTestMethod]
		public async Task PushModalInvokesNavigatedFromOnCoveredPageWithPushModalType()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);

			NavigatedFromEventArgs args = null;
			root.NavigatedFrom += (_, e) => args = e;

			await nav.PushModalAsync(new ContentPage { Header = "Modal" });

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.PushModal, args!.NavigationType);
		}

		[PresentationTestMethod]
		public async Task PushModalInvokesNavigatedToOnModalPageWithPushModalType()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var modal = new ContentPage { Header = "Modal" };
			NavigatedToEventArgs args = null;
			modal.NavigatedTo += (_, e) => args = e;

			await nav.PushModalAsync(modal);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.PushModal, args!.NavigationType);
		}

		[PresentationTestMethod]
		public async Task PushModalNavigatedFromDestinationPageIsTheModal()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);

			var modal = new ContentPage { Header = "Modal" };
			NavigatedFromEventArgs args = null;
			root.NavigatedFrom += (_, e) => args = e;

			await nav.PushModalAsync(modal);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(modal, args!.DestinationPage);
		}

		[PresentationTestMethod]
		public async Task PushModalNavigatedToPreviousPageIsCoveredPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);

			var modal = new ContentPage { Header = "Modal" };
			NavigatedToEventArgs args = null;
			modal.NavigatedTo += (_, e) => args = e;

			await nav.PushModalAsync(modal);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(root, args!.PreviousPage);
		}

		[PresentationTestMethod]
		public async Task PushModalPageAlreadyInNavigationStackThrows()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);

			await Assert.ThrowsAsync<InvalidOperationException>(() => nav.PushModalAsync(root));
		}

		[PresentationTestMethod]
		public async Task PushModalWhenCoveredPageCancelsNavigatingDoesNotPushModal()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);

			var handlerInvoked = false;
			root.Navigating += args =>
			{
				handlerInvoked = true;
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PushModalAsync(new ContentPage { Header = "Modal" });

			CornerstoneTest.IsTrue(handlerInvoked);
			CornerstoneTest.Empty(nav.ModalStack);
		}

		[PresentationTestMethod]
		public async Task PushModalWhenTopModalCancelsNavigatingDoesNotPushAnotherModal()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage { Header = "Root" });

			var firstModal = new ContentPage { Header = "Modal 1" };
			await nav.PushModalAsync(firstModal);

			var handlerInvoked = false;
			firstModal.Navigating += args =>
			{
				handlerInvoked = true;
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PushModalAsync(new ContentPage { Header = "Modal 2" });

			CornerstoneTest.IsTrue(handlerInvoked);
			CornerstoneTest.Single(nav.ModalStack);
			CornerstoneTest.Same(firstModal, nav.ModalStack[0]);
		}

		#endregion
	}

	[TestClass]
	public class ModalTransitionCancellationTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopAllModalsAsyncCancelledTransitionStillClearsStackAndFiresEvents()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			var m1 = new ContentPage();
			var m2 = new ContentPage();
			await nav.PushModalAsync(m1);
			await nav.PushModalAsync(m2);

			var m1PoppedFired = false;
			var m2PoppedFired = false;
			var rootNavigatedToFired = false;
			nav.ModalPopped += (_, e) =>
			{
				if (ReferenceEquals(e.Modal, m1))
				{
					m1PoppedFired = true;
				}
				if (ReferenceEquals(e.Modal, m2))
				{
					m2PoppedFired = true;
				}
			};
			root.NavigatedTo += (_, _) => rootNavigatedToFired = true;

			await nav.PopAllModalsAsync(null);

			CornerstoneTest.AreEqual(0, nav.ModalStack.Count);
			CornerstoneTest.IsTrue(m1PoppedFired);
			CornerstoneTest.IsTrue(m2PoppedFired);
			CornerstoneTest.IsTrue(rootNavigatedToFired);
		}

		[PresentationTestMethod]
		public async Task PushModalAsyncCancelledTransitionStillFiresLifecycleEvents()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			var modal = new ContentPage();
			var navigatedFromFired = false;
			var navigatedToFired = false;
			var modalPushedFired = false;
			root.NavigatedFrom += (_, _) => navigatedFromFired = true;
			modal.NavigatedTo += (_, _) => navigatedToFired = true;
			nav.ModalPushed += (_, _) => modalPushedFired = true;

			await nav.PushModalAsync(modal, null);

			CornerstoneTest.IsTrue(navigatedFromFired);
			CornerstoneTest.IsTrue(navigatedToFired);
			CornerstoneTest.IsTrue(modalPushedFired);
			CornerstoneTest.AreEqual(1, nav.ModalStack.Count);
			CornerstoneTest.Same(modal, nav.ModalStack[0]);
		}

		#endregion
	}

	[TestClass]
	public class NavigatingEventTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task NavigatingCancelInFirstHandlerSkipsSubsequentHandlers()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var top = new ContentPage();
			await nav.PushAsync(top);

			var secondHandlerInvoked = false;
			top.Navigating += args =>
			{
				args.Cancel = true;
				return Task.CompletedTask;
			};
			top.Navigating += args =>
			{
				secondHandlerInvoked = true;
				return Task.CompletedTask;
			};

			await nav.PopAsync();

			CornerstoneTest.IsFalse(secondHandlerInvoked);
			CornerstoneTest.AreEqual(2, nav.StackDepth);
		}

		[PresentationTestMethod]
		public async Task NavigatingCancelInOnNavigatingFromSkipsNavigatingEvent()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			var top = new CancellingPage();
			await nav.PushAsync(top);

			var asyncHandlerInvoked = false;
			top.Navigating += args =>
			{
				asyncHandlerInvoked = true;
				return Task.CompletedTask;
			};

			await nav.PopAsync();

			CornerstoneTest.IsFalse(asyncHandlerInvoked);
			CornerstoneTest.AreEqual(2, nav.StackDepth);
		}

		[PresentationTestMethod]
		public async Task PopSyncInvokesNavigatingEvent()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var top = new ContentPage();
			await nav.PushAsync(top);

			var handlerInvoked = false;
			top.Navigating += args =>
			{
				handlerInvoked = true;
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PopAsync();

			CornerstoneTest.IsTrue(handlerInvoked);
			CornerstoneTest.AreEqual(2, nav.StackDepth);
		}

		[PresentationTestMethod]
		public async Task PopToPageAsyncAwaitsAsyncNavigatingHandler()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var middle = new ContentPage();
			await nav.PushAsync(middle);
			var top = new ContentPage();
			await nav.PushAsync(top);

			var handlerInvoked = false;
			top.Navigating += args =>
			{
				handlerInvoked = true;
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PopToPageAsync(root);

			CornerstoneTest.IsTrue(handlerInvoked);
			CornerstoneTest.AreEqual(3, nav.StackDepth);
			CornerstoneTest.Same(top, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PopToRootAsyncAwaitsAsyncNavigatingHandler()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var top = new ContentPage();
			await nav.PushAsync(top);

			var handlerInvoked = false;
			top.Navigating += args =>
			{
				handlerInvoked = true;
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PopToRootAsync();

			CornerstoneTest.IsTrue(handlerInvoked);
			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.Same(top, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PushSyncInvokesNavigatingEvent()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			var handlerInvoked = false;
			root.Navigating += args =>
			{
				handlerInvoked = true;
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PushAsync(new ContentPage());

			CornerstoneTest.IsTrue(handlerInvoked);
			CornerstoneTest.AreEqual(1, nav.StackDepth);
		}

		#endregion

		#region Classes

		private sealed class CancellingPage : ContentPage
		{
			#region Methods

			protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
			{
				args.Cancel = true;
			}

			#endregion
		}

		#endregion
	}

	[TestClass]
	public class NavigationStackTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task CanGoBackFalseAfterPop()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			await nav.PushAsync(new ContentPage());
			await nav.PopAsync();
			CornerstoneTest.IsFalse(nav.CanGoBack);
		}

		[PresentationTestMethod]
		public async Task CanGoBackFalseWithOneEntry()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			CornerstoneTest.IsFalse(nav.CanGoBack);
		}

		[PresentationTestMethod]
		public async Task CanGoBackTrueWithTwoEntries()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			await nav.PushAsync(new ContentPage());
			CornerstoneTest.IsTrue(nav.CanGoBack);
		}

		[PresentationTestMethod]
		public async Task NavigationStackRootAtIndexZeroTopAtLastIndex()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var middle = new ContentPage { Header = "Middle" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(middle);
			await nav.PushAsync(top);

			var stack = nav.NavigationStack;
			CornerstoneTest.AreEqual(3, stack.Count);
			CornerstoneTest.Same(root, stack[0]);
			CornerstoneTest.Same(middle, stack[1]);
			CornerstoneTest.Same(top, stack[2]);
		}

		[PresentationTestMethod]
		public async Task StackDepthAlwaysEqualsNavigationStackCount()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			CornerstoneTest.AreEqual(nav.NavigationStack.Count, nav.StackDepth);

			await nav.PushAsync(new ContentPage());
			CornerstoneTest.AreEqual(nav.NavigationStack.Count, nav.StackDepth);

			await nav.PopAsync();
			CornerstoneTest.AreEqual(nav.NavigationStack.Count, nav.StackDepth);
		}

		#endregion
	}

	[TestClass]
	public class PagesPropertyTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void PagesAssignNullThrowsInvalidOperationException()
		{
			var nav = new NavigationPage();
			var originalPages = nav.Pages;

			Assert.Throws<InvalidOperationException>(() => nav.Pages = null);

			CornerstoneTest.Same(originalPages, nav.Pages);
			CornerstoneTest.Empty(nav.NavigationStack);
			CornerstoneTest.IsNull(nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PagesDirectAssignmentDoesNotCorruptExistingStack()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);
			var originalPages = nav.Pages;

			Assert.Throws<InvalidOperationException>(() => nav.Pages = new List<Page> { new ContentPage() });

			CornerstoneTest.Same(originalPages, nav.Pages);
			CornerstoneTest.AreEqual(2, nav.NavigationStack.Count);
			CornerstoneTest.Same(root, nav.NavigationStack[0]);
			CornerstoneTest.Same(top, nav.NavigationStack[1]);
			CornerstoneTest.Same(top, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public void PagesDirectAssignmentThrowsInvalidOperationException()
		{
			var nav = new NavigationPage();
			var originalPages = nav.Pages;

			Assert.Throws<InvalidOperationException>(() => nav.Pages = new List<Page> { new ContentPage() });

			CornerstoneTest.Same(originalPages, nav.Pages);
			CornerstoneTest.Empty(nav.NavigationStack);
			CornerstoneTest.IsNull(nav.CurrentPage);
		}

		#endregion
	}

	[TestClass]
	public class PopAllModalsTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopAllModalsEmptyStackDoesNothing()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			await nav.PopAllModalsAsync();

			CornerstoneTest.AreEqual(0, nav.ModalStack.Count);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsFiresModalPoppedForEachModal()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var m1 = new ContentPage { Header = "M1" };
			var m2 = new ContentPage { Header = "M2" };
			var m3 = new ContentPage { Header = "M3" };
			await nav.PushModalAsync(m1);
			await nav.PushModalAsync(m2);
			await nav.PushModalAsync(m3);

			var popped = new List<Page>();
			nav.ModalPopped += (_, e) => popped.Add(e.Modal);

			await nav.PopAllModalsAsync();

			CornerstoneTest.AreEqual(3, popped.Count);

			// LIFO order: m3 was pushed last so must be popped first.
			CornerstoneTest.AreEqual(new[] { m3, m2, m1 }, popped);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsFiresNavigatedFromOnAllModals()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var m1 = new ContentPage { Header = "M1" };
			var m2 = new ContentPage { Header = "M2" };
			await nav.PushModalAsync(m1);
			await nav.PushModalAsync(m2);

			var navigatedFrom = new List<string>();
			m1.NavigatedFrom += (_, _) => navigatedFrom.Add("M1");
			m2.NavigatedFrom += (_, _) => navigatedFrom.Add("M2");

			await nav.PopAllModalsAsync();

			// LIFO order: M2 was pushed last, so it navigates from first.
			CornerstoneTest.AreEqual(new[] { "M2", "M1" }, navigatedFrom);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsFiresNavigatedToOnUnderlyingPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);
			await nav.PushModalAsync(new ContentPage { Header = "M1" });
			await nav.PushModalAsync(new ContentPage { Header = "M2" });

			var navigatedTo = false;
			root.NavigatedTo += (_, _) => navigatedTo = true;

			await nav.PopAllModalsAsync();

			CornerstoneTest.IsTrue(navigatedTo);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsMultipleModalsClearsEntireStack()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			await nav.PushModalAsync(new ContentPage { Header = "M1" });
			await nav.PushModalAsync(new ContentPage { Header = "M2" });
			await nav.PushModalAsync(new ContentPage { Header = "M3" });

			await nav.PopAllModalsAsync();

			CornerstoneTest.AreEqual(0, nav.ModalStack.Count);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsMultipleModalsNavigatedToFiredOnlyOnBaseCurrentPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);
			var m1 = new ContentPage { Header = "M1" };
			var m2 = new ContentPage { Header = "M2" };
			await nav.PushModalAsync(m1);
			await nav.PushModalAsync(m2);

			var navigatedToPages = new List<string>();
			root.NavigatedTo += (_, _) => navigatedToPages.Add("root");
			m1.NavigatedTo += (_, _) => navigatedToPages.Add("m1");
			m2.NavigatedTo += (_, _) => navigatedToPages.Add("m2");

			await nav.PopAllModalsAsync();

			CornerstoneTest.AreEqual(["root"], navigatedToPages);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsNavigatedFromNavigationTypeIsPopModal()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var m1 = new ContentPage();
			var m2 = new ContentPage();
			await nav.PushModalAsync(m1);
			await nav.PushModalAsync(m2);

			NavigationType? m1Type = null;
			NavigationType? m2Type = null;
			m1.NavigatedFrom += (_, e) => m1Type = e.NavigationType;
			m2.NavigatedFrom += (_, e) => m2Type = e.NavigationType;

			await nav.PopAllModalsAsync();

			CornerstoneTest.AreEqual(NavigationType.PopModal, m1Type);
			CornerstoneTest.AreEqual(NavigationType.PopModal, m2Type);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsNavigatedToNavigationTypeIsPopModal()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushModalAsync(new ContentPage());

			NavigationType? type = null;
			root.NavigatedTo += (_, e) => type = e.NavigationType;

			await nav.PopAllModalsAsync();

			CornerstoneTest.AreEqual(NavigationType.PopModal, type);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsNavigatedToPreviousPageIsTopModal()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var m1 = new ContentPage();
			var m2 = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushModalAsync(m1);
			await nav.PushModalAsync(m2);

			Page previousPage = null;
			root.NavigatedTo += (_, e) => previousPage = e.PreviousPage;

			await nav.PopAllModalsAsync();

			CornerstoneTest.Same(m2, previousPage);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsSingleModalClearsModalStack()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			await nav.PushModalAsync(new ContentPage());

			await nav.PopAllModalsAsync();

			CornerstoneTest.AreEqual(0, nav.ModalStack.Count);
		}

		[PresentationTestMethod]
		public async Task PopAllModalsWhenTopModalCancelsNavigatingDoesNotClearModalStack()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage { Header = "Root" });

			var m1 = new ContentPage { Header = "M1" };
			var m2 = new ContentPage { Header = "M2" };
			await nav.PushModalAsync(m1);
			await nav.PushModalAsync(m2);

			var handlerInvoked = false;
			m2.Navigating += args =>
			{
				handlerInvoked = true;
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PopAllModalsAsync();

			CornerstoneTest.IsTrue(handlerInvoked);
			CornerstoneTest.AreEqual(new[] { m1, m2 }, nav.ModalStack);
		}

		#endregion
	}

	[TestClass]
	public class PopTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopAsyncInvokesNavigatedToOnRevealedPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var top = new ContentPage();
			await nav.PushAsync(top);

			var navigatedTo = false;
			root.NavigatedTo += (_, _) => navigatedTo = true;

			await nav.PopAsync();

			CornerstoneTest.IsTrue(navigatedTo);
		}

		[PresentationTestMethod]
		public async Task PopAsyncWhenNavigatingFromCancelsDoesNotPop()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			var top = new ContentPage();
			await nav.PushAsync(top);

			top.Navigating += args =>
			{
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PopAsync();

			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.Same(top, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PopClearsIsInNavigationPageOnPoppedPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			await nav.PopAsync();

			CornerstoneTest.IsFalse(top.IsInNavigationPage);
			CornerstoneTest.IsNull(top.Navigation);
		}

		[PresentationTestMethod]
		public async Task PopFiresPoppedEvent()
		{
			var nav = new NavigationPage();
			NavigationEventArgs received = null;
			nav.Popped += (_, e) => received = e;

			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);
			await nav.PopAsync();

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.Same(top, received.Page);
			CornerstoneTest.AreEqual(NavigationType.Pop, received.NavigationType);
		}

		[PresentationTestMethod]
		public async Task PopInvokesNavigatedToOnRevealedPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			NavigatedToEventArgs args = null;
			root.NavigatedTo += (_, e) => args = e;
			await nav.PushAsync(root);

			var top = new ContentPage();
			await nav.PushAsync(top);
			await nav.PopAsync();

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(top, args.PreviousPage);
			CornerstoneTest.AreEqual(NavigationType.Pop, args.NavigationType);
		}

		[PresentationTestMethod]
		public async Task PopOnEmptyStackReturnsNull()
		{
			var nav = new NavigationPage();
			CornerstoneTest.IsNull(await nav.PopAsync());
		}

		[PresentationTestMethod]
		public async Task PopOnRootOnlyReturnsNullAndKeepsRoot()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var result = await nav.PopAsync();
			CornerstoneTest.IsNull(result);
			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(root, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PopReturnsPoppedPageAndDecrementsStack()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			var result = await nav.PopAsync();

			CornerstoneTest.Same(top, result);
			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(root, nav.CurrentPage);
		}

		#endregion
	}

	[TestClass]
	public class PopToPageLifecycleTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopToPageAsyncIntermediatePagesNavigationTypeIsPop()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var target = new ContentPage();
			var middle = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(target);
			await nav.PushAsync(middle);
			await nav.PushAsync(top);

			NavigationType? middleType = null;
			NavigationType? topType = null;
			middle.NavigatedFrom += (_, e) => middleType = e.NavigationType;
			top.NavigatedFrom += (_, e) => topType = e.NavigationType;

			await nav.PopToPageAsync(target);

			CornerstoneTest.AreEqual(NavigationType.Pop, topType);
			CornerstoneTest.AreEqual(NavigationType.Pop, middleType);
		}

		[PresentationTestMethod]
		public async Task PopToPageAsyncNavigationTypeIsPop()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var target = new ContentPage { Header = "Target" };
			await nav.PushAsync(root);
			await nav.PushAsync(target);
			await nav.PushAsync(new ContentPage());

			NavigationType? receivedType = null;
			target.NavigatedTo += (_, args) => receivedType = args.NavigationType;

			await nav.PopToPageAsync(target);

			CornerstoneTest.AreEqual(NavigationType.Pop, receivedType);
		}

		[PresentationTestMethod]
		public async Task PopToPageAsyncPageNotInStackThrowsArgumentException()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var stranger = new ContentPage();
			await Assert.ThrowsAsync<ArgumentException>(() => nav.PopToPageAsync(stranger));
		}

		[PresentationTestMethod]
		public async Task PopToPageAsyncPreviousPageIsNotNull()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var target = new ContentPage { Header = "Target" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(target);
			await nav.PushAsync(top);

			Page receivedPreviousPage = null;
			target.NavigatedTo += (_, args) => receivedPreviousPage = args.PreviousPage;

			await nav.PopToPageAsync(target);

			CornerstoneTest.Same(top, receivedPreviousPage);
		}

		[PresentationTestMethod]
		public async Task PopToPageAsyncTargetIsAlreadyTopIsNoOp()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			var navigatedToCount = 0;
			top.NavigatedTo += (_, _) => navigatedToCount++;

			await nav.PopToPageAsync(top);

			CornerstoneTest.Same(top, nav.CurrentPage);
			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.AreEqual(0, navigatedToCount);
		}

		#endregion
	}

	[TestClass]
	public class PopToRootLifecycleTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopToRootAsyncNavigationTypeIsPopToRoot()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);
			await nav.PushAsync(new ContentPage());

			NavigationType? receivedType = null;
			root.NavigatedTo += (_, args) => receivedType = args.NavigationType;

			await nav.PopToRootAsync();

			CornerstoneTest.AreEqual(NavigationType.PopToRoot, receivedType);
		}

		[PresentationTestMethod]
		public async Task PopToRootAsyncPreviousPageIsNotNull()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var second = new ContentPage { Header = "Second" };
			var third = new ContentPage { Header = "Third" };
			await nav.PushAsync(root);
			await nav.PushAsync(second);
			await nav.PushAsync(third);

			Page receivedPreviousPage = null;
			root.NavigatedTo += (_, args) => receivedPreviousPage = args.PreviousPage;

			await nav.PopToRootAsync();

			CornerstoneTest.Same(third, receivedPreviousPage);
		}

		#endregion
	}

	[TestClass]
	public class PopToRootTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopToRootFiresPoppedToRootEvent()
		{
			var nav = new NavigationPage();
			var fired = false;
			nav.PoppedToRoot += (_, _) => fired = true;

			await nav.PushAsync(new ContentPage());
			await nav.PushAsync(new ContentPage());
			await nav.PopToRootAsync();

			CornerstoneTest.IsTrue(fired);
		}

		[PresentationTestMethod]
		public async Task PopToRootInvokesNavigatedFromOnCurrentPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			NavigatedFromEventArgs args = null;
			top.NavigatedFrom += (_, e) => args = e;

			await nav.PopToRootAsync();

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.PopToRoot, args!.NavigationType);
			CornerstoneTest.Same(root, args!.DestinationPage);
		}

		[PresentationTestMethod]
		public async Task PopToRootInvokesNavigatedToOnRootPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			NavigatedToEventArgs args = null;
			root.NavigatedTo += (_, e) => args = e;

			await nav.PopToRootAsync();

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.PopToRoot, args!.NavigationType);
			CornerstoneTest.Same(top, args!.PreviousPage);
		}

		[PresentationTestMethod]
		public async Task PopToRootLeavesOnlyFirstPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);
			await nav.PushAsync(new ContentPage());
			await nav.PushAsync(new ContentPage());
			await nav.PushAsync(new ContentPage());

			await nav.PopToRootAsync();

			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(root, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PopToRootNavigatedFromFiresAfterStateChange()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			var stackDepthAtEvent = -1;
			top.NavigatedFrom += (_, _) => stackDepthAtEvent = nav.StackDepth;

			await nav.PopToRootAsync();

			CornerstoneTest.AreEqual(1, stackDepthAtEvent);
		}

		[PresentationTestMethod]
		public async Task PopToRootWhenAlreadyAtRootDoesNothing()
		{
			var nav = new NavigationPage();
			var fired = false;
			nav.PoppedToRoot += (_, _) => fired = true;

			await nav.PushAsync(new ContentPage());
			await nav.PopToRootAsync();

			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.IsFalse(fired);
		}

		#endregion
	}

	[TestClass]
	public class PropertyTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void IsGestureEnabledDefaultIsTrue()
		{
			var nav = new NavigationPage();
			CornerstoneTest.IsTrue(nav.IsGestureEnabled);
		}

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void IsGestureEnabledRoundTrips(bool value)
		{
			var nav = new NavigationPage { IsGestureEnabled = value };
			CornerstoneTest.AreEqual(value, nav.IsGestureEnabled);
		}

		[PresentationTestMethod]
		public async Task SafeAreaPaddingAffecctsNavBarHeight()
		{
			var nav = new NavigationPage
			{
				SafeAreaPadding = new Thickness(10)
			};
			var page = new ContentPage();
			NavigationPage.SetBarHeightOverride(page, 60.0);
			await nav.PushAsync(page);
			CornerstoneTest.AreEqual(70.0, nav.EffectiveBarHeight);
		}

		#endregion
	}

	[TestClass]
	public class PushTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PushAsyncWhenNavigatingFromCancelsDoesNotPush()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			root.Navigating += args =>
			{
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PushAsync(new ContentPage());

			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(root, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PushDuplicatePageThrows()
		{
			var nav = new NavigationPage();
			var page = new ContentPage();
			await nav.PushAsync(page);
			await Assert.ThrowsAsync<InvalidOperationException>(() => nav.PushAsync(page));
		}

		[PresentationTestMethod]
		public async Task PushFiresPushedEvent()
		{
			var nav = new NavigationPage();
			NavigationEventArgs received = null;
			nav.Pushed += (_, e) => received = e;

			var page = new ContentPage();
			await nav.PushAsync(page);

			CornerstoneTest.IsNotNull(received);
			CornerstoneTest.Same(page, received.Page);
			CornerstoneTest.AreEqual(NavigationType.Push, received.NavigationType);
		}

		[PresentationTestMethod]
		public async Task PushInvokesNavigatedFromOnPreviousPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			NavigatedFromEventArgs args = null;
			root.NavigatedFrom += (_, e) => args = e;
			await nav.PushAsync(root);

			var second = new ContentPage();
			await nav.PushAsync(second);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(second, args.DestinationPage);
			CornerstoneTest.AreEqual(NavigationType.Push, args.NavigationType);
		}

		[PresentationTestMethod]
		public async Task PushInvokesNavigatedToOnPushedPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			NavigatedToEventArgs args = null;
			var second = new ContentPage();
			second.NavigatedTo += (_, e) => args = e;

			await nav.PushAsync(second);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.Same(root, args.PreviousPage);
			CornerstoneTest.AreEqual(NavigationType.Push, args.NavigationType);
		}

		[PresentationTestMethod]
		[DataRow(1)]
		[DataRow(3)]
		[DataRow(10)]
		public async Task PushMultipleTimesStackDepthMatchesCount(int n)
		{
			var nav = new NavigationPage();
			for (var i = 0; i < n; i++)
			{
				await nav.PushAsync(new ContentPage { Header = $"Page {i}" });
			}
			CornerstoneTest.AreEqual(n, nav.StackDepth);
		}

		[PresentationTestMethod]
		public async Task PushPageAlreadyPresentedModallyThrows()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var modal = new ContentPage();
			await nav.PushModalAsync(modal);

			await Assert.ThrowsAsync<InvalidOperationException>(() => nav.PushAsync(modal));
		}

		[PresentationTestMethod]
		public async Task PushReentrantFromNavigatedToIsIgnoredNotThrown()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			var second = new ContentPage();
			second.NavigatedTo += async (_, _) => await nav.PushAsync(new ContentPage());

			await nav.PushAsync(second);

			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.Same(second, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PushSetsCurrentPageToTopPage()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);
			CornerstoneTest.Same(top, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PushSetsIsInNavigationPage()
		{
			var nav = new NavigationPage();
			var page = new ContentPage();
			CornerstoneTest.IsFalse(page.IsInNavigationPage);
			await nav.PushAsync(page);
			CornerstoneTest.IsTrue(page.IsInNavigationPage);
		}

		[PresentationTestMethod]
		public async Task PushSetsNavigationProperty()
		{
			var nav = new NavigationPage();
			var page = new ContentPage();
			await nav.PushAsync(page);
			CornerstoneTest.Same(nav, page.Navigation);
		}

		[PresentationTestMethod]
		public async Task PushSinglePageStackDepthBecomesOne()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			CornerstoneTest.AreEqual(1, nav.StackDepth);
		}

		#endregion
	}

	[TestClass]
	public class ReentrantNavigationTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopModalReentrantFromNavigatedToIsIgnored()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var modal = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushModalAsync(modal);

			root.NavigatedTo += async (_, _) => await nav.PopModalAsync();

			await nav.PopModalAsync();

			CornerstoneTest.AreEqual(0, nav.ModalStack.Count);
			CornerstoneTest.Same(root, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PopReentrantFromNavigatedToIsIgnored()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var top = new ContentPage();
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			root.NavigatedTo += async (_, _) => await nav.PopAsync();

			await nav.PopAsync();

			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(root, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PushModalReentrantFromNavigatedToIsIgnored()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());

			var modal = new ContentPage();
			modal.NavigatedTo += async (_, _) => await nav.PushModalAsync(new ContentPage());

			await nav.PushModalAsync(modal);

			CornerstoneTest.AreEqual(1, nav.ModalStack.Count);
			CornerstoneTest.Same(modal, nav.ModalStack[0]);
		}

		#endregion
	}

	[TestClass]
	public class ReplaceTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task ReplaceAsyncFiresLifecycleEventsInOrder()
		{
			var nav = new NavigationPage();
			var original = new ContentPage { Header = "Original" };
			await nav.PushAsync(original);

			var replacement = new ContentPage { Header = "Replacement" };
			var order = new List<string>();
			original.NavigatedFrom += (_, _) => order.Add("Original: NavigatedFrom");
			replacement.NavigatedTo += (_, _) => order.Add("Replacement: NavigatedTo");

			await nav.ReplaceAsync(replacement);

			CornerstoneTest.AreEqual(new[]
			{
				"Original: NavigatedFrom",
				"Replacement: NavigatedTo"
			}, order);
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncNavigationTypeIsReplace()
		{
			var nav = new NavigationPage();
			var original = new ContentPage { Header = "Original" };
			await nav.PushAsync(original);

			var replacement = new ContentPage { Header = "Replacement" };
			NavigationType? arrivedType = null;
			NavigationType? departedType = null;
			replacement.NavigatedTo += (_, e) => arrivedType = e.NavigationType;
			original.NavigatedFrom += (_, e) => departedType = e.NavigationType;

			await nav.ReplaceAsync(replacement);

			CornerstoneTest.AreEqual(NavigationType.Replace, arrivedType);
			CornerstoneTest.AreEqual(NavigationType.Replace, departedType);
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncNullPageThrows()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage());
			await Assert.ThrowsAsync<ArgumentNullException>(() => nav.ReplaceAsync(null!));
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncOnEmptyStackPushesPage()
		{
			var nav = new NavigationPage();
			var page = new ContentPage { Header = "Replaced" };

			await nav.ReplaceAsync(page);

			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(page, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncReplacesTopPageStackDepthUnchanged()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			await nav.PushAsync(root);

			var replacement = new ContentPage { Header = "Replacement" };
			await nav.ReplaceAsync(replacement);

			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(replacement, nav.CurrentPage);
			CornerstoneTest.DoesNotContain(nav.NavigationStack, root);
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncWhenCancelledDoesNotReplace()
		{
			var nav = new NavigationPage();
			var original = new ContentPage { Header = "Original" };
			await nav.PushAsync(original);

			original.Navigating += args =>
			{
				args.Cancel = true;
				return Task.CompletedTask;
			};

			var replacement = new ContentPage { Header = "Replacement" };
			await nav.ReplaceAsync(replacement);

			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(original, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncWhenReplacementAlreadyExistsInModalStackThrows()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage { Header = "Root" });
			await nav.PushAsync(new ContentPage { Header = "Top" });

			var modal = new ContentPage { Header = "Modal" };
			await nav.PushModalAsync(modal);

			await Assert.ThrowsAsync<InvalidOperationException>(() => nav.ReplaceAsync(modal));
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncWhenReplacementAlreadyExistsInNavigationStackThrows()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			await Assert.ThrowsAsync<InvalidOperationException>(() => nav.ReplaceAsync(root));
		}

		[PresentationTestMethod]
		public async Task ReplaceAsyncWithCurrentPageIsNoOp()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var top = new ContentPage { Header = "Top" };
			await nav.PushAsync(root);
			await nav.PushAsync(top);

			var lifecycleEvents = 0;
			top.NavigatedFrom += (_, _) => lifecycleEvents++;
			top.NavigatedTo += (_, _) => lifecycleEvents++;

			await nav.ReplaceAsync(top);

			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.Same(top, nav.CurrentPage);
			CornerstoneTest.AreEqual(0, lifecycleEvents);
		}

		#endregion
	}

	[TestClass]
	public class SwipeGestureTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task HandledPointerPressedAtEdgeAllowsSwipePop()
		{
			var nav = new NavigationPage();
			var rootPage = new ContentPage { Header = "Root" };
			var topPage = new ContentPage { Header = "Top" };

			await nav.PushAsync(rootPage);
			await nav.PushAsync(topPage);

			var root = new TestRoot { Child = nav };
			root.ExecuteInitialLayoutPass();

			RaiseHandledPointerPressed(nav, new Point(5, 5));

			var swipe = new SwipeGestureEventArgs(1, new Vector(-20, 0), default);
			nav.RaiseEvent(swipe);
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsTrue(swipe.Handled);
			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(rootPage, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task MouseEdgeDragAllowsSwipePop()
		{
			var nav = new NavigationPage
			{
				Width = 400,
				Height = 300
			};
			nav.GestureRecognizers.OfType<SwipeGestureRecognizer>().First().IsMouseEnabled = true;
			var rootPage = new ContentPage { Header = "Root" };
			var topPage = new ContentPage { Header = "Top" };

			await nav.PushAsync(rootPage);
			await nav.PushAsync(topPage);

			var root = new TestRoot
			{
				ClientSize = new Size(400, 300),
				Child = nav
			};
			root.ExecuteInitialLayoutPass();

			var mouse = new MouseTestHelper();
			mouse.Down(nav, position: new Point(5, 5));
			mouse.Move(nav, new Point(40, 5));
			mouse.Up(nav, position: new Point(40, 5));
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(rootPage, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task SameGestureIdOnlyPopsOnePage()
		{
			var nav = new NavigationPage
			{
				Width = 400,
				Height = 300
			};
			var page1 = new ContentPage { Header = "1" };
			var page2 = new ContentPage { Header = "2" };
			var page3 = new ContentPage { Header = "3" };

			await nav.PushAsync(page1);
			await nav.PushAsync(page2);
			await nav.PushAsync(page3);

			var root = new TestRoot
			{
				ClientSize = new Size(400, 300),
				Child = nav
			};
			root.ExecuteInitialLayoutPass();

			RaiseHandledPointerPressed(nav, new Point(5, 5));

			nav.RaiseEvent(new SwipeGestureEventArgs(42, new Vector(-20, 0), default));
			nav.RaiseEvent(new SwipeGestureEventArgs(42, new Vector(-30, 0), default));
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.Same(page2, nav.CurrentPage);
		}

		private static void RaiseHandledPointerPressed(Interactive target, Point position)
		{
			var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
			var args = new PointerPressedEventArgs(
				target,
				pointer,
				target,
				position,
				1,
				new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
				KeyModifiers.None)
			{
				Handled = true
			};

			target.RaiseEvent(args);
		}

		#endregion
	}

	[TestClass]
	public class SystemBackButtonTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task BackButtonForwardsToTopModalBeforeAutoPop()
		{
			var nav = new NavigationPage();
			await nav.PushAsync(new ContentPage { Header = "Root" });

			var modal = new BackHandlingPage { HandleBack = true };
			await nav.PushModalAsync(modal);

			var args = RaiseBackButton(nav);

			CornerstoneTest.IsTrue(args.Handled);
			CornerstoneTest.AreEqual(1, modal.BackButtonPressCount);
			CornerstoneTest.Single(nav.ModalStack);
			CornerstoneTest.Same(modal, nav.ModalStack[0]);
		}

		[PresentationTestMethod]
		public async Task BackButtonWithHandledModalDoesNotForwardToCoveredCurrentPage()
		{
			var nav = new NavigationPage();
			var root = new BackHandlingPage { HandleBack = true };
			var modal = new BackHandlingPage { HandleBack = true };
			await nav.PushAsync(root);
			await nav.PushModalAsync(modal);

			var args = RaiseBackButton(nav);

			CornerstoneTest.IsTrue(args.Handled);
			CornerstoneTest.AreEqual(0, root.BackButtonPressCount);
			CornerstoneTest.AreEqual(1, modal.BackButtonPressCount);
			CornerstoneTest.Single(nav.ModalStack);
			CornerstoneTest.Same(modal, nav.ModalStack[0]);
		}

		[PresentationTestMethod]
		public async Task BackButtonWithModalAndDeepStackPopsModalBeforeStack()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var detail = new ContentPage { Header = "Detail" };
			var modal = new ContentPage { Header = "Modal" };
			await nav.PushAsync(root);
			await nav.PushAsync(detail);
			await nav.PushModalAsync(modal);

			var args = RaiseBackButton(nav);

			CornerstoneTest.IsTrue(args.Handled);
			CornerstoneTest.Empty(nav.ModalStack);
			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.Same(detail, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task BackButtonWithModalDoesNotForwardToCoveredCurrentPage()
		{
			var nav = new NavigationPage();
			var root = new BackHandlingPage { HandleBack = true };
			var modal = new BackHandlingPage();
			await nav.PushAsync(root);
			await nav.PushModalAsync(modal);

			var args = RaiseBackButton(nav);

			CornerstoneTest.IsTrue(args.Handled);
			CornerstoneTest.AreEqual(0, root.BackButtonPressCount);
			CornerstoneTest.AreEqual(1, modal.BackButtonPressCount);
			CornerstoneTest.Empty(nav.ModalStack);
			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(root, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task BackButtonWithModalOnRootPopsModal()
		{
			var nav = new NavigationPage();
			var root = new ContentPage { Header = "Root" };
			var modal = new ContentPage { Header = "Modal" };
			await nav.PushAsync(root);
			await nav.PushModalAsync(modal);

			var args = RaiseBackButton(nav);

			CornerstoneTest.IsTrue(args.Handled);
			CornerstoneTest.Empty(nav.ModalStack);
			CornerstoneTest.AreEqual(1, nav.StackDepth);
			CornerstoneTest.Same(root, nav.CurrentPage);
		}

		private static RoutedEventArgs RaiseBackButton(NavigationPage nav)
		{
			var args = new RoutedEventArgs(Page.PageNavigationSystemBackButtonPressedEvent);
			nav.RaiseEvent(args);
			return args;
		}

		#endregion

		#region Classes

		private sealed class BackHandlingPage : ContentPage
		{
			#region Properties

			public int BackButtonPressCount { get; private set; }

			public bool HandleBack { get; set; }

			#endregion

			#region Methods

			protected override bool OnSystemBackButtonPressed()
			{
				BackButtonPressCount++;
				return HandleBack;
			}

			#endregion
		}

		#endregion
	}

	[TestClass]
	public class TransitionCancellationTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task PopAsyncWithTransitionWhenCancelledStackUnchangedAfterCancel()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);
			var top = new ContentPage();
			await nav.PushAsync(top);

			top.Navigating += args =>
			{
				args.Cancel = true;
				return Task.CompletedTask;
			};

			await nav.PopAsync(new CrossFade(TimeSpan.FromMilliseconds(100)));

			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.Same(top, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PushAsyncWithTransitionWhenCancelledStackUnchangedAfterCancel()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			await nav.PushAsync(root);

			// Cancel the first navigation
			var shouldCancel = true;
			root.Navigating += args =>
			{
				if (shouldCancel)
				{
					args.Cancel = true;
				}
				return Task.CompletedTask;
			};

			var customTransition = new CrossFade(TimeSpan.FromMilliseconds(100));
			await nav.PushAsync(new ContentPage(), customTransition);

			CornerstoneTest.AreEqual(1, nav.StackDepth);

			// Stop cancelling and push again: override should not leak
			shouldCancel = false;
			var second = new ContentPage();
			await nav.PushAsync(second);
			CornerstoneTest.AreEqual(2, nav.StackDepth);
			CornerstoneTest.Same(second, nav.CurrentPage);
		}

		#endregion
	}

	[TestClass]
	public class VisualTreeLifecycleTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task DetachAndReattachPreservesModalStack()
		{
			var nav = new NavigationPage { Template = CreateNavigationPageTemplate() };

			var root = new TestRoot { Child = nav };
			root.LayoutManager.ExecuteInitialLayoutPass();

			var page = new ContentPage { Header = "Root" };
			var modal = new ContentPage { Header = "Modal" };

			await nav.PushAsync(page);
			await nav.PushModalAsync(modal);

			root.Child = null;

			CornerstoneTest.Single(nav.ModalStack);
			CornerstoneTest.Same(modal, nav.ModalStack[0]);
			CornerstoneTest.IsNull(modal.Navigation);
			CornerstoneTest.IsFalse(modal.IsInNavigationPage);

			root.Child = nav;
			root.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.Single(nav.ModalStack);
			CornerstoneTest.Same(modal, nav.ModalStack[0]);
			CornerstoneTest.Same(nav, modal.Navigation);
			CornerstoneTest.IsTrue(modal.IsInNavigationPage);
		}

		[PresentationTestMethod]
		public async Task PushGenericPushesPageWithCorrectType()
		{
			var nav = new NavigationPage();
			await nav.PushAsync<ContentPage>();

			CornerstoneTest.IsType<ContentPage>(nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task PushModalGenericPushesPageWithCorrectType()
		{
			var nav = new NavigationPage();
			await nav.PushModalAsync<ContentPage>();

			CornerstoneTest.IsType<ContentPage>(nav.ModalContent);
		}

		[PresentationTestMethod]
		public async Task PushModalPassesParameterWithEventArgs()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var param = new object();
			root.NavigatedTo += (s, e) => { CornerstoneTest.Same(e.Parameter, param); };
			await nav.PushModalAsync(root, null, param);
		}

		[PresentationTestMethod]
		public async Task PushPassesParameterWithEventArgs()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var param = new object();
			root.NavigatedTo += (s, e) => { CornerstoneTest.Same(e.Parameter, param); };
			await nav.PushAsync(root, null, param);
		}

		[PresentationTestMethod]
		public async Task ReplaceGenericReplacePageWithCorrectType()
		{
			var nav = new NavigationPage();
			await nav.ReplaceAsync<ContentPage>();

			CornerstoneTest.IsType<ContentPage>(nav.CurrentPage);
		}

		[PresentationTestMethod]
		public async Task ReplacePassesParameterWithEventArgs()
		{
			var nav = new NavigationPage();
			var root = new ContentPage();
			var param = new object();
			root.NavigatedTo += (s, e) => { CornerstoneTest.Same(e.Parameter, param); };
			await nav.ReplaceAsync(root, null, param);
		}

		#endregion
	}

	private sealed class ControllableTransition(Task gate) : IPageTransition
	{
		#region Methods

		public async Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
		{
			to?.IsVisible = true;
			await gate;
			from?.IsVisible = false;
		}

		#endregion
	}

	#endregion
}