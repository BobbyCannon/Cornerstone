#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class PanelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddingControlToItemsHostPanelShouldNotAffectLogicalChildren()
	{
		var child = new Control();
		var realParent = new ContentControl { Content = child };
		var panel = new Panel { IsItemsHost = true };

		panel.Children.Add(child);

		CornerstoneTest.Empty(panel.LogicalChildren);
		CornerstoneTest.Same(child.Parent, realParent);
		CornerstoneTest.Same(child.GetLogicalParent(), realParent);
		CornerstoneTest.Same(child.GetVisualParent(), panel);
	}

	[PresentationTestMethod]
	public void AddingControlToPanelShouldSetChildControlsParent()
	{
		var panel = new Panel();
		var child = new Control();

		panel.Children.Add(child);

		CornerstoneTest.Same(child.Parent, panel);
		CornerstoneTest.Same(child.GetLogicalParent(), panel);
		CornerstoneTest.Same(child.GetVisualParent(), panel);
	}

	[PresentationTestMethod]
	public void AddingNullChildShouldThrow()
	{
		var panel = new Panel();
		Assert.Throws<ArgumentNullException>(() => panel.Children.Add(null!));
	}

	[PresentationTestMethod]
	public void ChildControlShouldAppearInPanelLogicalAndVisualChildren()
	{
		var panel = new Panel();
		var child = new Control();

		panel.Children.Add(child);

		CornerstoneTest.AreEqual(new[] { child }, panel.Children);
		CornerstoneTest.AreEqual(new[] { child }, panel.GetLogicalChildren());
		CornerstoneTest.AreEqual(new[] { child }, panel.GetVisualChildren());
	}

	[PresentationTestMethod]
	public void ClearingPanelChildrenShouldClearChildControlsParent()
	{
		var panel = new Panel();
		var child1 = new Control();
		var child2 = new Control();

		panel.Children.Add(child1);
		panel.Children.Add(child2);
		panel.Children.Clear();

		CornerstoneTest.IsNull(child1.Parent);
		CornerstoneTest.IsNull(child1.GetLogicalParent());
		CornerstoneTest.IsNull(child1.GetVisualParent());
		CornerstoneTest.IsNull(child2.Parent);
		CornerstoneTest.IsNull(child2.GetLogicalParent());
		CornerstoneTest.IsNull(child2.GetVisualParent());
	}

	[PresentationTestMethod]
	public void MovingPanelChildrenShouldReoderLogicalAndVisualChildren()
	{
		var panel = new Panel();
		var child1 = new Control();
		var child2 = new Control();

		panel.Children.Add(child1);
		panel.Children.Add(child2);
		panel.Children.Move(1, 0);

		CornerstoneTest.AreEqual(new[] { child2, child1 }, panel.GetLogicalChildren());
		CornerstoneTest.AreEqual(new[] { child2, child1 }, panel.GetVisualChildren());
	}

	[PresentationTestMethod]
	public void RemovingChildControlShouldRemoveFromPanelLogicalAndVisualChildren()
	{
		var panel = new Panel();
		var child = new Control();

		panel.Children.Add(child);
		panel.Children.Remove(child);

		CornerstoneTest.AreEqual(new Control[0], panel.Children);
		CornerstoneTest.Empty(panel.GetLogicalChildren());
		CornerstoneTest.Empty(panel.GetVisualChildren());
	}

	[PresentationTestMethod]
	public void RemovingControlFromPanelShouldClearChildControlsParent()
	{
		var panel = new Panel();
		var child = new Control();

		panel.Children.Add(child);
		panel.Children.Remove(child);

		CornerstoneTest.IsNull(child.Parent);
		CornerstoneTest.IsNull(child.GetLogicalParent());
		CornerstoneTest.IsNull(child.GetVisualParent());
	}

	[PresentationTestMethod]
	public void ReplacingPanelChildrenShouldClearAndSetControlParent()
	{
		var panel = new Panel();
		var child1 = new Control();
		var child2 = new Control();

		panel.Children.Add(child1);
		panel.Children[0] = child2;

		CornerstoneTest.IsNull(child1.Parent);
		CornerstoneTest.IsNull(child1.GetLogicalParent());
		CornerstoneTest.IsNull(child1.GetVisualParent());
		CornerstoneTest.Same(child2.Parent, panel);
		CornerstoneTest.Same(child2.GetLogicalParent(), panel);
		CornerstoneTest.Same(child2.GetVisualParent(), panel);
	}

	#endregion
}