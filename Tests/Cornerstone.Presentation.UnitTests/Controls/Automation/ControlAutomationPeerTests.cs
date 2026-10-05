#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class ControlAutomationPeerTests
{
	#region Methods

	private static AutomationPeer CreatePeer(Control control)
	{
		return ControlAutomationPeer.CreatePeerForElement(control);
	}

	#endregion

	#region Classes

	[TestClass]
	public class AutomationIdNotifications : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void AutomationIdOverridesNameFallbackInNotification()
		{
			// Name="fallback" is set before styles apply (the only valid window),
			// then AutomationId is set at runtime and the notification should reflect
			// the explicit AutomationId, not the Name fallback.
			var button = new Button { Name = "fallback" };
			var peer = CreatePeer(button);
			var raised = new List<AutomationPropertyChangedEventArgs>();
			peer.PropertyChanged += (_, e) => raised.Add(e);

			AutomationProperties.SetAutomationId(button, "explicit");

			CornerstoneTest.Single(raised);
			CornerstoneTest.Same(AutomationElementIdentifiers.AutomationIdProperty, raised[0].Property);
			CornerstoneTest.AreEqual("explicit", raised[0].NewValue);
		}

		[PresentationTestMethod]
		public void RaisesPropertyChangedWhenAutomationIdSet()
		{
			var button = new Button();
			var peer = CreatePeer(button);
			var raised = new List<AutomationPropertyChangedEventArgs>();
			peer.PropertyChanged += (_, e) => raised.Add(e);

			AutomationProperties.SetAutomationId(button, "btn1");

			CornerstoneTest.Single(raised);
			CornerstoneTest.Same(AutomationElementIdentifiers.AutomationIdProperty, raised[0].Property);
			CornerstoneTest.AreEqual("btn1", raised[0].NewValue);
		}

		#endregion
	}

	[TestClass]
	public class Children : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CreatesChildrenForControlsInVisualTree()
		{
			var panel = new Panel
			{
				Children =
				{
					new Border(),
					new Border()
				}
			};

			var target = CreatePeer(panel);

			CornerstoneTest.AreEqual(panel.GetVisualChildren(), target.GetChildren().Cast<ControlAutomationPeer>().Select(x => x.Owner));
		}

		[PresentationTestMethod]
		public void CreatesChildrenwhenControlsAttachedToVisualTree()
		{
			var contentControl = new ContentControl
			{
				Template = new FuncControlTemplate<ContentControl>((o, _) =>
					new ContentPresenter
					{
						Name = "PART_ContentPresenter",
						[!ContentPresenter.ContentProperty] = o[!ContentControl.ContentProperty]
					}),
				Content = new Border()
			};

			var target = CreatePeer(contentControl);

			CornerstoneTest.Empty(target.GetChildren());

			contentControl.Measure(Size.Infinity);

			CornerstoneTest.AreEqual(1, target.GetChildren().Count);
		}

		[PresentationTestMethod]
		public void UpdatesChildrenWhenVisibilityChangesFromInvisibleToVisible()
		{
			var panel = new Panel
			{
				Children =
				{
					new Border(),
					new Border { IsVisible = false }
				}
			};

			var target = CreatePeer(panel);
			var children = target.GetChildren();
			CornerstoneTest.AreEqual(1, children.Count);

			panel.Children[1].IsVisible = true;
			children = target.GetChildren();
			CornerstoneTest.AreEqual(2, children.Count);
		}

		[PresentationTestMethod]
		public void UpdatesChildrenWhenVisibilityChangesFromVisibleToInvisible()
		{
			var panel = new Panel
			{
				Children =
				{
					new Border(),
					new Border()
				}
			};

			var target = CreatePeer(panel);
			var children = target.GetChildren();

			CornerstoneTest.AreEqual(2, children.Count);

			panel.Children[1].IsVisible = false;
			children = target.GetChildren();
			CornerstoneTest.AreEqual(1, children.Count);

			panel.Children[1].IsVisible = true;
			children = target.GetChildren();
			CornerstoneTest.AreEqual(2, children.Count);
		}

		[PresentationTestMethod]
		public void UpdatesChildrenWhenVisualChildrenAdded()
		{
			var panel = new Panel
			{
				Children =
				{
					new Border(),
					new Border()
				}
			};

			var target = CreatePeer(panel);
			var children = target.GetChildren();

			CornerstoneTest.AreEqual(2, children.Count);

			panel.Children.Add(new Decorator());

			children = target.GetChildren();
			CornerstoneTest.AreEqual(3, children.Count);
		}

		[PresentationTestMethod]
		public void UpdatesChildrenWhenVisualChildrenRemoved()
		{
			var panel = new Panel
			{
				Children =
				{
					new Border(),
					new Border()
				}
			};

			var target = CreatePeer(panel);
			var children = target.GetChildren();

			CornerstoneTest.AreEqual(2, children.Count);

			panel.Children.RemoveAt(1);

			children = target.GetChildren();
			CornerstoneTest.AreEqual(1, children.Count);
		}

		#endregion
	}

	[TestClass]
	public class Parent : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ConnectsPeerToTreeWhenGetParentCalled()
		{
			var border = new Border();
			var tree = new Decorator
			{
				Child = new Decorator
				{
					Child = border
				}
			};

			// We're accessing Border directly without going via its ancestors. Because the tree
			// is built lazily, ensure that calling GetParent causes the ancestor tree to be built.
			var target = CreatePeer(border);

			var parentPeer = CornerstoneTest.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
			CornerstoneTest.Same(border.GetVisualParent(), parentPeer.Owner);
		}

		[PresentationTestMethod]
		public void ParentUpdatedWhenMovedToSeparateVisualTree()
		{
			var border = new Border();
			var root1 = new Decorator { Child = border };
			var root2 = new Decorator();
			var target = CreatePeer(border);

			var parentPeer = CornerstoneTest.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
			CornerstoneTest.Same(root1, parentPeer.Owner);

			root1.Child = null;

			CornerstoneTest.IsNull(target.GetParent());

			root2.Child = border;

			parentPeer = CornerstoneTest.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
			CornerstoneTest.Same(root2, parentPeer.Owner);
		}

		#endregion
	}

	[TestClass]
	public class ToScreen : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ConvertsRectWhenAttachedToRootedTree()
		{
			var border = new Border();
			var root = new TestRoot(border);
			var peer = CreatePeer(border);

			CornerstoneTest.AreEqual(new Rect(10, 20, 30, 40), peer.ToScreen(new Rect(10, 20, 30, 40)));
		}

		[PresentationTestMethod]
		public void ReturnsNullWhenDetached()
		{
			var border = new Border();
			var root = new TestRoot(border);
			var peer = CreatePeer(border);

			root.Child = null;

			CornerstoneTest.IsNull(peer.ToScreen(new Rect(10, 20, 30, 40)));
		}

		#endregion
	}

	#endregion
}