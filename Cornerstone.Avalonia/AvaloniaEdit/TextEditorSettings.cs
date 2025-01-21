#region References

using System;
using System.Collections.Generic;
using System.Reflection;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit;

/// <summary>
/// A container for the text editor options.
/// </summary>
public class TextEditorSettings : Notifiable
{
	#region Fields

	private int _indentationSize;
	private double _wordWrapIndentation;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an empty instance of TextEditorOptions.
	/// </summary>
	public TextEditorSettings() : this(null)
	{
	}

	/// <summary>
	/// Initializes a new instance of TextEditorOptions by copying all values
	/// from <paramref name="settings" /> to the new instance.
	/// </summary>
	public TextEditorSettings(TextEditorSettings settings)
	{
		_indentationSize = 4;

		AllowScrollBelowDocument = false;
		ColumnRulerPositions = new List<int> { 80 };
		CutCopyWholeLine = true;
		EnableEmailHyperlinks = true;
		EnableHyperlinks = true;
		EnableImeSupport = true;
		EnableRectangularSelection = true;
		EnableTextDragDrop = true;
		EndOfLineCRGlyph = "\\r";
		EndOfLineCRLFGlyph = "¶";
		EndOfLineLFGlyph = "\\n";
		ExtendSelectionOnMouseUp = true;
		HighlightCurrentLine = true;
		HideCursorWhileTyping = true;
		InheritWordWrapIndentation = true;
		RequireControlModifierForHyperlinkClick = true;
		ShowBoxForControlCharacters = true;
		ShowSpacesGlyph = "\u00B7";
		ShowTabsGlyph = "\u2192";
		ShowTabs = false;

		if (settings == null)
		{
			return;
		}

		// get all the fields in the class
		var fields = typeof(TextEditorSettings).GetRuntimeFields();

		// copy each value over to 'this'
		foreach (var fi in fields)
		{
			if (!fi.IsStatic)
			{
				fi.SetValue(this, fi.GetValue(settings));
			}
		}
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets whether the user can scroll below the bottom of the document.
	/// The default value is true; but it's a good idea to set this property to true when using folding.
	/// </summary>
	public bool AllowScrollBelowDocument { get; set; }

	/// <summary>
	/// Gets/Sets if the user is allowed to enable/disable overstrike mode.
	/// </summary>
	public bool AllowToggleOverstrikeMode { get; set; }

	/// <summary>
	/// Gets/Sets the positions the column rulers should be shown.
	/// </summary>
	public IEnumerable<int> ColumnRulerPositions { get; set; }

	/// <summary>
	/// Gets/Sets whether to use spaces for indentation instead of tabs.
	/// </summary>
	/// <remarks> The default value is <c> false </c>. </remarks>
	[AlsoNotifyFor(nameof(IndentationString))]
	public bool ConvertTabsToSpaces { get; set; }

	/// <summary>
	/// Gets/Sets whether copying without a selection copies the whole current line.
	/// </summary>
	public bool CutCopyWholeLine { get; set; }

	/// <summary>
	/// Gets/Sets whether to enable clickable hyperlinks for e-mail addresses in the editor.
	/// </summary>
	/// <remarks> The default value is True. </remarks>
	public bool EnableEmailHyperlinks { get; set; }

	/// <summary>
	/// Gets/Sets whether to enable clickable hyperlinks in the editor.
	/// </summary>
	/// <remarks> The default value is True. </remarks>
	public bool EnableHyperlinks { get; set; }

	/// <summary>
	/// Gets/Sets whether the support for Input Method Editors (IME)
	/// for non-alphanumeric scripts (Chinese, Japanese, Korean, ...) is enabled.
	/// </summary>
	public bool EnableImeSupport { get; set; }

	/// <summary>
	/// Enables rectangular selection (press ALT and select a rectangle)
	/// </summary>
	public bool EnableRectangularSelection { get; set; }

	/// <summary>
	/// Enable dragging text within the text area.
	/// </summary>
	public bool EnableTextDragDrop { get; set; }

	/// <summary>
	/// Gets/Sets whether the user can set the caret behind the line ending
	/// (into "space").
	/// Note that space is always used (independent of this setting)
	/// when doing rectangle selections.
	/// </summary>
	public bool EnableVirtualSpace { get; set; }

	/// <summary>
	/// Gets/Sets the char to show for CR (\r) when ShowEndOfLine option is enabled
	/// </summary>
	/// <remarks> The default value is <c> \r </c>. </remarks>
	public string EndOfLineCRGlyph { get; set; }

	/// <summary>
	/// Gets/Sets the char to show for CRLF (\r\n) when ShowEndOfLine option is enabled
	/// </summary>
	/// <remarks> The default value is <c> ¶ </c>. </remarks>
	public string EndOfLineCRLFGlyph { get; set; }

	/// <summary>
	/// Gets/Sets the char to show for LF (\n) when ShowEndOfLine option is enabled
	/// </summary>
	/// <remarks> The default value is <c> \n </c>. </remarks>
	public string EndOfLineLFGlyph { get; set; }

	/// <summary>
	/// Gets/Sets if the mouse up event should extend the editor selection to the mouse position.
	/// </summary>
	public bool ExtendSelectionOnMouseUp { get; set; }

	/// <summary>
	/// Gets/Sets if mouse cursor should be hidden while user is typing.
	/// </summary>
	public bool HideCursorWhileTyping { get; set; }

	/// <summary>
	/// Gets/Sets if current line should be shown.
	/// </summary>
	public bool HighlightCurrentLine { get; set; }

	/// <summary>
	/// Gets/Sets the width of one indentation unit.
	/// </summary>
	/// <remarks> The default value is 4. </remarks>
	public int IndentationSize
	{
		get => _indentationSize;
		set
		{
			var newValue = value.EnsureRange(1, 1000);
			if (_indentationSize != newValue)
			{
				_indentationSize = newValue;
				OnPropertyChanged(nameof(IndentationSize));
				OnPropertyChanged(nameof(IndentationString));
			}
		}
	}

	/// <summary>
	/// Gets the text used for indentation.
	/// </summary>
	public string IndentationString => GetIndentationString(1);

	/// <summary>
	/// Gets/Sets whether the indentation is inherited from the first line when word-wrapping.
	/// The default value is true.
	/// </summary>
	/// <remarks> When combined with <see cref="WordWrapIndentation" />, the inherited indentation is added to the word wrap indentation. </remarks>
	public bool InheritWordWrapIndentation { get; set; }

	/// <summary>
	/// Gets/Sets whether the user needs to press Control to click hyperlinks.
	/// The default value is true.
	/// </summary>
	/// <remarks> The default value is True. </remarks>
	public bool RequireControlModifierForHyperlinkClick { get; set; }

	/// <summary>
	/// Gets/Sets whether to show a box with the hex code for control characters.
	/// </summary>
	/// <remarks> The default value is True. </remarks>
	public bool ShowBoxForControlCharacters { get; set; }

	/// <summary>
	/// Gets/Sets whether the column rulers should be shown.
	/// </summary>
	public bool ShowColumnRulers { get; set; }

	/// <summary>
	/// Gets/Sets whether to show EOL char at the end of lines. The glyphs displayed can be set via <see cref="EndOfLineCRLFGlyph" />, <see cref="EndOfLineCRGlyph" /> and <see cref="EndOfLineLFGlyph" />.
	/// </summary>
	/// <remarks> The default value is <c> false </c>. </remarks>
	public bool ShowEndOfLine { get; set; }

	/// <summary>
	/// Gets/Sets whether to show a visible glyph for spaces. The glyph displayed can be set via <see cref="ShowSpacesGlyph" />
	/// </summary>
	/// <remarks> The default value is <c> false </c>. </remarks>
	public bool ShowSpaces { get; set; }

	/// <summary>
	/// Gets/Sets the char to show when ShowSpaces option is enabled
	/// </summary>
	/// <remarks> The default value is <c> · </c>. </remarks>
	public string ShowSpacesGlyph { get; set; }

	/// <summary>
	/// Gets/Sets whether to show a visible glyph for tab. The glyph displayed can be set via <see cref="ShowTabsGlyph" />
	/// </summary>
	/// <remarks> The default value is <c> false </c>. </remarks>
	public bool ShowTabs { get; set; }

	/// <summary>
	/// Gets/Sets the char to show when ShowTabs option is enabled
	/// </summary>
	/// <remarks> The default value is <c> → </c>. </remarks>
	public string ShowTabsGlyph { get; set; }

	/// <summary>
	/// Gets/Sets the indentation used for all lines except the first when word-wrapping.
	/// The default value is 0.
	/// </summary>
	public double WordWrapIndentation
	{
		get => _wordWrapIndentation;
		set
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "value must not be NaN/infinity");
			}
			if (value != _wordWrapIndentation)
			{
				_wordWrapIndentation = value;
				OnPropertyChanged(nameof(WordWrapIndentation));
			}
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Gets text required to indent from the specified <paramref name="column" /> to the next indentation level.
	/// </summary>
	public string GetIndentationString(int column)
	{
		if (column < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(column), column, "Value must be at least 1.");
		}
		var indentationSize = IndentationSize;
		if (ConvertTabsToSpaces)
		{
			return new string(' ', indentationSize - ((column - 1) % indentationSize));
		}
		return "\t";
	}

	#endregion
}