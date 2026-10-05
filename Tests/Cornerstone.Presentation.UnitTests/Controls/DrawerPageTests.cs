#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Navigation;
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
public class DrawerPageTests
{
	#region Methods

	private static IControlTemplate BackdropOnlyTemplate()
	{
		return new FuncControlTemplate<DrawerPage>((_, scope) =>
			new Canvas
			{
				Children =
				{
					new Border
					{
						Name = "PART_Backdrop"
					}.RegisterInNameScope(scope)
				}
			});
	}

	private static void RaisePointerPressed(Interactive target, Point? position = null)
	{
		var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
		var args = new PointerPressedEventArgs(
			target,
			pointer,
			target,
			position ?? default,
			1,
			new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
			KeyModifiers.None);

		target.RaiseEvent(args);
	}

	#endregion

	#region Classes

	[TestClass]
	public class DetachmentTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public async Task DetachAndReattachRestoresDrawerPageReference()
		{
			var nav = new NavigationPage();
			var dp = new DrawerPage { Content = nav };
			var root = new TestRoot { Child = dp };
			var page = new ContentPage();
			await nav.PushAsync(page);

			CornerstoneTest.IsTrue(nav.IsBackButtonEffectivelyVisible);

			root.Child = null;
			CornerstoneTest.IsFalse(nav.IsBackButtonEffectivelyVisible);

			root.Child = dp;
			CornerstoneTest.IsTrue(nav.IsBackButtonEffectivelyVisible);
		}

		[PresentationTestMethod]
		public async Task OnDetachedClearsDrawerPageReferenceOnNavigationPage()
		{
			var root = new TestRoot();
			var nav = new NavigationPage();
			var dp = new DrawerPage { Content = nav };
			root.Child = dp;

			// Detach: should clear the DrawerPage reference
			root.Child = null;

			// NavigationPage should no longer reference the DrawerPage.
			// Pushing a page should not show a hamburger icon (which requires DrawerPage).
			var page = new ContentPage();
			await nav.PushAsync(page);
			CornerstoneTest.IsNull(NavigationPage.GetBackButtonContent(page));
		}

		#endregion
	}

	[TestClass]
	public class DisabledBehaviorClosingTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ClosingCancelCannotPreventDisabledClose()
		{
			var dp = new DrawerPage { IsOpen = true };
			dp.Closing += (_, e) => e.Cancel = true;

			dp.DrawerBehavior = DrawerBehavior.Disabled;

			CornerstoneTest.IsFalse(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void ClosingNotFiredWhenDisabledForcesClose()
		{
			var dp = new DrawerPage { IsOpen = true };
			var closingFired = false;
			dp.Closing += (_, _) => closingFired = true;

			dp.DrawerBehavior = DrawerBehavior.Disabled;

			CornerstoneTest.IsFalse(closingFired);
		}

		#endregion
	}

	[TestClass]
	public class DisplayModeMappingTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DrawerBehaviorFlyoutOverridesCompactInlineToOverlay()
		{
			var dp = new DrawerPage
			{
				DrawerLayoutBehavior = DrawerLayoutBehavior.CompactInline,
				DrawerBehavior = DrawerBehavior.Flyout
			};
			CornerstoneTest.AreEqual(SplitViewDisplayMode.Overlay, dp.DisplayMode);
		}

		[PresentationTestMethod]
		public void DrawerBehaviorLockedOverridesCompactOverlayToInline()
		{
			var dp = new DrawerPage
			{
				DrawerLayoutBehavior = DrawerLayoutBehavior.CompactOverlay,
				DrawerBehavior = DrawerBehavior.Locked
			};
			CornerstoneTest.AreEqual(SplitViewDisplayMode.Inline, dp.DisplayMode);
		}

		[PresentationTestMethod]
		public void DrawerBreakpointLengthBeforeLayoutDoesNotOverrideLayoutBehavior()
		{
			var dp = new DrawerPage
			{
				DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
				DrawerBreakpointLength = 1200
			};
			CornerstoneTest.AreEqual(SplitViewDisplayMode.Inline, dp.DisplayMode);
		}

		[PresentationTestMethod]
		[DataRow(DrawerPlacement.Left)]
		[DataRow(DrawerPlacement.Right)]
		public void DrawerBreakpointLengthHorizontalAboveBreakpointUsesConfiguredLayout(DrawerPlacement placement)
		{
			var dp = new DrawerPage
			{
				DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
				DrawerBreakpointLength = 600,
				DrawerPlacement = placement
			};
			dp.Measure(new Size(800, 600));
			dp.Arrange(new Rect(0, 0, 800, 600));

			CornerstoneTest.AreEqual(SplitViewDisplayMode.Inline, dp.DisplayMode);
		}

		[PresentationTestMethod]
		[DataRow(DrawerPlacement.Left)]
		[DataRow(DrawerPlacement.Right)]
		public void DrawerBreakpointLengthHorizontalBelowBreakpointForcesOverlay(DrawerPlacement placement)
		{
			var dp = new DrawerPage
			{
				DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
				DrawerBreakpointLength = 1200,
				DrawerPlacement = placement
			};
			dp.Measure(new Size(800, 600));
			dp.Arrange(new Rect(0, 0, 800, 600));

			CornerstoneTest.AreEqual(SplitViewDisplayMode.Overlay, dp.DisplayMode);
		}

		[PresentationTestMethod]
		[DataRow(DrawerPlacement.Top)]
		[DataRow(DrawerPlacement.Bottom)]
		public void DrawerBreakpointLengthVerticalAboveBreakpointUsesConfiguredLayout(DrawerPlacement placement)
		{
			var dp = new DrawerPage
			{
				DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
				DrawerBreakpointLength = 400,
				DrawerPlacement = placement
			};
			dp.Measure(new Size(800, 600));
			dp.Arrange(new Rect(0, 0, 800, 600));

			// Vertical: breakpoint compares against Bounds.Height (600 > 400 → Inline)
			CornerstoneTest.AreEqual(SplitViewDisplayMode.Inline, dp.DisplayMode);
		}

		[PresentationTestMethod]
		[DataRow(DrawerPlacement.Top)]
		[DataRow(DrawerPlacement.Bottom)]
		public void DrawerBreakpointLengthVerticalBelowBreakpointForcesOverlay(DrawerPlacement placement)
		{
			var dp = new DrawerPage
			{
				DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
				DrawerBreakpointLength = 800,
				DrawerPlacement = placement
			};
			dp.Measure(new Size(800, 600));
			dp.Arrange(new Rect(0, 0, 800, 600));

			// Vertical: breakpoint compares against Bounds.Height (600 < 800 → Overlay)
			CornerstoneTest.AreEqual(SplitViewDisplayMode.Overlay, dp.DisplayMode);
		}

		[PresentationTestMethod]
		public void DrawerBreakpointLengthZeroDoesNotOverrideLayoutBehavior()
		{
			// Breakpoint == 0 means the feature is disabled; DrawerLayoutBehavior drives DisplayMode.
			var dp = new DrawerPage
			{
				DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
				DrawerBreakpointLength = 0
			};
			CornerstoneTest.AreEqual(SplitViewDisplayMode.Inline, dp.DisplayMode);
		}

		[PresentationTestMethod]
		public void DrawerLayoutBehaviorCompactInlineMapsToCompactInline()
		{
			var dp = new DrawerPage { DrawerLayoutBehavior = DrawerLayoutBehavior.CompactInline };
			CornerstoneTest.AreEqual(SplitViewDisplayMode.CompactInline, dp.DisplayMode);
		}

		[PresentationTestMethod]
		public void DrawerLayoutBehaviorCompactOverlayMapsToCompactOverlay()
		{
			var dp = new DrawerPage { DrawerLayoutBehavior = DrawerLayoutBehavior.CompactOverlay };
			CornerstoneTest.AreEqual(SplitViewDisplayMode.CompactOverlay, dp.DisplayMode);
		}

		[PresentationTestMethod]
		public void DrawerLayoutBehaviorOverlayMapsToOverlay()
		{
			var dp = new DrawerPage { DrawerLayoutBehavior = DrawerLayoutBehavior.Overlay };
			CornerstoneTest.AreEqual(SplitViewDisplayMode.Overlay, dp.DisplayMode);
		}

		[PresentationTestMethod]
		public void DrawerLayoutBehaviorSplitMapsToInline()
		{
			var dp = new DrawerPage { DrawerLayoutBehavior = DrawerLayoutBehavior.Split };
			CornerstoneTest.AreEqual(SplitViewDisplayMode.Inline, dp.DisplayMode);
		}

		#endregion
	}

	[TestClass]
	public class DrawerEventTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void BackdropPressWithCanceledCloseFiresClosingOnceWhenTemplateWasAppliedBeforeAttach()
		{
			var dp = new DrawerPage
			{
				Template = BackdropOnlyTemplate(),
				IsOpen = true,
				BackdropBrush = Brushes.Black,
				DisplayMode = SplitViewDisplayMode.Overlay
			};

			dp.ApplyTemplate();

			var closingCount = 0;
			dp.Closing += (_, e) =>
			{
				closingCount++;
				e.Cancel = true;
			};

			var root = new TestRoot { Child = dp };
			root.ExecuteInitialLayoutPass();

			var backdrop = CornerstoneTest.Single(dp.GetVisualDescendants().OfType<Border>(), x => x.Name == "PART_Backdrop");

			RaisePointerPressed(backdrop);

			CornerstoneTest.AreEqual(1, closingCount);
			CornerstoneTest.IsTrue(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void ClosingCancelDoesNotFireClosed()
		{
			var dp = new DrawerPage { IsOpen = true };
			dp.Closing += (_, e) => e.Cancel = true;
			var closedFired = false;
			dp.Closed += (_, _) => closedFired = true;

			dp.IsOpen = false;

			CornerstoneTest.IsFalse(closedFired);
		}

		[PresentationTestMethod]
		public void ClosingCancelDoesNotFireOpened()
		{
			var dp = new DrawerPage { IsOpen = true };
			dp.Closing += (_, e) => e.Cancel = true;
			var openedFired = false;
			dp.Opened += (_, _) => openedFired = true;

			dp.IsOpen = false;

			CornerstoneTest.IsFalse(openedFired);
		}

		[PresentationTestMethod]
		public void ClosingCancelPreventsClose()
		{
			var dp = new DrawerPage { IsOpen = true };
			dp.Closing += (_, e) => e.Cancel = true;

			dp.IsOpen = false;

			CornerstoneTest.IsTrue(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void ClosingCancelPreventsCloseEvenWithReentrantIsOpenFalse()
		{
			var dp = new DrawerPage { IsOpen = true };
			dp.Closing += (_, e) => e.Cancel = true;

			dp.PropertyChanged += (_, e) =>
			{
				if (e.Property == DrawerPage.IsOpenProperty)
				{
					dp.SetCurrentValue(DrawerPage.IsOpenProperty, false);
				}
			};

			dp.IsOpen = false;

			CornerstoneTest.IsTrue(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void DrawerBehaviorLockedForcesIsOpenTrue()
		{
			var dp = new DrawerPage { DrawerBehavior = DrawerBehavior.Locked };
			CornerstoneTest.IsTrue(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void DrawerBehaviorLockedWhileClosedOpensWithoutFiringClosing()
		{
			var dp = new DrawerPage();
			var closingFired = false;
			dp.Closing += (_, _) => closingFired = true;

			dp.DrawerBehavior = DrawerBehavior.Locked;

			CornerstoneTest.IsTrue(dp.IsOpen);
			CornerstoneTest.IsFalse(closingFired);
		}

		[PresentationTestMethod]
		public void IsOpenAlreadyFalseSetFalseDoesNotFireClosingOrClosed()
		{
			var dp = new DrawerPage();
			var closingFired = false;
			var closedFired = false;
			dp.Closing += (_, _) => closingFired = true;
			dp.Closed += (_, _) => closedFired = true;

			dp.IsOpen = false;

			CornerstoneTest.IsFalse(closingFired);
			CornerstoneTest.IsFalse(closedFired);
		}

		[PresentationTestMethod]
		public void IsOpenAlreadyTrueSetTrueDoesNotFireOpened()
		{
			var dp = new DrawerPage { IsOpen = true };
			var fired = false;
			dp.Opened += (_, _) => fired = true;

			dp.IsOpen = true;

			CornerstoneTest.IsFalse(fired);
		}

		[PresentationTestMethod]
		public void IsOpenRapidToggleEventsFiredExactlyOncePerChange()
		{
			var dp = new DrawerPage();
			var openedCount = 0;
			var closedCount = 0;
			dp.Opened += (_, _) => openedCount++;
			dp.Closed += (_, _) => closedCount++;

			for (var i = 0; i < 5; i++)
			{
				dp.IsOpen = true;
				dp.IsOpen = false;
			}

			CornerstoneTest.AreEqual(5, openedCount);
			CornerstoneTest.AreEqual(5, closedCount);
			CornerstoneTest.IsFalse(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void IsOpenSetFalseFiresClosed()
		{
			var dp = new DrawerPage { IsOpen = true };
			var fired = false;
			dp.Closed += (_, _) => fired = true;

			dp.IsOpen = false;

			CornerstoneTest.IsTrue(fired);
		}

		[PresentationTestMethod]
		public void IsOpenSetFalseFiresClosingBeforeClosed()
		{
			var dp = new DrawerPage { IsOpen = true };
			var order = new List<string>();
			dp.Closing += (_, _) => order.Add("Closing");
			dp.Closed += (_, _) => order.Add("Closed");

			dp.IsOpen = false;

			CornerstoneTest.AreEqual(new[] { "Closing", "Closed" }, order);
		}

		[PresentationTestMethod]
		public void IsOpenSetTrueDoesNotFireClosing()
		{
			var dp = new DrawerPage();
			var closingFired = false;
			dp.Closing += (_, _) => closingFired = true;

			dp.IsOpen = true;

			CornerstoneTest.IsFalse(closingFired);
		}

		[PresentationTestMethod]
		public void IsOpenSetTrueFiresOpened()
		{
			var dp = new DrawerPage();
			var fired = false;
			dp.Opened += (_, _) => fired = true;

			dp.IsOpen = true;

			CornerstoneTest.IsTrue(fired);
		}

		#endregion
	}

	[TestClass]
	public class DrawerLengthValidationTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CompactDrawerLengthAcceptsZero()
		{
			var dp = new DrawerPage { CompactDrawerLength = 0 };
			CornerstoneTest.AreEqual(0.0, dp.CompactDrawerLength);
		}

		[PresentationTestMethod]
		[DataRow(double.NaN)]
		[DataRow(double.PositiveInfinity)]
		[DataRow(double.NegativeInfinity)]
		[DataRow(-1.0)]
		[DataRow(-100.0)]
		public void CompactDrawerLengthRejectsInvalidValues(double invalid)
		{
			var dp = new DrawerPage();
			CornerstoneTest.Throws<ArgumentException>(() => dp.CompactDrawerLength = invalid);
			CornerstoneTest.AreEqual(48.0, dp.CompactDrawerLength);
		}

		[PresentationTestMethod]
		public void DrawerLengthAcceptsZero()
		{
			var dp = new DrawerPage { DrawerLength = 0 };
			CornerstoneTest.AreEqual(0.0, dp.DrawerLength);
		}

		[PresentationTestMethod]
		[DataRow(double.NaN)]
		[DataRow(double.PositiveInfinity)]
		[DataRow(double.NegativeInfinity)]
		[DataRow(-1.0)]
		[DataRow(-100.0)]
		public void DrawerLengthRejectsInvalidValues(double invalid)
		{
			var dp = new DrawerPage { DrawerLength = 200.0 };
			CornerstoneTest.Throws<ArgumentException>(() => dp.DrawerLength = invalid);
			CornerstoneTest.AreEqual(200.0, dp.DrawerLength);
		}

		#endregion
	}

	[TestClass]
	public class EscapeKeyTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void EscapeKeyClosesCompactOverlayDrawer()
		{
			var dp = new DrawerPage
			{
				DisplayMode = SplitViewDisplayMode.CompactOverlay,
				IsOpen = true
			};
			var root = new TestRoot { Child = dp };

			dp.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });

			CornerstoneTest.IsFalse(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void EscapeKeyClosesOverlayDrawer()
		{
			var dp = new DrawerPage
			{
				DisplayMode = SplitViewDisplayMode.Overlay,
				IsOpen = true
			};
			var root = new TestRoot { Child = dp };

			dp.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });

			CornerstoneTest.IsFalse(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void EscapeKeyDoesNotCloseInlineDrawer()
		{
			var dp = new DrawerPage
			{
				DisplayMode = SplitViewDisplayMode.Inline,
				IsOpen = true
			};
			var root = new TestRoot { Child = dp };

			dp.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });

			CornerstoneTest.IsTrue(dp.IsOpen);
		}

		#endregion
	}

	[TestClass]
	public class HeaderFooterTemplateTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DrawerFooterTemplateClearingTemplateFallsBackToDirectContent()
		{
			var directControl = new TextBlock { Text = "Direct" };
			var (dp, _, footer, _) = Create(
				drawerFooter: directControl,
				footerTemplate: new FuncDataTemplate<object>((_, _) => new Canvas()));

			footer.UpdateChild();
			CornerstoneTest.IsType<Canvas>(footer.Child);

			dp.DrawerFooterTemplate = null;
			footer.UpdateChild();

			CornerstoneTest.Same(directControl, footer.Child);
		}

		[PresentationTestMethod]
		public void DrawerFooterTemplateIsForwardedToContentPresenter()
		{
			var template = new FuncDataTemplate<string>((_, _) => new TextBlock());
			var (_, _, footer, _) = Create(drawerFooter: "v1.0", footerTemplate: template);

			CornerstoneTest.Same(template, footer.ContentTemplate);
		}

		[PresentationTestMethod]
		public void DrawerFooterTemplateReceivesDrawerFooterAsData()
		{
			object receivedData = null;
			var (_, _, footer, _) = Create(
				drawerFooter: "v2.0",
				footerTemplate: new FuncDataTemplate<string>((data, _) =>
				{
					receivedData = data;
					return new TextBlock { Text = data };
				}));

			footer.UpdateChild();

			CornerstoneTest.AreEqual("v2.0", receivedData);
		}

		[PresentationTestMethod]
		public void DrawerFooterTemplateRendersControlProducedByFactory()
		{
			var (_, _, footer, _) = Create(
				drawerFooter: "v1.0",
				footerTemplate: new FuncDataTemplate<string>((_, _) => new Canvas()));

			footer.UpdateChild();

			CornerstoneTest.IsType<Canvas>(footer.Child);
		}

		[PresentationTestMethod]
		public void DrawerFooterTemplateSwapTemplateUpdatesContentPresenter()
		{
			var second = new FuncDataTemplate<string>((_, _) => new Border());
			var (dp, _, footer, _) = Create(
				drawerFooter: "v1.0",
				footerTemplate: new FuncDataTemplate<string>((_, _) => new Canvas()));

			footer.UpdateChild();
			CornerstoneTest.IsType<Canvas>(footer.Child);

			dp.DrawerFooterTemplate = second;
			footer.UpdateChild();

			CornerstoneTest.IsType<Border>(footer.Child);
		}

		[PresentationTestMethod]
		public void DrawerHeaderTemplateClearingTemplateFallsBackToDirectContent()
		{
			var directControl = new TextBlock { Text = "Direct" };
			var (dp, header, _, _) = Create(
				directControl,
				new FuncDataTemplate<object>((_, _) => new Canvas()));

			header.UpdateChild();
			CornerstoneTest.IsType<Canvas>(header.Child);

			dp.DrawerHeaderTemplate = null;
			header.UpdateChild();

			CornerstoneTest.Same(directControl, header.Child);
		}

		[PresentationTestMethod]
		public void DrawerHeaderTemplateIsForwardedToContentPresenter()
		{
			var template = new FuncDataTemplate<string>((_, _) => new TextBlock());
			var (_, header, _, _) = Create("App", template);

			CornerstoneTest.Same(template, header.ContentTemplate);
		}

		[PresentationTestMethod]
		public void DrawerHeaderTemplateReceivesDrawerHeaderAsData()
		{
			object receivedData = null;
			var (_, header, _, _) = Create(
				"MyTitle",
				new FuncDataTemplate<string>((data, _) =>
				{
					receivedData = data;
					return new TextBlock { Text = data };
				}));

			header.UpdateChild();

			CornerstoneTest.AreEqual("MyTitle", receivedData);
		}

		[PresentationTestMethod]
		public void DrawerHeaderTemplateRendersControlProducedByFactory()
		{
			var (_, header, _, _) = Create(
				"App",
				new FuncDataTemplate<string>((_, _) => new Canvas()));

			header.UpdateChild();

			CornerstoneTest.IsType<Canvas>(header.Child);
		}

		[PresentationTestMethod]
		public void DrawerHeaderTemplateSwapTemplateUpdatesContentPresenter()
		{
			var second = new FuncDataTemplate<string>((_, _) => new Border());
			var (dp, header, _, _) = Create(
				"App",
				new FuncDataTemplate<string>((_, _) => new Canvas()));

			header.UpdateChild();
			CornerstoneTest.IsType<Canvas>(header.Child);

			dp.DrawerHeaderTemplate = second;
			header.UpdateChild();

			CornerstoneTest.IsType<Border>(header.Child);
		}

		private static (DrawerPage dp, ContentPresenter header, ContentPresenter footer, TestRoot root) Create(
			object drawerHeader = null,
			IDataTemplate headerTemplate = null,
			object drawerFooter = null,
			IDataTemplate footerTemplate = null)
		{
			var dp = new DrawerPage
			{
				Template = MinimalPaneTemplate(),
				DrawerHeader = drawerHeader,
				DrawerHeaderTemplate = headerTemplate,
				DrawerFooter = drawerFooter,
				DrawerFooterTemplate = footerTemplate
			};
			var root = new TestRoot { Child = dp };
			dp.ApplyTemplate();

			var header = dp.GetVisualDescendants().OfType<ContentPresenter>().First(x => x.Name == "PART_DrawerHeader");
			var footer = dp.GetVisualDescendants().OfType<ContentPresenter>().First(x => x.Name == "PART_DrawerFooter");

			return (dp, header, footer, root);
		}

		// Wires PART_DrawerHeader and PART_DrawerFooter to the drawer's properties.
		// Other parts omitted; OnApplyTemplate uses Find (nullable) so they are safe to skip.
		private static IControlTemplate MinimalPaneTemplate()
		{
			return new FuncControlTemplate<DrawerPage>((dp, scope) =>
			{
				var header = new ContentPresenter { Name = "PART_DrawerHeader" }.RegisterInNameScope(scope);
				header.Bind(ContentPresenter.ContentProperty, dp.GetObservable(DrawerPage.DrawerHeaderProperty));
				header.Bind(ContentPresenter.ContentTemplateProperty, dp.GetObservable(DrawerPage.DrawerHeaderTemplateProperty));

				var footer = new ContentPresenter { Name = "PART_DrawerFooter" }.RegisterInNameScope(scope);
				footer.Bind(ContentPresenter.ContentProperty, dp.GetObservable(DrawerPage.DrawerFooterProperty));
				footer.Bind(ContentPresenter.ContentTemplateProperty, dp.GetObservable(DrawerPage.DrawerFooterTemplateProperty));

				return new StackPanel { Children = { header, footer } };
			});
		}

		#endregion
	}

	[TestClass]
	public class IconTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void DrawerIconTemplateRoundTrips()
		{
			var template = new FuncDataTemplate<object>((_, _) => new PathIcon());
			var dp = new DrawerPage { DrawerIconTemplate = template };
			CornerstoneTest.Same(template, dp.DrawerIconTemplate);
		}

		[PresentationTestMethod]
		public void DrawerIconWithGeometryDoesNotThrow()
		{
			var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
			var dp = new DrawerPage
			{
				DrawerIcon = geometry,
				DrawerIconTemplate = new FuncDataTemplate<object>((_, _) => new PathIcon())
			};
			var root = new TestRoot { Child = dp };

			dp.DrawerIcon = new EllipseGeometry { Rect = new Rect(0, 0, 20, 20) };
			CornerstoneTest.IsNotNull(dp.DrawerIcon);
		}

		#endregion
	}

	[TestClass]
	public class LifecycleEventTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ContentChangedFiresLifecycleEventsInOrder()
		{
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };
			var dp = new DrawerPage();
			var root = new TestRoot { Child = dp };
			dp.Content = first;

			var order = new List<string>();
			first.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
			second.NavigatedTo += (_, _) => order.Add("NavigatedTo");

			dp.Content = second;

			CornerstoneTest.AreEqual(2, order.Count);
			CornerstoneTest.AreEqual("NavigatedFrom", order[0]);
			CornerstoneTest.AreEqual("NavigatedTo", order[1]);
		}

		[PresentationTestMethod]
		public void ContentChangedNavigatedFromAndToNavigationTypeIsReplace()
		{
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };
			var dp = new DrawerPage();
			var root = new TestRoot { Child = dp };
			dp.Content = first;

			NavigatedFromEventArgs fromArgs = null;
			NavigatedToEventArgs toArgs = null;
			first.NavigatedFrom += (_, e) => fromArgs = e;
			second.NavigatedTo += (_, e) => toArgs = e;

			dp.Content = second;

			CornerstoneTest.IsNotNull(fromArgs);
			CornerstoneTest.AreEqual(NavigationType.Replace, fromArgs!.NavigationType);
			CornerstoneTest.IsNotNull(toArgs);
			CornerstoneTest.AreEqual(NavigationType.Replace, toArgs!.NavigationType);
		}

		[PresentationTestMethod]
		public void ContentChangedWhileOverlayDrawerOpenNonPageDrawerFiresLifecycleEvents()
		{
			var home = new ContentPage { Header = "Home" };
			var profile = new ContentPage { Header = "Profile" };
			var dp = new DrawerPage
			{
				DisplayMode = SplitViewDisplayMode.Overlay
			};
			var root = new TestRoot { Child = dp };
			dp.Drawer = new StackPanel();
			dp.Content = home;

			var events = new List<string>();
			home.NavigatedFrom += (_, _) => events.Add("Home: NavigatedFrom");
			profile.NavigatedTo += (_, _) => events.Add("Profile: NavigatedTo");

			dp.IsOpen = true;
			CornerstoneTest.Empty(events);

			dp.Content = profile;
			CornerstoneTest.AreEqual(2, events.Count);
			CornerstoneTest.AreEqual("Home: NavigatedFrom", events[0]);
			CornerstoneTest.AreEqual("Profile: NavigatedTo", events[1]);
		}

		[PresentationTestMethod]
		public void ContentSetBeforeAttachFiresLifecycleEventsOnLoad()
		{
			// Content set before the control enters the visual tree (simulating XAML parsing).
			// Events must fire exactly once when the control is attached and Loaded fires.
			var page = new ContentPage { Header = "Home" };
			var events = new List<string>();
			page.NavigatedTo += (_, _) => events.Add("NavigatedTo");

			var dp = new DrawerPage { Content = page };
			CornerstoneTest.Empty(events); // suppressed before visual tree

			var root = new TestRoot { Child = dp };
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None); // pump the posted Loaded dispatch

			CornerstoneTest.Single(events);
			CornerstoneTest.AreEqual("NavigatedTo", events[0]);
		}

		[PresentationTestMethod]
		public void ContentSetBeforeAttachNavigatedToNavigationTypeIsPush()
		{
			var page = new ContentPage { Header = "Home" };
			NavigatedToEventArgs args = null;
			page.NavigatedTo += (_, e) => args = e;

			var dp = new DrawerPage { Content = page };
			var root = new TestRoot { Child = dp };
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Push, args!.NavigationType);
		}

		// --- Initial-attach lifecycle (the _hasHadFirstPage / OnLoaded fix) ---

		[PresentationTestMethod]
		public void ContentSetBeforeAttachSuppressedUntilLoad()
		{
			// Events must NOT fire during XAML parsing (before VisualRoot is set).
			var page = new ContentPage { Header = "Home" };
			var events = new List<string>();
			page.NavigatedTo += (_, _) => events.Add("NavigatedTo");

			var _ = new DrawerPage { Content = page };

			CornerstoneTest.Empty(events);
		}

		[PresentationTestMethod]
		public void ContentSetBeforeAttachThenChangedAfterAttachNoDoubleFire()
		{
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };

			var dp = new DrawerPage { Content = first };
			var root = new TestRoot { Child = dp };
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None); // fire the deferred NavigatedTo on first

			var events = new List<string>();
			first.NavigatedFrom += (_, _) => events.Add("First: NavigatedFrom");
			second.NavigatedTo += (_, _) => events.Add("Second: NavigatedTo");

			dp.Content = second;

			CornerstoneTest.AreEqual(2, events.Count);
			CornerstoneTest.AreEqual("First: NavigatedFrom", events[0]);
			CornerstoneTest.AreEqual("Second: NavigatedTo", events[1]);
		}

		[PresentationTestMethod]
		public void ContentSetInitiallyFiresNavigatedTo()
		{
			var page = new ContentPage { Header = "Home" };
			var events = new List<string>();
			page.NavigatedTo += (_, _) => events.Add("NavigatedTo");

			var dp = new DrawerPage();
			var root = new TestRoot { Child = dp };
			dp.Content = page;

			CornerstoneTest.Single(events);
			CornerstoneTest.AreEqual("NavigatedTo", events[0]);
		}

		[PresentationTestMethod]
		public void ContentSetInitiallyNavigatedToNavigationTypeIsReplace()
		{
			var page = new ContentPage { Header = "Home" };
			NavigatedToEventArgs args = null;
			page.NavigatedTo += (_, e) => args = e;

			var dp = new DrawerPage();
			var root = new TestRoot { Child = dp };
			dp.Content = page;

			CornerstoneTest.IsNotNull(args);
			CornerstoneTest.AreEqual(NavigationType.Replace, args!.NavigationType);
		}

		[PresentationTestMethod]
		public void ContentSetInitiallySetsCurrentPage()
		{
			var page = new ContentPage { Header = "Home" };
			var dp = new DrawerPage { Content = page };
			CornerstoneTest.Same(page, dp.CurrentPage);
		}

		[PresentationTestMethod]
		public void ContentSetToSameInstanceNoLifecycleEvents()
		{
			// Re-assigning the same Content instance must not re-fire lifecycle events.
			var page = new ContentPage { Header = "Home" };
			var dp = new DrawerPage();
			var root = new TestRoot { Child = dp };
			dp.Content = page; // initial assignment fires NavigatedTo

			var events = new List<string>();
			page.NavigatedTo += (_, _) => events.Add("NavigatedTo");
			page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");

			dp.Content = page; // same instance — must not fire anything

			CornerstoneTest.Empty(events);
		}

		[PresentationTestMethod]
		public void IsOpenChangesNeverFirePageLifecycleEvents()
		{
			// Toggling IsOpen (open, close, repeated) must never raise page lifecycle events.
			var page = new ContentPage { Header = "Content" };
			var dp = new DrawerPage();
			var root = new TestRoot { Child = dp };
			dp.Content = page; // fires initial NavigatedTo

			var events = new List<string>();
			page.NavigatedTo += (_, _) => events.Add("NavigatedTo");
			page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");

			dp.IsOpen = true;
			dp.IsOpen = false;
			dp.IsOpen = false; // same value

			CornerstoneTest.Empty(events);
		}

		#endregion
	}

	[TestClass]
	public class LogicalChildrenTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ContentReplacedOldRemovedNewAdded()
		{
			var dp = new DrawerPage();
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };

			dp.Content = first;
			dp.Content = second;

			var children = ((ILogical) dp).LogicalChildren;
			CornerstoneTest.DoesNotContain(children, first);
			CornerstoneTest.Contains(children, second);
		}

		[PresentationTestMethod]
		public void ContentSetPageAddedToLogicalChildren()
		{
			var dp = new DrawerPage();
			var detail = new ContentPage { Header = "Content" };
			dp.Content = detail;
			CornerstoneTest.Contains(((ILogical) dp).LogicalChildren, detail);
		}

		[PresentationTestMethod]
		public void ContentSetToNullRemovedFromLogicalChildren()
		{
			var dp = new DrawerPage();
			var detail = new ContentPage { Header = "Content" };
			dp.Content = detail;
			dp.Content = null;
			CornerstoneTest.DoesNotContain(((ILogical) dp).LogicalChildren, detail);
		}

		[PresentationTestMethod]
		public void DrawerAndContentBothSetBothInLogicalChildren()
		{
			var dp = new DrawerPage();
			var drawer = new ContentPage { Header = "Menu" };
			var detail = new ContentPage { Header = "Home" };
			dp.Drawer = drawer;
			dp.Content = detail;

			var children = ((ILogical) dp).LogicalChildren;
			CornerstoneTest.Contains(children, drawer);
			CornerstoneTest.Contains(children, detail);
		}

		[PresentationTestMethod]
		public void DrawerMultipleReplacementsOnlyLastInLogicalChildren()
		{
			var dp = new DrawerPage();
			var first = new ContentPage { Header = "1st" };
			var second = new ContentPage { Header = "2nd" };
			var third = new ContentPage { Header = "3rd" };

			dp.Drawer = first;
			dp.Drawer = second;
			dp.Drawer = third;

			var children = ((ILogical) dp).LogicalChildren;
			CornerstoneTest.DoesNotContain(children, first);
			CornerstoneTest.DoesNotContain(children, second);
			CornerstoneTest.Contains(children, third);
		}

		[PresentationTestMethod]
		public void DrawerReplacedOldRemovedNewAdded()
		{
			var dp = new DrawerPage();
			var first = new ContentPage { Header = "First" };
			var second = new ContentPage { Header = "Second" };

			dp.Drawer = first;
			dp.Drawer = second;

			var children = ((ILogical) dp).LogicalChildren;
			CornerstoneTest.DoesNotContain(children, first);
			CornerstoneTest.Contains(children, second);
		}

		[PresentationTestMethod]
		public void DrawerSetPageAddedToLogicalChildren()
		{
			var dp = new DrawerPage();
			var drawer = new ContentPage { Header = "Menu" };
			dp.Drawer = drawer;
			CornerstoneTest.Contains(((ILogical) dp).LogicalChildren, drawer);
		}

		[PresentationTestMethod]
		public void DrawerSetToNullRemovedFromLogicalChildren()
		{
			var dp = new DrawerPage();
			var drawer = new ContentPage { Header = "Menu" };
			dp.Drawer = drawer;
			dp.Drawer = null;
			CornerstoneTest.DoesNotContain(((ILogical) dp).LogicalChildren, drawer);
		}

		#endregion
	}

	[TestClass]
	public class PropertyRoundTrips : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void BackdropBrushCanBeSetToNull()
		{
			var dp = new DrawerPage { BackdropBrush = Brushes.Black };
			dp.BackdropBrush = null;
			CornerstoneTest.IsNull(dp.BackdropBrush);
		}

		[PresentationTestMethod]
		public void BackdropBrushRoundTrips()
		{
			var brush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
			var dp = new DrawerPage { BackdropBrush = brush };
			CornerstoneTest.Same(brush, dp.BackdropBrush);
		}

		[PresentationTestMethod]
		[DataRow(40.0)]
		[DataRow(56.0)]
		[DataRow(80.0)]
		public void CompactDrawerLengthRoundTrips(double length)
		{
			var dp = new DrawerPage { CompactDrawerLength = length };
			CornerstoneTest.AreEqual(length, dp.CompactDrawerLength);
		}

		[PresentationTestMethod]
		public void ContentAcceptsContentPage()
		{
			var page = new ContentPage { Header = "Main" };
			var dp = new DrawerPage { Content = page };
			CornerstoneTest.Same(page, dp.Content);
		}

		[PresentationTestMethod]
		public void ContentAcceptsString()
		{
			var dp = new DrawerPage { Content = "ContentValue" };
			CornerstoneTest.AreEqual("ContentValue", dp.Content);
		}

		[PresentationTestMethod]
		public void ContentTemplateCanBeSetToNull()
		{
			var dp = new DrawerPage { ContentTemplate = null };
			CornerstoneTest.IsNull(dp.ContentTemplate);
		}

		[PresentationTestMethod]
		[DataRow(SplitViewDisplayMode.Overlay)]
		[DataRow(SplitViewDisplayMode.CompactOverlay)]
		[DataRow(SplitViewDisplayMode.Inline)]
		[DataRow(SplitViewDisplayMode.CompactInline)]
		public void DisplayModeRoundTrips(SplitViewDisplayMode mode)
		{
			var dp = new DrawerPage { DisplayMode = mode };
			CornerstoneTest.AreEqual(mode, dp.DisplayMode);
		}

		[PresentationTestMethod]
		public void DrawerAcceptsContentPage()
		{
			var page = new ContentPage { Header = "Menu" };
			var dp = new DrawerPage { Drawer = page };
			CornerstoneTest.Same(page, dp.Drawer);
		}

		[PresentationTestMethod]
		public void DrawerAcceptsString()
		{
			var dp = new DrawerPage { Drawer = "MenuContent" };
			CornerstoneTest.AreEqual("MenuContent", dp.Drawer);
		}

		[PresentationTestMethod]
		public void DrawerBackgroundRoundTrips()
		{
			var brush = new SolidColorBrush(Colors.DodgerBlue);
			var dp = new DrawerPage { DrawerBackground = brush };
			CornerstoneTest.Same(brush, dp.DrawerBackground);
		}

		[PresentationTestMethod]
		public void DrawerBehaviorDisabledPreventsIsOpenSetToTrue()
		{
			var dp = new DrawerPage { DrawerBehavior = DrawerBehavior.Disabled };
			dp.IsOpen = true;
			CornerstoneTest.IsFalse(dp.IsOpen);
		}

		[PresentationTestMethod]
		[DataRow(DrawerBehavior.Auto)]
		[DataRow(DrawerBehavior.Flyout)]
		[DataRow(DrawerBehavior.Locked)]
		[DataRow(DrawerBehavior.Disabled)]
		public void DrawerBehaviorRoundTrips(DrawerBehavior behavior)
		{
			var dp = new DrawerPage { DrawerBehavior = behavior };
			CornerstoneTest.AreEqual(behavior, dp.DrawerBehavior);
		}

		[PresentationTestMethod]
		[DataRow(600.0)]
		[DataRow(800.0)]
		[DataRow(1200.0)]
		public void DrawerBreakpointLengthRoundTrips(double width)
		{
			var dp = new DrawerPage { DrawerBreakpointLength = width };
			CornerstoneTest.AreEqual(width, dp.DrawerBreakpointLength);
		}

		[PresentationTestMethod]
		public void DrawerFooterAcceptsControl()
		{
			var ctrl = new TextBlock { Text = "Footer" };
			var dp = new DrawerPage { DrawerFooter = ctrl };
			CornerstoneTest.Same(ctrl, dp.DrawerFooter);
		}

		[PresentationTestMethod]
		public void DrawerFooterAcceptsString()
		{
			var dp = new DrawerPage { DrawerFooter = "v2.0" };
			CornerstoneTest.AreEqual("v2.0", dp.DrawerFooter);
		}

		[PresentationTestMethod]
		public void DrawerFooterBackgroundRoundTrips()
		{
			var brush = new SolidColorBrush(Colors.DarkGray);
			var dp = new DrawerPage { DrawerFooterBackground = brush };
			CornerstoneTest.Same(brush, dp.DrawerFooterBackground);
		}

		[PresentationTestMethod]
		public void DrawerFooterForegroundRoundTrips()
		{
			var brush = Brushes.LightGray;
			var dp = new DrawerPage { DrawerFooterForeground = brush };
			CornerstoneTest.Same(brush, dp.DrawerFooterForeground);
		}

		[PresentationTestMethod]
		public void DrawerHeaderAcceptsControl()
		{
			var ctrl = new TextBlock { Text = "My App" };
			var dp = new DrawerPage { DrawerHeader = ctrl };
			CornerstoneTest.Same(ctrl, dp.DrawerHeader);
		}

		[PresentationTestMethod]
		public void DrawerHeaderAcceptsString()
		{
			var dp = new DrawerPage { DrawerHeader = "My App" };
			CornerstoneTest.AreEqual("My App", dp.DrawerHeader);
		}

		[PresentationTestMethod]
		public void DrawerHeaderBackgroundRoundTrips()
		{
			var brush = new SolidColorBrush(Colors.Indigo);
			var dp = new DrawerPage { DrawerHeaderBackground = brush };
			CornerstoneTest.Same(brush, dp.DrawerHeaderBackground);
		}

		[PresentationTestMethod]
		public void DrawerHeaderForegroundRoundTrips()
		{
			var brush = Brushes.White;
			var dp = new DrawerPage { DrawerHeaderForeground = brush };
			CornerstoneTest.Same(brush, dp.DrawerHeaderForeground);
		}

		[PresentationTestMethod]
		public void DrawerIconAcceptsControl()
		{
			var icon = new PathIcon();
			var dp = new DrawerPage { DrawerIcon = icon };
			CornerstoneTest.Same(icon, dp.DrawerIcon);
		}

		[PresentationTestMethod]
		[DataRow(DrawerLayoutBehavior.Overlay)]
		[DataRow(DrawerLayoutBehavior.Split)]
		[DataRow(DrawerLayoutBehavior.CompactOverlay)]
		[DataRow(DrawerLayoutBehavior.CompactInline)]
		public void DrawerLayoutBehaviorRoundTrips(DrawerLayoutBehavior behavior)
		{
			var dp = new DrawerPage { DrawerLayoutBehavior = behavior };
			CornerstoneTest.AreEqual(behavior, dp.DrawerLayoutBehavior);
		}

		[PresentationTestMethod]
		[DataRow(100.0)]
		[DataRow(280.0)]
		[DataRow(500.0)]
		public void DrawerLengthRoundTrips(double length)
		{
			var dp = new DrawerPage { DrawerLength = length };
			CornerstoneTest.AreEqual(length, dp.DrawerLength);
		}

		[PresentationTestMethod]
		[DataRow(DrawerPlacement.Left)]
		[DataRow(DrawerPlacement.Right)]
		[DataRow(DrawerPlacement.Top)]
		[DataRow(DrawerPlacement.Bottom)]
		public void DrawerPlacementRoundTrips(DrawerPlacement placement)
		{
			var dp = new DrawerPage { DrawerPlacement = placement };
			CornerstoneTest.AreEqual(placement, dp.DrawerPlacement);
		}

		[PresentationTestMethod]
		public void DrawerTemplateCanBeSetToNull()
		{
			var dp = new DrawerPage { DrawerTemplate = null };
			CornerstoneTest.IsNull(dp.DrawerTemplate);
		}

		[PresentationTestMethod]
		public void HeaderRoundTrips()
		{
			var dp = new DrawerPage { Header = "My Drawer Page" };
			CornerstoneTest.AreEqual("My Drawer Page", dp.Header);
		}

		[PresentationTestMethod]
		[DataRow(HorizontalAlignment.Left)]
		[DataRow(HorizontalAlignment.Center)]
		[DataRow(HorizontalAlignment.Right)]
		[DataRow(HorizontalAlignment.Stretch)]
		public void HorizontalContentAlignmentRoundTrips(HorizontalAlignment value)
		{
			var dp = new DrawerPage { HorizontalContentAlignment = value };
			CornerstoneTest.AreEqual(value, dp.HorizontalContentAlignment);
		}

		[PresentationTestMethod]
		public void IconRoundTrips()
		{
			var icon = new Image();
			var dp = new DrawerPage { Icon = icon };
			CornerstoneTest.Same(icon, dp.Icon);
		}

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void IsGestureEnabledRoundTrips(bool value)
		{
			var dp = new DrawerPage { IsGestureEnabled = value };
			CornerstoneTest.AreEqual(value, dp.IsGestureEnabled);
		}

		[PresentationTestMethod]
		public void IsOpenToggle()
		{
			var dp = new DrawerPage();
			dp.IsOpen = true;
			CornerstoneTest.IsTrue(dp.IsOpen);
			dp.IsOpen = false;
			CornerstoneTest.IsFalse(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void SafeAreaPaddingRoundTrips()
		{
			var dp = new DrawerPage();
			var padding = new Thickness(10, 20, 10, 34);
			dp.SafeAreaPadding = padding;
			CornerstoneTest.AreEqual(padding, dp.SafeAreaPadding);
		}

		[PresentationTestMethod]
		[DataRow(VerticalAlignment.Top)]
		[DataRow(VerticalAlignment.Center)]
		[DataRow(VerticalAlignment.Bottom)]
		[DataRow(VerticalAlignment.Stretch)]
		public void VerticalContentAlignmentRoundTrips(VerticalAlignment value)
		{
			var dp = new DrawerPage { VerticalContentAlignment = value };
			CornerstoneTest.AreEqual(value, dp.VerticalContentAlignment);
		}

		#endregion
	}

	[TestClass]
	public class SwipeGestureTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void HandledPointerPressedAtEdgeAllowsSwipeOpen()
		{
			var dp = new DrawerPage
			{
				DrawerPlacement = DrawerPlacement.Left,
				DisplayMode = SplitViewDisplayMode.Overlay,
				Width = 400,
				Height = 300
			};
			dp.GestureRecognizers.OfType<SwipeGestureRecognizer>().First().IsMouseEnabled = true;

			var root = new TestRoot
			{
				ClientSize = new Size(400, 300),
				Child = dp
			};
			root.ExecuteInitialLayoutPass();

			RaiseHandledPointerPressed(dp, new Point(5, 5));

			var swipe = new SwipeGestureEventArgs(1, new Vector(-20, 0), default);
			dp.RaiseEvent(swipe);

			CornerstoneTest.IsTrue(swipe.Handled);
			CornerstoneTest.IsTrue(dp.IsOpen);
		}

		[PresentationTestMethod]
		public void MouseEdgeDragAllowsSwipeOpen()
		{
			var dp = new DrawerPage
			{
				DrawerPlacement = DrawerPlacement.Left,
				DisplayMode = SplitViewDisplayMode.Overlay,
				Width = 400,
				Height = 300
			};
			dp.GestureRecognizers.OfType<SwipeGestureRecognizer>().First().IsMouseEnabled = true;

			var root = new TestRoot
			{
				ClientSize = new Size(400, 300),
				Child = dp
			};
			root.ExecuteInitialLayoutPass();

			var mouse = new MouseTestHelper();
			mouse.Down(dp, position: new Point(5, 5));
			mouse.Move(dp, new Point(40, 5));
			mouse.Up(dp, position: new Point(40, 5));

			CornerstoneTest.IsTrue(dp.IsOpen);
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
		public void BackButtonClosesOpenDrawer()
		{
			var dp = new DrawerPage { IsOpen = true };
			var root = new TestRoot { Child = dp };

			var args = RaiseBackButton(dp);

			CornerstoneTest.IsFalse(dp.IsOpen);
			CornerstoneTest.IsTrue(args.Handled);
		}

		[PresentationTestMethod]
		public async Task BackButtonClosesOpenDrawerBeforeForwardingToNestedNavigationPage()
		{
			var dp = new DrawerPage { IsOpen = true };
			var nav = new NavigationPage();
			var coveredPage = new BackHandlingPage { HandleBack = true };
			var modal = new BackHandlingPage();
			await nav.PushAsync(coveredPage);
			await nav.PushModalAsync(modal);
			dp.Content = nav;
			var root = new TestRoot { Child = dp };

			var args = RaiseBackButton(dp);

			CornerstoneTest.IsTrue(args.Handled);
			CornerstoneTest.IsFalse(dp.IsOpen);
			CornerstoneTest.AreEqual(0, coveredPage.BackButtonPressCount);
			CornerstoneTest.AreEqual(0, modal.BackButtonPressCount);
			CornerstoneTest.Single(nav.ModalStack);
			CornerstoneTest.Same(modal, nav.ModalStack[0]);
		}

		[PresentationTestMethod]
		public void BackButtonDoesNotActOnDisabledDrawer()
		{
			var dp = new DrawerPage { DrawerBehavior = DrawerBehavior.Disabled };
			var root = new TestRoot { Child = dp };

			var args = RaiseBackButton(dp);

			CornerstoneTest.IsFalse(dp.IsOpen);
			CornerstoneTest.IsFalse(args.Handled);
		}

		[PresentationTestMethod]
		public void BackButtonDoesNotActWhenAlreadyClosed()
		{
			var dp = new DrawerPage();
			var root = new TestRoot { Child = dp };

			var args = RaiseBackButton(dp);

			CornerstoneTest.IsFalse(dp.IsOpen);
			CornerstoneTest.IsFalse(args.Handled);
		}

		[PresentationTestMethod]
		public void BackButtonDoesNotCloseLockedDrawer()
		{
			var dp = new DrawerPage
			{
				DrawerBehavior = DrawerBehavior.Locked,
				IsOpen = true
			};
			var root = new TestRoot { Child = dp };

			var args = RaiseBackButton(dp);

			CornerstoneTest.IsTrue(dp.IsOpen);
			CornerstoneTest.IsFalse(args.Handled);
		}

		[PresentationTestMethod]
		public async Task BackButtonForwardsThroughNavigationPageToModalBeforeCoveredPage()
		{
			var dp = new DrawerPage();
			var nav = new NavigationPage();
			var coveredPage = new BackHandlingPage { HandleBack = true };
			var modal = new BackHandlingPage();
			await nav.PushAsync(coveredPage);
			await nav.PushModalAsync(modal);
			dp.Content = nav;
			var root = new TestRoot { Child = dp };

			var args = RaiseBackButton(dp);

			CornerstoneTest.IsTrue(args.Handled);
			CornerstoneTest.AreEqual(0, coveredPage.BackButtonPressCount);
			CornerstoneTest.AreEqual(1, modal.BackButtonPressCount);
			CornerstoneTest.Empty(nav.ModalStack);
			CornerstoneTest.Same(coveredPage, nav.CurrentPage);
		}

		[PresentationTestMethod]
		public void BackEventIsForwardedToContent()
		{
			var dp = new DrawerPage();
			var page = new ContentPage();
			var isRaised = false;
			page.PageNavigationSystemBackButtonPressed += (s, e) => { isRaised = true; };
			var root = new TestRoot { Child = dp };
			dp.CurrentPage = page;

			var args = RaiseBackButton(dp);

			CornerstoneTest.IsTrue(isRaised);
			CornerstoneTest.IsFalse(args.Handled);
		}

		private static RoutedEventArgs RaiseBackButton(DrawerPage dp)
		{
			var args = new RoutedEventArgs(Page.PageNavigationSystemBackButtonPressedEvent);
			dp.RaiseEvent(args);
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

	#endregion
}