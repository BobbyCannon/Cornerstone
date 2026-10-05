#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class WindowBaseTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ActivateShouldCallImplActivate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockWindowBaseImpl();
			var target = new TestWindowBase(impl);

			target.Activate();

			impl.Calls.VerifyCalled("Activate");
		}
	}

	[PresentationTestMethod]
	public void ActiveWindowShouldBeDeactivatedWhenImplSignalsClose()
	{
		var windowImpl = new StubWindowImpl();

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase(windowImpl);
			var deactivated = 0;
			target.Deactivated += (s, e) => deactivated++;

			target.Show();
			windowImpl.Activated!();
			CornerstoneTest.IsTrue(target.IsActive);

			// Some backends (e.g. X11) never deliver a deactivation notification on close.
			windowImpl.Closed!();

			CornerstoneTest.IsFalse(target.IsActive);
			CornerstoneTest.AreEqual(1, deactivated);
		}
	}

	[PresentationTestMethod]
	public void HidingShouldStopRenderer()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase();

			target.Show();
			target.Hide();

			CornerstoneTest.IsFalse(MediaContext.Instance.IsTopLevelActive(target));
		}
	}

	[PresentationTestMethod]
	public void ImplActivateShouldCallRaiseActivatedEvent()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockWindowBaseImpl(true);

			var raised = false;
			var target = new TestWindowBase(impl);
			target.Activated += (s, e) => raised = true;

			impl.Activated!();

			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void ImplDeactivateShouldCallRaiseDeativatedEvent()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var impl = CreateMockWindowBaseImpl(true);

			var raised = false;
			var target = new TestWindowBase(impl);
			target.Deactivated += (s, e) => raised = true;

			impl.Deactivated!();

			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void InactiveWindowShouldNotRaiseDeactivatedWhenImplSignalsClose()
	{
		var windowImpl = new StubWindowImpl();

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase(windowImpl);
			var deactivated = 0;
			target.Deactivated += (s, e) => deactivated++;

			target.Show();
			CornerstoneTest.IsFalse(target.IsActive);

			// Backends that deactivate before closing must not produce a duplicate event.
			windowImpl.Closed!();

			CornerstoneTest.IsFalse(target.IsActive);
			CornerstoneTest.AreEqual(0, deactivated);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldBeFalseAtferHide()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase();

			target.Show();
			target.Hide();

			CornerstoneTest.IsFalse(target.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldBeFalseAtferImplSignalsClose()
	{
		var windowImpl = new StubWindowImpl();

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase(windowImpl);

			target.Show();
			windowImpl.Closed!();

			CornerstoneTest.IsFalse(target.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldBeTrueAfterShow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase();

			target.Show();

			CornerstoneTest.IsTrue(target.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void IsVisibleShouldInitiallyBeFalse()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var target = new TestWindowBase();

			CornerstoneTest.IsFalse(target.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void RendererShouldBeDisposedWhenImplSignalsClose()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var windowImpl = new StubWindowImpl();

			var target = new TestWindowBase(windowImpl);

			target.Show();
			windowImpl.Closed!();
			CornerstoneTest.IsTrue(target.Renderer.IsDisposed);
		}
	}

	[PresentationTestMethod]
	public void SettingIsVisibleFalseHidesWindow()
	{
		var windowImpl = new StubWindowImpl();

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase(windowImpl);
			target.Show();
			target.IsVisible = false;

			windowImpl.Calls.VerifyCalled("Hide");
		}
	}

	[PresentationTestMethod]
	public void SettingIsVisibleTrueShowsWindow()
	{
		var windowImpl = new StubWindowImpl();

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase(windowImpl);
			target.IsVisible = true;

			windowImpl.Calls.VerifyLastPrefix("Show", true, false);
		}
	}

	[PresentationTestMethod]
	public void ShowingShouldRaiseOpened()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase();
			var raised = false;

			target.Opened += (s, e) => raised = true;

			target.Show();

			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void ShowingShouldStartRenderer()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TestWindowBase();

			target.Show();

			CornerstoneTest.IsTrue(MediaContext.Instance.IsTopLevelActive(target));
		}
	}

	private static StubWindowImpl CreateMockWindowBaseImpl(bool setupAllProperties = false)
	{
		var renderer = new StubWindowImpl();
		if (setupAllProperties)
		{
		}
		return renderer;
	}

	private static FuncControlTemplate<TestWindowBase> CreateTemplate()
	{
		return new FuncControlTemplate<TestWindowBase>((x, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
			}.RegisterInNameScope(scope));
	}

	#endregion

	#region Classes

	private class TestWindowBase : WindowBase
	{
		#region Constructors

		public TestWindowBase()
			: base(CreateMockWindowBaseImpl())
		{
		}

		public TestWindowBase(IWindowBaseImpl impl)
			: base(impl)
		{
		}

		#endregion

		#region Properties

		public bool IsClosed { get; private set; }

		#endregion
	}

	#endregion
}