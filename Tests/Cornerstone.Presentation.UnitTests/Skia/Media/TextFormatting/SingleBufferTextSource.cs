#region References

using System;
using Cornerstone.Presentation.Media.TextFormatting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

internal class SingleBufferTextSource : ITextSource
{
	#region Fields

	private readonly bool _addEndOfParagraph;
	private readonly GenericTextRunProperties _defaultGenericPropertiesRunProperties;
	private readonly string _text;

	#endregion

	#region Constructors

	public SingleBufferTextSource(string text, GenericTextRunProperties defaultProperties, bool addEndOfParagraph = false)
	{
		_text = text;
		_defaultGenericPropertiesRunProperties = defaultProperties;
		_addEndOfParagraph = addEndOfParagraph;
	}

	#endregion

	#region Methods

	public TextRun GetTextRun(int textSourceIndex)
	{
		if (textSourceIndex >= _text.Length)
		{
			return _addEndOfParagraph ? new TextEndOfParagraph() : null;
		}

		var runText = _text.AsMemory(textSourceIndex);

		if (runText.IsEmpty)
		{
			return _addEndOfParagraph ? new TextEndOfParagraph() : null;
		}

		return new TextCharacters(runText, _defaultGenericPropertiesRunProperties);
	}

	#endregion
}