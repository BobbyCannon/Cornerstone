#region References

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Primitives.PopupPositioning;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class PopupTests : ScopedTestBase
{
	#region Fields

	protected bool UsePopupHost;

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ChangingChildShouldFireLogicalChildrenCollectionChanged()
	{
		var target = new Popup();
		var child1 = new Control();
		var child2 = new Control();
		var called = false;

		target.Child = child1;

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) => called = true;

		target.Child = child2;

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void ChildControlShouldAppearInLogicalChildren()
	{
		var target = new Popup();
		var child = new Control();

		target.Child = child;

		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ClearingChildShouldClearChildControlsParent()
	{
		var target = new Popup();
		var child = new Control();

		target.Child = child;
		target.Child = null;

		CornerstoneTest.IsNull(child.Parent);
		CornerstoneTest.IsNull(((ILogical) child).LogicalParent);
	}

	[PresentationTestMethod]
	public void ClearingChildShouldFireLogicalChildrenCollectionChanged()
	{
		var target = new Popup();
		var child = new Control();
		var called = false;

		target.Child = child;

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) =>
			called = e.Action == NotifyCollectionChangedAction.Remove;

		target.Child = null;

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void ClearingChildShouldRemoveFromLogicalChildren()
	{
		var target = new Popup();
		var child = new Control();

		target.Child = child;
		target.Child = null;

		CornerstoneTest.AreEqual(new ILogical[0], ((ILogical) target).LogicalChildren.ToList());
	}

	[PresentationTestMethod]
	public void ClosingPopupSetsFocusOnPlacementTarget()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();
			window.Focusable = true;

			var tb = new TextBox();
			var p = new Popup
			{
				PlacementTarget = window,
				Child = tb
			};

			window.Content = p;
			window.Show();
			window.Focus();
			p.Open();

			if (p.Host is OverlayPopupHost host)
			{
				//Need to measure/arrange for visual children to show up
				//in OverlayPopupHost
				host.Measure(Size.Infinity);
				host.Arrange(new Rect(host.DesiredSize));
			}

			tb.Focus();

			p.Close();

			var focusManager = window.FocusManager;
			CornerstoneTest.IsNotNull(focusManager);
			var focus = focusManager.GetFocusedElement();
			CornerstoneTest.Same(window, focus);
		}
	}

	[PresentationTestMethod]
	public void ClosingPopupWithIsOpenShouldRemoveItFromOpenedPopups()
	{
		using (CreateServices())
		{
			var target = new Popup();
			var window = PreparedWindow(target);

			target.IsOpen = true;

			CornerstoneTest.AreEqual(new[] { target }, window.OpenedPopups);

			target.IsOpen = false;

			CornerstoneTest.Empty(window.OpenedPopups);
		}
	}

	[PresentationTestMethod]
	public void ClosingPreviousLightDismissPopupShouldNotAffectOverlayForNextPopup()
	{
		using (CreateServices())
		{
			var placementTarget = new Border();
			var window = PreparedWindow(placementTarget);
			var first = new Popup
			{
				PlacementTarget = placementTarget,
				IsLightDismissEnabled = true
			};
			var second = new Popup
			{
				PlacementTarget = placementTarget,
				IsLightDismissEnabled = true
			};

			first.Open();
			second.Open();

			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);

			first.Close();

			CornerstoneTest.IsTrue(overlay.IsVisible);

			overlay.RaiseEvent(CreatePointerPressedEventArgs(window, new Point(10, 15)));

			CornerstoneTest.IsFalse(second.IsOpen);
			CornerstoneTest.IsFalse(overlay.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void ClosingWindowShouldClearOpenedPopups()
	{
		using (CreateServices())
		{
			var target = new Popup();
			var window = PreparedWindow(target);

			target.Open();
			window.Close();

			CornerstoneTest.Empty(window.OpenedPopups);
		}
	}

	[PresentationTestMethod]
	public void ContentControlWithPopupInTemplateShouldSetTemplatedParent()
	{
		// Test uses OverlayPopupHost default template
		using (CreateServices())
		{
			PopupContentControl target;
			var root = PreparedWindow(target = new PopupContentControl
			{
				Content = new Border(),
				Template = new FuncControlTemplate<PopupContentControl>(PopupContentControlTemplate)
			});
			root.Show();

			target.ApplyTemplate();

			var popup = (Popup) target.GetTemplateDescendants().First(x => x.Name == "popup");
			popup.Open();

			var popupRoot = (Control) popup.Host!;
			popupRoot.Measure(Size.Infinity);
			popupRoot.Arrange(new Rect(popupRoot.DesiredSize));

			var children = popupRoot.GetVisualDescendants().ToList();
			var types = children.Select(x => x.GetType().Name).ToList();

			if (UsePopupHost)
			{
				CornerstoneTest.AreEqual(new[]
				{
					"LayoutTransformControl",
					"VisualLayerManager",
					"ContentPresenter",
					"ContentPresenter",
					"Border"
				}, types);
			}
			else
			{
				CornerstoneTest.AreEqual(new[]
				{
					"LayoutTransformControl",
					"Panel",
					"Border",
					"VisualLayerManager",
					"ContentPresenter",
					"ContentPresenter",
					"Border"
				}, types);
			}

			var templatedParents = children
				.OfType<Control>()
				.Select(x => x.TemplatedParent).ToList();

			if (UsePopupHost)
			{
				CornerstoneTest.AreEqual(new object[]
				{
					popupRoot,
					popupRoot,
					popupRoot,
					target,
					null
				}, templatedParents);
			}
			else
			{
				CornerstoneTest.AreEqual(new object[]
				{
					popupRoot,
					popupRoot,
					popupRoot,
					popupRoot,
					popupRoot,
					target,
					null
				}, templatedParents);
			}
		}
	}

	[PresentationTestMethod]
	public void CustomPlacementCallbackIsExecuted()
	{
		using (CreateServices())
		{
			var callbackExecuted = 0;
			var popupContent = new Border { Width = 100, Height = 100 };
			var popup = new Popup
			{
				Child = popupContent,
				Placement = PlacementMode.Custom,
				HorizontalOffset = 42,
				VerticalOffset = 21
			};
			var popupParent = new Border { Child = popup };
			var root = PreparedWindow(popupParent);

			popup.CustomPopupPlacementCallback = parameters =>
			{
				CornerstoneTest.AreEqual(popupContent.Width, parameters.PopupSize.Width);
				CornerstoneTest.AreEqual(popupContent.Height, parameters.PopupSize.Height);

				CornerstoneTest.AreEqual(root.Width, parameters.AnchorRectangle.Width);
				CornerstoneTest.AreEqual(root.Height, parameters.AnchorRectangle.Height);

				CornerstoneTest.AreEqual(popup.HorizontalOffset, parameters.Offset.X);
				CornerstoneTest.AreEqual(popup.VerticalOffset, parameters.Offset.Y);

				callbackExecuted++;

				parameters.Anchor = PopupAnchor.Top;
				parameters.Gravity = PopupGravity.Bottom;
			};

			root.LayoutManager.ExecuteInitialLayoutPass();
			popup.Open();
			root.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(1, callbackExecuted);
		}
	}

	[PresentationTestMethod]
	public void DataContextBeginUpdateShouldNotBeCalledForControlsThatDontInherit()
	{
		using (CreateServices())
		{
			TestControl child;
			var popup = new Popup
			{
				Child = child = new TestControl(),
				DataContext = "foo",
				PlacementTarget = PreparedWindow()
			};

			var beginCalled = false;
			child.DataContextBeginUpdate += (s, e) => beginCalled = true;

			// Test for #1245. Here, the child's logical parent is the popup but it's not yet
			// attached to a visual tree because the popup hasn't been opened.
			CornerstoneTest.Same(popup, ((ILogical) child).LogicalParent);
			CornerstoneTest.Same(popup, child.InheritanceParent);
			CornerstoneTest.IsNull(child.GetVisualRoot());

			popup.Open();

			// #1245 was caused by the fact that DataContextBeginUpdate was called on `target`
			// when the PopupRoot was created, even though PopupRoot isn't the
			// InheritanceParent of child.
			CornerstoneTest.IsFalse(beginCalled);
		}
	}

	[PresentationTestMethod]
	public void EventsShouldBeRoutedToPopupParent()
	{
		using (CreateServices())
		{
			var popupContent = new Border();
			var popup = new Popup { Child = popupContent };
			var popupParent = new Border { Child = popup };
			var root = PreparedWindow(popupParent);
			var raised = 0;

			root.LayoutManager.ExecuteInitialLayoutPass();
			popup.Open();
			root.LayoutManager.ExecuteLayoutPass();

			var ev = new RoutedEventArgs(Button.ClickEvent);

			popupParent.AddHandler(Button.ClickEvent, (s, e) => ++raised);
			popupContent.RaiseEvent(ev);

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void FocusableControlsInPopupShouldGetFocus()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow(new Panel { Children = { new Slider() } });

			var textBox = new TextBox();
			var button = new Button();
			var popup = new Popup
			{
				PlacementTarget = window,
				Child = new StackPanel
				{
					Children =
					{
						textBox,
						button
					}
				}
			};

			((ISetLogicalParent) popup).SetParent(popup.PlacementTarget);
			window.Show();
			popup.Open();

			button.Focus();

			var inputRoot = ((Visual) popup.Host!).GetInputRoot();

			var focusManager = inputRoot!.FocusManager!;
			CornerstoneTest.Same(button, focusManager.GetFocusedElement());

			//Ensure focus remains in the popup
			#pragma warning disable CS0618 // Type or member is obsolete
			var handler = popup.Host switch
			{
				PopupRoot popupRoot => popupRoot.Tests_KeyboardNavigationHandler,
				OverlayPopupHost overlayPopupHost => overlayPopupHost.Tests_KeyboardNavigationHandler,
				_ => throw new InvalidOperationException("Unknown popup host type")
			};

			handler.Move(focusManager.GetFocusedElement()!, NavigationDirection.Next);
			#pragma warning restore CS0618 // Type or member is obsolete
			CornerstoneTest.Same(textBox, focusManager.GetFocusedElement());

			popup.Close();
		}
	}

	[PresentationTestMethod]
	public void GetPositionOnControlInPopupCalledFromParentShouldReturnValidCoordinates()
	{
		// This test only applies when using a PopupRoot host and not an overlay popup.
		if (UsePopupHost)
		{
			return;
		}

		using (CreateServices())
		{
			var popupContent = new Border { Height = 100, Width = 100, Background = Brushes.Red };
			var popup = new Popup
			{
				Child = popupContent, HorizontalOffset = 40, VerticalOffset = 40, Placement = PlacementMode.AnchorAndGravity,
				PlacementAnchor = PopupAnchor.TopLeft, PlacementGravity = PopupGravity.BottomRight
			};
			var popupParent = new Border { Child = popup };
			var root = PreparedWindow(popupParent);

			popup.Open();

			// Verify that the popup is positioned at 40,40 as descibed by the Horizontal/
			// VerticalOffset: 10,10 becomes 50,50 in screen coordinates.
			CornerstoneTest.AreEqual(new PixelPoint(50, 50), popupContent.PointToScreen(new Point(10, 10)));

			// The popup parent is positioned at 0,0 in screen coordinates so client and
			// screen coordinates are the same.
			CornerstoneTest.AreEqual(new PixelPoint(10, 10), popupParent.PointToScreen(new Point(10, 10)));

			// The event will be raised on the popup content at 50,50 (90,90 in screen coordinates)
			var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
			var ev = new PointerPressedEventArgs(
				popupContent,
				pointer,
				(PopupRoot) TopLevel.GetTopLevel(popupContent)!,
				new Point(50, 50),
				0,
				new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
				KeyModifiers.None);

			var contentRaised = 0;
			var parentRaised = 0;

			// The event is raised on the popup content in popup coordinates.
			popupContent.AddHandler(Button.PointerPressedEvent, (s, e) =>
			{
				++contentRaised;
				CornerstoneTest.AreEqual(new Point(50, 50), e.GetPosition(popupContent));
			});

			// The event is raised on the parent in root coordinates (which in this case are 
			// the same as screen coordinates).
			popupParent.AddHandler(Button.PointerPressedEvent, (s, e) =>
			{
				++parentRaised;
				CornerstoneTest.AreEqual(new Point(90, 90), e.GetPosition(popupParent));
			});

			popupContent.RaiseEvent(ev);

			CornerstoneTest.AreEqual(1, contentRaised);
			CornerstoneTest.AreEqual(1, parentRaised);
		}
	}

	[PresentationTestMethod]
	public void ItemsControlWithPopupInTemplateShouldSetTemplatedParent()
	{
		// Test uses OverlayPopupHost default template
		using (CreateServices())
		{
			PopupItemsControl target;
			var item = new Border();
			var root = PreparedWindow(target = new PopupItemsControl
			{
				Items = { item },
				Template = new FuncControlTemplate<PopupItemsControl>(PopupItemsControlTemplate)
			});
			;
			root.Show();

			target.ApplyTemplate();

			var popup = (Popup) target.GetTemplateDescendants().First(x => x.Name == "popup");
			popup.Open();

			var popupRoot = (Control) popup.Host!;
			popupRoot.Measure(Size.Infinity);
			popupRoot.Arrange(new Rect(popupRoot.DesiredSize));

			var children = popupRoot.GetVisualDescendants().ToList();
			var types = children.Select(x => x.GetType().Name).ToList();

			if (UsePopupHost)
			{
				CornerstoneTest.AreEqual(new[]
				{
					"LayoutTransformControl",
					"VisualLayerManager",
					"ContentPresenter",
					"ItemsPresenter",
					"StackPanel",
					"Border"
				}, types);
			}
			else
			{
				CornerstoneTest.AreEqual(new[]
				{
					"LayoutTransformControl",
					"Panel",
					"Border",
					"VisualLayerManager",
					"ContentPresenter",
					"ItemsPresenter",
					"StackPanel",
					"Border"
				}, types);
			}

			var templatedParents = children
				.OfType<Control>()
				.Select(x => x.TemplatedParent).ToList();

			if (UsePopupHost)
			{
				CornerstoneTest.AreEqual(new object[]
				{
					popupRoot,
					popupRoot,
					popupRoot,
					target,
					target,
					null
				}, templatedParents);
			}
			else
			{
				CornerstoneTest.AreEqual(new object[]
				{
					popupRoot,
					popupRoot,
					popupRoot,
					popupRoot,
					popupRoot,
					target,
					target,
					null
				}, templatedParents);
			}
		}
	}

	[PresentationTestMethod]
	public void NestedPopupShouldBeInParentPopupOpenedPopups()
	{
		using (CreateServices())
		{
			var nestedTarget = new Border { Width = 20, Height = 20 };
			var nestedPopup = new Popup
			{
				PlacementTarget = nestedTarget,
				Child = new Border { Width = 10, Height = 10 }
			};
			var target = new Border();
			var popup = new Popup
			{
				PlacementTarget = target,
				Child = new Panel { Children = { nestedTarget, nestedPopup } }
			};
			var window = PreparedWindow(new Panel { Children = { target, popup } });

			popup.Open();

			if (popup.Host is OverlayPopupHost host)
			{
				//Need to measure/arrange for visual children to show up
				//in OverlayPopupHost
				host.Measure(Size.Infinity);
				host.Arrange(new Rect(host.DesiredSize));
			}

			nestedPopup.Open();

			CornerstoneTest.AreEqual([popup], window.OpenedPopups);
			CornerstoneTest.AreEqual([nestedPopup], popup.OpenedPopups);
			CornerstoneTest.Empty(nestedPopup.OpenedPopups);

			if (popup.Host is PopupRoot popupRoot)
			{
				// A popup root exposes the popups opened by its own popup.
				CornerstoneTest.AreEqual([nestedPopup], popupRoot.OpenedPopups);
			}

			nestedPopup.Close();

			CornerstoneTest.AreEqual([popup], window.OpenedPopups);
			CornerstoneTest.Empty(popup.OpenedPopups);

			popup.Close();

			CornerstoneTest.Empty(window.OpenedPopups);
		}
	}

	[PresentationTestMethod]
	public void OpenedPopupShouldBeInOpenedPopups()
	{
		using (CreateServices())
		{
			var target = new Popup();
			var window = PreparedWindow(target);

			target.Open();

			CornerstoneTest.AreEqual(new[] { target }, window.OpenedPopups);

			target.Close();

			CornerstoneTest.Empty(window.OpenedPopups);
		}
	}

	[PresentationTestMethod]
	public void OpeningPopupShouldntThrowWhenInTreeWithoutTopLevel()
	{
		var c = new Control();
		var target = new Popup();
		((ISetLogicalParent) target).SetParent(c);
		target.IsOpen = true;
	}

	[PresentationTestMethod]
	public void OpeningPopupShouldntThrowWhenNotInVisualTree()
	{
		var target = new Popup();
		target.IsOpen = true;
	}

	[PresentationTestMethod]
	public void OverlayDismissEventPassThroughShouldPassEventToWindowContents()
	{
		using (CreateServices())
		{
			var compositor = RendererMocks.CreateDummyCompositor();
			var platform = PresentationLocator.Current.GetRequiredService<IWindowingPlatform>();
			var windowImpl = (StubWindowImpl) platform.CreateWindow();
			windowImpl.Compositor = compositor;
			var hitTester = new StubHitTester();

			var window = new Window(windowImpl)
			{
				HitTesterOverride = hitTester
			};
			window.ApplyStyling();
			window.ApplyTemplate();

			var target = new Popup
			{
				PlacementTarget = window,
				IsLightDismissEnabled = true,
				OverlayDismissEventPassThrough = true
			};

			var raised = 0;
			var border = new Border();
			window.Content = border;

			hitTester.SetHit(border);

			border.PointerPressed += (s, e) =>
			{
				CornerstoneTest.Same(border, e.Source);
				++raised;
			};

			target.Open();
			CornerstoneTest.IsTrue(target.IsOpen);

			var e = CreatePointerPressedEventArgs(window, new Point(10, 15));
			var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
			CornerstoneTest.IsNotNull(overlay);
			overlay.RaiseEvent(e);

			CornerstoneTest.AreEqual(1, raised);
			CornerstoneTest.IsFalse(target.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void PopupAttachedToAdornerRespectsAdornerPosition()
	{
		using (CreateServices())
		{
			var popupTarget = new Border { Height = 30, Background = Brushes.Red, [DockPanel.DockProperty] = Dock.Top };
			var popupContent = new Border { Height = 30, Width = 50, Background = Brushes.Yellow };
			var popup = new Popup
			{
				Child = popupContent,
				Placement = PlacementMode.AnchorAndGravity,
				PlacementTarget = popupTarget,
				PlacementAnchor = PopupAnchor.BottomRight,
				PlacementGravity = PopupGravity.BottomRight
			};
			var adorner = new DockPanel
			{
				Children = { popupTarget, popup },
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 40,
				Margin = new Thickness(50, 5, 0, 0)
			};

			var adorned = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Blue,
				[Canvas.LeftProperty] = 20,
				[Canvas.TopProperty] = 40
			};
			var windowContent = new Canvas();
			windowContent.Children.Add(adorned);

			var root = PreparedWindow(windowContent);

			var adornerLayer = AdornerLayer.GetAdornerLayer(adorned);
			CornerstoneTest.IsNotNull(adornerLayer);
			adornerLayer.Children.Add(adorner);
			AdornerLayer.SetAdornedElement(adorner, adorned);

			root.LayoutManager.ExecuteInitialLayoutPass();
			popup.Open();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender, CancellationToken.None);

			// X: Adorned Canvas.Left + Adorner Margin Left + Adorner Width
			// Y: Adorned Canvas.Top + Adorner Margin Top + Adorner Height
			CornerstoneTest.AreEqual(new PixelPoint(110, 75), popupContent.PointToScreen(new Point(0, 0)));
		}
	}

	[PresentationTestMethod]
	public void PopupCloseOnClosedPopupShouldNotRaiseClosedEvent()
	{
		using (CreateServices())
		{
			var window = PreparedWindow();
			var target = new Popup { Placement = PlacementMode.Pointer };

			window.Content = target;
			window.ApplyTemplate();

			var closedCount = 0;

			target.Closed += (sender, args) => { closedCount++; };

			target.Close();
			target.Close();
			target.Close();
			target.Close();

			CornerstoneTest.AreEqual(0, closedCount);
		}
	}

	[PresentationTestMethod]
	public void PopupCloseShouldRaiseSingleClosedEvent()
	{
		using (CreateServices())
		{
			var window = PreparedWindow();
			var target = new Popup { Placement = PlacementMode.Pointer };

			window.Content = target;
			window.ApplyTemplate();
			target.Open();

			var closedCount = 0;

			target.Closed += (sender, args) => { closedCount++; };

			target.Close();

			CornerstoneTest.AreEqual(1, closedCount);
		}
	}

	[PresentationTestMethod]
	public void PopupForwardsIsHitTestVisibleChangesToOpenHost()
	{
		using (CreateServices())
		{
			var target = new Popup();
			var window = PreparedWindow(target);
			window.Show();

			target.Open();
			CornerstoneTest.IsTrue(target.Host!.IsHitTestVisible);

			target.IsHitTestVisible = false;

			CornerstoneTest.IsFalse(target.Host!.IsHitTestVisible);
		}
	}

	[PresentationTestMethod]
	public void PopupForwardsIsHitTestVisibleToHostOnOpen()
	{
		using (CreateServices())
		{
			var target = new Popup { IsHitTestVisible = false };
			var window = PreparedWindow(target);
			window.Show();

			target.Open();

			CornerstoneTest.IsFalse(target.Host!.IsHitTestVisible);
		}
	}

	[PresentationTestMethod]
	public void PopupHostTypeShouldMatchPlatformPreference()
	{
		using (CreateServices())
		{
			var target = new Popup { PlacementTarget = PreparedWindow() };

			target.Open();
			if (UsePopupHost)
			{
				CornerstoneTest.IsType<OverlayPopupHost>(target.Host);
			}
			else
			{
				CornerstoneTest.IsType<PopupRoot>(target.Host);
			}
		}
	}

	[PresentationTestMethod]
	public void PopupIsHitTestVisibleDefaultsToTrue()
	{
		using (CreateServices())
		{
			CornerstoneTest.IsTrue(new Popup().IsHitTestVisible);
		}
	}

	[PresentationTestMethod]
	public void PopupOpenShouldRaiseSingleOpenedEvent()
	{
		using (CreateServices())
		{
			var window = PreparedWindow();
			var target = new Popup { Placement = PlacementMode.Pointer };

			window.Content = target;

			var openedCount = 0;

			target.Opened += (sender, args) => { openedCount++; };

			target.Open();

			CornerstoneTest.AreEqual(1, openedCount);
		}
	}

	[PresentationTestMethod]
	public void PopupOpenWithCorrectIsUsingOverlayLayerAndDisabledOverlayLayer()
	{
		using (CreateServices())
		{
			var target = new Popup();
			target.IsOpen = true;
			target.ShouldUseOverlayLayer = false;

			var window = PreparedWindow(target);
			window.Show();

			CornerstoneTest.AreEqual(UsePopupHost, target.IsUsingOverlayLayer);
		}
	}

	[PresentationTestMethod]
	public void PopupOpenWithCorrectIsUsingOverlayLayerAndEnabledOverlayLayer()
	{
		using (CreateServices())
		{
			var target = new Popup();
			target.IsOpen = true;
			target.ShouldUseOverlayLayer = true;

			var window = PreparedWindow(target);
			window.Show();

			CornerstoneTest.AreEqual(true, target.IsUsingOverlayLayer);
		}
	}

	[PresentationTestMethod]
	public void PopupOpenWithoutTargetShouldAttachItselfLater()
	{
		using (CreateServices())
		{
			var openedEvent = 0;
			var target = new Popup();
			target.Opened += (s, a) => openedEvent++;
			target.IsOpen = true;

			var window = PreparedWindow(target);
			window.Show();
			CornerstoneTest.AreEqual(1, openedEvent);
		}
	}

	[PresentationTestMethod]
	public void PopupRootShouldBeDetachedFromLogicalTreeWhenPopupIsDetached()
	{
		using (CreateServices())
		{
			var target = new Popup { Placement = PlacementMode.Pointer };
			var root = PreparedWindow(target);

			target.Open();

			var popupRoot = (ILogical) (Visual) target.Host!;

			CornerstoneTest.IsTrue(popupRoot.IsAttachedToLogicalTree);
			root.Content = null;
			CornerstoneTest.IsFalse(((ILogical) target).IsAttachedToLogicalTree);
		}
	}

	[PresentationTestMethod]
	public void PopupRootShouldHavePopupAsLogicalParent()
	{
		using (CreateServices())
		{
			var target = new Popup { PlacementTarget = PreparedWindow() };

			target.Open();

			CornerstoneTest.AreEqual(target, ((Visual) target.Host!).Parent);
			CornerstoneTest.AreEqual(target, ((Visual) target.Host).GetLogicalParent());
		}
	}

	[PresentationTestMethod]
	public void PopupRootShouldInitiallyBeNull()
	{
		using (CreateServices())
		{
			var target = new Popup();

			CornerstoneTest.IsNull((Visual) target.Host!);
		}
	}

	[PresentationTestMethod]
	public void PopupShouldClearKeyboardFocusFromChildrenWhenClosed()
	{
		using (CreateServicesWithFocus())
		{
			var winButton = new Button();
			var window = PreparedWindow(new Panel { Children = { winButton } });

			var border1 = new Border();
			var border2 = new Border();
			var button = new Button();
			border1.Child = border2;
			border2.Child = button;
			var popup = new Popup
			{
				PlacementTarget = window,
				Child = new StackPanel
				{
					Children =
					{
						border1
					}
				}
			};

			((ISetLogicalParent) popup).SetParent(popup.PlacementTarget);
			window.Show();
			winButton.Focus();
			popup.Open();

			button.Focus();

			var inputRoot = ((Visual) popup.Host!).GetInputRoot();

			var focusManager = inputRoot!.FocusManager!;
			CornerstoneTest.Same(button, focusManager.GetFocusedElement());

			border1.Child = null;

			winButton.Focus();

			CornerstoneTest.IsFalse(border2.IsKeyboardFocusWithin);
		}
	}

	[PresentationTestMethod]
	public void PopupShouldFollowPlacementTargetOnTargetMoved()
	{
		using (CreateServices())
		{
			var placementTarget = new Panel
			{
				Width = 10,
				Height = 10,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			var popup = new Popup
			{
				PlacementTarget = placementTarget,
				Placement = PlacementMode.Bottom,
				Width = 10,
				Height = 10
			};
			((ISetLogicalParent) popup).SetParent(popup.PlacementTarget);

			var window = PreparedWindow(placementTarget);
			window.Show();
			popup.Open();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			// The target's initial placement is (395,295) which is a 10x10 panel centered in a 800x600 window
			CornerstoneTest.AreEqual(placementTarget.Bounds, new Rect(395D, 295D, 10, 10));

			var raised = false;

			// Margin will move placement target
			if (popup.Host is PopupRoot popupRoot)
			{
				popupRoot.PositionChanged += (_, args) =>
				{
					CornerstoneTest.AreEqual(new PixelPoint(400, 305), args.Point);
					raised = true;
				};
			}
			else if (popup.Host is OverlayPopupHost overlayPopupHost)
			{
				overlayPopupHost.PropertyChanged += (_, args) =>
				{
					if (((args.Property == Canvas.TopProperty)
							|| (args.Property == Canvas.LeftProperty))
						&& (Canvas.GetLeft(overlayPopupHost) == 400)
						&& (Canvas.GetTop(overlayPopupHost) == 305))
					{
						raised = true;
					}
				};
			}
			placementTarget.Margin = new Thickness(10, 0, 0, 0);
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void PopupShouldFollowPlacementTargetOnWindowResize()
	{
		using (CreateServices())
		{
			var placementTarget = new Panel
			{
				Width = 10,
				Height = 10,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			var popup = new Popup
			{
				PlacementTarget = placementTarget,
				Placement = PlacementMode.Bottom,
				Width = 10,
				Height = 10
			};
			((ISetLogicalParent) popup).SetParent(popup.PlacementTarget);

			var window = PreparedWindow(placementTarget);
			window.Show();
			popup.Open();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender, CancellationToken.None);

			// The target's initial placement is (395,295) which is a 10x10 panel centered in a 800x600 window
			CornerstoneTest.AreEqual(placementTarget.Bounds, new Rect(395D, 295D, 10, 10));

			var raised = false;

			// Resizing the window to 700x500 must move the popup to (345,255) as this is the new
			// location of the placement target
			if (popup.Host is PopupRoot popupRoot)
			{
				popupRoot.PositionChanged += (_, args) =>
				{
					CornerstoneTest.AreEqual(new PixelPoint(345, 255), args.Point);
					raised = true;
				};
			}
			else if (popup.Host is OverlayPopupHost overlayPopupHost)
			{
				overlayPopupHost.PropertyChanged += (_, args) =>
				{
					if (((args.Property == Canvas.TopProperty)
							|| (args.Property == Canvas.LeftProperty))
						&& (Canvas.GetLeft(overlayPopupHost) == 345)
						&& (Canvas.GetTop(overlayPopupHost) == 255))
					{
						raised = true;
					}
				};
			}
			window.PlatformImpl?.Resize(new Size(700D, 500D), WindowResizeReason.Unspecified);
			Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender, CancellationToken.None);
			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void PopupShouldFollowPopupRootPlacementTarget()
	{
		// When the placement target of a popup is another popup (e.g. nested menu items), the child popup must
		// follow the parent popup if it moves (due to root window movement or resize)
		using (CreateServices())
		{
			// The child popup is placed directly over the parent popup for position testing
			var parentPopup = new Popup { Width = 10, Height = 10 };
			var childPopup = new Popup
			{
				Width = 20,
				Height = 20,
				PlacementTarget = parentPopup,
				Placement = PlacementMode.AnchorAndGravity,
				PlacementAnchor = PopupAnchor.TopLeft,
				PlacementGravity = PopupGravity.BottomRight
			};
			((ISetLogicalParent) childPopup).SetParent(childPopup.PlacementTarget);

			var window = PreparedWindow(parentPopup);
			window.Show();
			parentPopup.Open();
			childPopup.Open();

			if (childPopup.Host is PopupRoot popupRoot)
			{
				var raised = false;
				popupRoot.PositionChanged += (_, args) =>
				{
					// The parent's initial placement is (395,295) which is a 10x10 popup centered
					// in a 800x600 window. When the window is moved, the child's final placement is (405, 305)
					// which is the parent's placement moved 10 pixels left and down.
					CornerstoneTest.AreEqual(new PixelPoint(405, 305), args.Point);
					raised = true;
				};

				window.Position = new PixelPoint(10, 10);
				CornerstoneTest.IsTrue(raised);
			}
		}
	}

	[PresentationTestMethod]
	public void PopupShouldNotFollowPlacementTargetOnTargetMovedIfPointer()
	{
		using (CreateServices())
		{
			var placementTarget = new Panel
			{
				Width = 10,
				Height = 10,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			var popup = new Popup
			{
				PlacementTarget = placementTarget,
				Placement = PlacementMode.Pointer,
				Width = 10,
				Height = 10
			};
			((ISetLogicalParent) popup).SetParent(popup.PlacementTarget);

			var window = PreparedWindow(placementTarget);
			window.Show();
			popup.Open();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			// The target's initial placement is (395,295) which is a 10x10 panel centered in a 800x600 window
			CornerstoneTest.AreEqual(placementTarget.Bounds, new Rect(395D, 295D, 10, 10));

			var raised = false;
			if (popup.Host is PopupRoot popupRoot)
			{
				popupRoot.PositionChanged += (_, args) => { raised = true; };
			}
			else if (popup.Host is OverlayPopupHost overlayPopupHost)
			{
				overlayPopupHost.PropertyChanged += (_, args) =>
				{
					if ((args.Property == Canvas.TopProperty)
						|| (args.Property == Canvas.LeftProperty))
					{
						raised = true;
					}
				};
			}
			placementTarget.Margin = new Thickness(10, 0, 0, 0);
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			CornerstoneTest.IsFalse(raised);
		}
	}

	[PresentationTestMethod]
	public void PopupShouldNotFollowPlacementTargetOnWindowMoveIfPointer()
	{
		using (CreateServices())
		{
			var popup = new Popup
			{
				Width = 400,
				Height = 200,
				Placement = PlacementMode.Pointer
			};
			var window = PreparedWindow(popup);
			window.Show();
			popup.Open();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender, CancellationToken.None);

			var raised = false;
			if (popup.Host is PopupRoot popupRoot)
			{
				popupRoot.PositionChanged += (_, args) => { raised = true; };
			}
			else if (popup.Host is OverlayPopupHost overlayPopupHost)
			{
				overlayPopupHost.PropertyChanged += (_, args) =>
				{
					if ((args.Property == Canvas.TopProperty)
						|| (args.Property == Canvas.LeftProperty))
					{
						raised = true;
					}
				};
			}
			window.Position = new PixelPoint(10, 10);
			Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender, CancellationToken.None);
			CornerstoneTest.IsFalse(raised);
		}
	}

	[PresentationTestMethod]
	public void PopupShouldNotFollowPlacementTargetOnWindowResizeIfPointerIfPointer()
	{
		using (CreateServices())
		{
			var placementTarget = new Panel
			{
				Width = 10,
				Height = 10,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			var popup = new Popup
			{
				PlacementTarget = placementTarget,
				Placement = PlacementMode.Pointer,
				Width = 10,
				Height = 10
			};
			((ISetLogicalParent) popup).SetParent(popup.PlacementTarget);

			var window = PreparedWindow(placementTarget);
			window.Show();
			popup.Open();
			Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender, CancellationToken.None);

			// The target's initial placement is (395,295) which is a 10x10 panel centered in a 800x600 window
			CornerstoneTest.AreEqual(placementTarget.Bounds, new Rect(395D, 295D, 10, 10));

			var raised = false;
			if (popup.Host is PopupRoot popupRoot)
			{
				popupRoot.PositionChanged += (_, args) => { raised = true; };
			}
			else if (popup.Host is OverlayPopupHost overlayPopupHost)
			{
				overlayPopupHost.PropertyChanged += (_, args) =>
				{
					if ((args.Property == Canvas.TopProperty)
						|| (args.Property == Canvas.LeftProperty))
					{
						raised = true;
					}
				};
			}
			window.PlatformImpl?.Resize(new Size(700D, 500D), WindowResizeReason.Unspecified);
			Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender, CancellationToken.None);
			CornerstoneTest.IsFalse(raised);
		}
	}

	[PresentationTestMethod]
	public void PopupWithoutTopLevelShouldntCallOpen()
	{
		var openedEvent = 0;
		var target = new Popup();
		target.Opened += (s, a) => openedEvent++;
		target.IsOpen = true;

		CornerstoneTest.AreEqual(0, openedEvent);
	}

	[PresentationTestMethod]
	public void ProgClosePopupNoLightDismissDoesntMoveFocusToPlacementTarget()
	{
		using (CreateServicesWithFocus())
		{
			var window = PreparedWindow();

			var windowTB = new TextBox();
			window.Content = windowTB;

			var popupTB = new TextBox();
			var p = new Popup
			{
				PlacementTarget = window,
				IsLightDismissEnabled = false,
				Child = popupTB
			};
			((ISetLogicalParent) p).SetParent(p.PlacementTarget);
			window.Show();

			p.Open();

			if (p.Host is OverlayPopupHost host)
			{
				//Need to measure/arrange for visual children to show up
				//in OverlayPopupHost
				host.Measure(Size.Infinity);
				host.Arrange(new Rect(host.DesiredSize));
			}

			popupTB.Focus();

			windowTB.Focus();

			var focusManager = window.FocusManager;
			CornerstoneTest.IsNotNull(focusManager);
			var focus = focusManager.GetFocusedElement();

			CornerstoneTest.IsTrue(focus == windowTB);

			p.Close();

			CornerstoneTest.IsTrue(focus == windowTB);
		}
	}

	[PresentationTestMethod]
	public void SettingChildShouldFireLogicalChildrenCollectionChanged()
	{
		var target = new Popup();
		var child = new Control();
		var called = false;

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) =>
			called = e.Action == NotifyCollectionChangedAction.Add;

		target.Child = child;

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void SettingChildShouldNotSetChildsVisualParent()
	{
		var target = new Popup();
		var child = new Control();

		target.Child = child;

		CornerstoneTest.IsNull(child.VisualParent);
	}

	[PresentationTestMethod]
	public void SettingChildShouldSetChildControlsLogicalParent()
	{
		var target = new Popup();
		var child = new Control();

		target.Child = child;

		CornerstoneTest.AreEqual(child.Parent, target);
		CornerstoneTest.AreEqual(((ILogical) child).LogicalParent, target);
	}

	[PresentationTestMethod]
	public void ShouldCloseWhenControlDetaches()
	{
		using (CreateServices())
		{
			var button = new Button();
			var target = new Popup { Placement = PlacementMode.Pointer, PlacementTarget = button };
			var root = PreparedWindow(button);

			target.Open();

			CornerstoneTest.IsTrue(target.IsOpen);
			root.Content = null;
			CornerstoneTest.IsFalse(target.IsOpen);
		}
	}

	[PresentationTestMethod]
	public void ShouldNotOverwriteTemplatedParentOfItemInItemsControlWithPopupOnSecondOpen()
	{
		// Test uses OverlayPopupHost default template
		using (CreateServices())
		{
			PopupItemsControl target;
			var item = new Border();
			var root = PreparedWindow(target = new PopupItemsControl
			{
				Items = { item },
				Template = new FuncControlTemplate<PopupItemsControl>(PopupItemsControlTemplate)
			});
			root.Show();

			target.ApplyTemplate();

			var popup = (Popup) target.GetTemplateDescendants().First(x => x.Name == "popup");
			popup.Open();

			var popupRoot = (Control) popup.Host!;
			popupRoot.Measure(Size.Infinity);
			popupRoot.Arrange(new Rect(popupRoot.DesiredSize));

			CornerstoneTest.IsNull(item.TemplatedParent);

			popup.Close();
			popup.Open();

			CornerstoneTest.IsNull(item.TemplatedParent);
		}
	}

	private MockWindowingPlatform CreateMockWindowingPlatform()
	{
		return new MockWindowingPlatform(() =>
		{
			var mock = MockWindowingPlatform.CreateWindowMock();

			mock.CreatePopupHandler = () =>
			{
				if (UsePopupHost)
				{
					return null;
				}
				return CreatePopupMock(mock);
			};

			return mock;
		}, null);
	}

	private static PointerPressedEventArgs CreatePointerPressedEventArgs(Window source, Point p)
	{
		var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
		return new PointerPressedEventArgs(
			source,
			pointer,
			source,
			p,
			0,
			new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
			KeyModifiers.None);
	}

	private static IPopupImpl CreatePopupMock(IWindowBaseImpl parent)
	{
		var mock = MockWindowingPlatform.CreatePopupMock(parent);

		mock.CreatePopupHandler = () => CreatePopupMock(mock);

		return mock;
	}

	private static PopupRoot CreateRoot(TopLevel popupParent, IPopupImpl impl = null)
	{
		impl ??= popupParent.PlatformImpl!.CreatePopup()!;

		var result = new PopupRoot(popupParent, impl)
		{
			Template = new FuncControlTemplate<PopupRoot>((parent, scope) =>
				new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[!ContentPresenter.ContentProperty] = parent[!PopupRoot.ContentProperty]
				}.RegisterInNameScope(scope))
		};

		result.ApplyTemplate();

		return result;
	}

	private IDisposable CreateServices()
	{
		return UnitTestApplication.Start(TestServices.StyledWindow.With(
			windowingPlatform: CreateMockWindowingPlatform()));
	}

	private IDisposable CreateServicesWithFocus()
	{
		return UnitTestApplication.Start(TestServices.StyledWindow.With(
			windowingPlatform: CreateMockWindowingPlatform(),
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler()));
	}

	private static Control PopupContentControlTemplate(PopupContentControl control, INameScope scope)
	{
		return new Popup
		{
			Name = "popup",
			PlacementTarget = control,
			Child = new ContentPresenter
			{
				[~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty]
			}
		}.RegisterInNameScope(scope);
	}

	private static Control PopupItemsControlTemplate(PopupItemsControl control, INameScope scope)
	{
		return new Popup
		{
			Name = "popup",
			PlacementTarget = control,
			Child = new ItemsPresenter()
		}.RegisterInNameScope(scope);
	}

	private static Window PreparedWindow(object content = null)
	{
		var w = new Window { Content = content };
		w.Show();
		w.ApplyStyling();
		w.ApplyTemplate();
		return w;
	}

	#endregion

	#region Classes

	private class PopupContentControl : ContentControl
	{
	}

	private class PopupItemsControl : ItemsControl
	{
	}

	private class TestControl : Decorator
	{
		#region Properties

		public new PresentationObject InheritanceParent => base.InheritanceParent;

		#endregion

		#region Methods

		protected override void OnDataContextBeginUpdate()
		{
			DataContextBeginUpdate?.Invoke(this, EventArgs.Empty);
			base.OnDataContextBeginUpdate();
		}

		#endregion

		#region Events

		public event EventHandler DataContextBeginUpdate;

		#endregion
	}

	#endregion
}

[TestClass]
public class PopupTestsWithPopupRoot : PopupTests
{
	#region Constructors

	public PopupTestsWithPopupRoot()
	{
		UsePopupHost = true;
	}

	#endregion
}