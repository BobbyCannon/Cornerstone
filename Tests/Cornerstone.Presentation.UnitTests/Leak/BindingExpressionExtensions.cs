#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.ExpressionNodes;
using Cornerstone.Presentation.Data.Core.Parsers;

#endregion

namespace Cornerstone.Presentation.UnitTests.Leak;

/// <summary>
/// Test extensions for creating BindingExpression instances from lambda expressions.
/// </summary>
internal static class BindingExpressionExtensions
{
	#region Methods

	[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
	public static BindingExpression CreateBindingExpression<TIn, TOut>(
		TIn source,
		Expression<Func<TIn, TOut>> expression,
		IValueConverter converter = null,
		CultureInfo converterCulture = null,
		object converterParameter = null,
		bool enableDataValidation = false,
		Optional<object> fallbackValue = default,
		BindingMode mode = BindingMode.OneWay,
		BindingPriority priority = BindingPriority.LocalValue,
		object targetNullValue = null,
		bool allowReflection = true)
		where TIn : class
	{
		var path = BindingExpressionVisitor<TIn>.BuildPath(expression);
		var nodes = new List<ExpressionNode>();
		path.BuildExpression(nodes, out _);
		var fallback = fallbackValue.HasValue ? fallbackValue.Value : PresentationProperty.UnsetValue;

		return new BindingExpression(
			source,
			nodes,
			fallback,
			converter: converter,
			converterCulture: converterCulture,
			converterParameter: converterParameter,
			enableDataValidation: enableDataValidation,
			mode: mode,
			priority: priority,
			targetNullValue: targetNullValue,
			targetTypeConverter: allowReflection ? TargetTypeConverter.GetReflectionConverter() : TargetTypeConverter.GetDefaultConverter());
	}

	#endregion
}