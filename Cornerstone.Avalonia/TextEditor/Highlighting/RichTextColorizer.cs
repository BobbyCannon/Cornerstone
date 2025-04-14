#region References

using System;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Rendering;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// A colorizer that applies the highlighting from a <see cref="RichTextModel" /> to the editor.
/// </summary>
public class RichTextColorizer : DocumentColorizingTransformer
{
	#region Fields

	private readonly RichTextModel _richTextModel;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new RichTextColorizer instance.
	/// </summary>
	public RichTextColorizer(RichTextModel richTextModel)
	{
		_richTextModel = richTextModel ?? throw new ArgumentNullException(nameof(richTextModel));
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void ColorizeLine(DocumentLine line)
	{
		if (line.Length > 10000)
		{
			return;
		}

		var sections = _richTextModel.GetHighlightedSections(line.StartIndex, line.Length);
		foreach (var section in sections)
		{
			if (HighlightingColorizer.IsEmptyColor(section.Color))
			{
				continue;
			}
			ChangeLinePart(section.StartIndex, section.StartIndex + section.Length,
				visualLineElement => HighlightingColorizer.ApplyColorToElement(visualLineElement, section.Color, CurrentContext));
		}
	}

	#endregion
}