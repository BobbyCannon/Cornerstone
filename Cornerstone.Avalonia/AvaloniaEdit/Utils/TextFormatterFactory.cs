#region References

using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Utils;

/// <summary>
/// Creates TextFormatter instances that with the correct TextFormattingMode, if running on .NET 4.0.
/// </summary>
internal static class TextFormatterFactory
{
	#region Methods

	/// <summary>
	/// Creates a <see cref="TextFormatter" /> using the formatting mode used by the specified owner object.
	/// </summary>
	public static TextFormatter Create(Control owner)
	{
		return TextFormatter.Current;
	}

	/// <summary>
	/// Creates formatted text.
	/// </summary>
	/// <param name="element"> The owner element. The text formatter setting are read from this element. </param>
	/// <param name="text"> The text. </param>
	/// <param name="typeface"> The typeface to use. If this parameter is null, the typeface of the <paramref name="element" /> will be used. </param>
	/// <param name="emSize"> The font size. If this parameter is null, the font size of the <paramref name="element" /> will be used. </param>
	/// <param name="foreground"> The foreground color. If this parameter is null, the foreground of the <paramref name="element" /> will be used. </param>
	/// <returns> A FormattedText object using the specified settings. </returns>
	public static FormattedText CreateFormattedText(Control element, string text, Typeface typeface, double? emSize, IBrush foreground)
	{
		if (element == null)
		{
			throw new ArgumentNullException(nameof(element));
		}
		if (text == null)
		{
			throw new ArgumentNullException(nameof(text));
		}
		if (typeface == default)
		{
			typeface = element.CreateTypeface();
		}
		if (emSize == null)
		{
			emSize = TextElement.GetFontSize(element);
		}
		if (foreground == null)
		{
			foreground = TextElement.GetForeground(element);
		}

		return new FormattedText(
			text,
			CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			typeface,
			emSize.Value,
			foreground);
	}

	#endregion
}