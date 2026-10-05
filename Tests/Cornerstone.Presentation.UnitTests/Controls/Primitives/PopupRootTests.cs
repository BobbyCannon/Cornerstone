#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Primitives.PopupPositioning;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class PopupRootTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AttachingPopupRootToParentLogicalTreeRaisesDetachedFromLogicalTreeAndAttachedToLogicalTree()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var child = new Decorator();
			var window = new Window();
			var target = CreateTarget(window);
			var detachedCount = 0;
			var attachedCount = 0;

			target.Content = child;

			target.DetachedFromLogicalTree += (s, e) => ++detachedCount;
			child.DetachedFromLogicalTree += (s, e) => ++detachedCount;
			target.AttachedToLogicalTree += (s, e) => ++attachedCount;
			child.AttachedToLogicalTree += (s, e) => ++attachedCount;

			((ISetLogicalParent) target).SetParent(window);

			CornerstoneTest.AreEqual(2, detachedCount);
			CornerstoneTest.AreEqual(2, attachedCount);
		}
	}

	[PresentationTestMethod]
	public void ChildShouldBeMeasuredWithMaxAutoSizeHint()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var child = new ChildControl();
			var window = new Window();
			var popupImpl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl!);
			popupImpl.MaxAutoSizeHint = new Size(1200, 1000);
			var target = CreateTarget(window, popupImpl);

			target.Content = child;
			target.Show();

			CornerstoneTest.AreEqual(1, child.MeasureSizes.Count);
			CornerstoneTest.AreEqual(new Size(1200, 1000), child.MeasureSizes[0]);
		}
	}

	[PresentationTestMethod]
	public void ChildShouldBeMeasuredWithMaxWidthMaxHeightWhenSet()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var child = new ChildControl();
			var window = new Window();
			var target = CreateTarget(window);

			target.MaxWidth = 500;
			target.MaxHeight = 600;
			target.Content = child;
			target.Show();

			CornerstoneTest.AreEqual(1, child.MeasureSizes.Count);
			CornerstoneTest.AreEqual(new Size(500, 600), child.MeasureSizes[0]);
		}
	}

	[PresentationTestMethod]
	public void ChildShouldBeMeasuredWithWidthHeightWhenSet()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var child = new ChildControl();
			var window = new Window();
			var target = CreateTarget(window);

			target.Width = 500;
			target.Height = 600;
			target.Content = child;
			target.Show();

			CornerstoneTest.AreEqual(1, child.MeasureSizes.Count);
			CornerstoneTest.AreEqual(new Size(500, 600), child.MeasureSizes[0]);
		}
	}

	[PresentationTestMethod]
	public void ClearingContentOfPopupInControlTemplateDoesntCrash()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var target = new TemplatedControlWithPopup
			{
				PopupContent = new Canvas()
			};
			window.Content = target;

			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			target.ApplyTemplate();
			target.Popup!.Open();
			target.PopupContent = null;
		}
	}

	[PresentationTestMethod]
	public void DetachingPopupRootFromParentLogicalTreeRaisesDetachedFromLogicalTreeAndAttachedToLogicalTree()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var child = new Decorator();
			var window = new Window();
			var target = CreateTarget(window);
			var detachedCount = 0;
			var attachedCount = 0;

			target.Content = child;
			((ISetLogicalParent) target).SetParent(window);

			target.DetachedFromLogicalTree += (s, e) => ++detachedCount;
			child.DetachedFromLogicalTree += (s, e) => ++detachedCount;
			target.AttachedToLogicalTree += (s, e) => ++attachedCount;
			child.AttachedToLogicalTree += (s, e) => ++attachedCount;

			((ISetLogicalParent) target).SetParent(null);

			// Despite being detached from the parent logical tree, we're still attached to a
			// logical tree as PopupRoot itself is a logical tree root.
			CornerstoneTest.IsTrue(((ILogical) target).IsAttachedToLogicalTree);
			CornerstoneTest.IsTrue(((ILogical) child).IsAttachedToLogicalTree);
			CornerstoneTest.AreEqual(2, detachedCount);
			CornerstoneTest.AreEqual(2, attachedCount);
		}
	}

	[PresentationTestMethod]
	public void MinWidthMinHeightShouldBeRespected()
	{
		// Issue #3796
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var popupImpl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl!);

			var target = CreateTarget(window, popupImpl);
			target.MinWidth = 400;
			target.MinHeight = 800;
			target.Content = new Border
			{
				Width = 100,
				Height = 100
			};

			((IPopupHost) target).ConfigurePosition(new PopupPositionRequest(window, PlacementMode.Top));
			target.Show();

			CornerstoneTest.AreEqual(new Rect(0, 0, 400, 800), target.Bounds);
			CornerstoneTest.AreEqual(new Size(400, 800), target.ClientSize);
			CornerstoneTest.AreEqual(new Size(400, 800), target.PlatformImpl!.ClientSize);
		}
	}

	[PresentationTestMethod]
	public void PopupAnchorRectShouldAccountForDecorationInset()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var windowImpl = MockWindowingPlatform.CreateWindowMock();
			windowImpl.NeedsManagedDecorations = true;
			windowImpl.RequestedDrawnDecorations =
				PlatformRequestedDrawnDecoration.TitleBar | PlatformRequestedDrawnDecoration.Border;

			var placementTarget = new Panel
			{
				Width = 10,
				Height = 10,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};

			var window = new Window(windowImpl)
			{
				SizeToContent = SizeToContent.Manual,
				Content = placementTarget
			};
			window.Show();

			var inset = window.TopLevelHost.DecorationInset;
			CornerstoneTest.IsTrue(inset.Top > 0, "Expected non-zero decoration inset top (title bar)");

			var popupImpl = MockWindowingPlatform.CreatePopupMock(windowImpl);
			var target = CreateTarget(window, popupImpl);
			target.Width = 10;
			target.Height = 10;

			((IPopupHost) target).ConfigurePosition(
				new PopupPositionRequest(placementTarget, PlacementMode.Bottom));
			target.Show();

			// The popup should be placed below the placement target.
			var popupScreenPos = popupImpl.Position;
			CornerstoneTest.IsTrue(popupScreenPos.Y >= (int) (inset.Top + placementTarget.Bounds.Height), $"Popup Y ({popupScreenPos.Y}) should be >= inset.Top ({inset.Top}) + target height ({placementTarget.Bounds.Height}). " +
				$"If this fails, CalculateAnchorRect is not accounting for the decoration offset.");
		}
	}

	[PresentationTestMethod]
	public void PopupRootForwardsInitialIsHitTestVisibleToImpl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = MockWindowingPlatform.CreatePopupMock(new StubWindowImpl());

			CreateTarget(new Window(), impl);

			impl.Calls.VerifyLastPrefix("SetHitTestVisible", true);
		}
	}

	[PresentationTestMethod]
	public void PopupRootForwardsIsHitTestVisibleChangesToImpl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = MockWindowingPlatform.CreatePopupMock(new StubWindowImpl());
			var target = CreateTarget(new Window(), impl);

			target.IsHitTestVisible = false;

			impl.Calls.VerifyLastPrefix("SetHitTestVisible", false);
		}
	}

	[PresentationTestMethod]
	public void PopupRootIsAttachedToLogicalTreeIsTrue()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = CreateTarget(new Window());

			CornerstoneTest.IsTrue(((ILogical) target).IsAttachedToLogicalTree);
		}
	}

	[PresentationTestMethod]
	public void PopupRootShouldHaveTemplateApplied()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var target = new Popup { Placement = PlacementMode.Pointer };
			var child = new Control();

			window.Content = target;
			window.ApplyTemplate();
			target.Open();

			var host = CornerstoneTest.IsAssignableFrom<Visual>(target.Host);
			CornerstoneTest.Single(host.GetVisualChildren());

			var templatedChild = host.GetVisualChildren().Single();

			CornerstoneTest.IsType<LayoutTransformControl>(templatedChild);

			var panel = templatedChild.GetVisualChildren().Single();

			CornerstoneTest.IsType<Panel>(panel);

			var visualLayerManager = panel.GetVisualChildren().Skip(1).Single();

			CornerstoneTest.IsType<VisualLayerManager>(visualLayerManager);

			var contentPresenter = visualLayerManager.VisualChildren.Single();
			CornerstoneTest.IsType<ContentPresenter>(contentPresenter);

			CornerstoneTest.AreEqual((PopupRoot) host, ((Control) templatedChild).TemplatedParent);
			CornerstoneTest.AreEqual((PopupRoot) host, ((Control) contentPresenter).TemplatedParent);
		}
	}

	[PresentationTestMethod]
	public void PopupRootShouldHaveTopLevelHostVisualParent()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new Popup { PlacementTarget = new Window() };

			target.Open();

			CornerstoneTest.IsType<TopLevelHost>(((Visual) target.Host!).GetVisualParent());
		}
	}

	[PresentationTestMethod]
	public void PopupRootStylingParentIsPopup()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var target = new TemplatedControlWithPopup
			{
				PopupContent = new Canvas()
			};
			window.Content = target;

			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			target.ApplyTemplate();
			target.Popup!.Open();

			CornerstoneTest.AreEqual(target.Popup, ((IStyleHost) target.Popup.Host!).StylingParent);
		}
	}

	[PresentationTestMethod]
	public void SettingWidthShouldResizeWindowImpl()
	{
		// Issue #3796
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var popupImpl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl!);
			var positioner = new StubPopupPositioner();
			popupImpl.PopupPositioner = positioner;

			var target = CreateTarget(window, popupImpl);
			target.Width = 400;
			target.Height = 800;

			((IPopupHost) target).ConfigurePosition(new PopupPositionRequest(window, PlacementMode.Top));
			target.Show();

			CornerstoneTest.AreEqual(400, target.Width);
			CornerstoneTest.AreEqual(800, target.Height);

			target.Width = 410;
			target.LayoutManager.ExecuteLayoutPass();

			var widths = positioner.Calls.Arguments("Update");
			CornerstoneTest.IsTrue(widths.Any(a => ((PopupPositionerParameters) a[0]).Size.Width == 410));
			CornerstoneTest.AreEqual(410, target.Width);
		}
	}

	[PresentationTestMethod]
	public void ShouldNotHaveOffsetOnBoundsWhenContentLargerThanMaxWindowSize()
	{
		// Issue #3784.
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var popupImpl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl!);

			var child = new Canvas
			{
				Width = 400,
				Height = 1344
			};

			var target = CreateTarget(window, popupImpl);
			target.Content = child;

			((IPopupHost) target).ConfigurePosition(new PopupPositionRequest(window, PlacementMode.Top));
			target.Show();

			CornerstoneTest.AreEqual(new Size(400, 1024), target.Bounds.Size);

			// Issue #3784 causes this to be (0, 160) which makes no sense as Window has no
			// parent control to be offset against.
			CornerstoneTest.AreEqual(new Point(0, 0), target.Bounds.Position);
		}
	}

	[PresentationTestMethod]
	public void TemplatedChildIsAttachedToLogicalTreeIsTrue()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = CreateTarget(new Window());

			CornerstoneTest.IsTrue(((ILogical) target.Presenter!).IsAttachedToLogicalTree);
		}
	}

	private static PopupRoot CreateTarget(TopLevel popupParent, IPopupImpl impl = null)
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

	#endregion

	#region Classes

	private class ChildControl : Control
	{
		#region Properties

		public List<Size> MeasureSizes { get; } = new();

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureSizes.Add(availableSize);
			return base.MeasureOverride(availableSize);
		}

		#endregion
	}

	private class TemplatedControlWithPopup : TemplatedControl
	{
		#region Fields

		public static readonly StyledProperty<Control> PopupContentProperty =
			PresentationProperty.Register<TemplatedControlWithPopup, Control>(nameof(PopupContent));

		#endregion

		#region Constructors

		public TemplatedControlWithPopup()
		{
			Template = new FuncControlTemplate<TemplatedControlWithPopup>((parent, _) =>
				new Popup
				{
					[!Popup.ChildProperty] = parent[!PopupContentProperty],
					PlacementTarget = parent
				});
		}

		#endregion

		#region Properties

		public Popup Popup { get; private set; }

		public Control PopupContent
		{
			get => GetValue(PopupContentProperty);
			set => SetValue(PopupContentProperty, value);
		}

		#endregion

		#region Methods

		protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
		{
			Popup = (Popup) this.GetVisualChildren().Single();
		}

		#endregion
	}

	#endregion
}