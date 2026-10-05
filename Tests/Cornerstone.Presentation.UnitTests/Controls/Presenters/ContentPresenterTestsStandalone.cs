#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

/// <summary>
/// Tests for ContentControls that aren't hosted in a control template.
/// </summary>
[TestClass]
public class ContentPresenterTestsStandalone : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ContentPresenterLetterSpacingCanBeNegative()
	{
		var presenter = new ContentPresenter { LetterSpacing = -2.0 };
		CornerstoneTest.AreEqual(-2.0, presenter.LetterSpacing);
	}

	[PresentationTestMethod]
	public void ContentPresenterLetterSpacingCanBeSetAndRetrieved()
	{
		var presenter = new ContentPresenter { LetterSpacing = 3.5 };
		CornerstoneTest.AreEqual(3.5, presenter.LetterSpacing);
	}

	[PresentationTestMethod]
	public void ContentPresenterLetterSpacingDefaultValueIsZero()
	{
		var presenter = new ContentPresenter();
		CornerstoneTest.AreEqual(0, presenter.LetterSpacing);
	}

	[PresentationTestMethod]
	public void ContentPresenterLetterSpacingPropagatesToTextBlockChild()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var presenter = new ContentPresenter
			{
				Content = "Test Content",
				LetterSpacing = 4.0
			};
			var root = new TestRoot { Child = presenter };

			presenter.UpdateChild();

			var textBlock = presenter.Child as TextBlock;
			CornerstoneTest.IsNotNull(textBlock);
			CornerstoneTest.AreEqual(4.0, textBlock.LetterSpacing);
		}
	}

	[PresentationTestMethod]
	public void ContentPresenterLetterSpacingPropertyInheritsFromTextBlock()
	{
		// Verify that ContentPresenter's LetterSpacing uses the TextElement letter spacing definition
		CornerstoneTest.Same(TextElement.LetterSpacingProperty, ContentPresenter.LetterSpacingProperty);
	}

	[PresentationTestMethod]
	public void ContentPresenterLetterSpacingUpdatesTextBlockWhenChanged()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var presenter = new ContentPresenter
			{
				Content = "Test Content",
				LetterSpacing = 1.0
			};
			var root = new TestRoot { Child = presenter };

			presenter.UpdateChild();
			var textBlock = presenter.Child as TextBlock;

			presenter.LetterSpacing = 6.0;

			CornerstoneTest.IsNotNull(textBlock);
			CornerstoneTest.AreEqual(6.0, textBlock.LetterSpacing);
		}
	}

	[PresentationTestMethod]
	public void ShouldAddChildToOwnLogicalChildrenStandalone()
	{
		var content = new Border();
		var target = new ContentPresenter { Content = content };

		target.UpdateChild();

		var logicalChildren = target.GetLogicalChildren();

		CornerstoneTest.Single(logicalChildren);
		CornerstoneTest.AreEqual(content, logicalChildren.First());
	}

	[PresentationTestMethod]
	public void ShouldCreateChildEvenWithNullContentWhenContentTemplateIsSet()
	{
		var target = new ContentPresenter
		{
			ContentTemplate = new FuncDataTemplate<object>(_ => true, (_, __) => new TextBlock
			{
				Text = "Hello World"
			}),
			Content = null
		};

		target.UpdateChild();

		var textBlock = CornerstoneTest.IsType<TextBlock>(target.Child);
		CornerstoneTest.AreEqual("Hello World", textBlock.Text);
	}

	[PresentationTestMethod]
	public void ShouldCreateChildWhenContentIsNullAndExpectedNullableValueTypeWithFuncDataTemplate()
	{
		var target = new ContentPresenter
		{
			ContentTemplate = new FuncDataTemplate<int?>(_ => true, (_, __) => new TextBlock
			{
				Text = "Hello World"
			}),
			Content = null
		};

		target.UpdateChild();

		CornerstoneTest.IsNotNull(target.Child);
	}

	[PresentationTestMethod]
	public void ShouldNotBindOldChildToNewDataContext()
	{
		// Test for issue #1099.
		var textBlock = new TextBlock
		{
			[!TextBlock.TextProperty] = new Binding()
		};

		var target = new ContentPresenter
		{
			DataTemplates =
			{
				new FuncDataTemplate<string>((x, _) => textBlock),
				new FuncDataTemplate<int>((x, _) => new Canvas())
			}
		};

		var root = new TestRoot(target);
		target.Content = "foo";
		CornerstoneTest.Same(textBlock, target.Child);

		textBlock.PropertyChanged += (s, e) => { CornerstoneTest.AreNotEqual(e.NewValue, "42"); };

		target.Content = 42;
	}

	[PresentationTestMethod]
	public void ShouldNotCreateChildEvenWithNullContentAndDataTemplatesInsteadOfContentTemplate()
	{
		var target = new ContentPresenter
		{
			DataTemplates =
			{
				new FuncDataTemplate<object>(_ => true, (_, __) => new TextBlock
				{
					Text = "Hello World"
				})
			},
			Content = null
		};

		target.UpdateChild();

		CornerstoneTest.IsNull(target.Child);
	}

	[PresentationTestMethod]
	public void ShouldNotCreateChildWhenContentAndTemplateAreNull()
	{
		var target = new ContentPresenter
		{
			ContentTemplate = null,
			Content = null
		};

		target.UpdateChild();

		CornerstoneTest.IsNull(target.Child);
	}

	[PresentationTestMethod]
	public void ShouldNotCreateWhenChildContentIsNullButExpectedValueTypeWithFuncDataTemplate()
	{
		var target = new ContentPresenter
		{
			ContentTemplate = new FuncDataTemplate<int>(_ => true, (_, __) => new TextBlock
			{
				Text = "Hello World"
			}),
			Content = null
		};

		target.UpdateChild();

		CornerstoneTest.IsNull(target.Child);
	}

	[PresentationTestMethod]
	public void ShouldRaiseDetachedFromLogicalTreeInContentControlOnContentChangedStandalone()
	{
		var contentControl = new ContentControl
		{
			Template = new FuncControlTemplate<ContentControl>((c, scope) => new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[~ContentPresenter.ContentProperty] = c[~ContentControl.ContentProperty],
				[~ContentPresenter.ContentTemplateProperty] = c[~ContentControl.ContentTemplateProperty]
			}.RegisterInNameScope(scope)),
			ContentTemplate =
				new FuncDataTemplate<string>((t, _) => new ContentControl { Content = t }, false)
		};

		var parentMock = new TestRoot();
		(contentControl as ISetLogicalParent).SetParent(parentMock);

		contentControl.ApplyTemplate();
		var target = contentControl.Presenter;
		CornerstoneTest.IsNotNull(target);

		contentControl.Content = "foo";

		target.UpdateChild();

		var tbfoo = target.Child as ContentControl;

		var foodetached = false;

		CornerstoneTest.IsNotNull(tbfoo);
		CornerstoneTest.AreEqual("foo", tbfoo.Content);

		tbfoo.DetachedFromLogicalTree += delegate { foodetached = true; };

		contentControl.Content = "bar";
		target.UpdateChild();

		var tbbar = target.Child as ContentControl;

		CornerstoneTest.IsNotNull(tbbar);

		CornerstoneTest.IsTrue(tbbar != tbfoo);
		CornerstoneTest.IsFalse((tbfoo as ILogical).IsAttachedToLogicalTree);
		CornerstoneTest.IsTrue(foodetached);
	}

	[PresentationTestMethod]
	public void ShouldRaiseDetachedFromLogicalTreeOnContentChangedStandalone()
	{
		var target = new ContentPresenter
		{
			ContentTemplate =
				new FuncDataTemplate<string>((t, _) => new ContentControl { Content = t }, false)
		};

		var parentMock = new TestRoot();
		(target as ISetLogicalParent).SetParent(parentMock);

		target.Content = "foo";

		target.UpdateChild();

		var foo = target.Child as ContentControl;

		var foodetached = false;

		CornerstoneTest.IsNotNull(foo);
		CornerstoneTest.AreEqual("foo", foo.Content);

		foo.DetachedFromLogicalTree += delegate { foodetached = true; };

		target.Content = "bar";
		target.UpdateChild();

		var bar = target.Child as ContentControl;

		CornerstoneTest.IsNotNull(bar);
		CornerstoneTest.IsTrue(bar != foo);
		CornerstoneTest.IsFalse((foo as ILogical).IsAttachedToLogicalTree);
		CornerstoneTest.IsTrue(foodetached);
	}

	[PresentationTestMethod]
	public void ShouldRaiseDetachedFromLogicalTreeOnDetachedStandalone()
	{
		var target = new ContentPresenter
		{
			ContentTemplate =
				new FuncDataTemplate<string>((t, _) => new ContentControl { Content = t }, false)
		};

		var parentMock = new TestRoot();
		(target as ISetLogicalParent).SetParent(parentMock);

		target.Content = "foo";

		target.UpdateChild();

		var foo = target.Child as ContentControl;

		var foodetached = false;

		CornerstoneTest.IsNotNull(foo);
		CornerstoneTest.AreEqual("foo", foo.Content);

		foo.DetachedFromLogicalTree += delegate { foodetached = true; };

		(target as ISetLogicalParent).SetParent(null);

		CornerstoneTest.IsFalse((foo as ILogical).IsAttachedToLogicalTree);
		CornerstoneTest.IsTrue(foodetached);
	}

	[PresentationTestMethod]
	public void ShouldRemoveOldChildFromLogicalChildrenOnContentChangedStandalone()
	{
		var target = new ContentPresenter
		{
			ContentTemplate =
				new FuncDataTemplate<string>((t, _) => new ContentControl { Content = t }, false)
		};

		target.Content = "foo";

		target.UpdateChild();

		var foo = target.Child as ContentControl;

		CornerstoneTest.IsNotNull(foo);

		var logicalChildren = target.GetLogicalChildren();

		CornerstoneTest.Single(logicalChildren);

		target.Content = "bar";
		target.UpdateChild();

		CornerstoneTest.IsNull(foo.Parent);

		logicalChildren = target.GetLogicalChildren();

		var logicalChild = CornerstoneTest.Single(logicalChildren);
		CornerstoneTest.AreNotEqual(foo, logicalChild);
	}

	[PresentationTestMethod]
	public void ShouldResetInheritanceParentWhenChildRemoved()
	{
		var logicalParent = new Canvas();
		var child = new TextBlock();
		var target = new ContentPresenter();
		var root = new TestRoot(target);

		((ISetLogicalParent) child).SetParent(logicalParent);
		target.Content = child;
		target.Content = null;

		// InheritanceParent is exposed via StylingParent.
		CornerstoneTest.Same(logicalParent, ((IStyleHost) child).StylingParent);
	}

	[PresentationTestMethod]
	public void ShouldSetChildsParentToItselfStandalone()
	{
		var content = new Border();
		var target = new ContentPresenter { Content = content };

		target.UpdateChild();

		CornerstoneTest.Same(target, content.Parent);
	}

	#endregion
}