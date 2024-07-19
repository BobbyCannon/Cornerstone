#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Include / Exclude options for filtering.
/// </summary>
public class IncludeExcludeOptions
{
	#region Constructors

	/// <summary>
	/// Initializes options.
	/// </summary>
	/// <param name="including"> The parameters to include when updating. </param>
	/// <param name="excluding"> The parameters to exclude when updating. </param>
	public IncludeExcludeOptions(IEnumerable<string> including, IEnumerable<string> excluding)
		: this(including, excluding, true)
	{
	}

	/// <summary>
	/// Initializes options.
	/// </summary>
	/// <param name="including"> The parameters to include when updating. </param>
	/// <param name="excluding"> The parameters to exclude when updating. </param>
	/// <param name="ignoreCase"> If true then include / exclude collections are case-insensitive. </param>
	public IncludeExcludeOptions(IEnumerable<string> including, IEnumerable<string> excluding, bool ignoreCase)
	{
		var comparer = ignoreCase
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;

		ExcludedProperties = (excluding != null
				? new HashSet<string>(excluding, comparer)
				: new HashSet<string>(comparer))
			.ToReadOnlySet();

		IncludedProperties = (including != null
				? new HashSet<string>(including, comparer)
				: new HashSet<string>(comparer))
			.ToReadOnlySet();
	}

	static IncludeExcludeOptions()
	{
		Empty = new IncludeExcludeOptions([], [], true);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents an empty set of options.
	/// </summary>
	public static IncludeExcludeOptions Empty { get; }

	/// <summary>
	/// Properties to be excluded.
	/// </summary>
	public ReadOnlySet<string> ExcludedProperties { get; }

	/// <summary>
	/// Properties to be included.
	/// </summary>
	public ReadOnlySet<string> IncludedProperties { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Return update options with only exclusions.
	/// </summary>
	/// <param name="exclusions"> The exclusions. </param>
	/// <returns> The options with only exclusions. </returns>
	public static IncludeExcludeOptions FromExclusions(params string[] exclusions)
	{
		return new IncludeExcludeOptions(null, exclusions);
	}

	/// <summary>
	/// Returns true if the options are empty.
	/// </summary>
	/// <returns> True if all options are empty. </returns>
	public bool IsEmpty()
	{
		return (IncludedProperties.Count <= 0)
			&& (ExcludedProperties.Count <= 0);
	}

	/// <summary>
	/// Return update options with only including.
	/// </summary>
	/// <param name="including"> The including. </param>
	/// <returns> The options with only inclusions. </returns>
	public static IncludeExcludeOptions OnlyIncluding(params string[] including)
	{
		return new IncludeExcludeOptions(including, null);
	}

	/// <summary>
	/// Check to see if a property should be processed.
	/// </summary>
	/// <param name="propertyName"> The name of the property to test. </param>
	/// <returns> True if the property should be processed otherwise false. </returns>
	public bool ShouldProcessProperty(string propertyName)
	{
		if (IsEmpty())
		{
			return true;
		}

		if (IncludedProperties.Any() && !IncludedProperties.Contains(propertyName))
		{
			// Ignore this property because we only want to include it
			return false;
		}

		if (ExcludedProperties.Contains(propertyName))
		{
			// Ignore this property because we only want to exclude it
			return false;
		}

		return true;
	}

	/// <summary>
	/// Creates a new set of options with more exclusions.
	/// </summary>
	/// <param name="exclusions"> An extra set of exclusions. </param>
	/// <returns> The modified set of updateable options. </returns>
	public IncludeExcludeOptions WithMoreExclusions(params string[] exclusions)
	{
		var allExclusions = new HashSet<string>(ExcludedProperties);
		allExclusions.AddRange(exclusions);
		return new IncludeExcludeOptions(IncludedProperties, allExclusions);
	}

	/// <summary>
	/// Creates a new set of options with more options.
	/// </summary>
	/// <param name="options"> An extra set of options. </param>
	/// <returns> The modified set of options. </returns>
	public IncludeExcludeOptions WithMoreOptions(IncludeExcludeOptions options)
	{
		if (options == null)
		{
			return this;
		}

		return new IncludeExcludeOptions(
			ArrayExtensions.CombineArrays(IncludedProperties?.ToArray(), options?.IncludedProperties?.ToArray()),
			ArrayExtensions.CombineArrays(ExcludedProperties?.ToArray(), options?.ExcludedProperties?.ToArray())
		);
	}

	#endregion
}