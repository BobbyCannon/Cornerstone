#region References

using System;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

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
		var sections = _richTextModel.GetHighlightedSections(line.Offset, line.Length);
		foreach (var section in sections)
		{
			if (HighlightingColorizer.IsEmptyColor(section.Color))
			{
				continue;
			}
			ChangeLinePart(section.Offset, section.Offset + section.Length,
				visualLineElement => HighlightingColorizer.ApplyColorToElement(visualLineElement, section.Color, CurrentContext));
		}
	}

	#endregion
}