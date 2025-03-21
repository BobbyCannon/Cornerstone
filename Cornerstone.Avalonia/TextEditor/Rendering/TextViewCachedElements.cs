#region References

using System.Collections.Generic;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

internal sealed class TextViewCachedElements
{
	#region Fields

	private Dictionary<string, TextLine> _nonPrintableCharacterTexts;

	#endregion

	#region Methods

	public TextLine GetTextForNonPrintableCharacter(string text, TextRunProperties properties)
	{
		if (_nonPrintableCharacterTexts == null)
		{
			_nonPrintableCharacterTexts = new Dictionary<string, TextLine>();
		}

		TextLine textLine;
		if (!_nonPrintableCharacterTexts.TryGetValue(text, out textLine))
		{
			textLine = FormattedTextElement.PrepareText(TextFormatter.Current, text, properties);
			_nonPrintableCharacterTexts[text] = textLine;
		}
		return textLine;
	}

	#endregion
}