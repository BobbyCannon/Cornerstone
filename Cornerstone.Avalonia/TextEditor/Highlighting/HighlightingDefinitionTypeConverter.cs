#region References

using System;
using System.ComponentModel;
using System.Globalization;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// Converts between strings and <see cref="IHighlightingDefinition" /> by treating the string as the definition name
/// and calling <c> HighlightingManager.Instance.<see cref="HighlightingManager.GetDefinition"> GetDefinition </see>(name) </c>.
/// </summary>
public sealed class HighlightingDefinitionTypeConverter : TypeConverter
{
	#region Methods

	/// <inheritdoc />
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	/// <inheritdoc />
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		return destinationType == typeof(string);
	}

	/// <inheritdoc />
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		var definitionName = value as string;
		return definitionName != null ? HighlightingManager.Instance.GetDefinition(definitionName) : null;
	}

	/// <inheritdoc />
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (value is IHighlightingDefinition definition && (destinationType == typeof(string)))
		{
			return definition.Name;
		}
		return null;
	}

	#endregion
}