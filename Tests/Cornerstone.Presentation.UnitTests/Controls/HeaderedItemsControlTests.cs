#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class HeaderedItemsControlTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClearingContentShouldClearLogicalChild()
	{
		var target = new HeaderedItemsControl();
		var child = new Control();

		target.Header = child;
		target.Header = null;

		CornerstoneTest.IsNull(child.Parent);
		CornerstoneTest.IsNull(child.GetLogicalParent());
		CornerstoneTest.Empty(target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ControlHeaderShouldBeLogicalChildBeforeApplyTemplate()
	{
		var target = new HeaderedItemsControl
		{
			Template = GetTemplate()
		};

		var child = new Control();
		target.Header = child;

		CornerstoneTest.AreEqual(child.Parent, target);
		CornerstoneTest.AreEqual(child.GetLogicalParent(), target);
		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void DataTemplateCreatedControlShouldBeLogicalChildAfterApplyTemplate()
	{
		var target = new HeaderedItemsControl
		{
			Template = GetTemplate()
		};

		target.Header = "Foo";
		target.ApplyTemplate();
		target.HeaderPresenter!.UpdateChild();

		var child = target.HeaderPresenter.Child;

		CornerstoneTest.IsNotNull(child);
		CornerstoneTest.AreEqual(target, child.Parent);
		CornerstoneTest.AreEqual(target, child.GetLogicalParent());
		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());
	}

	private static FuncControlTemplate GetTemplate()
	{
		return new FuncControlTemplate<HeaderedItemsControl>((parent, scope) =>
		{
			return new Border
			{
				Child = new ContentPresenter
				{
					Name = "PART_HeaderPresenter",
					[~ContentPresenter.ContentProperty] = parent[~HeaderedItemsControl.HeaderProperty]
				}.RegisterInNameScope(scope)
			};
		});
	}

	#endregion
}