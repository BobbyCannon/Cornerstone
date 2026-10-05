#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Markup;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ContentControlTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingContentTemplateAfterContentDoesNotLeaveOrpanedTextBlock()
	{
		// Test for #1271.
		var children = new List<Control>();
		var presenter = new ContentPresenter();

		// The content and then the content template property need to be bound with delayed bindings
		// as they are in Cornerstone.Presentation.Markup.Xaml.
		DelayedBinding.Add(presenter, ContentPresenter.ContentProperty, new Binding("Content")
		{
			Priority = BindingPriority.Template,
			RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
		});

		DelayedBinding.Add(presenter, ContentPresenter.ContentTemplateProperty, new Binding("ContentTemplate")
		{
			Priority = BindingPriority.Template,
			RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
		});

		presenter.GetObservable(ContentPresenter.ChildProperty).Subscribe(children.Add);

		var target = new ContentControl
		{
			Template = new FuncControlTemplate<ContentControl>((_, __) => presenter),
			ContentTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
			Content = "foo"
		};

		// The control must be rooted.
		var root = new TestRoot
		{
			Child = target
		};

		target.ApplyTemplate();

		// When the template is applied, the Content property is bound before the ContentTemplate
		// property, causing a TextBlock to be created by the default template before ContentTemplate
		// is bound.
		CornerstoneTest.Collection(children, x => CornerstoneTest.IsNull(x), x => CornerstoneTest.IsType<TextBlock>(x), x => CornerstoneTest.IsType<Canvas>(x));

		var textBlock = (TextBlock) children[1]!;

		// The leak in #1271 was caused by the TextBlock's logical parent not being cleared when
		// it is replaced by the Canvas.
		CornerstoneTest.IsNull(textBlock.GetLogicalParent());
	}

	[PresentationTestMethod]
	public void ChangingContentShouldFireLogicalChildrenCollectionChanged()
	{
		var target = new ContentControl();
		var child1 = new Control();
		var child2 = new Control();
		var called = false;

		target.Template = GetTemplate();
		target.Content = child1;
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) => called = true;

		target.Content = child2;
		target.Presenter.ApplyTemplate();

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void ChangingContentShouldUpdatePresenter()
	{
		var target = new ContentControl();

		target.Template = GetTemplate();
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		target.Content = "Foo";
		target.Presenter.UpdateChild();
		CornerstoneTest.AreEqual("Foo", ((TextBlock) target.Presenter.Child!).Text);
		target.Content = "Bar";
		target.Presenter.UpdateChild();
		CornerstoneTest.AreEqual("Bar", ((TextBlock) target.Presenter.Child!).Text);
	}

	[PresentationTestMethod]
	public void ClearingContentShouldClearLogicalChild()
	{
		var target = new ContentControl();
		var child = new Control();

		target.Content = child;

		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());

		target.Content = null;

		CornerstoneTest.IsNull(child.Parent);
		CornerstoneTest.IsNull(child.GetLogicalParent());
		CornerstoneTest.Empty(target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ClearingContentShouldFireLogicalChildrenCollectionChanged()
	{
		var target = new ContentControl();
		var child = new Control();
		var called = false;

		target.Template = GetTemplate();
		target.Content = child;
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) => called = true;

		target.Content = null;
		target.Presenter.UpdateChild();

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void ContentPresenterShouldHaveTemplatedParentSet()
	{
		var target = new ContentControl();
		var child = new Border();

		target.Template = GetTemplate();
		target.Content = child;
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		var contentPresenter = child.GetVisualParent<ContentPresenter>();
		CornerstoneTest.IsNotNull(contentPresenter);
		CornerstoneTest.AreEqual(target, contentPresenter.TemplatedParent);
	}

	[PresentationTestMethod]
	public void ContentShouldHaveTemplatedParentSetToNull()
	{
		var target = new ContentControl();
		var child = new Border();

		target.Template = GetTemplate();
		target.Content = child;
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		CornerstoneTest.IsNull(child.TemplatedParent);
	}

	[PresentationTestMethod]
	public void ControlContentShouldBeLogicalChildAfterApplyTemplate()
	{
		var target = new ContentControl
		{
			Template = GetTemplate()
		};

		var child = new Control();
		target.Content = child;
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		CornerstoneTest.AreEqual(child.Parent, target);
		CornerstoneTest.AreEqual(child.GetLogicalParent(), target);
		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ControlContentShouldBeLogicalChildBeforeApplyTemplate()
	{
		var target = new ContentControl
		{
			Template = GetTemplate()
		};

		var child = new Control();
		target.Content = child;

		CornerstoneTest.AreEqual(child.Parent, target);
		CornerstoneTest.AreEqual(child.GetLogicalParent(), target);
		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void DataContextShouldBeSetForDataTemplateCreatedContent()
	{
		var target = new ContentControl();

		target.Template = GetTemplate();
		target.Content = "Foo";
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		CornerstoneTest.AreEqual("Foo", target.Presenter.Child!.DataContext);
	}

	[PresentationTestMethod]
	public void DataContextShouldNotBeSetForControlContent()
	{
		var target = new ContentControl();

		target.Template = GetTemplate();
		target.Content = new TextBlock();
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		CornerstoneTest.IsNull(target.Presenter.Child!.DataContext);
	}

	[PresentationTestMethod]
	public void DataTemplateCreatedControlShouldBeLogicalChildAfterApplyTemplate()
	{
		var target = new ContentControl
		{
			Template = GetTemplate()
		};

		target.Content = "Foo";
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		var child = target.Presenter.Child;

		CornerstoneTest.IsNotNull(child);
		CornerstoneTest.AreEqual(target, child.Parent);
		CornerstoneTest.AreEqual(target, child.GetLogicalParent());
		CornerstoneTest.AreEqual(new[] { child }, target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void EmptyPseudoClassShouldTrackContent()
	{
		var target = new ContentControl();

		CornerstoneTest.Contains(target.Classes, ":empty");

		target.Content = "Content";
		CornerstoneTest.DoesNotContain(target.Classes, ":empty");

		target.Content = null;
		CornerstoneTest.Contains(target.Classes, ":empty");
	}

	[PresentationTestMethod]
	public void SettingContentShouldFireLogicalChildrenCollectionChanged()
	{
		var target = new ContentControl();
		var child = new Control();
		var called = false;

		((ILogical) target).LogicalChildren.CollectionChanged += (s, e) =>
			called = e.Action == NotifyCollectionChangedAction.Add;

		target.Template = GetTemplate();
		target.Content = child;
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void ShouldSetChildLogicalParentAfterRemovingAndAddingBackToLogicalTree()
	{
		var target = new ContentControl();
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<ContentControl>())
				{
					Setters =
					{
						new Setter(ContentControl.TemplateProperty, GetTemplate())
					}
				}
			},
			Child = target
		};

		target.Content = "Foo";
		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		CornerstoneTest.AreEqual(target, target.Presenter!.Child!.GetLogicalParent());
		CornerstoneTest.AreEqual(new[] { target.Presenter.Child }, target.LogicalChildren);

		root.Child = null;

		target.Content = null;

		CornerstoneTest.Empty(target.LogicalChildren);

		root.Child = target;
		target.Content = "Bar";

		CornerstoneTest.AreEqual(target, target.Presenter.Child!.GetLogicalParent());
		CornerstoneTest.AreEqual(new[] { target.Presenter.Child }, target.LogicalChildren);
	}

	[PresentationTestMethod]
	public void ShouldUseContentTemplateToCreateControl()
	{
		var target = new ContentControl
		{
			Template = GetTemplate(),
			ContentTemplate = new FuncDataTemplate<string>((_, __) => new Canvas())
		};

		target.Content = "Foo";
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		var child = target.Presenter.Child;

		CornerstoneTest.IsType<Canvas>(child);
	}

	[PresentationTestMethod]
	public void TemplateShouldBeInstantiated()
	{
		var target = new ContentControl();
		target.Content = "Foo";
		target.Template = GetTemplate();
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();

		var child = target.VisualChildren.Single();
		CornerstoneTest.IsType<Border>(child);
		child = child.VisualChildren.Single();
		CornerstoneTest.IsType<ContentPresenter>(child);
		child = child.VisualChildren.Single();
		CornerstoneTest.IsType<TextBlock>(child);
	}

	[PresentationTestMethod]
	public void TemplatedChildrenShouldBeStyled()
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

		var target = new ContentControl();

		target.Content = "Foo";
		target.Template = GetTemplate();
		root.Child = target;

		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		foreach (var child in target.GetTemplateDescendants().OfType<Control>())
		{
			CornerstoneTest.AreEqual("foo", child.Tag);
		}
	}

	private static FuncControlTemplate GetTemplate()
	{
		return new FuncControlTemplate<ContentControl>((parent, scope) =>
		{
			return new Border
			{
				Background = new SolidColorBrush(0xffffffff),
				Child = new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
					[~ContentPresenter.ContentTemplateProperty] = parent[~ContentControl.ContentTemplateProperty]
				}.RegisterInNameScope(scope)
			};
		});
	}

	#endregion
}