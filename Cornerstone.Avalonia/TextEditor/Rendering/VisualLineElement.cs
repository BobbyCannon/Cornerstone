﻿#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.TextEditor.Document;
using LogicalDirection = Cornerstone.Avalonia.TextEditor.Document.LogicalDirection;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Represents a visual element in the document.
/// </summary>
public abstract class VisualLineElement
{
	#region Constructors

	/// <summary>
	/// Creates a new VisualLineElement.
	/// </summary>
	/// <param name="visualLength"> The length of the element in VisualLine coordinates. Must be positive. </param>
	/// <param name="documentLength"> The length of the element in the document. Must be non-negative. </param>
	protected VisualLineElement(int visualLength, int documentLength)
	{
		if (visualLength < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(visualLength), visualLength, "Value must be at least 1");
		}
		if (documentLength < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(documentLength), documentLength, "Value must be at least 0");
		}
		VisualLength = visualLength;
		DocumentLength = documentLength;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/sets the brush used for the background of this <see cref="VisualLineElement" />.
	/// </summary>
	public IBrush Background03 { get; set; }

	/// <summary>
	/// Gets if this VisualLineElement can be split.
	/// </summary>
	public virtual bool CanSplit => false;

	/// <summary>
	/// Gets the length of this element in the text document.
	/// </summary>
	public int DocumentLength { get; private set; }

	/// <summary>
	/// Gets whether the <see cref="GetNextCaretPosition" /> implementation handles line borders.
	/// If this property returns false, the caller of GetNextCaretPosition should handle the line
	/// borders (i.e. place caret stops at the start and end of the line).
	/// This property has an effect only for VisualLineElements that are at the start or end of a
	/// <see cref="VisualLine" />.
	/// </summary>
	public virtual bool HandlesLineBorders => false;

	/// <summary>
	/// Gets the text offset where this element starts, relative to the start text offset of the visual line.
	/// </summary>
	public int RelativeTextOffset { get; internal set; }

	/// <summary>
	/// Gets the text run properties.
	/// A unique <see cref="VisualLineElementTextRunProperties" /> instance is used for each
	/// <see cref="VisualLineElement" />; colorizing code may assume that modifying the
	/// <see cref="VisualLineElementTextRunProperties" /> will affect only this
	/// <see cref="VisualLineElement" />.
	/// </summary>
	public VisualLineElementTextRunProperties TextRunProperties { get; private set; }

	/// <summary>
	/// Gets the visual column where this element starts.
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods",
		Justification = "This property holds the start visual column, use GetVisualColumn to get inner visual columns.")]
	public int VisualColumn { get; internal set; }

	/// <summary>
	/// Gets the length of this element in visual columns.
	/// </summary>
	public int VisualLength { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Creates the TextRun for this line element.
	/// </summary>
	/// <param name="startVisualColumn">
	/// The visual column from which the run should be constructed.
	/// Normally the same value as the <see cref="VisualColumn" /> property is used to construct the full run;
	/// but when word-wrapping is active, partial runs might be created.
	/// </param>
	/// <param name="context">
	/// Context object that contains information relevant for text run creation.
	/// </param>
	public abstract TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context);

	/// <summary>
	/// Gets the next caret position inside this element.
	/// </summary>
	/// <param name="visualColumn"> The visual column from which the search should be started. </param>
	/// <param name="direction"> The search direction (forwards or backwards). </param>
	/// <param name="mode"> Whether to stop only at word borders. </param>
	/// <returns> The visual column of the next caret position, or -1 if there is no next caret position. </returns>
	/// <remarks>
	/// In the space between two line elements, it is sufficient that one of them contains a caret position;
	/// though in many cases, both of them contain one.
	/// </remarks>
	public virtual int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
	{
		var stop1 = VisualColumn;
		var stop2 = VisualColumn + VisualLength;
		if (direction == LogicalDirection.Backward)
		{
			if ((visualColumn > stop2) && (mode != CaretPositioningMode.WordStart) && (mode != CaretPositioningMode.WordStartOrSymbol))
			{
				return stop2;
			}
			if (visualColumn > stop1)
			{
				return stop1;
			}
		}
		else
		{
			if (visualColumn < stop1)
			{
				return stop1;
			}
			if ((visualColumn < stop2) && (mode != CaretPositioningMode.WordStart) && (mode != CaretPositioningMode.WordStartOrSymbol))
			{
				return stop2;
			}
		}
		return -1;
	}

	/// <summary>
	/// Retrieves the text span immediately before the visual column.
	/// </summary>
	/// <remarks> This method is used for word-wrapping in bidirectional text. </remarks>
	public virtual ReadOnlyMemory<char> GetPrecedingText(int visualColumnLimit, ITextRunConstructionContext context)
	{
		return ReadOnlyMemory<char>.Empty;
	}

	/// <summary>
	/// Gets the text offset of a visual column inside this element.
	/// </summary>
	/// <returns> A text offset relative to the visual line start. </returns>
	public virtual int GetRelativeOffset(int visualColumn)
	{
		if (visualColumn >= (VisualColumn + VisualLength))
		{
			return RelativeTextOffset + DocumentLength;
		}
		return RelativeTextOffset;
	}

	/// <summary>
	/// Gets the visual column of a text location inside this element.
	/// The text offset is given relative to the visual line start.
	/// </summary>
	public virtual int GetVisualColumn(int relativeTextOffset)
	{
		if (relativeTextOffset >= (RelativeTextOffset + DocumentLength))
		{
			return VisualColumn + VisualLength;
		}
		return VisualColumn;
	}

	/// <summary>
	/// Gets whether the specified offset in this element is considered whitespace.
	/// </summary>
	public virtual bool IsWhitespace(int visualColumn)
	{
		return false;
	}

	/// <summary>
	/// Splits the element.
	/// </summary>
	/// <param name="splitVisualColumn"> Position inside this element at which it should be broken </param>
	/// <param name="elements"> The collection of line elements </param>
	/// <param name="elementIndex"> The index at which this element is in the elements list. </param>
	public virtual void Split(int splitVisualColumn, IList<VisualLineElement> elements, int elementIndex)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Allows the visual line element to handle a pointer event.
	/// </summary>
	protected internal virtual void OnPointerPressed(PointerPressedEventArgs e)
	{
	}

	/// <summary>
	/// Allows the visual line element to handle a pointer event.
	/// </summary>
	protected internal virtual void OnPointerReleased(PointerEventArgs e)
	{
	}

	/// <summary>
	/// Queries the cursor over the visual line element.
	/// </summary>
	protected internal virtual void OnQueryCursor(PointerEventArgs e)
	{
	}

	/// <summary>
	/// Helper method for splitting this line element into two, correctly updating the
	/// <see cref="VisualLength" />, <see cref="DocumentLength" />, <see cref="VisualColumn" />
	/// and <see cref="RelativeTextOffset" /> properties.
	/// </summary>
	/// <param name="firstPart"> The element before the split position. </param>
	/// <param name="secondPart"> The element after the split position. </param>
	/// <param name="splitVisualColumn"> The split position as visual column. </param>
	/// <param name="splitRelativeTextOffset"> The split position as text offset. </param>
	protected void SplitHelper(VisualLineElement firstPart, VisualLineElement secondPart, int splitVisualColumn, int splitRelativeTextOffset)
	{
		if (firstPart == null)
		{
			throw new ArgumentNullException(nameof(firstPart));
		}
		if (secondPart == null)
		{
			throw new ArgumentNullException(nameof(secondPart));
		}
		var relativeSplitVisualColumn = splitVisualColumn - VisualColumn;
		var relativeSplitRelativeTextOffset = splitRelativeTextOffset - RelativeTextOffset;

		if ((relativeSplitVisualColumn <= 0) || (relativeSplitVisualColumn >= VisualLength))
		{
			throw new ArgumentOutOfRangeException(nameof(splitVisualColumn), splitVisualColumn, "Value must be between " + (VisualColumn + 1) + " and " + ((VisualColumn + VisualLength) - 1));
		}
		if ((relativeSplitRelativeTextOffset < 0) || (relativeSplitRelativeTextOffset > DocumentLength))
		{
			throw new ArgumentOutOfRangeException(nameof(splitRelativeTextOffset), splitRelativeTextOffset, "Value must be between " + RelativeTextOffset + " and " + (RelativeTextOffset + DocumentLength));
		}
		var oldVisualLength = VisualLength;
		var oldDocumentLength = DocumentLength;
		var oldVisualColumn = VisualColumn;
		var oldRelativeTextOffset = RelativeTextOffset;
		firstPart.VisualColumn = oldVisualColumn;
		secondPart.VisualColumn = oldVisualColumn + relativeSplitVisualColumn;
		firstPart.RelativeTextOffset = oldRelativeTextOffset;
		secondPart.RelativeTextOffset = oldRelativeTextOffset + relativeSplitRelativeTextOffset;
		firstPart.VisualLength = relativeSplitVisualColumn;
		secondPart.VisualLength = oldVisualLength - relativeSplitVisualColumn;
		firstPart.DocumentLength = relativeSplitRelativeTextOffset;
		secondPart.DocumentLength = oldDocumentLength - relativeSplitRelativeTextOffset;
		if (firstPart.TextRunProperties == null)
		{
			firstPart.TextRunProperties = TextRunProperties.Clone();
		}
		if (secondPart.TextRunProperties == null)
		{
			secondPart.TextRunProperties = TextRunProperties.Clone();
		}
		firstPart.Background03 = Background03;
		secondPart.Background03 = Background03;
	}

	internal void SetTextRunProperties(VisualLineElementTextRunProperties p)
	{
		TextRunProperties = p;
	}

	#endregion
}