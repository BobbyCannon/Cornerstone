﻿#region References

using System.Collections.Generic;
using System.ComponentModel;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// A highlighting definition.
/// </summary>
[TypeConverter(typeof(HighlightingDefinitionTypeConverter))]
public interface IHighlightingDefinition
{
	#region Properties

	/// <summary>
	/// Gets the extensions for the resource.
	/// </summary>
	string[] Extensions { get; }

	/// <summary>
	/// Gets the main rule set.
	/// </summary>
	HighlightingRuleSet MainRuleSet { get; }

	/// <summary>
	/// Gets the name of the highlighting definition.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the list of named highlighting colors.
	/// </summary>
	IEnumerable<HighlightingColor> NamedHighlightingColors { get; }

	/// <summary>
	/// Gets the list of properties.
	/// </summary>
	IDictionary<string, string> Properties { get; }

	/// <summary>
	/// Gets the resource name.
	/// </summary>
	string ResourceName { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets a named highlighting color.
	/// </summary>
	/// <returns> The highlighting color, or null if it is not found. </returns>
	HighlightingColor GetNamedColor(string name);

	/// <summary>
	/// Gets a rule set by name.
	/// </summary>
	/// <returns> The rule set, or null if it is not found. </returns>
	HighlightingRuleSet GetNamedRuleSet(string name);

	#endregion
}