#region References

using System;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// Represents a selected segment.
/// </summary>
public class SelectionSegment : ISegment
{
	#region Constructors

	/// <summary>
	/// Creates a SelectionSegment from two offsets.
	/// </summary>
	public SelectionSegment(int startOffset, int endOffset)
	{
		StartOffset = Math.Min(startOffset, endOffset);
		EndOffset = Math.Max(startOffset, endOffset);
		StartVisualColumn = EndVisualColumn = -1;
	}

	/// <summary>
	/// Creates a SelectionSegment from two offsets and visual columns.
	/// </summary>
	public SelectionSegment(int startOffset, int startVisualColumn, int endOffset, int endVisualColumn)
	{
		if ((startOffset < endOffset) || ((startOffset == endOffset) && (startVisualColumn <= endVisualColumn)))
		{
			StartOffset = startOffset;
			StartVisualColumn = startVisualColumn;
			EndOffset = endOffset;
			EndVisualColumn = endVisualColumn;
		}
		else
		{
			StartOffset = endOffset;
			StartVisualColumn = endVisualColumn;
			EndOffset = startOffset;
			EndVisualColumn = startVisualColumn;
		}
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the end offset.
	/// </summary>
	public int EndOffset { get; }

	/// <summary>
	/// Gets the end visual column.
	/// </summary>
	public int EndVisualColumn { get; }

	/// <inheritdoc />
	public int Length => EndOffset - StartOffset;

	/// <summary>
	/// Gets the start offset.
	/// </summary>
	public int StartOffset { get; }

	/// <summary>
	/// Gets the start visual column.
	/// </summary>
	public int StartVisualColumn { get; }

	/// <inheritdoc />
	int ISegment.Offset => StartOffset;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[SelectionSegment StartOffset={StartOffset}, EndOffset={EndOffset}, StartVC={StartVisualColumn}, EndVC={EndVisualColumn}]";
	}

	#endregion
}