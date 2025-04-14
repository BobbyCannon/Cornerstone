#region References

using System;
using System.Diagnostics;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Utils;
using ITextSource = Avalonia.Media.TextFormatting.ITextSource;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// WPF TextSource implementation that creates TextRuns for a VisualLine.
/// </summary>
internal sealed class VisualLineTextSource : ITextSource, ITextRunConstructionContext
{
	#region Fields

	private string _cachedString;
	private int _cachedStringOffset;

	#endregion

	#region Constructors

	public VisualLineTextSource(VisualLine visualLine)
	{
		VisualLine = visualLine;
	}

	#endregion

	#region Properties

	public TextEditorDocument Document { get; set; }
	public TextRunProperties GlobalTextRunProperties { get; set; }
	public TextView TextView { get; set; }

	public VisualLine VisualLine { get; }

	#endregion

	#region Methods

	public ReadOnlyMemory<char> GetPrecedingText(int textSourceCharacterIndexLimit)
	{
		try
		{
			foreach (var element in VisualLine.Elements)
			{
				if ((textSourceCharacterIndexLimit > element.VisualColumn)
					&& (textSourceCharacterIndexLimit <= (element.VisualColumn + element.VisualLength)))
				{
					var span = element.GetPrecedingText(textSourceCharacterIndexLimit, this);
					if (span.IsEmpty)
					{
						break;
					}
					var relativeOffset = textSourceCharacterIndexLimit - element.VisualColumn;
					if (span.Length > relativeOffset)
					{
						throw new ArgumentException("The returned TextSpan is too long.", element.GetType().Name + ".GetPrecedingText");
					}
					return span;
				}
			}

			return ReadOnlyMemory<char>.Empty;
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			throw;
		}
	}

	public StringSegment GetText(int offset, int length)
	{
		if (_cachedString != null)
		{
			if ((offset >= _cachedStringOffset) && ((offset + length) <= (_cachedStringOffset + _cachedString.Length)))
			{
				return new StringSegment(_cachedString, offset - _cachedStringOffset, length);
			}
		}
		_cachedStringOffset = offset;
		return new StringSegment(_cachedString = Document.GetText(offset, length));
	}

	public TextRun GetTextRun(int textSourceCharacterIndex)
	{
		try
		{
			foreach (var element in VisualLine.Elements)
			{
				if ((textSourceCharacterIndex >= element.VisualColumn)
					&& (textSourceCharacterIndex < (element.VisualColumn + element.VisualLength)))
				{
					var relativeOffset = textSourceCharacterIndex - element.VisualColumn;
					var run = element.CreateTextRun(textSourceCharacterIndex, this);
					if (run == null)
					{
						throw new ArgumentNullException(element.GetType().Name + ".CreateTextRun");
					}
					if (run.Length == 0)
					{
						throw new ArgumentException("The returned TextRun must not have length 0.", element.GetType().Name + ".Length");
					}
					if ((relativeOffset + run.Length) > element.VisualLength)
					{
						throw new ArgumentException("The returned TextRun is too long.", element.GetType().Name + ".CreateTextRun");
					}
					if (run is InlineObjectRun inlineRun)
					{
						inlineRun.VisualLine = VisualLine;
						VisualLine.HasInlineObjects = true;
						TextView.AddInlineObject(inlineRun);
					}
					return run;
				}
			}
			if (TextView.Settings.ShowEndOfLine && (textSourceCharacterIndex == VisualLine.VisualLength))
			{
				return CreateTextRunForNewLine();
			}
			return new TextEndOfParagraph(1);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			throw;
		}
	}

	private TextRun CreateTextRunForNewLine()
	{
		var newlineText = "";
		var lastDocumentLine = VisualLine.LastDocumentLine;
		if (lastDocumentLine.DelimiterLength == 2)
		{
			newlineText = TextView.Settings.EndOfLineCRLFGlyph;
		}
		else if (lastDocumentLine.DelimiterLength == 1)
		{
			var newlineChar = Document.GetCharAt(lastDocumentLine.StartIndex + lastDocumentLine.Length);
			if (newlineChar == '\r')
			{
				newlineText = TextView.Settings.EndOfLineCRGlyph;
			}
			else if (newlineChar == '\n')
			{
				newlineText = TextView.Settings.EndOfLineLFGlyph;
			}
			else
			{
				newlineText = "?";
			}
		}

		var p = new VisualLineElementTextRunProperties(GlobalTextRunProperties);
		p.SetForegroundBrush(TextView.NonPrintableCharacterBrush);
		p.SetFontRenderingEmSize(GlobalTextRunProperties.FontRenderingEmSize - 2);
		var textElement = new FormattedTextElement(TextView.CachedElements.GetTextForNonPrintableCharacter(newlineText, p), 0);

		textElement.RelativeTextOffset = lastDocumentLine.StartIndex + lastDocumentLine.Length;

		return new FormattedTextRun(textElement, GlobalTextRunProperties);
	}

	#endregion
}