#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class VirtualizingCarouselPanelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingSelectedIndexRepositionsFractionalViewport()
	{
		using var app = Start();
		var items = new[] { "foo", "bar", "baz" };
		var (target, carousel) = CreateTarget(items, viewportFraction: 0.8, clientSize: new Size(400, 300));

		carousel.SelectedIndex = 1;
		Layout(target);

		var realized = target.GetRealizedContainers()!
			.OfType<ContentPresenter>()
			.ToDictionary(x => (string) x.Content!);

		CornerstoneTest.AreEqual(40d, realized["bar"].Bounds.X, 6);
		CornerstoneTest.AreEqual(-280d, realized["foo"].Bounds.X, 6);
	}

	[PresentationTestMethod]
	public void ChangingViewportFractionDoesNotChangeSelectedItem()
	{
		using var app = Start();
		var items = new[] { "foo", "bar", "baz" };
		var (target, carousel) = CreateTarget(items, viewportFraction: 0.72, clientSize: new Size(400, 300));

		carousel.WrapSelection = true;
		carousel.SelectedIndex = 2;
		Layout(target);

		carousel.ViewportFraction = 1d;
		Layout(target);

		var visible = target.Children
			.OfType<ContentPresenter>()
			.Where(x => x.IsVisible)
			.ToList();

		CornerstoneTest.Single(visible);
		CornerstoneTest.AreEqual("baz", visible[0].Content);
		CornerstoneTest.AreEqual(2, carousel.SelectedIndex);
	}

	[PresentationTestMethod]
	public void DisplaysNextItem()
	{
		using var app = Start();
		var items = new[] { "foo", "bar" };
		var (target, carousel) = CreateTarget(items);

		carousel.SelectedIndex = 1;
		Layout(target);

		CornerstoneTest.Single(target.Children);
		var container = CornerstoneTest.IsType<ContentPresenter>(target.Children[0]);
		CornerstoneTest.AreEqual("bar", container.Content);
	}

	[PresentationTestMethod]
	public void HandlesInsertedItem()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar" };
		var (target, carousel) = CreateTarget(items);
		var container = CornerstoneTest.IsType<ContentPresenter>(target.Children[0]);

		items.Insert(0, "baz");
		Layout(target);

		CornerstoneTest.Single(target.Children);
		CornerstoneTest.Same(container, target.Children[0]);
		CornerstoneTest.AreEqual("foo", container.Content);
	}

	[PresentationTestMethod]
	public void HandlesMovedItem()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar" };
		var (target, carousel) = CreateTarget(items);
		var container = CornerstoneTest.IsType<ContentPresenter>(target.Children[0]);

		items.Move(0, 1);
		Layout(target);

		CornerstoneTest.Single(target.Children);
		CornerstoneTest.Same(container, target.Children[0]);
		CornerstoneTest.AreEqual("bar", container.Content);
	}

	[PresentationTestMethod]
	public void HandlesMovedItemRange()
	{
		using var app = Start();
		OldPresentationList<string> items = ["foo", "bar", "baz", "qux", "quux"];
		var (target, carousel) = CreateTarget(items);
		var container = CornerstoneTest.IsType<ContentPresenter>(target.Children[0]);

		carousel.SelectedIndex = 3;
		Layout(target);
		items.MoveRange(0, 2, 4);
		Layout(target);

		CornerstoneTest.Multiple(() =>
		{
			CornerstoneTest.Single(target.Children);
			CornerstoneTest.Same(container, target.Children[0]);
			CornerstoneTest.AreEqual("qux", container.Content);
			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
		});
	}

	[PresentationTestMethod]
	public void HandlesRemovedItem()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar" };
		var (target, carousel) = CreateTarget(items);
		var container = CornerstoneTest.IsType<ContentPresenter>(target.Children[0]);

		items.RemoveAt(0);
		Layout(target);

		CornerstoneTest.Single(target.Children);
		CornerstoneTest.Same(container, target.Children[0]);
		CornerstoneTest.AreEqual("bar", container.Content);
	}

	[PresentationTestMethod]
	public void HandlesReplacedItem()
	{
		using var app = Start();
		var items = new ObservableCollection<string> { "foo", "bar" };
		var (target, carousel) = CreateTarget(items);
		var container = CornerstoneTest.IsType<ContentPresenter>(target.Children[0]);

		items[0] = "baz";
		Layout(target);

		CornerstoneTest.Single(target.Children);
		CornerstoneTest.Same(container, target.Children[0]);
		CornerstoneTest.AreEqual("baz", container.Content);
	}

	[PresentationTestMethod]
	public void InitialItemIsDisplayed()
	{
		using var app = Start();
		var items = new[] { "foo", "bar" };
		var (target, _) = CreateTarget(items);

		CornerstoneTest.Single(target.Children);
		var container = CornerstoneTest.IsType<ContentPresenter>(target.Children[0]);
		CornerstoneTest.AreEqual("foo", container.Content);
	}

	[PresentationTestMethod]
	public void InitialSelectedIndexIsDisplayed()
	{
		using var app = Start();
		var items = new[] { "foo", "bar" };
		var (target, _) = CreateTarget(items, selectedIndex: 1);

		CornerstoneTest.Single(target.Children);
		var container = CornerstoneTest.IsType<ContentPresenter>(target.Children[0]);
		CornerstoneTest.AreEqual("bar", container.Content);
	}

	[PresentationTestMethod]
	public void RefreshingSwipeWiringReusesASingleRecognizer()
	{
		using var app = Start();
		var items = new[] { "foo", "bar" };
		var (target, carousel) = CreateTarget(items);

		carousel.IsSwipeEnabled = true;
		carousel.PageTransition = new PageSlide(TimeSpan.FromMilliseconds(1));
		carousel.IsSwipeEnabled = false;
		carousel.IsSwipeEnabled = true;
		carousel.PageTransition = null;
		carousel.PageTransition = new PageSlide(TimeSpan.FromMilliseconds(1));

		var recognizers = target.GestureRecognizers.OfType<SwipeGestureRecognizer>().ToArray();
		var recognizer = CornerstoneTest.Single(recognizers);
		CornerstoneTest.IsTrue(recognizer.IsEnabled);
	}

	[PresentationTestMethod]
	public void ViewportFractionCentersSelectedItemAndPeeksNeighbors()
	{
		using var app = Start();
		var items = new[] { "foo", "bar", "baz" };
		var (target, _) = CreateTarget(items, viewportFraction: 0.8, clientSize: new Size(400, 300));

		var realized = target.GetRealizedContainers()!
			.OfType<ContentPresenter>()
			.ToDictionary(x => (string) x.Content!);

		CornerstoneTest.AreEqual(2, realized.Count);
		CornerstoneTest.AreEqual(40d, realized["foo"].Bounds.X, 6);
		CornerstoneTest.AreEqual(320d, realized["foo"].Bounds.Width, 6);
		CornerstoneTest.AreEqual(360d, realized["bar"].Bounds.X, 6);
	}

	[PresentationTestMethod]
	public void ViewportFractionOneThirdShowsThreeFullItems()
	{
		using var app = Start();
		var items = new[] { "foo", "bar", "baz", "qux" };
		var (target, carousel) = CreateTarget(items, viewportFraction: 1d / 3d, clientSize: new Size(300, 120));

		carousel.SelectedIndex = 1;
		Layout(target);

		var realized = target.GetRealizedContainers()!
			.OfType<ContentPresenter>()
			.ToDictionary(x => (string) x.Content!);

		CornerstoneTest.AreEqual(3, realized.Count);
		CornerstoneTest.AreEqual(0d, realized["foo"].Bounds.X, 6);
		CornerstoneTest.AreEqual(100d, realized["bar"].Bounds.X, 6);
		CornerstoneTest.AreEqual(200d, realized["baz"].Bounds.X, 6);
		CornerstoneTest.AreEqual(100d, realized["bar"].Bounds.Width, 6);
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

	private static (VirtualizingCarouselPanel, Carousel) CreateTarget(
		IEnumerable items,
		IPageTransition transition = null,
		int? selectedIndex = null,
		double viewportFraction = 1d,
		Size? clientSize = null)
	{
		var size = clientSize ?? new Size(400, 300);
		var carousel = new Carousel
		{
			ItemsSource = items,
			Template = CarouselTemplate(),
			PageTransition = transition,
			ViewportFraction = viewportFraction,
			Width = size.Width,
			Height = size.Height
		};

		if (selectedIndex.HasValue)
		{
			carousel.SelectedIndex = selectedIndex.Value;
		}

		var root = new TestRoot(carousel)
		{
			ClientSize = size
		};
		root.LayoutManager.ExecuteInitialLayoutPass();
		return ((VirtualizingCarouselPanel) carousel.Presenter!.Panel!, carousel);
	}

	private static void Layout(Control c)
	{
		c.GetLayoutManager()?.ExecuteLayoutPass();
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

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
	}

	#endregion

	#region Classes

	[TestClass]
	public class Gestures : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void NewSwipeInterruptsActiveCompletionAnimation()
		{
			var clock = new MockGlobalClock();

			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var items = new[] { "foo", "bar", "baz" };
			var transition = new TrackingInteractiveTransition();
			var (panel, carousel) = CreateTarget(items, transition);
			carousel.IsSwipeEnabled = true;

			panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(1000, 0), default));
			panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, new Vector(1000, 0)));

			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromMilliseconds(50));
			sync.ExecutePostedCallbacks();

			CornerstoneTest.AreEqual(0, carousel.SelectedIndex);

			panel.RaiseEvent(new SwipeGestureEventArgs(2, new Vector(10, 0), default));

			CornerstoneTest.IsTrue(carousel.IsSwiping);
			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
		}

		[PresentationTestMethod]
		public void RubberBandSwipeReleaseAnimatesBackThroughIntermediateProgress()
		{
			var clock = new MockGlobalClock();

			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var items = new[] { "foo", "bar" };
			var transition = new ProgressTrackingInteractiveTransition();
			var (panel, carousel) = CreateTarget(items, transition);
			carousel.IsSwipeEnabled = true;
			carousel.WrapSelection = false;

			panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(-100, 0), default));

			var releaseStartProgress = transition.Progresses[^1];
			var updatesBeforeRelease = transition.Progresses.Count;

			panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, default));

			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(0.1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var postReleaseProgresses = transition.Progresses.Skip(updatesBeforeRelease).ToArray();

			CornerstoneTest.Contains(postReleaseProgresses, p => (p > 0) && (p < releaseStartProgress));

			clock.Pulse(TimeSpan.FromSeconds(1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(0d, transition.Progresses[^1]);
			CornerstoneTest.AreEqual(0, carousel.SelectedIndex);
		}

		[PresentationTestMethod]
		public void SwipeCompletionDoesNotUpdateWithSameFromAndTo()
		{
			var clock = new MockGlobalClock();

			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var items = new[] { "foo", "bar" };
			var transition = new TrackingInteractiveTransition();
			var (panel, carousel) = CreateTarget(items, transition);
			carousel.IsSwipeEnabled = true;

			panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(1000, 0), default));
			panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, new Vector(1000, 0)));

			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsTrue(transition.UpdateCallCount > 0);
			CornerstoneTest.IsFalse(transition.SawAliasedUpdate);
			CornerstoneTest.AreEqual(1d, transition.LastProgress);
			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
		}

		[PresentationTestMethod]
		public void SwipeCompletionHidesOutgoingPageBeforeResettingVisualState()
		{
			var clock = new MockGlobalClock();

			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var items = new[] { "foo", "bar" };
			var transition = new OutgoingTransformTrackingInteractiveTransition();
			var (panel, carousel) = CreateTarget(items, transition);
			carousel.IsSwipeEnabled = true;

			var outgoing = CornerstoneTest.Single(panel.Children.OfType<ContentPresenter>(), x => Equals(x.Content, "foo"));
			bool? hiddenWhenReset = null;
			outgoing.PropertyChanged += (_, args) =>
			{
				if ((args.Property == Visual.RenderTransformProperty) &&
					args.GetNewValue<ITransform>() is null)
				{
					hiddenWhenReset = !outgoing.IsVisible;
				}
			};

			panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(1000, 0), default));
			panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, new Vector(1000, 0)));

			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsTrue(hiddenWhenReset);
		}

		[PresentationTestMethod]
		public void SwipeCompletionKeepsTargetFinalInteractiveVisualState()
		{
			var clock = new MockGlobalClock();

			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var items = new[] { "foo", "bar" };
			var transition = new TransformTrackingInteractiveTransition();
			var (panel, carousel) = CreateTarget(items, transition);
			carousel.IsSwipeEnabled = true;

			panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(1000, 0), default));
			panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, new Vector(1000, 0)));

			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
			var realized = CornerstoneTest.Single(panel.Children.OfType<ContentPresenter>(), x => Equals(x.Content, "bar"));
			CornerstoneTest.IsNotNull(transition.LastTargetTransform);
			CornerstoneTest.Same(transition.LastTargetTransform, realized.RenderTransform);
		}

		[PresentationTestMethod]
		public void SwipeWithNonInteractiveTransitionDoesNotCrash()
		{
			using var app = Start();
			var items = new[] { "foo", "bar" };
			var transition = new StubPageTransition();
			var (panel, carousel) = CreateTarget(items, transition);
			carousel.IsSwipeEnabled = true;

			var e = new SwipeGestureEventArgs(1, new Vector(10, 0), default);
			panel.RaiseEvent(e);

			CornerstoneTest.IsTrue(carousel.IsSwiping);
			CornerstoneTest.AreEqual(2, panel.Children.Count);
		}

		[PresentationTestMethod]
		public void SwipingBackwardAtStartRubberBandsWhenWrapSelectionFalse()
		{
			using var app = Start();
			var items = new[] { "foo", "bar" };
			var (panel, carousel) = CreateTarget(items);
			carousel.IsSwipeEnabled = true;
			carousel.WrapSelection = false;

			var e = new SwipeGestureEventArgs(1, new Vector(-10, 0), default);
			panel.RaiseEvent(e);

			CornerstoneTest.IsTrue(carousel.IsSwiping);
			CornerstoneTest.Single(panel.Children);
		}

		[PresentationTestMethod]
		public void SwipingBackwardAtStartWrapsWhenWrapSelectionTrue()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var (panel, carousel) = CreateTarget(items);
			carousel.IsSwipeEnabled = true;
			carousel.WrapSelection = true;

			var e = new SwipeGestureEventArgs(1, new Vector(-10, 0), default);
			panel.RaiseEvent(e);

			CornerstoneTest.IsTrue(carousel.IsSwiping);
			CornerstoneTest.AreEqual(2, panel.Children.Count);
			var target = panel.Children[1];
			CornerstoneTest.AreEqual("baz", (target as ContentPresenter)?.Content);
		}

		[PresentationTestMethod]
		public void SwipingForwardAtEndRubberBandsWhenWrapSelectionFalse()
		{
			using var app = Start();
			var items = new[] { "foo", "bar" };
			var (panel, carousel) = CreateTarget(items);
			carousel.IsSwipeEnabled = true;
			carousel.WrapSelection = false;
			carousel.SelectedIndex = 1;

			Layout(panel);
			Layout(panel);

			CornerstoneTest.AreEqual(2, ((IReadOnlyList<string>) carousel.ItemsSource)?.Count);
			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
			CornerstoneTest.IsFalse(carousel.WrapSelection, "WrapSelection should be false");

			var container = CornerstoneTest.IsType<ContentPresenter>(panel.Children[0]);
			CornerstoneTest.AreEqual("bar", container.Content);

			var e = new SwipeGestureEventArgs(1, new Vector(10, 0), default);
			panel.RaiseEvent(e);

			CornerstoneTest.IsTrue(carousel.IsSwiping);
			CornerstoneTest.Single(panel.Children);
		}

		[PresentationTestMethod]
		public void SwipingForwardRealizesNextItem()
		{
			using var app = Start();
			var items = new[] { "foo", "bar" };
			var (panel, carousel) = CreateTarget(items);
			carousel.IsSwipeEnabled = true;

			var e = new SwipeGestureEventArgs(1, new Vector(10, 0), default);
			panel.RaiseEvent(e);

			CornerstoneTest.IsTrue(carousel.IsSwiping);
			CornerstoneTest.AreEqual(2, panel.Children.Count);
			var target = panel.Children[1];
			CornerstoneTest.IsNotNull(target);
			CornerstoneTest.IsTrue(target.IsVisible);
			CornerstoneTest.AreEqual("bar", (target as ContentPresenter)?.Content);
		}

		[PresentationTestMethod]
		public void SwipingLocksToDominantAxis()
		{
			using var app = Start();
			var items = new[] { "foo", "bar" };
			var (panel, carousel) = CreateTarget(items, new CrossFade(TimeSpan.FromSeconds(1)));
			carousel.IsSwipeEnabled = true;

			var e = new SwipeGestureEventArgs(1, new Vector(10, 2), default);
			panel.RaiseEvent(e);

			CornerstoneTest.IsTrue(carousel.IsSwiping);
		}

		[PresentationTestMethod]
		public void VerticalSwipeForwardRealizesNextItem()
		{
			using var app = Start();
			var items = new[] { "foo", "bar" };
			var transition = new PageSlide(TimeSpan.FromSeconds(1), PageSlide.SlideAxis.Vertical);
			var (panel, carousel) = CreateTarget(items, transition);
			carousel.IsSwipeEnabled = true;

			var e = new SwipeGestureEventArgs(1, new Vector(0, 10), default);
			panel.RaiseEvent(e);

			CornerstoneTest.IsTrue(carousel.IsSwiping);
			CornerstoneTest.AreEqual(2, panel.Children.Count);
			var target = panel.Children[1] as ContentPresenter;
			CornerstoneTest.IsNotNull(target);
			CornerstoneTest.AreEqual("bar", target.Content);
		}

		[PresentationTestMethod]
		public void ViewportFractionSelectedIndexChangeDrivesProgressUpdates()
		{
			var clock = new MockGlobalClock();

			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var items = new[] { "foo", "bar", "baz" };
			var transition = new ProgressTrackingInteractiveTransition();
			var (panel, carousel) = CreateTarget(items, transition, viewportFraction: 0.8);

			carousel.SelectedIndex = 1;

			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(0.1));
			clock.Pulse(TimeSpan.FromSeconds(1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.NotEmpty(transition.Progresses);
			CornerstoneTest.Contains(transition.Progresses, p => (p > 0) && (p < 1));
			CornerstoneTest.AreEqual(1d, transition.Progresses[^1]);
			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
		}

		[PresentationTestMethod]
		public void ViewportFractionSwipingBackwardAtStartWrapsWhenWrapSelectionTrue()
		{
			var clock = new MockGlobalClock();

			using var app = UnitTestApplication.Start(
				TestServices.MockPlatformRenderInterface.With(globalClock: clock));
			using var sync = UnitTestSynchronizationContext.Begin();

			var items = new[] { "foo", "bar", "baz" };
			var (panel, carousel) = CreateTarget(items, viewportFraction: 0.8);
			carousel.IsSwipeEnabled = true;
			carousel.WrapSelection = true;
			Layout(panel);

			panel.RaiseEvent(new SwipeGestureEventArgs(1, new Vector(-120, 0), default));

			CornerstoneTest.IsTrue(carousel.IsSwiping);
			CornerstoneTest.Contains(panel.Children.OfType<ContentPresenter>(), x => Equals(x.Content, "baz"));

			panel.RaiseEvent(new SwipeGestureEndedEventArgs(1, default));

			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(1));
			sync.ExecutePostedCallbacks();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(2, carousel.SelectedIndex);
		}

		#endregion

		#region Classes

		private sealed class OutgoingTransformTrackingInteractiveTransition : IProgressPageTransition
		{
			#region Methods

			public void Reset(Visual visual)
			{
				visual.RenderTransform = null;
			}

			public Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
			{
				return Task.CompletedTask;
			}

			public void Update(
				double progress,
				Visual from,
				Visual to,
				bool forward,
				double pageLength,
				IReadOnlyList<PageTransitionItem> visibleItems)
			{
				if (from is Control source)
				{
					source.RenderTransform = new TranslateTransform(100 * progress, 0);
				}

				if (to is Control target)
				{
					target.RenderTransform = new TranslateTransform(100 * (1 - progress), 0);
				}
			}

			#endregion
		}

		private sealed class ProgressTrackingInteractiveTransition : IProgressPageTransition
		{
			#region Properties

			public List<double> Progresses { get; } = new();

			#endregion

			#region Methods

			public void Reset(Visual visual)
			{
				visual.RenderTransform = null;
				visual.Opacity = 1;
				visual.ZIndex = 0;
				visual.Clip = null;
			}

			public Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
			{
				return Task.CompletedTask;
			}

			public void Update(
				double progress,
				Visual from,
				Visual to,
				bool forward,
				double pageLength,
				IReadOnlyList<PageTransitionItem> visibleItems)
			{
				Progresses.Add(progress);
			}

			#endregion
		}

		private sealed class TrackingInteractiveTransition : IProgressPageTransition
		{
			#region Properties

			public double LastProgress { get; private set; }
			public bool SawAliasedUpdate { get; private set; }
			public int UpdateCallCount { get; private set; }

			#endregion

			#region Methods

			public void Reset(Visual visual)
			{
				visual.RenderTransform = null;
				visual.Opacity = 1;
				visual.ZIndex = 0;
				visual.Clip = null;
			}

			public Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
			{
				return Task.CompletedTask;
			}

			public void Update(
				double progress,
				Visual from,
				Visual to,
				bool forward,
				double pageLength,
				IReadOnlyList<PageTransitionItem> visibleItems)
			{
				UpdateCallCount++;
				LastProgress = progress;

				if (from is not null && ReferenceEquals(from, to))
				{
					SawAliasedUpdate = true;
				}
			}

			#endregion
		}

		private sealed class TransformTrackingInteractiveTransition : IProgressPageTransition
		{
			#region Properties

			public TransformGroup LastTargetTransform { get; private set; }

			#endregion

			#region Methods

			public void Reset(Visual visual)
			{
				visual.RenderTransform = null;
			}

			public Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
			{
				return Task.CompletedTask;
			}

			public void Update(
				double progress,
				Visual from,
				Visual to,
				bool forward,
				double pageLength,
				IReadOnlyList<PageTransitionItem> visibleItems)
			{
				if (to is not Control target)
				{
					return;
				}

				if (target.RenderTransform is not TransformGroup group)
				{
					group = new TransformGroup
					{
						Children =
						{
							new ScaleTransform(),
							new TranslateTransform()
						}
					};
					target.RenderTransform = group;
				}

				var scale = CornerstoneTest.IsType<ScaleTransform>(group.Children[0]);
				var translate = CornerstoneTest.IsType<TranslateTransform>(group.Children[1]);
				scale.ScaleX = scale.ScaleY = 0.9 + (0.1 * progress);
				translate.X = 100 * (1 - progress);
				LastTargetTransform = group;
			}

			#endregion
		}

		#endregion
	}

	[TestClass]
	public class Transitions : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ChangingSelectedIndexFromFirstToLastTransitionsForward()
		{
			using var app = Start();
			Dispatcher.UIThread.Invoke(() => // This sets up a proper sync context
			{
				var items = new Control[] { new Button(), new Canvas(), new Label() };
				var transition = new StubPageTransition();
				var (target, carousel) = CreateTarget(items, transition);

				carousel.SelectedIndex = 2;
				Layout(target);

				Dispatcher.UIThread.RunJobs();

				transition.Calls.VerifyCalled("Start", 1);
				transition.Calls.VerifyLastPrefix("Start", items[0], items[2], true);
			});
		}

		[PresentationTestMethod]
		public void ChangingSelectedIndexFromLastToFirstTransitionsBackward()
		{
			using var app = Start();
			Dispatcher.UIThread.Invoke(() => // This sets up a proper sync context
			{
				var items = new Control[] { new Button(), new Canvas(), new Label() };
				var transition = new StubPageTransition();
				var (target, carousel) = CreateTarget(items, transition);

				carousel.SelectedIndex = 2;
				Layout(target);
				Dispatcher.UIThread.RunJobs();

				carousel.SelectedIndex = 0;
				Layout(target);
				Dispatcher.UIThread.RunJobs();

				transition.Calls.VerifyLastPrefix("Start", items[2], items[0], false);
			});
		}

		[PresentationTestMethod]
		public void ChangingSelectedIndexStartsTransition()
		{
			using var app = Start();
			var items = new Control[] { new Button(), new Canvas() };
			var transition = new StubPageTransition();
			var (target, carousel) = CreateTarget(items, transition);

			carousel.SelectedIndex = 1;
			Layout(target);

			transition.Calls.VerifyCalled("Start", 1);
			transition.Calls.VerifyLastPrefix("Start", items[0], items[1], true);
		}

		[PresentationTestMethod]
		public void CompletedTransitionIsFlushedBeforeStartingNextTransition()
		{
			using var app = Start();
			using var sync = UnitTestSynchronizationContext.Begin();
			var items = new Control[] { new Button(), new Canvas(), new Label() };
			var transition = new StubPageTransition();

			var (target, carousel) = CreateTarget(items, transition);

			carousel.SelectedIndex = 1;
			Layout(target);

			carousel.SelectedIndex = 2;
			Layout(target);

			CornerstoneTest.AreEqual(2, transition.Calls.Count("Start"));
			var starts = transition.Calls.Arguments("Start");
			CornerstoneTest.Same(items[0], starts[0][0]);
			CornerstoneTest.Same(items[1], starts[0][1]);
			CornerstoneTest.AreEqual(true, starts[0][2]);
			CornerstoneTest.Same(items[1], starts[1][0]);
			CornerstoneTest.Same(items[2], starts[1][1]);
			CornerstoneTest.AreEqual(true, starts[1][2]);

			sync.ExecutePostedCallbacks();
		}

		[PresentationTestMethod]
		public void ExistingTransitionIsCanceledIfInterrupted()
		{
			using var app = Start();
			using var sync = UnitTestSynchronizationContext.Begin();
			var items = new Control[] { new Button(), new Canvas() };
			var transition = new StubPageTransition();
			var (target, carousel) = CreateTarget(items, transition);
			var transitionTask = new TaskCompletionSource();
			CancellationToken? cancelationToken = null;

			transition.StartHandler = (_, _, _, c) =>
			{
				if (cancelationToken == null)
				{
					cancelationToken = c;
				}

				return transitionTask.Task;
			};

			carousel.SelectedIndex = 1;
			Layout(target);

			CornerstoneTest.IsNotNull(cancelationToken);
			CornerstoneTest.IsFalse(cancelationToken!.Value.IsCancellationRequested);

			carousel.SelectedIndex = 0;
			Layout(target);

			CornerstoneTest.IsTrue(cancelationToken!.Value.IsCancellationRequested);
		}

		[PresentationTestMethod]
		public void InitialItemDoesNotStartTransition()
		{
			using var app = Start();
			var items = new Control[] { new Button(), new Canvas() };
			var transition = new StubPageTransition();
			var (target, _) = CreateTarget(items, transition);

			transition.Calls.VerifyNotCalled("Start");
		}

		[PresentationTestMethod]
		public void InterruptedTransitionResetsCurrentPageBeforeStartingNextTransition()
		{
			using var app = Start();
			var items = new Control[] { new Button(), new Canvas(), new Label() };
			var transition = new DirtyStateTransition();
			var (target, carousel) = CreateTarget(items, transition);

			carousel.SelectedIndex = 1;
			Layout(target);

			carousel.SelectedIndex = 2;
			Layout(target);

			CornerstoneTest.AreEqual(2, transition.Starts.Count);
			CornerstoneTest.AreEqual(1d, transition.Starts[1].FromOpacity);
			CornerstoneTest.IsNull(transition.Starts[1].FromTransform);
		}

		[PresentationTestMethod]
		public void TransitionFromControlIsRecycledWhenTransitionCompletes()
		{
			using var app = Start();
			using var sync = UnitTestSynchronizationContext.Begin();
			var items = new Control[] { new Button(), new Canvas() };
			var transition = new StubPageTransition();
			var (target, carousel) = CreateTarget(items, transition);
			var transitionTask = new TaskCompletionSource();

			transition.StartResult = transitionTask.Task;

			carousel.SelectedIndex = 1;
			Layout(target);

			CornerstoneTest.AreEqual(items, target.Children);
			CornerstoneTest.All(items, x => CornerstoneTest.IsTrue(x.IsVisible));

			transitionTask.SetResult();
			sync.ExecutePostedCallbacks();

			CornerstoneTest.AreEqual(items, target.Children);
			CornerstoneTest.IsFalse(items[0].IsVisible);
			CornerstoneTest.IsTrue(items[1].IsVisible);
		}

		#endregion
	}

	[TestClass]
	public class WrapSelectionTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void NextDoesNotWrapWhenWrapSelectionDisabled()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var (target, carousel) = CreateTarget(items);

			carousel.WrapSelection = false;
			carousel.SelectedIndex = 2; // Last item
			Layout(target);

			carousel.Next();
			Layout(target);

			CornerstoneTest.AreEqual(2, carousel.SelectedIndex); // Should stay at last item
		}

		[PresentationTestMethod]
		public void NextWrapsToFirstItemWhenWrapSelectionEnabled()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var (target, carousel) = CreateTarget(items);

			carousel.WrapSelection = true;
			carousel.SelectedIndex = 2; // Last item
			Layout(target);

			carousel.Next();
			Layout(target);

			CornerstoneTest.AreEqual(0, carousel.SelectedIndex);
		}

		[PresentationTestMethod]
		public void PreviousDoesNotWrapWhenWrapSelectionDisabled()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var (target, carousel) = CreateTarget(items);

			carousel.WrapSelection = false;
			carousel.SelectedIndex = 0; // First item
			Layout(target);

			carousel.Previous();
			Layout(target);

			CornerstoneTest.AreEqual(0, carousel.SelectedIndex); // Should stay at first item
		}

		[PresentationTestMethod]
		public void PreviousWrapsToLastItemWhenWrapSelectionEnabled()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var (target, carousel) = CreateTarget(items);

			carousel.WrapSelection = true;
			carousel.SelectedIndex = 0; // First item
			Layout(target);

			carousel.Previous();
			Layout(target);

			CornerstoneTest.AreEqual(2, carousel.SelectedIndex); // Should wrap to last item
		}

		[PresentationTestMethod]
		public void WrapSelectionDoesNotApplyToSingleItem()
		{
			using var app = Start();
			var items = new[] { "foo" };
			var (target, carousel) = CreateTarget(items);

			carousel.WrapSelection = true;
			carousel.SelectedIndex = 0;
			Layout(target);

			carousel.Next();
			Layout(target);

			CornerstoneTest.AreEqual(0, carousel.SelectedIndex);

			carousel.Previous();
			Layout(target);

			CornerstoneTest.AreEqual(0, carousel.SelectedIndex);
		}

		[PresentationTestMethod]
		public void WrapSelectionWorksWithTwoItems()
		{
			using var app = Start();
			var items = new[] { "foo", "bar" };
			var (target, carousel) = CreateTarget(items);

			carousel.WrapSelection = true;
			carousel.SelectedIndex = 1;
			Layout(target);

			carousel.Next();
			Layout(target);

			CornerstoneTest.AreEqual(0, carousel.SelectedIndex);

			carousel.Previous();
			Layout(target);

			CornerstoneTest.AreEqual(1, carousel.SelectedIndex);
		}

		#endregion
	}

	private sealed class DirtyStateTransition : IPageTransition
	{
		#region Properties

		public List<(double FromOpacity, ITransform FromTransform)> Starts { get; } = new();

		#endregion

		#region Methods

		public Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
		{
			Starts.Add((from?.Opacity ?? 1d, from?.RenderTransform));

			if (to is not null)
			{
				to.Opacity = 0.25;
				to.RenderTransform = new TranslateTransform { X = 50 };
			}

			return Task.Delay(Timeout.Infinite, cancellationToken);
		}

		#endregion
	}

	#endregion
}