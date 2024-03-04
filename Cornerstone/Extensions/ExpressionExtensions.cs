#region References

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for expression and predicates.
/// </summary>
public static class ExpressionExtensions
{
	#region Methods

	/// <summary>
	/// Specifies additional related data to be further included based on a related type that was just included.
	/// </summary>
	/// <typeparam name="T"> The type of entity being queried. </typeparam>
	/// <typeparam name="TPreviousProperty"> The type of the entity that was just included. </typeparam>
	/// <typeparam name="TProperty"> The type of the related entity to be included. </typeparam>
	/// <param name="source"> The source query. </param>
	/// <param name="include"> A lambda expression representing the navigation property to be included (<c> t =&gt; t.Property1 </c>). </param>
	/// <returns> A new query with the related data included. </returns>
	public static IIncludableQueryable<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(this IIncludableQueryable<T, ICollection<TPreviousProperty>> source, Expression<Func<TPreviousProperty, TProperty>> include) where T : class
	{
		return source.ThenInclude(include);
	}

	/// <summary>
	/// Try to get the name of an expression where the expression must be a property.
	/// </summary>
	/// <typeparam name="T"> The type passed into the expression. </typeparam>
	/// <param name="expression"> The expression to process. </param>
	/// <param name="name"> The name of the expression. </param>
	/// <returns> True if the property name was found otherwise false. </returns>
	public static bool TryGetPropertyName<T>(this Expression<Func<T, object>> expression, out string name)
	{
		var exp = expression.Body;

		if (exp is UnaryExpression cast)
		{
			exp = cast.Operand;
		}

		if (exp is MemberExpression { Member.MemberType: MemberTypes.Property } member)
		{
			name = member.Member.Name;
			return true;
		}

		name = null;
		return false;
	}

	#endregion
}