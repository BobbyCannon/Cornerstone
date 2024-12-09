#region References

using System;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// Represents a selected segment.
/// </summary>
public class SelectionRange : IRange
{
	#region Constructors

	/// <summary>
	/// Creates a SelectionSegment from two offsets.
	/// </summary>
	public SelectionRange(int startIndex, int endOffset)
	{
		StartIndex = Math.Min(startIndex, endOffset);
		EndIndex = Math.Max(startIndex, endOffset);
		StartVisualColumn = EndVisualColumn = -1;
	}

	/// <summary>
	/// Creates a SelectionSegment from two offsets and visual columns.
	/// </summary>
	public SelectionRange(int startIndex, int startVisualColumn, int endIndex, int endVisualColumn)
	{
		if ((startIndex < endIndex) || ((startIndex == endIndex) && (startVisualColumn <= endVisualColumn)))
		{
			StartIndex = startIndex;
			StartVisualColumn = startVisualColumn;
			EndIndex = endIndex;
			EndVisualColumn = endVisualColumn;
		}
		else
		{
			StartIndex = endIndex;
			StartVisualColumn = endVisualColumn;
			EndIndex = startIndex;
			EndVisualColumn = startVisualColumn;
		}
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the end offset.
	/// </summary>
	public int EndIndex { get; }

	/// <summary>
	/// Gets the end visual column.
	/// </summary>
	public int EndVisualColumn { get; }

	/// <inheritdoc />
	public int Length => EndIndex - StartIndex;

	/// <inheritdoc />
	public int StartIndex { get; }

	/// <summary>
	/// Gets the start visual column.
	/// </summary>
	public int StartVisualColumn { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[SelectionSegment StartOffset={StartIndex}, EndOffset={EndIndex}, StartVC={StartVisualColumn}, EndVC={EndVisualColumn}]";
	}

	#endregion
}