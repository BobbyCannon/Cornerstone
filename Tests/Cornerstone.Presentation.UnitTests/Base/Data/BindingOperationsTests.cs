#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class BindingOperationsTests
{
	#region Methods

	[PresentationTestMethod]
	public void GetBindingExpressionBaseReturnsBindingWhenBoundViaControlTheme()
	{
		var target = new Control();
		var binding = new Binding("Tag");
		var theme = new ControlTheme(typeof(Control))
		{
			Setters = { new Setter(Control.TagProperty, binding) }
		};

		target.Theme = theme;
		var root = new TestRoot(target);
		root.UpdateLayout();

		var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
		CornerstoneTest.IsNotNull(expression);
	}

	[PresentationTestMethod]
	public void GetBindingExpressionBaseReturnsBindingWhenBoundViaControlThemeStyle()
	{
		var target = new Control { Classes = { "foo" } };
		var binding = new Binding("Tag");
		var theme = new ControlTheme(typeof(Control))
		{
			Children =
			{
				new Style(x => x.Nesting().Class("foo"))
				{
					Setters = { new Setter(Control.TagProperty, binding) }
				}
			}
		};

		target.Theme = theme;
		var root = new TestRoot(target);
		root.UpdateLayout();

		var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
		CornerstoneTest.IsNotNull(expression);
	}

	[PresentationTestMethod]
	public void GetBindingExpressionBaseReturnsBindingWhenBoundViaControlThemeTemplateBinding()
	{
		var target = new Control();
		var binding = new TemplateBinding(Control.TagProperty);
		var theme = new ControlTheme(typeof(Control))
		{
			Setters = { new Setter(Control.TagProperty, binding) }
		};

		target.Theme = theme;
		var root = new TestRoot(target);
		root.UpdateLayout();

		var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
		CornerstoneTest.IsNotNull(expression);
	}

	[PresentationTestMethod]
	public void GetBindingExpressionBaseReturnsBindingWhenBoundViaStyle()
	{
		var target = new Control();
		var binding = new Binding("Tag");
		var style = new Style(x => x.OfType<Control>())
		{
			Setters = { new Setter(Control.TagProperty, binding) }
		};

		var root = new TestRoot();
		root.Styles.Add(style);
		root.Child = target;
		root.UpdateLayout();

		var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
		CornerstoneTest.IsNotNull(expression);
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.Animation)]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	[DataRow(BindingPriority.StyleTrigger)]
	public void GetBindingExpressionBaseReturnsExpressionWhenBound(BindingPriority priority)
	{
		var data = new { Tag = "foo" };
		var target = new Control { DataContext = data };
		var binding = new Binding("Tag") { Priority = priority };
		target.Bind(Control.TagProperty, binding);

		var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
		CornerstoneTest.IsNotNull(expression);
	}

	[PresentationTestMethod]
	public void GetBindingExpressionBaseReturnsExpressionWhenBoundLocallyWithBindingError()
	{
		// Target has no data context so binding will fail.
		var target = new Control();
		var binding = new Binding("Tag");
		target.Bind(Control.TagProperty, binding);

		var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
		CornerstoneTest.IsNotNull(expression);
	}

	[PresentationTestMethod]
	public void GetBindingExpressionBaseReturnsExpressionWhenBoundToMultiBinding()
	{
		var data = new { Tag = "foo" };
		var target = new Control { DataContext = data };
		var binding = new MultiBinding
		{
			Converter = new FuncMultiValueConverter<object, string>(x => string.Join(',', x)),
			Bindings =
			{
				new Binding("Tag"),
				new Binding("Tag")
			}
		};

		target.Bind(Control.TagProperty, binding);

		var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
		CornerstoneTest.IsNotNull(expression);
	}

	[PresentationTestMethod]
	public void GetBindingExpressionBaseReturnsNullWhenNotBound()
	{
		var target = new Control();
		var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
		CornerstoneTest.IsNull(expression);
	}

	#endregion
}