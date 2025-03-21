#region References

using System;
using Cornerstone.Avalonia.TextEditor.Rendering;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

internal sealed class SelectionColorizer : ColorizingTransformer
{
	#region Fields

	private readonly TextArea _textArea;

	#endregion

	#region Constructors

	public SelectionColorizer(TextArea textArea)
	{
		_textArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
	}

	#endregion

	#region Methods

	protected override void Colorize(ITextRunConstructionContext context)
	{
		// if SelectionForeground is null, keep the existing foreground color
		if (_textArea.SelectionForeground == null)
		{
			return;
		}

		var lineStartOffset = context.VisualLine.FirstDocumentLine.StartIndex;
		var lineEndOffset = context.VisualLine.LastDocumentLine.StartIndex + context.VisualLine.LastDocumentLine.TotalLength;

		foreach (var segment in _textArea.Selection.Segments)
		{
			var segmentStart = segment.StartIndex;
			var segmentEnd = segment.EndIndex;
			if (segmentEnd <= lineStartOffset)
			{
				continue;
			}
			if (segmentStart >= lineEndOffset)
			{
				continue;
			}
			var startColumn = segmentStart < lineStartOffset
				? 0
				: context.VisualLine.ValidateVisualColumn(segment.StartIndex, segment.StartVisualColumn,
					_textArea.Selection.EnableVirtualSpace);

			var endColumn = segmentEnd > lineEndOffset
				? _textArea.Selection.EnableVirtualSpace ? int.MaxValue : context.VisualLine.VisualLengthWithEndOfLineMarker
				: context.VisualLine.ValidateVisualColumn(segment.EndIndex, segment.EndVisualColumn, _textArea.Selection.EnableVirtualSpace);

			ChangeVisualElements(
				startColumn, endColumn,
				element => element.TextRunProperties.SetForegroundBrush(_textArea.SelectionForeground)
			);
		}
	}

	#endregion
}