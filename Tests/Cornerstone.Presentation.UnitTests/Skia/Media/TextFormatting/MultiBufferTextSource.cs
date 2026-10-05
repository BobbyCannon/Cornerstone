#region References

using Cornerstone.Presentation.Media.TextFormatting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

internal class MultiBufferTextSource : ITextSource
{
	#region Fields

	private readonly GenericTextRunProperties _defaultStyle;
	private readonly string[] _runTexts;

	#endregion

	#region Constructors

	public MultiBufferTextSource(GenericTextRunProperties defaultStyle)
	{
		_defaultStyle = defaultStyle;

		_runTexts = new[] { "A123456789", "B123456789", "C123456789", "D123456789", "E123456789" };
	}

	#endregion

	#region Methods

	public TextRun GetTextRun(int textSourceIndex)
	{
		if (textSourceIndex >= 50)
		{
			return null;
		}

		var index = textSourceIndex / 10;

		var runText = _runTexts[index];

		return new TextCharacters(runText, _defaultStyle);
	}

	#endregion
}