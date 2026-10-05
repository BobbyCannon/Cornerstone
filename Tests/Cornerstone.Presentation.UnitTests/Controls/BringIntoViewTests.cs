#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class BringIntoViewTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BringIntoViewBeforeLayoutIsDeferredUntilEndOfLayoutPass()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var child = new Border { Width = 50, Height = 50 };
		var root = new TestRoot(child);

		var raised = 0;
		var targetRect = default(Rect);
		root.AddHandler(Control.RequestBringIntoViewEvent, (_, e) =>
		{
			++raised;
			targetRect = e.TargetRect;
		});

		child.BringIntoView();

		CornerstoneTest.AreEqual(0, raised);

		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(1, raised);
		CornerstoneTest.AreEqual(new Rect(0, 0, 50, 50), targetRect);
	}

	[PresentationTestMethod]
	public void BringIntoViewOnInvisibleControlIsIgnored()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var child = new Border { Width = 50, Height = 50, IsVisible = false };
		var root = new TestRoot(child);
		root.LayoutManager.ExecuteInitialLayoutPass();

		var raised = false;
		root.AddHandler(Control.RequestBringIntoViewEvent, (_, _) => raised = true);

		child.BringIntoView();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void BringIntoViewOnLaidOutControlRaisesRequestBringIntoViewSynchronously()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var child = new Border { Width = 50, Height = 50 };
		var root = new TestRoot(child);
		root.LayoutManager.ExecuteInitialLayoutPass();

		var raised = false;
		root.AddHandler(Control.RequestBringIntoViewEvent, (_, _) => raised = true);

		child.BringIntoView();

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void DeferredBringIntoViewIsAbandonedWhenControlBecomesInvisible()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var child = new Border { Width = 50, Height = 50 };
		var root = new TestRoot(child);

		var raised = false;
		root.AddHandler(Control.RequestBringIntoViewEvent, (_, _) => raised = true);

		child.BringIntoView();
		child.IsVisible = false;
		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.IsFalse(raised);
	}

	#endregion
}