#region References

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Compare;

/// <summary>
/// The options for the comparers.
/// </summary>
public struct ComparerOptions
{
	#region Constructors

	/// <summary>
	/// Initialize the options for the comparer.
	/// </summary>
	public ComparerOptions() : this(
		floatTolerance: float.Epsilon,
		doubleTolerance: double.Epsilon,
		ignoreMissingProperties: false,
		ignoreObjectTypes: true,
		stringComparison: StringComparison.CurrentCulture,
		includeExcludeOptions: new())
	{
	}

	/// <summary>
	/// Initialize the options for the comparer.
	/// </summary>
	/// <param name="doubleTolerance"> The tolerance for double. </param>
	/// <param name="floatTolerance"> The tolerance for float. </param>
	/// <param name="ignoreMissingProperties"> Option to ignore missing properties. </param>
	/// <param name="ignoreObjectTypes"> Option to ignore the property type. </param>
	/// <param name="stringComparison"> The default comparison for comparing strings. </param>
	/// <param name="includeExcludeOptions"> An optional set of included or excluded properties. </param>
	public ComparerOptions(double doubleTolerance, float floatTolerance, bool ignoreMissingProperties,
		bool ignoreObjectTypes, StringComparison stringComparison,
		Dictionary<Type, IncludeExcludeOptions> includeExcludeOptions)
	{
		DoubleTolerance = doubleTolerance;
		FloatTolerance = floatTolerance;
		IgnoreMissingProperties = ignoreMissingProperties;
		IgnoreObjectTypes = ignoreObjectTypes;
		IncludeExcludeOptions = includeExcludeOptions ?? new();
		StringComparison = stringComparison;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The tolerance when comparing double numbers.
	/// </summary>
	public double DoubleTolerance { get; set; }

	/// <summary>
	/// The tolerance when comparing float numbers.
	/// </summary>
	public float FloatTolerance { get; set; }

	/// <summary>
	/// Ignore missing properties.
	/// Ex. Expected is ClientAccount and Actual is Account. This means the
	/// comparer can skip the [ClientAccount].[Id] that Account does not have.
	/// </summary>
	public bool IgnoreMissingProperties { get; set; }

	/// <summary>
	/// Options to ignore object types.
	/// Ex. Expected is Account and Actual is ClientAccount. This means the
	/// comparer will allow comparing of the two objects. The comparer will
	/// expect all Expected properties to be on the Actual object. See
	/// <see cref="IgnoreMissingProperties" /> if you want to ignore Expected
	/// properties that do not exist on Actual.
	/// </summary>
	public bool IgnoreObjectTypes { get; set; }

	/// <summary>
	/// Include / Exclude options for each type.
	/// </summary>
	public Dictionary<Type, IncludeExcludeOptions> IncludeExcludeOptions { get; set; }

	/// <summary>
	/// The comparison to use for strings.
	/// </summary>
	public StringComparison StringComparison { get; set; }

	#endregion

	#region Methods

	public void IgnoreProperty<T>(Expression<Func<T, object>> expression)
	{
		var name = expression.GetExpressionName();

		IncludeExcludeOptions.AddOrUpdate(typeof(T),
			() => new IncludeExcludeOptions(null, [name]),
			existing => existing.WithMoreExclusions(name)
		);
	}

	#endregion
}