#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsElementName : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldBindToElement()
	{
		TextBlock source;
		ContentControl target;

		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					(source = new TextBlock
					{
						Name = "source",
						Text = "foo"
					}),
					(target = new ContentControl
					{
						Name = "target"
					})
				}
			}
		};
		root.RegisterChildrenNames();

		var binding = new Binding
		{
			ElementName = "source",
			NameScope = new WeakReference<INameScope>(NameScope.GetNameScope(root))
		};

		target.Bind(ContentControl.ContentProperty, binding);

		CornerstoneTest.Same(source, target.Content);
	}

	[PresentationTestMethod]
	public void ShouldBindToElementPath()
	{
		TextBlock target;
		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					new TextBlock
					{
						Name = "source",
						Text = "foo"
					},
					(target = new TextBlock
					{
						Name = "target"
					})
				}
			}
		};

		root.RegisterChildrenNames();

		var binding = new Binding
		{
			ElementName = "source",
			Path = "Text",
			NameScope = new WeakReference<INameScope>(NameScope.GetNameScope(root))
		};

		target.Bind(TextBox.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldBindToLaterAddedElement()
	{
		ContentControl target;
		StackPanel stackPanel;

		var root = new TestRoot
		{
			Child = stackPanel = new StackPanel
			{
				Children =
				{
					(target = new ContentControl
					{
						Name = "target"
					})
				}
			}
		};
		root.RegisterChildrenNames();

		var binding = new Binding
		{
			ElementName = "source",
			NameScope = new WeakReference<INameScope>(NameScope.GetNameScope(root))
		};

		target.Bind(ContentControl.ContentProperty, binding);

		var source = new TextBlock
		{
			Name = "source",
			Text = "foo"
		};

		stackPanel.Children.Add(source);
		root.RegisterChildrenNames();

		CornerstoneTest.Same(source, target.Content);
	}

	[PresentationTestMethod]
	public void ShouldBindToLaterAddedElementPath()
	{
		TextBlock target;
		StackPanel stackPanel;

		var root = new TestRoot
		{
			Child = stackPanel = new StackPanel
			{
				Children =
				{
					(target = new TextBlock
					{
						Name = "target"
					})
				}
			}
		};
		root.RegisterChildrenNames();

		var binding = new Binding
		{
			ElementName = "source",
			Path = "Text",
			NameScope = new WeakReference<INameScope>(NameScope.GetNameScope(root))
		};

		target.Bind(TextBox.TextProperty, binding);

		stackPanel.Children.Add(new TextBlock
		{
			Name = "source",
			Text = "foo"
		});
		root.RegisterChildrenNames();
		CornerstoneTest.AreEqual("foo", target.Text);
	}

	#endregion
}