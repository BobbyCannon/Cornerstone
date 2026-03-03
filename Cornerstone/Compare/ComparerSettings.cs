#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Compare;

/// <summary>
/// The options for the comparers.
/// </summary>
[SourceReflection]
public struct ComparerSettings
{
	#region Constructors

	/// <summary>
	/// Initialize the options for the comparer.
	/// </summary>
	public ComparerSettings() : this(
		floatTolerance: float.Epsilon,
		doubleTolerance: double.Epsilon,
		globalIncludeExcludeSettings: IncludeExcludeSettings.Empty,
		ignoreMissingDictionaryEntries: false,
		ignoreMissingProperties: false,
		ignoreObjectTypes: true,
		maxDepth: int.MaxValue,
		stringComparison: StringComparison.CurrentCulture,
		typeIncludeExcludeOptions: new())
	{
	}

	/// <summary>
	/// Initialize the options for the comparer.
	/// </summary>
	/// <param name="doubleTolerance"> The tolerance for double. </param>
	/// <param name="floatTolerance"> The tolerance for float. </param>
	/// <param name="globalIncludeExcludeSettings"> An optional set of included or excluded properties for all types. </param>
	/// <param name="ignoreMissingProperties"> Option to ignore missing properties. </param>
	/// <param name="ignoreMissingDictionaryEntries"> Options to ignore missing dictionary entries (keys). </param>
	/// <param name="ignoreObjectTypes"> Option to ignore the property type. </param>
	/// <param name="maxDepth"> The maximum depth to compare. </param>
	/// <param name="stringComparison"> The default comparison for comparing strings. </param>
	/// <param name="typeIncludeExcludeOptions"> An optional set of included or excluded properties. </param>
	public ComparerSettings(double doubleTolerance, float floatTolerance,
		bool ignoreMissingProperties, bool ignoreMissingDictionaryEntries,
		bool ignoreObjectTypes, int maxDepth, StringComparison stringComparison,
		IncludeExcludeSettings globalIncludeExcludeSettings,
		Dictionary<Type, IncludeExcludeSettings> typeIncludeExcludeOptions)
	{
		DoubleTolerance = doubleTolerance;
		FloatTolerance = floatTolerance;
		GlobalIncludeExcludeSettings = globalIncludeExcludeSettings;
		IgnoreMissingDictionaryEntries = ignoreMissingDictionaryEntries;
		IgnoreMissingProperties = ignoreMissingProperties;
		IgnoreObjectTypes = ignoreObjectTypes;
		MaxDepth = maxDepth;
		StringComparison = stringComparison;
		TypeIncludeExcludeSettings = typeIncludeExcludeOptions ?? new();
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
	/// Include / Exclude options for all types.
	/// </summary>
	public IncludeExcludeSettings GlobalIncludeExcludeSettings { get; set; }

	/// <summary>
	/// Allow dictionaries to have missing entries.
	/// </summary>
	public bool IgnoreMissingDictionaryEntries { get; set; }

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
	/// The maximum depth to compare.
	/// </summary>
	public int MaxDepth { get; set; }

	/// <summary>
	/// The comparison to use for strings.
	/// </summary>
	public StringComparison StringComparison { get; set; }

	/// <summary>
	/// Include / Exclude options for specific types.
	/// </summary>
	public Dictionary<Type, IncludeExcludeSettings> TypeIncludeExcludeSettings { get; set; }

	#endregion

	#region Methods

	public void IgnoreTypeProperty<T>(string propertyName)
	{
		var type = typeof(T);

		if (TypeIncludeExcludeSettings.TryGetValue(type, out var settings))
		{
			TypeIncludeExcludeSettings[type] = settings.WithMoreExclusions(propertyName);
			return;
		}

		TypeIncludeExcludeSettings.Add(type, new IncludeExcludeSettings(null, [propertyName]));
	}

	#endregion
}