#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class LayoutableTestsEffectiveViewportChanged : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public async Task EffectiveViewportChangedNotRaisedWhenControlAddedToTreeAndLayoutPassHasNotRun()
	{
		#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		await RunOnUIThread.Execute(async () =>
			#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			var root = CreateRoot();
			var target = new Canvas();
			var raised = 0;

			target.EffectiveViewportChanged += (s, e) => { ++raised; };

			root.Child = target;

			CornerstoneTest.AreEqual(0, raised);
		});
	}

	[PresentationTestMethod]
	public async Task EffectiveViewportChangedRaisedBeforeLayoutUpdated()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas();
			var raised = 0;

			target.EffectiveViewportChanged += (s, e) => { ++raised; };

			root.Child = target;

			await ExecuteInitialLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task EffectiveViewportChangedRaisedWhenControlAddedToTreeAndLayoutPassHasRun()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas();
			var raised = 0;

			target.EffectiveViewportChanged += (s, e) => { ++raised; };

			root.Child = target;

			CornerstoneTest.AreEqual(0, raised);

			await ExecuteInitialLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task EffectiveViewportChangedRaisedWhenRootLayedOutAndThenControlAddedToTreeAndLayoutPassRuns()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas();
			var raised = 0;

			target.EffectiveViewportChanged += (s, e) => { ++raised; };

			await ExecuteInitialLayoutPass(root);

			root.Child = target;

			CornerstoneTest.AreEqual(0, raised);

			await ExecuteInitialLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task EffectiveViewportChangedShouldNotBeRaisedTwiceIfSubcribedInAttachedToVisualTree()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas();
			var raised = 0;

			target.AttachedToVisualTree += (_, _) => { target.EffectiveViewportChanged += (_, _) => ++raised; };

			root.Child = target;

			await ExecuteInitialLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task EventUnsubscribedWhileInsideCallback()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas();
			var raised = 0;

			void OnTargetOnEffectiveViewportChanged(object s, EffectiveViewportChangedEventArgs e)
			{
				target.EffectiveViewportChanged -= OnTargetOnEffectiveViewportChanged;
				++raised;
			}

			target.EffectiveViewportChanged += OnTargetOnEffectiveViewportChanged;

			root.Child = target;

			await ExecuteInitialLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task InvalidatingInHandlerCausesLayoutToBeRerunBeforeLayoutUpdatedRaised()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new TestCanvas();
			var raised = 0;
			var layoutUpdatedRaised = 0;

			root.LayoutUpdated += (s, e) =>
			{
				CornerstoneTest.AreEqual(2, target.MeasureCount);
				CornerstoneTest.AreEqual(2, target.ArrangeCount);
				++layoutUpdatedRaised;
			};

			target.EffectiveViewportChanged += (s, e) =>
			{
				target.InvalidateMeasure();
				++raised;
			};

			root.Child = target;

			await ExecuteInitialLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
			CornerstoneTest.AreEqual(1, layoutUpdatedRaised);
		});
	}

	[PresentationTestMethod]
	public async Task MovingParentUpdatesEffectiveViewport()
	{
		using var scope = PresentationLocator.EnterScope();
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 100, Height = 100 };
			var parent = new Border { Width = 200, Height = 200, Child = target };
			var raised = 0;

			root.Child = parent;

			await ExecuteInitialLayoutPass(root);

			target.EffectiveViewportChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new Rect(-554, -400, 1200, 900), e.EffectiveViewport);
				++raised;
			};

			parent.Margin = new Thickness(8, 0, 0, 0);
			await ExecuteLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task ParentAffectsEffectiveViewport()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 100, Height = 100 };
			var parent = new Border { Width = 200, Height = 200, Child = target };
			var raised = 0;

			root.Child = parent;

			target.EffectiveViewportChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new Rect(-550, -400, 1200, 900), e.EffectiveViewport);
				++raised;
			};

			await ExecuteInitialLayoutPass(root);
		});
	}

	[PresentationTestMethod]
	public async Task RotateTransformOnParentAffectsEffectiveViewport()
	{
		using var scope = PresentationLocator.EnterScope();
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 100, Height = 100 };
			var parent = new Border { Width = 200, Height = 200, Child = target };
			var raised = 0;

			root.Child = parent;

			await ExecuteInitialLayoutPass(root);

			target.EffectiveViewportChanged += (s, e) =>
			{
				AssertArePixelEqual(new Rect(-651, -792, 1484, 1484), e.EffectiveViewport);
				++raised;
			};

			parent.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
			parent.RenderTransform = new RotateTransform { Angle = 45 };
			parent.InvalidateMeasure();
			await ExecuteLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task ScrollViewerDeterminesEffectiveViewport()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 200, Height = 200 };
			var scroller = new ScrollViewer { Width = 100, Height = 100, Content = target, Template = ScrollViewerTemplate(), HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden };
			var raised = 0;

			target.EffectiveViewportChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), e.EffectiveViewport);
				++raised;
			};

			root.Child = scroller;

			await ExecuteInitialLayoutPass(root);
			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task ScrolledScrollViewerDeterminesEffectiveViewport()
	{
		using var scope = PresentationLocator.EnterScope();
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 200, Height = 200 };
			var scroller = new ScrollViewer { Width = 100, Height = 100, Content = target, Template = ScrollViewerTemplate(), HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden };
			var raised = 0;

			root.Child = scroller;

			await ExecuteInitialLayoutPass(root);
			scroller.Offset = new Vector(0, 10);

			await ExecuteScrollerLayoutPass(root, scroller, target, (s, e) =>
			{
				CornerstoneTest.AreEqual(new Rect(0, 10, 100, 100), e.EffectiveViewport);
				++raised;
			});

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task TranslateTransformDoesntAffectEffectiveViewport()
	{
		using var scope = PresentationLocator.EnterScope();
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 100, Height = 100 };
			var parent = new Border { Width = 200, Height = 200, Child = target };
			var raised = 0;

			root.Child = parent;

			target.EffectiveViewportChanged += (s, e) => ++raised;
			await ExecuteInitialLayoutPass(root);

			raised = 0; // The initial layout pass is expected to raise.

			target.RenderTransform = new TranslateTransform { X = 8 };
			target.InvalidateMeasure();
			await ExecuteLayoutPass(root);

			CornerstoneTest.AreEqual(0, raised);
		});
	}

	[PresentationTestMethod]
	public async Task TranslateTransformOnParentAffectsEffectiveViewport()
	{
		using var scope = PresentationLocator.EnterScope();
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 100, Height = 100 };
			var parent = new Border { Width = 200, Height = 200, Child = target };
			var raised = 0;

			root.Child = parent;

			await ExecuteInitialLayoutPass(root);

			target.EffectiveViewportChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new Rect(-558, -400, 1200, 900), e.EffectiveViewport);
				++raised;
			};

			// Change the parent render transform to move it. A layout is then needed before
			// EffectiveViewportChanged is raised.
			parent.RenderTransform = new TranslateTransform { X = 8 };
			parent.InvalidateMeasure();
			await ExecuteLayoutPass(root);

			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task ViewportExtendsBeyondCenteredControl()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 52, Height = 52 };
			var raised = 0;

			target.EffectiveViewportChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
				++raised;
			};

			root.Child = target;

			await ExecuteInitialLayoutPass(root);
			CornerstoneTest.AreEqual(1, raised);
		});
	}

	[PresentationTestMethod]
	public async Task ViewportExtendsBeyondNestedCenteredControl()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var root = CreateRoot();
			var target = new Canvas { Width = 52, Height = 52 };
			var parent = new Border { Width = 100, Height = 100, Child = target };
			var raised = 0;

			target.EffectiveViewportChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(new Rect(-574, -424, 1200, 900), e.EffectiveViewport);
				++raised;
			};

			root.Child = parent;

			await ExecuteInitialLayoutPass(root);
			CornerstoneTest.AreEqual(1, raised);
		});
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/12452
	[PresentationTestMethod]
	public async Task ZeroScaleTransformSetsEmptyEffectiveViewport()
	{
		await RunOnUIThread.Execute(async () =>
		{
			var effectiveViewport = new Rect(Size.Infinity);

			var root = CreateRoot();
			var target = new Canvas { Width = 100, Height = 100 };
			var parent = new Border { Width = 100, Height = 100, Child = target };

			target.EffectiveViewportChanged += (_, e) => effectiveViewport = e.EffectiveViewport;

			root.Child = parent;

			await ExecuteInitialLayoutPass(root);

			parent.RenderTransform = new ScaleTransform(0, 0);

			await ExecuteLayoutPass(root);

			CornerstoneTest.AreEqual(new Rect(0, 0, 0, 0), effectiveViewport);
		});
	}

	private static void AssertArePixelEqual(Rect expected, Rect actual)
	{
		var expectedRounded = new Rect((int) expected.X, (int) expected.Y, (int) expected.Width, (int) expected.Height);
		var actualRounded = new Rect((int) actual.X, (int) actual.Y, (int) actual.Width, (int) actual.Height);
		CornerstoneTest.AreEqual(expectedRounded, actualRounded);
	}

	private static TestRoot CreateRoot()
	{
		return new() { Width = 1200, Height = 900 };
	}

	private static Task ExecuteInitialLayoutPass(TestRoot root)
	{
		root.LayoutManager.ExecuteInitialLayoutPass();
		return Task.CompletedTask;
	}

	private static Task ExecuteLayoutPass(TestRoot root)
	{
		root.LayoutManager.ExecuteLayoutPass();
		return Task.CompletedTask;
	}

	private static Task ExecuteScrollerLayoutPass(
		TestRoot root,
		ScrollViewer scroller,
		Control target,
		Action<object, EffectiveViewportChangedEventArgs> handler)
	{
		void ViewportChanged(object sender, EffectiveViewportChangedEventArgs e)
		{
			handler(sender, e);
		}

		target.EffectiveViewportChanged += ViewportChanged;
		root.LayoutManager.ExecuteLayoutPass();
		return Task.CompletedTask;
	}

	private static IControlTemplate ScrollViewerTemplate()
	{
		return new FuncControlTemplate<ScrollViewer>((control, scope) => new Grid
		{
			ColumnDefinitions = new ColumnDefinitions
			{
				new ColumnDefinition(1, GridUnitType.Star),
				new ColumnDefinition(GridLength.Auto)
			},
			RowDefinitions = new RowDefinitions
			{
				new RowDefinition(1, GridUnitType.Star),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				new ScrollContentPresenter
				{
					Name = "PART_ContentPresenter"
				}.RegisterInNameScope(scope),
				new ScrollBar
				{
					Name = "horizontalScrollBar",
					Orientation = Orientation.Horizontal,
					[Grid.RowProperty] = 1
				}.RegisterInNameScope(scope),
				new ScrollBar
				{
					Name = "verticalScrollBar",
					Orientation = Orientation.Vertical,
					[Grid.ColumnProperty] = 1
				}.RegisterInNameScope(scope)
			}
		});
	}

	#endregion

	#region Classes

	private static class RunOnUIThread
	{
		#region Methods

		public static async Task Execute(Func<Task> func)
		{
			await func();
		}

		#endregion
	}

	private class TestCanvas : Canvas
	{
		#region Properties

		public int ArrangeCount { get; private set; }
		public int MeasureCount { get; private set; }

		#endregion

		#region Methods

		protected override Size ArrangeOverride(Size finalSize)
		{
			++ArrangeCount;
			return base.ArrangeOverride(finalSize);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			++MeasureCount;
			return base.MeasureOverride(availableSize);
		}

		#endregion
	}

	#endregion
}