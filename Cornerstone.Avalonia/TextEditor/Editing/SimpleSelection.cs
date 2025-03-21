#region References

using System;
using System.Collections.Generic;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;
using Cornerstone.Internal;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// A simple selection.
/// </summary>
public sealed class SimpleSelection : Selection
{
	#region Fields

	private readonly TextViewPosition _end;
	private readonly int _endOffset;
	private readonly TextViewPosition _start;
	private readonly int _startOffset;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new SimpleSelection instance.
	/// </summary>
	internal SimpleSelection(TextArea textArea, TextViewPosition start, TextViewPosition end)
		: base(textArea)
	{
		_start = start;
		_end = end;
		_startOffset = textArea.Document.GetOffset(start.Location);
		_endOffset = textArea.Document.GetOffset(end.Location);
	}

	#endregion

	#region Properties

	public override TextViewPosition EndPosition => _end;

	/// <inheritdoc />
	public override bool IsEmpty => (_startOffset == _endOffset) && (_start.VisualColumn == _end.VisualColumn);

	/// <inheritdoc />
	public override int Length => Math.Abs(_endOffset - _startOffset);

	/// <inheritdoc />
	public override IEnumerable<SelectionRange> Segments => ExtensionMethods.Sequence(new SelectionRange(_startOffset, _start.VisualColumn, _endOffset, _end.VisualColumn));

	public override TextViewPosition StartPosition => _start;

	/// <inheritdoc />
	public override IRange SurroundingRange => new SelectionRange(_startOffset, _endOffset);

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		var other = obj as SimpleSelection;
		if (other == null)
		{
			return false;
		}
		// ReSharper disable ImpureMethodCallOnReadonlyValueField
		return _start.Equals(other._start) && _end.Equals(other._end)
			&& (_startOffset == other._startOffset) && (_endOffset == other._endOffset)
			&& (TextArea == other.TextArea);
		// ReSharper restore ImpureMethodCallOnReadonlyValueField
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			return (_startOffset * 27811) + _endOffset + TextArea.GetHashCode();
		}
	}

	/// <inheritdoc />
	public override void ReplaceSelectionWithText(string newText)
	{
		if (newText == null)
		{
			throw new ArgumentNullException(nameof(newText));
		}
		using (TextArea.Document.RunUpdate())
		{
			var segmentsToDelete = TextArea.GetDeletableSegments(SurroundingRange);
			for (var i = segmentsToDelete.Length - 1; i >= 0; i--)
			{
				if (i == (segmentsToDelete.Length - 1))
				{
					if ((segmentsToDelete[i].StartIndex == SurroundingRange.StartIndex) && (segmentsToDelete[i].Length == SurroundingRange.Length))
					{
						newText = AddSpacesIfRequired(newText, _start, _end);
					}
					if (string.IsNullOrEmpty(newText))
					{
						// place caret at the beginning of the selection
						// ReSharper disable once ImpureMethodCallOnReadonlyValueField
						TextArea.Caret.Position = _start.CompareTo(_end) <= 0 ? _start : _end;
					}
					else
					{
						// place caret so that it ends up behind the new text
						TextArea.Caret.Offset = segmentsToDelete[i].EndIndex;
					}
					TextArea.Document.Replace(segmentsToDelete[i], newText);
				}
				else
				{
					TextArea.Document.Remove(segmentsToDelete[i]);
				}
			}
			if (segmentsToDelete.Length != 0)
			{
				TextArea.ClearSelection();
			}
		}
	}

	/// <inheritdoc />
	public override Selection SetEndpoint(TextViewPosition endPosition)
	{
		return Create(TextArea, _start, endPosition);
	}

	public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
	{
		var document = TextArea.Document;
		if (document == null)
		{
			throw ThrowUtil.NoDocumentAssigned();
		}
		return Create(TextArea, _start, endPosition);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return "[SimpleSelection Start=" + _start + " End=" + _end + "]";
	}

	/// <inheritdoc />
	public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
	{
		if (e == null)
		{
			throw new ArgumentNullException(nameof(e));
		}
		int newStartOffset, newEndOffset;
		if (_startOffset <= _endOffset)
		{
			newStartOffset = e.GetNewOffset(_startOffset);
			newEndOffset = Math.Max(newStartOffset, e.GetNewOffset(_endOffset, AnchorMovementType.BeforeInsertion));
		}
		else
		{
			newEndOffset = e.GetNewOffset(_endOffset);
			newStartOffset = Math.Max(newEndOffset, e.GetNewOffset(_startOffset, AnchorMovementType.BeforeInsertion));
		}
		return Create(
			TextArea,
			new TextViewPosition(TextArea.Document.GetLocation(newStartOffset), _start.VisualColumn),
			new TextViewPosition(TextArea.Document.GetLocation(newEndOffset), _end.VisualColumn)
		);
	}

	#endregion
}