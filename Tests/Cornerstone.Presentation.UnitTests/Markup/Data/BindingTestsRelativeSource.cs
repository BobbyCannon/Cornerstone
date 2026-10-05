#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsRelativeSource : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldBindToDerivedAncestorType()
	{
		TextBlock target;
		var root = new TestRoot
		{
			Child = new Border
			{
				Name = "border",
				Child = target = new TextBlock()
			}
		};

		var binding = new Binding
		{
			Path = "Name",
			RelativeSource = new RelativeSource
			{
				AncestorType = typeof(Decorator)
			}
		};

		target.Bind(TextBox.TextProperty, binding);
		CornerstoneTest.AreEqual("border", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldBindToFirstAncestor()
	{
		TextBlock target;
		var root = new TestRoot
		{
			Child = new Decorator
			{
				Name = "decorator",
				Child = target = new TextBlock()
			}
		};

		var binding = new Binding
		{
			Path = "Name",
			RelativeSource = new RelativeSource
			{
				AncestorType = typeof(Decorator)
			}
		};

		target.Bind(TextBox.TextProperty, binding);
		CornerstoneTest.AreEqual("decorator", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldBindToSecondAncestor()
	{
		TextBlock target;
		var root = new TestRoot
		{
			Child = new Decorator
			{
				Name = "decorator1",
				Child = new Decorator
				{
					Name = "decorator2",
					Child = target = new TextBlock()
				}
			}
		};

		var binding = new Binding
		{
			Path = "Name",
			RelativeSource = new RelativeSource
			{
				AncestorType = typeof(Decorator),
				AncestorLevel = 2
			}
		};

		target.Bind(TextBox.TextProperty, binding);
		CornerstoneTest.AreEqual("decorator1", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldProduceNullIfAncestorNotFound()
	{
		TextBlock target;
		var root = new TestRoot
		{
			Child = new Decorator
			{
				Name = "decorator",
				Child = target = new TextBlock()
			}
		};

		var binding = new Binding
		{
			Path = "Name",
			RelativeSource = new RelativeSource
			{
				AncestorType = typeof(Decorator),
				AncestorLevel = 2
			}
		};

		target.Bind(TextBox.TextProperty, binding);
		CornerstoneTest.IsNull(target.Text);
	}

	[PresentationTestMethod]
	public void ShouldUpdateWhenDetachedAndAttachedToVisualTree()
	{
		TextBlock target;
		Decorator decorator1;
		Decorator decorator2;
		var root1 = new TestRoot
		{
			Child = decorator1 = new Decorator
			{
				Name = "decorator1",
				Child = target = new TextBlock()
			}
		};

		var root2 = new TestRoot
		{
			Child = decorator2 = new Decorator
			{
				Name = "decorator2"
			}
		};

		var binding = new Binding
		{
			Path = "Name",
			RelativeSource = new RelativeSource
			{
				AncestorType = typeof(Decorator)
			}
		};

		target.Bind(TextBox.TextProperty, binding);
		CornerstoneTest.AreEqual("decorator1", target.Text);

		decorator1.Child = null;
		CornerstoneTest.IsNull(target.Text);

		decorator2.Child = target;
		CornerstoneTest.AreEqual("decorator2", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldUpdateWhenDetachedAndAttachedToVisualTreeWithBindingPath()
	{
		TextBlock target;
		Decorator decorator1;
		Decorator decorator2;

		var viewModel = new { Value = "Foo" };

		var root1 = new TestRoot
		{
			Child = decorator1 = new Decorator
			{
				Name = "decorator1",
				Child = target = new TextBlock()
			},
			DataContext = viewModel
		};

		var root2 = new TestRoot
		{
			Child = decorator2 = new Decorator
			{
				Name = "decorator2"
			},
			DataContext = viewModel
		};

		var binding = new Binding
		{
			Path = "DataContext.Value",
			RelativeSource = new RelativeSource
			{
				AncestorType = typeof(Decorator)
			}
		};

		target.Bind(TextBox.TextProperty, binding);
		CornerstoneTest.AreEqual("Foo", target.Text);

		decorator1.Child = null;
		CornerstoneTest.IsNull(target.Text);

		decorator2.Child = target;
		CornerstoneTest.AreEqual("Foo", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldUpdateWhenDetachedAndAttachedToVisualTreeWithComplexBindingPath()
	{
		TextBlock target;
		Decorator decorator1;
		Decorator decorator2;

		var vm = new { Foo = new { Value = "Foo" } };

		var root1 = new TestRoot
		{
			Child = decorator1 = new Decorator
			{
				Name = "decorator1",
				Child = target = new TextBlock()
			},
			DataContext = vm
		};

		var root2 = new TestRoot
		{
			Child = decorator2 = new Decorator
			{
				Name = "decorator2"
			},
			DataContext = vm
		};

		var binding = new Binding
		{
			Path = "DataContext.Foo.Value",
			RelativeSource = new RelativeSource
			{
				AncestorType = typeof(Decorator)
			}
		};

		target.Bind(TextBox.TextProperty, binding);
		CornerstoneTest.AreEqual("Foo", target.Text);

		decorator1.Child = null;
		CornerstoneTest.IsNull(target.Text);

		decorator2.Child = target;
		CornerstoneTest.AreEqual("Foo", target.Text);
	}

	#endregion
}