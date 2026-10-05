#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TopLevelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddingResourceToApplicationShouldRaiseResourcesChanged()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);
			var target = new TestTopLevel(impl);
			var raised = false;

			target.ResourcesChanged += (_, __) => raised = true;
			Application.Current!.Resources.Add("foo", "bar");

			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void AddingTopLevelAsChildShouldThrowException()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);
			var target = new TestTopLevel(impl);
			var child = new TestTopLevel(impl);

			target.Template = CreateTemplate();
			target.Content = child;
			target.ApplyTemplate();
			Assert.Throws<InvalidOperationException>(() => target.Presenter!.ApplyTemplate());
		}
	}

	[PresentationTestMethod]
	public void BoundsShouldBeSetAfterLayoutPass()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl();

			var target = new TestTopLevel(impl)
			{
				IsVisible = true,
				Template = CreateTemplate(),
				Content = new TextBlock
				{
					Width = 321,
					Height = 432
				}
			};

			target.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual(new Rect(0, 0, 321, 432), target.Bounds);
		}
	}

	[PresentationTestMethod]
	public void ClientSizeShouldBeSetOnConstruction()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl();
			impl.ClientSize = new Size(123, 456);

			var target = new TestTopLevel(impl);

			CornerstoneTest.AreEqual(new Size(123, 456), target.ClientSize);
		}
	}

	[PresentationTestMethod]
	public void EmbeddableControlRootDisposeAfterImplCloseShouldRaiseClosedEventOnlyOnce()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);

			var raised = 0;
			var target = new EmbeddableControlRoot(impl);
			target.Closed += (s, e) => raised++;

			impl.Closed!();
			target.Dispose();

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void EmbeddableControlRootDisposeShouldDisposeImplBeforeTeardown()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);

			var closedRaised = false;
			var implDisposedBeforeClosed = false;
			impl.Disposing = () => implDisposedBeforeClosed = !closedRaised;

			var target = new EmbeddableControlRoot(impl);
			target.Closed += (s, e) => closedRaised = true;

			target.Dispose();

			impl.Calls.VerifyCalled("Dispose", 1);
			CornerstoneTest.IsTrue(implDisposedBeforeClosed);
			CornerstoneTest.IsTrue(closedRaised);
		}
	}

	[PresentationTestMethod]
	public void EmbeddableControlRootDisposeShouldRaiseClosedEvent()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);

			var raised = 0;
			var target = new EmbeddableControlRoot(impl);
			target.Closed += (s, e) => raised++;

			target.Dispose();

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void HeightShouldNotBeSetOnConstruction()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl();
			impl.ClientSize = new Size(123, 456);

			var target = new TestTopLevel(impl);

			CornerstoneTest.AreEqual(double.NaN, target.Height);
		}
	}

	[PresentationTestMethod]
	public void ImplCloseShouldCallRaiseClosedEvent()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);

			var raised = false;
			var target = new TestTopLevel(impl);
			target.Closed += (s, e) => raised = true;

			impl.Closed!();

			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void ImplCloseShouldRaiseClosedEventOnlyOnce()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);

			var raised = 0;
			var target = new TestTopLevel(impl);
			target.Closed += (s, e) => raised++;

			impl.Closed!();
			impl.Closed!();

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void ImplCloseShouldRaiseDetachedFromLogicalTreeEvent()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);

			var target = new TestTopLevel(impl);
			var raised = 0;

			target.DetachedFromLogicalTree += (s, e) =>
			{
				CornerstoneTest.Same(target, e.Root);
				CornerstoneTest.Same(target, e.Source);
				CornerstoneTest.IsNull(e.Parent);
				++raised;
			};

			impl.Closed!();

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void ImplInputShouldPassInputToInputManager()
	{
		var inputManagerMock = new StubInputManager();

		var services = TestServices.StyledWindow.With(inputManager: inputManagerMock);

		using (UnitTestApplication.Start(services))
		{
			var impl = CreateMockTopLevelImpl(true);

			var target = new TestTopLevel(impl);

			var input = new RawKeyEventArgs(
				new KeyboardDevice(),
				0,
				target.InputRoot,
				RawKeyEventType.KeyDown,
				Key.A,
				RawInputModifiers.None,
				PhysicalKey.A,
				"a");

			impl.Input!(input);

			inputManagerMock.Calls.VerifyLastPrefix("ProcessInput", input);
		}
	}

	[PresentationTestMethod]
	public void IsAttachedToLogicalTreeIsTrue()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl();
			var target = new TestTopLevel(impl);

			CornerstoneTest.IsTrue(((ILogical) target).IsAttachedToLogicalTree);
		}
	}

	[PresentationTestMethod]
	public void LayoutPassShouldNotBeAutomaticallyScheduled()
	{
		var services = TestServices.StyledWindow;

		using (UnitTestApplication.Start(services))
		{
			var impl = CreateMockTopLevelImpl();

			var target = new TestTopLevel(impl);

			// The layout pass should be scheduled by the derived class.
			CornerstoneTest.AreEqual(0, target.Measured);
			CornerstoneTest.AreEqual(0, target.Arranged);
		}
	}

	[PresentationTestMethod]
	public void ReactsToChangesInGlobalStyles()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl();

			var child = new Border { Classes = { "foo" } };
			var target = new TestTopLevel(impl)
			{
				Template = CreateTemplate(),
				Content = child
			};

			target.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual(new Thickness(0), child.BorderThickness);

			var style = new Style(x => x.OfType<Border>().Class("foo"))
			{
				Setters =
				{
					new Setter(Border.BorderThicknessProperty, new Thickness(2))
				}
			};

			var application = Application.Current!;
			application.Styles.Add(style);
			target.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual(new Thickness(2), child.BorderThickness);

			application.Styles.Remove(style);

			CornerstoneTest.AreEqual(new Thickness(0), child.BorderThickness);
		}
	}

	[PresentationTestMethod]
	public void TopLevelShouldUnfocusWhenImplFocusIsLost()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var impl = CreateMockTopLevelImpl(true);
			var content = new TextBox
			{
				Focusable = true
			};
			var target = new TestTopLevel(impl)
			{
				Template = CreateTemplate(),
				Focusable = true,
				Content = content
			};

			target.LayoutManager.ExecuteInitialLayoutPass();

			content.Focus();
			CornerstoneTest.IsTrue(content.IsFocused);

			impl.LostFocus?.Invoke();

			CornerstoneTest.IsFalse(content.IsFocused);
		}
	}

	[PresentationTestMethod]
	public void WidthAndHeightShouldBeSetAfterWindowResizeNotification()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl(true);
			impl.ClientSize = new Size(123, 456);

			// The user has resized the window, so we can no longer auto-size.
			var target = new TestTopLevel(impl);
			impl.Resized!(new Size(100, 200), WindowResizeReason.Unspecified);

			CornerstoneTest.AreEqual(100, target.Width);
			CornerstoneTest.AreEqual(200, target.Height);
		}
	}

	[PresentationTestMethod]
	public void WidthAndHeightShouldNotBeSetAfterLayoutPass()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl();
			impl.ClientSize = new Size(123, 456);

			var target = new TestTopLevel(impl);
			target.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(double.NaN, target.Width);
			CornerstoneTest.AreEqual(double.NaN, target.Height);
		}
	}

	[PresentationTestMethod]
	public void WidthShouldNotBeSetOnConstruction()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockTopLevelImpl();
			impl.ClientSize = new Size(123, 456);

			var target = new TestTopLevel(impl);

			CornerstoneTest.AreEqual(double.NaN, target.Width);
		}
	}

	[PresentationTestMethod]
	public void XButton1DownShouldRaiseBackRequested()
	{
		// Regression test: prior to this fix, the PreProcess subscription compared
		// e.Root against 'this' (the TopLevel/Window), but e.Root is set to the
		// PresentationSource (the IInputRoot), not the Window itself. The comparison
		// always failed so BackRequested was never raised for XButton1Down.
		var services = TestServices.StyledWindow.With(inputManager: new InputManager());

		using (UnitTestApplication.Start(services))
		{
			var impl = CreateMockTopLevelImpl(true);
			var target = new TestTopLevel(impl);

			var raised = false;
			target.BackRequested += (_, _) => raised = true;

			var mouseDevice = new MouseDevice(new Pointer(0, PointerType.Mouse, true));
			impl.Input!(new RawPointerEventArgs(
				mouseDevice,
				0,
				target.InputRoot,
				RawPointerEventType.XButton1Down,
				new RawPointerPoint { Position = default },
				RawInputModifiers.None));

			CornerstoneTest.IsTrue(raised);
		}
	}

	private static StubWindowImpl CreateMockTopLevelImpl(bool setupProperties = false)
	{
		var topLevel = new StubWindowImpl();
		if (setupProperties)
		{
		}
		return topLevel;
	}

	private static FuncControlTemplate<TestTopLevel> CreateTemplate()
	{
		return new FuncControlTemplate<TestTopLevel>((x, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
			}.RegisterInNameScope(scope));
	}

	#endregion

	#region Classes

	private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
	{
		#region Fields

		public int Measured, Arranged;

		#endregion

		#region Methods

		protected override void ArrangeCore(Rect finalRect)
		{
			Arranged++;
			base.ArrangeCore(finalRect);
		}

		protected override Size MeasureCore(Size availableSize)
		{
			Measured++;
			return base.MeasureCore(availableSize);
		}

		#endregion
	}

	#endregion
}