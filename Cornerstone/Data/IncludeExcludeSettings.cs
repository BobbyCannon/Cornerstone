#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Include / Exclude settings for filtering.
/// </summary>
public class IncludeExcludeSettings : IObjectCodeWriter
{
	#region Constructors

	/// <summary>
	/// Initializes settings.
	/// </summary>
	public IncludeExcludeSettings() : this([], [], true)
	{
	}

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
	public IncludeExcludeSettings(IEnumerable<string> including, IEnumerable<string> excluding, bool ignoreCase)
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

	static IncludeExcludeSettings()
	{
		Empty = new IncludeExcludeSettings([], [], true);
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
	public ReadOnlySet<string> ExcludedProperties { get; }

	/// <summary>
	/// Properties to be included.
	/// </summary>
	public ReadOnlySet<string> IncludedProperties { get; }

	#endregion

	#region Methods

	/// <summary>
	/// 
	/// </summary>
	/// <param name="entityType"></param>
	/// <param name="actionType"></param>
	/// <returns></returns>
	public static IncludeExcludeSettings ForEntity(Type entityType, UpdateableAction actionType)
	{
		return Cache.GetSettings(entityType, actionType);
	}

	/// <summary>
	/// Return update settings with only exclusions.
	/// </summary>
	/// <param name="exclusions"> The exclusions. </param>
	/// <returns> The settings with only exclusions. </returns>
	public static IncludeExcludeSettings FromExclusions(params string[] exclusions)
	{
		return new IncludeExcludeSettings(null, exclusions);
	}

	/// <summary>
	/// Returns true if the settings are empty.
	/// </summary>
	/// <returns> True if all settings are empty. </returns>
	public bool IsEmpty()
	{
		return (IncludedProperties.Count <= 0)
			&& (ExcludedProperties.Count <= 0);
	}

	/// <summary>
	/// Return update settings with only including.
	/// </summary>
	/// <param name="including"> The including. </param>
	/// <returns> The settings with only inclusions. </returns>
	public static IncludeExcludeSettings OnlyIncluding(params string[] including)
	{
		return new IncludeExcludeSettings(including, null);
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
	/// Creates a new set of settings with more exclusions.
	/// </summary>
	/// <param name="exclusions"> An extra set of exclusions. </param>
	/// <returns> The modified set of updateable settings. </returns>
	public IncludeExcludeSettings WithMoreExclusions(params string[] exclusions)
	{
		var allExclusions = new HashSet<string>(ExcludedProperties);
		allExclusions.AddRange(exclusions);
		return new IncludeExcludeSettings(IncludedProperties, allExclusions);
	}

	/// <summary>
	/// Creates a new set of settings with more settings.
	/// </summary>
	/// <param name="settings"> An extra set of settings. </param>
	/// <returns> The modified set of settings. </returns>
	public IncludeExcludeSettings WithMoreOptions(IncludeExcludeSettings settings)
	{
		if (settings == null)
		{
			return this;
		}

		return new IncludeExcludeSettings(
			ArrayExtensions.CombineArrays(IncludedProperties?.ToArray(), settings?.IncludedProperties?.ToArray()),
			ArrayExtensions.CombineArrays(ExcludedProperties?.ToArray(), settings?.ExcludedProperties?.ToArray())
		);
	}

	#endregion

	/// <inheritdoc />
	public void Write(ICodeWriter writer)
	{
		if (IncludedProperties.Count > 0)
		{
			var properties = $"\"{string.Join("\", \"", IncludedProperties)}\"";
			writer.AppendLine($"IncludeExcludeSettings.OnlyIncluding({properties})");
		}
		else if (ExcludedProperties.Count > 0)
		{
			var properties = $"\"{string.Join("\", \"", ExcludedProperties)}\"";
			writer.AppendLine($"IncludeExcludeSettings.FromExclusions({properties})");
		}
		else
		{
			writer.AppendLine("IncludeExcludeSettings.Empty");
		}
	}
}