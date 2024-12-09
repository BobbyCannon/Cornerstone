#region References

using System;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

/// <summary>
/// Base class for <see cref="IVisualLineTransformer" /> that helps
/// colorizing the document. Derived classes can work with document lines
/// and text offsets and this class takes care of the visual lines and visual columns.
/// </summary>
public abstract class DocumentColorizingTransformer : ColorizingTransformer
{
	#region Fields

	private DocumentLine _currentDocumentLine;
	private int _currentDocumentLineStartOffset, _currentDocumentLineEndOffset;
	private int _firstLineStart;

	#endregion

	#region Properties

	/// <summary>
	/// Gets the current ITextRunConstructionContext.
	/// </summary>
	protected ITextRunConstructionContext CurrentContext { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Changes a part of the current document line.
	/// </summary>
	/// <param name="startOffset"> Start offset of the region to change </param>
	/// <param name="endOffset"> End offset of the region to change </param>
	/// <param name="action"> Action that changes an individual <see cref="VisualLineElement" />. </param>
	protected void ChangeLinePart(int startOffset, int endOffset, Action<VisualLineElement> action)
	{
		if ((startOffset < _currentDocumentLineStartOffset) || (startOffset > _currentDocumentLineEndOffset))
		{
			Debugger.Break();
			throw new ArgumentOutOfRangeException(nameof(startOffset), startOffset, "Value must be between " + _currentDocumentLineStartOffset + " and " + _currentDocumentLineEndOffset);
		}
		if ((endOffset < startOffset) || (endOffset > _currentDocumentLineEndOffset))
		{
			Debugger.Break();
			throw new ArgumentOutOfRangeException(nameof(endOffset), endOffset, "Value must be between " + startOffset + " and " + _currentDocumentLineEndOffset);
		}
		var vl = CurrentContext.VisualLine;
		var visualStart = vl.GetVisualColumn(startOffset - _firstLineStart);
		var visualEnd = vl.GetVisualColumn(endOffset - _firstLineStart);
		if (visualStart < visualEnd)
		{
			ChangeVisualElements(visualStart, visualEnd, action);
		}
	}

	/// <inheritdoc />
	protected override void Colorize(ITextRunConstructionContext context)
	{
		CurrentContext = context ?? throw new ArgumentNullException(nameof(context));

		_currentDocumentLine = context.VisualLine.FirstDocumentLine;
		_firstLineStart = _currentDocumentLineStartOffset = _currentDocumentLine.StartIndex;
		_currentDocumentLineEndOffset = _currentDocumentLineStartOffset + _currentDocumentLine.Length;
		var currentDocumentLineTotalEndOffset = _currentDocumentLineStartOffset + _currentDocumentLine.TotalLength;

		if (context.VisualLine.FirstDocumentLine == context.VisualLine.LastDocumentLine)
		{
			ColorizeLine(_currentDocumentLine);
		}
		else
		{
			ColorizeLine(_currentDocumentLine);
			// ColorizeLine modifies the visual line elements, loop through a copy of the line elements
			foreach (var e in context.VisualLine.Elements.ToArray())
			{
				var elementOffset = _firstLineStart + e.RelativeTextOffset;
				if (elementOffset >= currentDocumentLineTotalEndOffset)
				{
					_currentDocumentLine = context.Document.GetLineByOffset(elementOffset);
					_currentDocumentLineStartOffset = _currentDocumentLine.StartIndex;
					_currentDocumentLineEndOffset = _currentDocumentLineStartOffset + _currentDocumentLine.Length;
					currentDocumentLineTotalEndOffset = _currentDocumentLineStartOffset + _currentDocumentLine.TotalLength;
					ColorizeLine(_currentDocumentLine);
				}
			}
		}
		_currentDocumentLine = null;
		CurrentContext = null;
	}

	/// <summary>
	/// Override this method to colorize an individual document line.
	/// </summary>
	protected abstract void ColorizeLine(DocumentLine line);

	#endregion
}