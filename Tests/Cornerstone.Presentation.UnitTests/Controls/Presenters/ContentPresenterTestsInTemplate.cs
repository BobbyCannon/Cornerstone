#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Presenters;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

/// <summary>
/// Tests for ContentControls that are hosted in a control template.
/// </summary>
[TestClass]
public class ContentPresenterTestsInTemplate : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AssigningControlToContentAfterNonControlShouldClearDataContext()
	{
		var (target, _) = CreateTarget();

		target.Content = "foo";

		CornerstoneTest.IsTrue(target.IsSet(Control.DataContextProperty));

		target.Content = new Border();

		CornerstoneTest.IsFalse(target.IsSet(Control.DataContextProperty));
	}

	[PresentationTestMethod]
	public void AssigningControlToContentShouldNotSetDataContext()
	{
		var (target, _) = CreateTarget();
		target.Content = new Border();

		CornerstoneTest.IsFalse(target.IsSet(Control.DataContextProperty));
	}

	[PresentationTestMethod]
	public void AssigningNonControlToContentShouldSetDataContextOnUpdateChild()
	{
		var (target, _) = CreateTarget();
		target.Content = "foo";

		CornerstoneTest.AreEqual("foo", target.DataContext);
	}

	[PresentationTestMethod]
	public void ClearingControlContentShouldUpdateLogicalTree()
	{
		var (target, _) = CreateTarget();
		var child = new Border();

		target.Content = child;
		target.Content = null;

		CornerstoneTest.IsNull(child.GetLogicalParent());
		CornerstoneTest.Empty(target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ClearingControlContentShouldUpdateVisualTree()
	{
		var (target, _) = CreateTarget();
		var child = new Border();

		target.Content = child;
		target.Content = null;

		CornerstoneTest.IsNull(child.GetVisualParent());
		CornerstoneTest.Empty(target.GetVisualChildren());
	}

	[PresentationTestMethod]
	public void ContentShouldBecomeDataContextWhenControlTemplateIsNotNull()
	{
		var (target, _) = CreateTarget();

		var textBlock = new TextBlock
		{
			[!TextBlock.TextProperty] = new Binding("Name")
		};

		var canvas = new Canvas
		{
			Name = "Canvas"
		};

		target.ContentTemplate = new FuncDataTemplate<Canvas>((_, __) => textBlock);
		target.Content = canvas;

		CornerstoneTest.IsNotNull(target.DataContext);
		CornerstoneTest.AreEqual(canvas, target.DataContext);
		CornerstoneTest.AreEqual("Canvas", textBlock.Text);
	}

	[PresentationTestMethod]
	public void ControlContentShouldNotBeNameScope()
	{
		var (target, _) = CreateTarget();

		target.Content = new TextBlock();

		CornerstoneTest.IsType<TextBlock>(target.Child);
		CornerstoneTest.IsNull(NameScope.GetNameScope(target.Child));
	}

	[PresentationTestMethod]
	public void DetectsDataTemplateDoesntMatchAndDoesntRecycle()
	{
		var (target, _) = CreateTarget();
		target.DataTemplates.Add(new FuncDataTemplate<string>(x => x == "foo", _ => new Border(), true));

		target.Content = "foo";

		var control = target.Child;
		CornerstoneTest.IsType<Border>(control);

		target.Content = "bar";
		CornerstoneTest.IsType<TextBlock>(target.Child);
	}

	[PresentationTestMethod]
	public void DetectsDataTemplateDoesntSupportRecycling()
	{
		var (target, _) = CreateTarget();
		target.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => new Border(), false));

		target.Content = "foo";

		var control = target.Child;
		CornerstoneTest.IsType<Border>(control);

		target.Content = "bar";
		CornerstoneTest.NotSame(control, target.Child);
	}

	[PresentationTestMethod]
	public void RecyclesDataTemplate()
	{
		var (target, _) = CreateTarget();
		target.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => new Border(), true));

		target.Content = "foo";

		var control = target.Child;
		CornerstoneTest.IsType<Border>(control);

		target.Content = "bar";
		CornerstoneTest.Same(control, target.Child);
	}

	[PresentationTestMethod]
	public void ReevaluatesDataTemplatesWhenRecycling()
	{
		var (target, _) = CreateTarget();

		target.DataTemplates.Add(new FuncDataTemplate<string>(x => x == "bar", _ => new Canvas(), true));
		target.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => new Border(), true));

		target.Content = "foo";

		var control = target.Child;
		CornerstoneTest.IsType<Border>(control);

		target.Content = "bar";
		CornerstoneTest.IsType<Canvas>(target.Child);
	}

	[PresentationTestMethod]
	public void SettingContentToControlShouldSetChild()
	{
		var (target, _) = CreateTarget();
		var child = new Border();

		target.Content = child;

		CornerstoneTest.AreEqual(child, target.Child);
	}

	[PresentationTestMethod]
	public void SettingContentToControlShouldUpdateLogicalTree()
	{
		var (target, parent) = CreateTarget();
		var child = new Border();

		target.Content = child;

		CornerstoneTest.AreEqual(parent, child.GetLogicalParent());
		CornerstoneTest.AreEqual(new[] { child }, parent.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void SettingContentToControlShouldUpdateVisualTree()
	{
		var (target, _) = CreateTarget();
		var child = new Border();

		target.Content = child;

		CornerstoneTest.AreEqual(target, child.GetVisualParent());
		CornerstoneTest.AreEqual(new[] { child }, target.GetVisualChildren());
	}

	[PresentationTestMethod]
	public void SettingContentToStringShouldCreateTextBlock()
	{
		var (target, _) = CreateTarget();

		target.Content = "Foo";

		CornerstoneTest.IsType<TextBlock>(target.Child);
		CornerstoneTest.AreEqual("Foo", ((TextBlock) target.Child).Text);
	}

	[PresentationTestMethod]
	public void SettingContentToStringShouldUpdateLogicalTree()
	{
		var (target, parent) = CreateTarget();

		target.Content = "Foo";

		var child = target.Child;
		CornerstoneTest.IsNotNull(child);
		CornerstoneTest.AreEqual(parent, child.GetLogicalParent());
		CornerstoneTest.AreEqual(new[] { child }, parent.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void SettingContentToStringShouldUpdateVisualTree()
	{
		var (target, _) = CreateTarget();

		target.Content = "Foo";

		var child = target.Child;
		CornerstoneTest.IsNotNull(child);
		CornerstoneTest.AreEqual(target, child.GetVisualParent());
		CornerstoneTest.AreEqual(new[] { child }, target.GetVisualChildren());
	}

	[PresentationTestMethod]
	public void ShouldClearHostWhenHostTemplateCleared()
	{
		var (target, host) = CreateTarget();

		CornerstoneTest.Same(host, target.Host);

		host.Template = null;
		host.ApplyTemplate();

		CornerstoneTest.IsNull(target.Host);
	}

	[PresentationTestMethod]
	public void ShouldNotBindChildToWrongDataContextWhenRemoving()
	{
		// Test for issue #2823
		var canvas = new Canvas();
		var (target, host) = CreateTarget();
		var viewModel = new TestViewModel { Content = "foo" };
		var dataContexts = new List<object>();

		target.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
		canvas.GetObservable(ContentPresenter.DataContextProperty).Subscribe(x => dataContexts.Add(x));

		host.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => canvas));
		host.Bind(ContentControl.ContentProperty, new Binding(nameof(TestViewModel.Content)));
		host.DataContext = viewModel;

		CornerstoneTest.Same(canvas, target.Child);

		viewModel.Content = 42;

		CornerstoneTest.AreEqual(new object[]
		{
			null,
			"foo",
			null
		}, dataContexts);
	}

	[PresentationTestMethod]
	public void ShouldNotBindOldChildToNewDataContext()
	{
		// Test for issue #1099.
		var textBlock = new TextBlock
		{
			[!TextBlock.TextProperty] = new Binding()
		};

		var (target, host) = CreateTarget();
		host.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => textBlock));
		host.DataTemplates.Add(new FuncDataTemplate<int>((_, __) => new Canvas()));

		target.Content = "foo";
		CornerstoneTest.Same(textBlock, target.Child);

		textBlock.PropertyChanged += (s, e) => { CornerstoneTest.AreNotEqual(e.NewValue, "42"); };

		target.Content = 42;
	}

	[PresentationTestMethod]
	public void ShouldRegisterWithHostWhenTemplatedParentSet()
	{
		var host = new ContentControl();
		var target = new ContentPresenter { Name = "PART_ContentPresenter" };

		CornerstoneTest.IsNull(host.Presenter);

		target.TemplatedParent = host;

		CornerstoneTest.Same(target, host.Presenter);
	}

	[PresentationTestMethod]
	public void ShouldResetInheritanceParentWhenChildRemoved()
	{
		var logicalParent = new Canvas();
		var child = new TextBlock();
		var (target, _) = CreateTarget();

		((ISetLogicalParent) child).SetParent(logicalParent);
		target.Content = child;
		target.Content = null;

		// InheritanceParent is exposed via StylingParent.
		CornerstoneTest.Same(logicalParent, ((IStyleHost) child).StylingParent);
	}

	[PresentationTestMethod]
	public void ShouldSetInheritanceParentEvenWhenLogicalParentIsAlreadySet()
	{
		var logicalParent = new Canvas();
		var child = new TextBlock();
		var (target, host) = CreateTarget();

		((ISetLogicalParent) child).SetParent(logicalParent);
		target.Content = child;

		CornerstoneTest.Same(logicalParent, child.Parent);

		// InheritanceParent is exposed via StylingParent.
		CornerstoneTest.Same(target, ((IStyleHost) child).StylingParent);
	}

	[PresentationTestMethod]
	public void ShouldUpdateIfContentTemplateChanged()
	{
		var (target, _) = CreateTarget();

		target.Content = "Foo";
		CornerstoneTest.IsType<TextBlock>(target.Child);

		target.ContentTemplate = new FuncDataTemplate<string>((_, __) => new Canvas());
		CornerstoneTest.IsType<Canvas>(target.Child);

		target.ContentTemplate = null;
		CornerstoneTest.IsType<TextBlock>(target.Child);
	}

	[PresentationTestMethod]
	public void ShouldUseContentTemplateIfSpecified()
	{
		var (target, _) = CreateTarget();

		target.ContentTemplate = new FuncDataTemplate<string>((_, __) => new Canvas());
		target.Content = "Foo";

		CornerstoneTest.IsType<Canvas>(target.Child);
	}

	private static (ContentPresenter presenter, ContentControl templatedParent) CreateTarget()
	{
		var templatedParent = new ContentControl
		{
			Template = new FuncControlTemplate<ContentControl>((_, s) =>
				new ContentPresenter
				{
					Name = "PART_ContentPresenter"
				}.RegisterInNameScope(s))
		};
		var root = new TestRoot { Child = templatedParent };

		templatedParent.ApplyTemplate();

		return (templatedParent.Presenter!, templatedParent);
	}

	#endregion

	#region Classes

	private class TestContentControl : ContentControl, IContentPresenterHost
	{
		#region Properties

		public Control Child { get; set; }

		#endregion
	}

	private class TestViewModel : INotifyPropertyChanged
	{
		#region Fields

		private object _content;

		#endregion

		#region Properties

		public object Content
		{
			get => _content;
			set
			{
				if (_content != value)
				{
					_content = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
				}
			}
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	#endregion
}