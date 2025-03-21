#region References

using System;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

internal sealed class SimpleTextSource : ITextSource
{
	#region Fields

	private readonly TextRunProperties _properties;
	private readonly string _text;

	#endregion

	#region Constructors

	public SimpleTextSource(string text, TextRunProperties properties)
	{
		_text = text;
		_properties = properties;
	}

	#endregion

	#region Methods

	public TextRun GetTextRun(int textSourceCharacterIndex)
	{
		if (textSourceCharacterIndex < _text.Length)
		{
			return new TextCharacters(
				_text.AsMemory().Slice(textSourceCharacterIndex,
					_text.Length - textSourceCharacterIndex), _properties);
		}

		return new TextEndOfParagraph(1);
	}

	#endregion
}