#region References

using System;
using System.IO;
using System.Net;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

/// <summary>
/// Holds options for converting text to HTML.
/// </summary>
public class HtmlOptions
{
	#region Constructors

	/// <summary>
	/// Creates a default HtmlOptions instance.
	/// </summary>
	public HtmlOptions()
	{
		TabSize = 4;
	}

	/// <summary>
	/// Creates a new HtmlOptions instance that copies applicable options from the <see cref="TextEditorSettings" />.
	/// </summary>
	public HtmlOptions(TextEditorSettings settings) : this()
	{
		if (settings == null)
		{
			throw new ArgumentNullException(nameof(settings));
		}
		TabSize = settings.IndentationSize;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The amount of spaces a tab gets converted to.
	/// </summary>
	public int TabSize { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets whether the color needs to be written out to HTML.
	/// </summary>
	public virtual bool ColorNeedsSpanForStyling(HighlightingColor color)
	{
		if (color == null)
		{
			throw new ArgumentNullException(nameof(color));
		}
		return !string.IsNullOrEmpty(color.ToCss());
	}

	/// <summary>
	/// Writes the HTML attribute for the style to the text writer.
	/// </summary>
	public virtual void WriteStyleAttributeForColor(TextWriter writer, HighlightingColor color)
	{
		if (writer == null)
		{
			throw new ArgumentNullException(nameof(writer));
		}
		if (color == null)
		{
			throw new ArgumentNullException(nameof(color));
		}
		writer.Write(" style=\"");
		writer.Write(WebUtility.HtmlEncode(color.ToCss()));
		writer.Write('"');
	}

	#endregion
}