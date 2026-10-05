#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

/// <summary>
/// Tests for ContentControls that are not attached to a logical tree.
/// </summary>
[TestClass]
public class ContentPresenterTestsUnrooted : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddingToLogicalTreeShouldReevaluateDataTemplates()
	{
		var root = new TestRoot();
		var target = new ContentPresenter();

		target.Content = "Foo";
		CornerstoneTest.IsNull(target.Child);

		root.Child = target;
		target.ApplyTemplate();
		CornerstoneTest.IsType<TextBlock>(target.Child);

		root.Child = null;
		root = new TestRoot
		{
			DataTemplates =
			{
				new FuncDataTemplate<string>((x, _) => new Decorator())
			}
		};

		root.Child = target;
		target.ApplyTemplate();
		CornerstoneTest.IsType<Decorator>(target.Child);
	}

	[PresentationTestMethod]
	public void ClearingControlContentShouldRemoveChildImmediately()
	{
		var target = new ContentPresenter();
		var child = new Border();

		target.Content = child;
		target.UpdateChild();
		CornerstoneTest.AreEqual(child, target.Child);

		target.Content = null;
		CornerstoneTest.IsNull(target.Child);
	}

	[PresentationTestMethod]
	public void ClearingStringContentShouldRemoveChildImmediately()
	{
		var target = new ContentPresenter();

		target.Content = "Foo";
		target.UpdateChild();
		CornerstoneTest.IsType<TextBlock>(target.Child);

		target.Content = null;
		CornerstoneTest.IsNull(target.Child);
	}

	[PresentationTestMethod]
	public void SettingContentToControlShouldNotSetChildUnlessUpdateChildCalled()
	{
		var target = new ContentPresenter();
		var child = new Border();

		target.Content = child;
		CornerstoneTest.IsNull(target.Child);

		target.ApplyTemplate();
		CornerstoneTest.IsNull(target.Child);

		target.UpdateChild();
		CornerstoneTest.AreEqual(child, target.Child);
	}

	[PresentationTestMethod]
	public void SettingContentToStringShouldNotCreateTextBlockUnlessUpdateChildCalled()
	{
		var target = new ContentPresenter();

		target.Content = "Foo";
		CornerstoneTest.IsNull(target.Child);

		target.ApplyTemplate();
		CornerstoneTest.IsNull(target.Child);

		target.UpdateChild();
		CornerstoneTest.IsType<TextBlock>(target.Child);
		CornerstoneTest.AreEqual("Foo", ((TextBlock) target.Child).Text);
	}

	[PresentationTestMethod]
	public void ShouldResetInheritanceParentWhenChildRemoved()
	{
		var logicalParent = new Canvas();
		var child = new TextBlock();
		var target = new ContentPresenter();

		((ISetLogicalParent) child).SetParent(logicalParent);
		target.Content = child;
		target.UpdateChild();
		target.Content = null;
		target.UpdateChild();

		// InheritanceParent is exposed via StylingParent.
		CornerstoneTest.Same(logicalParent, ((IStyleHost) child).StylingParent);
	}

	#endregion
}