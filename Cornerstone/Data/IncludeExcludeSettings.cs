#region References

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Include / Exclude settings for filtering.
/// </summary>
[SourceReflection]
public class IncludeExcludeSettings
{
	#region Constructors

	/// <summary>
	/// Initializes settings.
	/// </summary>
	/// <param name="including"> The parameters to include when updating. </param>
	/// <param name="excluding"> The parameters to exclude when updating. </param>
	public IncludeExcludeSettings(IEnumerable<string> including, IEnumerable<string> excluding)
		: this(including, excluding, true)
	{
	}

	/// <summary>
	/// Initializes settings.
	/// </summary>
	/// <param name="including"> The parameters to include when updating. </param>
	/// <param name="excluding"> The parameters to exclude when updating. </param>
	/// <param name="ignoreCase"> If true then include / exclude collections are case-insensitive. </param>
	public IncludeExcludeSettings(IEnumerable<string> including, IEnumerable<string> excluding, bool ignoreCase = true)
	{
		var comparer = ignoreCase
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;

		IncludedProperties = including?.ToFrozenSet(comparer) ?? FrozenSet<string>.Empty;
		ExcludedProperties = excluding?.ToFrozenSet(comparer) ?? FrozenSet<string>.Empty;
	}

	static IncludeExcludeSettings()
	{
		Empty = new IncludeExcludeSettings();
	}

	/// <summary>
	/// Initializes settings.
	/// </summary>
	private IncludeExcludeSettings()
	{
		IncludedProperties = FrozenSet<string>.Empty;
		ExcludedProperties = FrozenSet<string>.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents an empty set of settings.
	/// </summary>
	public static IncludeExcludeSettings Empty { get; }

	/// <summary>
	/// Properties to be excluded.
	/// </summary>
	public FrozenSet<string> ExcludedProperties { get; }

	/// <summary>
	/// Properties to be included.
	/// </summary>
	public FrozenSet<string> IncludedProperties { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Returns true if the settings are empty.
	/// </summary>
	/// <returns> True if all settings are empty. </returns>
	public bool IsEmpty()
	{
		return (IncludedProperties.Count == 0)
			&& (ExcludedProperties.Count == 0);
	}

	/// <summary>
	/// Check to see if a property should be processed.
	/// </summary>
	/// <param name="propertyName"> The name of the property to test. </param>
	/// <returns> True if the property should be processed otherwise false. </returns>
	public bool ShouldProcessProperty(string propertyName)
	{
		if (propertyName == null)
		{
			return false;
		}

		if ((IncludedProperties.Count == 0)
			&& (ExcludedProperties.Count == 0))
		{
			return true;
		}

		if (IncludedProperties.Count == 0)
		{
			return !ExcludedProperties.Contains(propertyName);
		}

		if (ExcludedProperties.Count == 0)
		{
			return IncludedProperties.Contains(propertyName);
		}

		return IncludedProperties.Contains(propertyName)
			&& !ExcludedProperties.Contains(propertyName);
	}

	/// <summary>
	/// Creates a new set of settings with more exclusions.
	/// </summary>
	/// <param name="exclusions"> An extra set of exclusions. </param>
	/// <returns> The modified set of updateable settings. </returns>
	public IncludeExcludeSettings WithMoreExclusions(params string[] exclusions)
	{
		if ((exclusions == null)
			|| (exclusions.Length == 0))
		{
			return this;
		}

		var newExclusions = new HashSet<string>(ExcludedProperties);
		foreach (var exclusion in exclusions)
		{
			newExclusions.Add(exclusion);
		}

		return new IncludeExcludeSettings(IncludedProperties, newExclusions);
	}

	/// <summary>
	/// Creates a new set of settings with more settings.
	/// </summary>
	/// <param name="settings"> An extra set of settings. </param>
	/// <returns> The modified set of settings. </returns>
	public IncludeExcludeSettings WithMoreSettings(IncludeExcludeSettings settings)
	{
		if ((settings == null)
			|| settings.IsEmpty())
		{
			return this;
		}

		var newIncluded = new HashSet<string>(IncludedProperties);
		var newExcluded = new HashSet<string>(ExcludedProperties);

		foreach (var item in settings.IncludedProperties)
		{
			newIncluded.Add(item);
		}

		foreach (var item in settings.ExcludedProperties)
		{
			newExcluded.Add(item);
		}

		return new IncludeExcludeSettings(newIncluded, newExcluded);
	}

	#endregion
}