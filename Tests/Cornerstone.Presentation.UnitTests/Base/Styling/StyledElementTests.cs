#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class StyledElementTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddingElementWithNullParentToLogicalTreeShouldThrow()
	{
		var target = new Border();
		var visualParent = new Panel();
		var logicalParent = new Panel();
		var root = new TestRoot();

		// Set the logical parent...
		((ISetLogicalParent) target).SetParent(logicalParent);

		// ...so that when it's added to `visualParent`, the parent won't be set again.
		visualParent.Children.Add(target);

		// Clear the logical parent. It's now a logical child of `visualParent` but doesn't have
		// a logical parent itself.
		((ISetLogicalParent) target).SetParent(null);

		// In this case, attaching the control to a logical tree should throw.
		logicalParent.Children.Add(visualParent);
		Assert.Throws<InvalidOperationException>(() => root.Child = logicalParent);
	}

	[PresentationTestMethod]
	public void AddingToLogicalTreeRaisesResourcesChanged()
	{
		var target = new TestRoot();
		var parent = new Decorator { Resources = { { "foo", "bar" } } };
		var raised = 0;

		target.ResourcesChanged += (s, e) => ++raised;

		parent.Child = target;

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void AddingTreeToRootShouldStyleControls()
	{
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.Is<Control>())
				{
					Setters = { new Setter(Control.TagProperty, "foo") }
				}
			}
		};

		var grandchild = new Control();
		var child = new Border { Child = grandchild };
		var parent = new Border { Child = child };

		CornerstoneTest.IsNull(parent.Tag);
		CornerstoneTest.IsNull(child.Tag);
		CornerstoneTest.IsNull(grandchild.Tag);

		root.Child = parent;

		CornerstoneTest.AreEqual("foo", parent.Tag);
		CornerstoneTest.AreEqual("foo", child.Tag);
		CornerstoneTest.AreEqual("foo", grandchild.Tag);
	}

	[PresentationTestMethod]
	public void AssignedResourcesParentIsSet()
	{
		var resources = new StubResourceDictionary();
		var target = new TestControl { Resources = resources };

		resources.Calls.VerifyCalled("AddOwner");
	}

	[PresentationTestMethod]
	public void AssigningResourcesRaisesResourcesChanged()
	{
		var resources = new ResourceDictionary { { "foo", "bar" } };
		var target = new TestControl();
		var raised = 0;

		target.ResourcesChanged += (s, e) => ++raised;
		target.Resources = resources;

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void AttachedToLogicalTreeShouldBeCalledBeforeParentChangeSignalled()
	{
		var root = new TestRoot();
		var child = new Border();
		var raised = new List<string>();

		child.AttachedToLogicalTree += (s, e) =>
		{
			CornerstoneTest.AreEqual(root, child.Parent);
			raised.Add("attached");
		};

		child.GetObservable(StyledElement.ParentProperty).Skip(1).Subscribe(_ => raised.Add("parent"));

		root.Child = child;

		CornerstoneTest.AreEqual(new[] { "attached", "parent" }, raised);
	}

	[PresentationTestMethod]
	public void AttachedToLogicalTreeShouldBeCalledWhenAddedToTree()
	{
		var root = new TestRoot();
		var parent = new Border();
		var child = new Border();
		var grandchild = new Border();
		var parentRaised = false;
		var childRaised = false;
		var grandchildRaised = false;

		parent.AttachedToLogicalTree += (s, e) => parentRaised = true;
		child.AttachedToLogicalTree += (s, e) => childRaised = true;
		grandchild.AttachedToLogicalTree += (s, e) => grandchildRaised = true;

		parent.Child = child;
		child.Child = grandchild;

		CornerstoneTest.IsFalse(parentRaised);
		CornerstoneTest.IsFalse(childRaised);
		CornerstoneTest.IsFalse(grandchildRaised);

		root.Child = parent;

		CornerstoneTest.IsTrue(parentRaised);
		CornerstoneTest.IsTrue(childRaised);
		CornerstoneTest.IsTrue(grandchildRaised);
	}

	[PresentationTestMethod]
	public void AttachedToLogicalTreeShouldHaveParentSet()
	{
		var root = new TestRoot();
		var canvas = new Canvas();
		var border = new Border { Child = canvas };
		var raised = 0;

		void Attached(object sender, LogicalTreeAttachmentEventArgs e)
		{
			CornerstoneTest.Same(root, e.Parent);
			++raised;
		}

		border.AttachedToLogicalTree += Attached;
		canvas.AttachedToLogicalTree += Attached;

		root.Child = border;

		CornerstoneTest.AreEqual(2, raised);
	}

	[PresentationTestMethod]
	public void AttachedToLogicalTreeShouldHaveSourceSet()
	{
		var root = new TestRoot();
		var canvas = new Canvas();
		var border = new Border { Child = canvas };
		var raised = 0;

		void Attached(object sender, LogicalTreeAttachmentEventArgs e)
		{
			CornerstoneTest.Same(border, e.Source);
			++raised;
		}

		border.AttachedToLogicalTree += Attached;
		canvas.AttachedToLogicalTree += Attached;

		root.Child = border;

		CornerstoneTest.AreEqual(2, raised);
	}

	[PresentationTestMethod]
	public void AttachedToLogicalTreeShouldNotBeCalledWithGlobalStylesAsRoot()
	{
		var globalStyles = new StubGlobalStyles();
		var root = new TestRoot { StylingParent = globalStyles };
		var child = new Border();
		var raised = false;

		child.AttachedToLogicalTree += (s, e) =>
		{
			CornerstoneTest.AreEqual(root, e.Root);
			raised = true;
		};

		root.Child = child;

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void AttachingToVisualTreeShouldRaiseInitialized()
	{
		var root = new TestRoot();
		var target = new Border();
		var called = false;

		target.Initialized += (s, e) => called = true;
		root.Child = target;

		CornerstoneTest.IsTrue(called);
		CornerstoneTest.IsTrue(target.IsInitialized);
	}

	[PresentationTestMethod]
	public void ClassesShouldInitiallyBeEmpty()
	{
		var target = new StyledElement();

		CornerstoneTest.Empty(target.Classes);
	}

	[PresentationTestMethod]
	public void DataContextChangedShouldBeCalled()
	{
		var root = new TestStackPanel
		{
			Name = "root",
			Children =
			{
				new TestControl
				{
					Name = "a1",
					Child = new TestControl
					{
						Name = "b1"
					}
				},
				new TestControl
				{
					Name = "a2",
					DataContext = "foo"
				}
			}
		};

		var called = new List<string>();

		void Record(object sender, EventArgs e)
		{
			called.Add(((StyledElement) sender!).Name);
		}

		root.DataContextChanged += Record;

		foreach (TestControl c in root.GetLogicalDescendants())
		{
			c.DataContextChanged += Record;
		}

		root.DataContext = "foo";

		CornerstoneTest.AreEqual(new[] { "root", "a1", "b1" }, called);
	}

	[PresentationTestMethod]
	public void DataContextNotificationsShouldBeCalledInCorrectOrder()
	{
		var root = new TestStackPanel
		{
			Name = "root",
			Children =
			{
				new TestControl
				{
					Name = "a1",
					Child = new TestControl
					{
						Name = "b1"
					}
				},
				new TestControl
				{
					Name = "a2",
					DataContext = "foo"
				}
			}
		};

		var called = new List<string>();

		foreach (IDataContextEvents c in root.GetSelfAndLogicalDescendants())
		{
			c.DataContextBeginUpdate += (s, e) => called.Add("begin " + ((StyledElement) s!).Name);
			c.DataContextChanged += (s, e) => called.Add("changed " + ((StyledElement) s!).Name);
			c.DataContextEndUpdate += (s, e) => called.Add("end " + ((StyledElement) s!).Name);
		}

		root.DataContext = "foo";

		CornerstoneTest.AreEqual(new[]
		{
			"begin root",
			"begin a1",
			"begin b1",
			"changed root",
			"changed a1",
			"changed b1",
			"end b1",
			"end a1",
			"end root"
		}, called);
	}

	[PresentationTestMethod]
	public void DataContextNotificationsShouldBeCalledInCorrectOrderWhenSettingParent()
	{
		var root = new TestStackPanel
		{
			Name = "root",
			DataContext = "foo"
		};

		var children = new[]
		{
			new TestControl
			{
				Name = "a1",
				Child = new TestControl
				{
					Name = "b1"
				}
			},
			new TestControl
			{
				Name = "a2",
				DataContext = "foo"
			}
		};

		var called = new List<string>();

		foreach (var c in new[] { children[0], (IDataContextEvents) children[0].Child!, children[1] })
		{
			c.DataContextBeginUpdate += (s, e) => called.Add("begin " + ((StyledElement) s!).Name);
			c.DataContextChanged += (s, e) => called.Add("changed " + ((StyledElement) s!).Name);
			c.DataContextEndUpdate += (s, e) => called.Add("end " + ((StyledElement) s!).Name);
		}

		root.Children.AddRange(children);

		CornerstoneTest.AreEqual(new[]
		{
			"begin a1",
			"begin b1",
			"changed a1",
			"changed b1",
			"end b1",
			"end a1"
		}, called);
	}

	[PresentationTestMethod]
	public void DetachedFromLogicalTreeShouldBeCalledWhenRemovedFromTree()
	{
		var root = new TestRoot();
		var parent = new Border();
		var child = new Border();
		var grandchild = new Border();
		var parentRaised = false;
		var childRaised = false;
		var grandchildRaised = false;

		parent.Child = child;
		child.Child = grandchild;
		root.Child = parent;

		parent.DetachedFromLogicalTree += (s, e) => parentRaised = true;
		child.DetachedFromLogicalTree += (s, e) => childRaised = true;
		grandchild.DetachedFromLogicalTree += (s, e) => grandchildRaised = true;

		root.Child = null;

		CornerstoneTest.IsTrue(parentRaised);
		CornerstoneTest.IsTrue(childRaised);
		CornerstoneTest.IsTrue(grandchildRaised);
	}

	[PresentationTestMethod]
	public void DetachedFromLogicalTreeShouldNotBeCalledWithGlobalStylesAsRoot()
	{
		var globalStyles = new StubGlobalStyles();
		var root = new TestRoot { StylingParent = globalStyles };
		var child = new Border();
		var raised = false;

		child.DetachedFromLogicalTree += (s, e) =>
		{
			CornerstoneTest.AreEqual(root, e.Root);
			raised = true;
		};

		root.Child = child;
		root.Child = null;

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void EndInitShouldRaiseInitialized()
	{
		var root = new TestRoot();
		var target = new Border();
		var called = false;

		target.Initialized += (s, e) => called = true;
		((ISupportInitialize) target).BeginInit();
		root.Child = target;
		((ISupportInitialize) target).EndInit();

		CornerstoneTest.IsTrue(called);
		CornerstoneTest.IsTrue(target.IsInitialized);
	}

	[PresentationTestMethod]
	public void InheritanceParentShouldBeClearedWhenRemovedFromParent()
	{
		var parent = new Decorator();
		var target = new TestControl();

		parent.Child = target;
		parent.Child = null;

		CornerstoneTest.IsNull(target.InheritanceParent);
	}

	[PresentationTestMethod]
	public void LogicalChildrenCanBeAddedDuringAttachedToLogicalTree()
	{
		var root = new TestRoot();
		var parent = new StyledElement();
		var child1 = new StyledElement();
		var child2 = new StyledElement();

		parent.LogicalChildren.Add(child1);

		child1.AttachedToLogicalTree += (_, _) => parent.LogicalChildren.Add(child2);

		root.LogicalChildren.Add(parent);

		CornerstoneTest.AreEqual(new[] { child1, child2 }, parent.LogicalChildren);
		CornerstoneTest.IsTrue(((ILogical) child1).IsAttachedToLogicalTree);
		CornerstoneTest.IsTrue(((ILogical) child2).IsAttachedToLogicalTree);
	}

	[PresentationTestMethod]
	public void LogicalChildrenCanBeAddedDuringDetachedFromLogicalTree()
	{
		var root = new TestRoot();
		var parent = new StyledElement();
		var child1 = new StyledElement();
		var child2 = new StyledElement();

		parent.LogicalChildren.Add(child1);
		root.LogicalChildren.Add(parent);

		child1.DetachedFromLogicalTree += (_, _) => parent.LogicalChildren.Add(child2);

		root.LogicalChildren.Remove(parent);

		CornerstoneTest.AreEqual(new[] { child1, child2 }, parent.LogicalChildren);
		CornerstoneTest.IsFalse(((ILogical) child1).IsAttachedToLogicalTree);
		CornerstoneTest.IsFalse(((ILogical) child2).IsAttachedToLogicalTree);
	}

	[PresentationTestMethod]
	public void LogicalChildrenCanBeRemovedDuringAttachedToLogicalTree()
	{
		var root = new TestRoot();
		var parent = new StyledElement();
		var child1 = new StyledElement();
		var child2 = new StyledElement();

		parent.LogicalChildren.AddRange([child1, child2]);

		child1.AttachedToLogicalTree += (_, _) => parent.LogicalChildren.Remove(child2);

		root.LogicalChildren.Add(parent);

		CornerstoneTest.AreEqual(new[] { child1 }, parent.LogicalChildren);
		CornerstoneTest.IsTrue(((ILogical) child1).IsAttachedToLogicalTree);
		CornerstoneTest.IsFalse(((ILogical) child2).IsAttachedToLogicalTree);
	}

	[PresentationTestMethod]
	public void LogicalChildrenCanBeRemovedDuringDetachedFromLogicalTree()
	{
		var root = new TestRoot();
		var parent = new StyledElement();
		var child1 = new StyledElement();
		var child2 = new StyledElement();
		var child2Detached = 0;

		parent.LogicalChildren.AddRange([child1, child2]);
		root.LogicalChildren.Add(parent);

		child1.DetachedFromLogicalTree += (_, _) => parent.LogicalChildren.Remove(child2);
		child2.DetachedFromLogicalTree += (_, _) => ++child2Detached;

		root.LogicalChildren.Remove(parent);

		CornerstoneTest.AreEqual(new[] { child1 }, parent.LogicalChildren);
		CornerstoneTest.IsFalse(((ILogical) child1).IsAttachedToLogicalTree);
		CornerstoneTest.IsFalse(((ILogical) child2).IsAttachedToLogicalTree);
		CornerstoneTest.AreEqual(1, child2Detached);
	}

	[PresentationTestMethod]
	public void NameCanBeSetWhileInitializing()
	{
		using (PresentationLocator.EnterScope())
		{
			var root = new TestRoot();
			var child = new Border();

			child.BeginInit();
			root.Child = child;
			child.Name = "foo";
			child.EndInit();
		}
	}

	[PresentationTestMethod]
	public void NameCannotBeSetAfterAddedToLogicalTree()
	{
		var root = new TestRoot();
		var child = new Border();

		root.Child = child;

		Assert.Throws<InvalidOperationException>(() => child.Name = "foo");
	}

	[PresentationTestMethod]
	public void ParentShouldBeNullWhenDetachedFromLogicalTreeCalled()
	{
		var target = new TestControl();
		var root = new TestRoot(target);
		var called = 0;

		target.DetachedFromLogicalTree += (s, e) =>
		{
			CornerstoneTest.IsNull(target.Parent);
			CornerstoneTest.IsNull(target.InheritanceParent);
			++called;
		};

		root.Child = null;

		CornerstoneTest.AreEqual(1, called);
	}

	[PresentationTestMethod]
	public void ResourcesOwnerIsSet()
	{
		var target = new TestControl();

		CornerstoneTest.Same(target, ((ResourceDictionary) target.Resources).Owner);
	}

	[PresentationTestMethod]
	public void SetParentDoesNotCrashDueToReentrancy()
	{
		// Issue #3708
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		ContentControl target;
		var root = new TestRoot
		{
			DataContext = false,
			Child = target = new ContentControl
			{
				Styles =
				{
					new Style(x => x.OfType<ContentControl>())
					{
						Setters =
						{
							new Setter(
								ContentControl.ContentProperty,
								new FuncTemplate<Control>(() => new TextBlock { Text = "Enabled" }))
						}
					},
					new Style(x => x.OfType<ContentControl>().Class(":disabled"))
					{
						Setters =
						{
							new Setter(
								ContentControl.ContentProperty,
								new FuncTemplate<Control>(() => new TextBlock { Text = "Disabled" }))
						}
					}
				},
				[!ContentControl.IsEnabledProperty] = new Binding()
			}
		};

		root.Measure(Size.Infinity);
		root.Arrange(new Rect(0, 0, 100, 100));

		var textBlock = CornerstoneTest.IsType<TextBlock>(target.Content);
		CornerstoneTest.AreEqual("Disabled", textBlock.Text);

		// #3708 was crashing here with PresentationInternalException.
		root.Child = null;
	}

	[PresentationTestMethod]
	public void SettingParentShouldAlsoSetInheritanceParent()
	{
		var parent = new Decorator();
		var target = new TestControl();

		parent.Child = target;

		CornerstoneTest.AreEqual(parent, target.Parent);
		CornerstoneTest.AreEqual(parent, target.InheritanceParent);
	}

	[PresentationTestMethod]
	public void StyleIsRemovedWhenControlRemovedFromLogicalTree()
	{
		using var app = UnitTestApplication.Start();
		var target = new Border();
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Border>())
				{
					Setters =
					{
						new Setter(Border.BackgroundProperty, Brushes.Red)
					}
				}
			},
			Child = target
		};

		CornerstoneTest.AreEqual(Brushes.Red, target.Background);
		root.Child = null;
		CornerstoneTest.IsNull(target.Background);
	}

	[PresentationTestMethod]
	public void StylesNotAppliedUntilInitializationFinished()
	{
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.Is<Control>())
				{
					Setters = { new Setter(Control.TagProperty, "foo") }
				}
			}
		};

		var child = new Border();

		((ISupportInitialize) child).BeginInit();
		root.Child = child;
		CornerstoneTest.IsNull(child.Tag);

		((ISupportInitialize) child).EndInit();
		CornerstoneTest.AreEqual("foo", child.Tag);
	}

	[PresentationTestMethod]
	public void StylesOwnerIsSet()
	{
		var target = new TestControl();

		CornerstoneTest.Same(target, target.Styles.Owner);
	}

	#endregion

	#region Interfaces

	private interface IDataContextEvents
	{
		#region Events

		event EventHandler DataContextBeginUpdate;
		event EventHandler DataContextChanged;
		event EventHandler DataContextEndUpdate;

		#endregion
	}

	#endregion

	#region Classes

	private class TestControl : Decorator, IDataContextEvents
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

		protected override void OnDataContextEndUpdate()
		{
			DataContextEndUpdate?.Invoke(this, EventArgs.Empty);
			base.OnDataContextEndUpdate();
		}

		#endregion

		#region Events

		public event EventHandler DataContextBeginUpdate;
		public event EventHandler DataContextEndUpdate;

		#endregion
	}

	private class TestStackPanel : StackPanel, IDataContextEvents
	{
		#region Methods

		protected override void OnDataContextBeginUpdate()
		{
			DataContextBeginUpdate?.Invoke(this, EventArgs.Empty);
			base.OnDataContextBeginUpdate();
		}

		protected override void OnDataContextEndUpdate()
		{
			DataContextEndUpdate?.Invoke(this, EventArgs.Empty);
			base.OnDataContextEndUpdate();
		}

		#endregion

		#region Events

		public event EventHandler DataContextBeginUpdate;
		public event EventHandler DataContextEndUpdate;

		#endregion
	}

	#endregion
}