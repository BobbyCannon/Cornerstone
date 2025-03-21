#region References

using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Wraps a string into a <see cref="ITextSource" /> instance.
/// </summary>
internal readonly struct SimpleTextSource : ITextSource
{
	#region Fields

	private readonly TextRunProperties _defaultProperties;
	private readonly string _text;

	#endregion

	#region Constructors

	public SimpleTextSource(string text, TextRunProperties defaultProperties)
	{
		_text = text;
		_defaultProperties = defaultProperties;
	}

	#endregion

	#region Methods

	public TextRun GetTextRun(int textSourceIndex)
	{
		if (textSourceIndex >= _text.Length)
		{
			return new TextEndOfParagraph();
		}

		return new TextCharacters(_text, _defaultProperties);
	}

	#endregion
}