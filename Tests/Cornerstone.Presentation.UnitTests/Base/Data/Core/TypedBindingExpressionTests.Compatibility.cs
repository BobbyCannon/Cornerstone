#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

/// <summary>
/// Tests which compare the behaviour of <see cref="TypedBindingExpression{TSource,TValue}" /> with
/// the untyped <see cref="BindingExpression" /> for bindings which are eligible for the typed path.
/// </summary>
/// <remarks>
/// Each test is run twice: once with a binding which produces a typed expression and once with an
/// equivalent binding which produces an untyped expression. The assertions describe the behaviour
/// of the untyped expression, i.e. the behaviour of the binding before typed binding expressions
/// were introduced, so a failure in the <c> typed: true </c> case is a user-visible breaking change.
/// </remarks>
public partial class TypedBindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void IncompatibleDataContextShouldLogABindingError(bool typed)
	{
		// When the DataContext isn't of the expected type the untyped expression logs a binding
		// error; the typed expression silently produces no value.
		var errors = new List<string>();

		using var sink = TestLogSink.Start((level, area, source, template, values) =>
		{
			if ((level >= LogEventLevel.Warning) && (area == LogArea.Binding))
			{
				errors.Add(template);
			}
		});

		var target = new TextBlock { DataContext = new ViewModel { StringValue = "foo" } };
		var root = new TestRoot
		{
			Child = target
		};

		AssertExpressionType(typed, target.Bind(TextBlock.TextProperty, CreateStringBinding(typed)));

		CornerstoneTest.AreEqual("foo", target.Text);

		target.DataContext = new object();

		CornerstoneTest.NotEmpty(errors);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void NullDataContextShouldNotBreakPropertyInheritance(bool typed)
	{
		// As above, but for an inherited property: applying the property's default value at
		// LocalValue priority stops the value being inherited from the parent.
		var target = new TextBlock();
		var root = new TestRoot
		{
			Child = target,
			[TextBlock.FontSizeProperty] = 30.0
		};

		AssertExpressionType(typed, target.Bind(TextBlock.FontSizeProperty, CreateDoubleBinding(typed)));

		CornerstoneTest.AreEqual(30.0, target.FontSize);
		CornerstoneTest.AreEqual(BindingPriority.Inherited, target.GetDiagnostic(TextBlock.FontSizeProperty).Priority);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void NullDataContextShouldNotOverrideStyleSetter(bool typed)
	{
		// A binding which has no value must not contribute a value to the target property,
		// otherwise the property's default value is applied at LocalValue priority, hiding the
		// value from the style setter.
		var target = new TextBlock();
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<TextBlock>())
				{
					Setters = { new Setter(TextBlock.TextProperty, "styled") }
				}
			},
			Child = target
		};

		AssertExpressionType(typed, target.Bind(TextBlock.TextProperty, CreateStringBinding(typed)));

		CornerstoneTest.AreEqual("styled", target.Text);
		CornerstoneTest.AreEqual(BindingPriority.Style, target.GetDiagnostic(TextBlock.TextProperty).Priority);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void SettingObjectTargetPropertyToADifferentTypeShouldNotThrow(bool typed)
	{
		// The binding value type only needs to be assignable to the target property type, so a
		// string can be bound to an object-typed property. Writing a value of any other type to
		// that property must not throw when the binding reads the new target value.
		var data = new ViewModel { StringValue = "foo" };
		var target = new TextBlock { DataContext = data };
		var root = new TestRoot
		{
			Child = target
		};

		AssertExpressionType(typed, target.Bind(TextBlock.TagProperty, CreateStringBinding(typed)));

		CornerstoneTest.AreEqual("foo", target.Tag);

		var ex = Record.Exception(() => target.Tag = 5);

		CornerstoneTest.IsNull(ex);
		CornerstoneTest.AreEqual(5, target.Tag);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void SettingObjectTargetPropertyToNullShouldNotThrow(bool typed)
	{
		// As above, but with a value-typed binding: writing null to the object-typed target
		// property must not throw when the binding reads the new target value.
		var data = new ViewModel { DoubleValue = 1.0 };
		var target = new TextBlock { DataContext = data };
		var root = new TestRoot
		{
			Child = target
		};

		AssertExpressionType(typed, target.Bind(TextBlock.TagProperty, CreateDoubleBinding(typed)));

		CornerstoneTest.AreEqual(1.0, target.Tag);

		var ex = Record.Exception(() => target.SetValue(TextBlock.TagProperty, null));

		CornerstoneTest.IsNull(ex);
		CornerstoneTest.IsNull(target.Tag);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void SourceGetterExceptionShouldClearTheTargetValue(bool typed)
	{
		// When the source getter throws, the untyped expression reports a binding error and
		// reverts the target to its default value; the typed expression silently leaves the stale
		// value in place.
		var errors = new List<string>();

		using var sink = TestLogSink.Start((level, area, source, template, values) =>
		{
			if ((level >= LogEventLevel.Warning) && (area == LogArea.Binding))
			{
				errors.Add(template);
			}
		});

		var data = new ViewModel { StringValue = "foo" };
		var target = new TextBlock { DataContext = data };
		var root = new TestRoot
		{
			Child = target
		};

		AssertExpressionType(typed, target.Bind(TextBlock.TextProperty, CreateStringBinding(typed)));

		CornerstoneTest.AreEqual("foo", target.Text);

		data.ThrowOnGet = true;
		data.RaisePropertyChanged(nameof(ViewModel.StringValue));

		CornerstoneTest.IsNull(target.Text);
		CornerstoneTest.NotEmpty(errors);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void StylePriorityBindingWithNullDataContextShouldNotBreakPropertyInheritance(bool typed)
	{
		// The same problem occurs for bindings at a priority other than LocalValue, such as a
		// binding in a style setter.
		var target = new TextBlock();
		var root = new TestRoot
		{
			Child = target,
			[TextBlock.FontSizeProperty] = 30.0
		};

		var binding = CreateDoubleBinding(typed);
		binding.Priority = BindingPriority.Style;

		AssertExpressionType(typed, target.Bind(TextBlock.FontSizeProperty, binding));

		CornerstoneTest.AreEqual(30.0, target.FontSize);
		CornerstoneTest.AreEqual(BindingPriority.Inherited, target.GetDiagnostic(TextBlock.FontSizeProperty).Priority);
	}

	private static void AssertExpressionType(bool typed, BindingExpressionBase expression)
	{
		if (typed)
		{
			CornerstoneTest.IsNotType<BindingExpression>(expression);
		}
		else
		{
			CornerstoneTest.IsType<BindingExpression>(expression);
		}
	}

	private static CompiledBinding CreateDoubleBinding(bool typed, BindingMode mode = BindingMode.OneWay)
	{
		var builder = new CompiledBindingPathBuilder();

		if (typed)
		{
			builder.Property(
				new ClrPropertyInfo<ViewModel, double>(
					nameof(ViewModel.DoubleValue),
					o => o.DoubleValue,
					(o, v) => o.DoubleValue = v),
				PropertyInfoAccessorFactory.CreateInpcPropertyAccessor,
				false);
		}
		else
		{
			builder.Property(
				new ClrPropertyInfo(
					nameof(ViewModel.DoubleValue),
					o => ((ViewModel) o).DoubleValue,
					(o, v) => ((ViewModel) o).DoubleValue = (double) v!,
					typeof(double)),
				PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);
		}

		return new CompiledBinding(builder.Build()) { Mode = mode };
	}

	private static CompiledBinding CreateStringBinding(bool typed, BindingMode mode = BindingMode.OneWay)
	{
		if (typed)
		{
			return CreateBinding(mode);
		}

		var path = new CompiledBindingPathBuilder().Property(
			new ClrPropertyInfo(
				nameof(ViewModel.StringValue),
				o => ((ViewModel) o).StringValue,
				(o, v) => ((ViewModel) o).StringValue = (string) v,
				typeof(string)),
			PropertyInfoAccessorFactory.CreateInpcPropertyAccessor).Build();

		return new CompiledBinding(path) { Mode = mode };
	}

	#endregion
}