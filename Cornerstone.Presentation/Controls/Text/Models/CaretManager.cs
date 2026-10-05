#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Models;

/// <summary>
/// Owns the editor carets. Always at least one; Primary is the existing single-caret alias.
/// Extra carets can be added now; the view still only creates one until later PRs.
/// </summary>
public class CaretManager
{
	#region Constants

	public const int MaxCarets = 500;

	#endregion

	#region Fields

	private readonly List<Caret> _carets;
	private Caret _primary;
	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public CaretManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
		_primary = new Caret(viewModel);
		_carets = [_primary];
		Wire(_primary);
	}

	#endregion

	#region Properties

	public IReadOnlyList<Caret> All => _carets;

	public int Count => _carets.Count;

	public Caret Primary => _primary;

	#endregion

	#region Methods

	/// <summary>
	/// Adds a caret at offset, or removes a non-primary caret already there (toggle).
	/// No-op if offset matches Primary with no extra caret, or the cap is reached.
	/// </summary>
	public Caret AddAt(int offset)
	{
		offset = ClampOffset(offset);

		for (var i = 0; i < _carets.Count; i++)
		{
			var existing = _carets[i];
			if ((existing.Offset != offset) || (existing.Selection.Length > 0))
			{
				continue;
			}

			if (existing == _primary)
			{
				return _primary;
			}

			Unwire(existing);
			_carets.RemoveAt(i);
			OnCaretsChanged();
			return null;
		}

		if (_carets.Count >= MaxCarets)
		{
			return null;
		}

		return CreateCaretAt(offset);
	}

	/// <summary>
	/// Adds a caret one line toward lineDelta from the farthest caret in that direction,
	/// using that caret's column (clamped). Repeated Down/Up therefore stacks carets
	/// across many lines instead of toggling the neighbor of Primary.
	/// </summary>
	public Caret AddRelativeToPrimary(int lineDelta)
	{
		if (lineDelta == 0)
		{
			return _primary;
		}

		var lines = _viewModel.Lines;
		if ((lines == null) || (lines.Count == 0))
		{
			return null;
		}

		var origin = GetExtremalCaret(lineDelta);
		var originLine = origin.Line ?? lines.GetLineFromOffset(origin.Offset);
		if (originLine == null)
		{
			return null;
		}

		var targetIndex = (originLine.LineNumber - 1) + (lineDelta > 0 ? 1 : -1);
		if ((targetIndex < 0) || (targetIndex >= lines.Count))
		{
			return null;
		}

		var targetLine = lines[targetIndex];
		var column = origin.Offset - originLine.StartOffset;
		var maxColumn = Math.Max(0, targetLine.Length - targetLine.LineEndingLength);
		if (column > maxColumn)
		{
			column = maxColumn;
		}

		return EnsureAt(targetLine.StartOffset + column);
	}

	/// <summary>
	/// Removes extra carets. Returns true if any were removed.
	/// </summary>
	public bool CollapseToPrimary()
	{
		if (_carets.Count <= 1)
		{
			return false;
		}

		for (var i = 1; i < _carets.Count; i++)
		{
			Unwire(_carets[i]);
		}

		_carets.Clear();
		_carets.Add(_primary);
		OnCaretsChanged();
		return true;
	}

	/// <summary>
	/// Collapses carets whose empty offsets match, or whose selections overlap or touch.
	/// Primary is kept when it participates.
	/// </summary>
	public void MergeOverlapping()
	{
		if (_carets.Count <= 1)
		{
			return;
		}

		var merged = true;
		while (merged && (_carets.Count > 1))
		{
			merged = false;
			_carets.Sort(CompareDocumentOrder);

			for (var i = 0; i < (_carets.Count - 1); i++)
			{
				var left = _carets[i];
				var right = _carets[i + 1];
				GetRange(left, out var leftStart, out var leftEnd);
				GetRange(right, out var rightStart, out var rightEnd);

				if ((rightStart > leftEnd) || (leftStart > rightEnd))
				{
					continue;
				}

				var keepPrimary = (left == _primary) || (right == _primary);
				var survivor = keepPrimary ? _primary : left;
				var other = survivor == left ? right : left;
				var unionStart = Math.Min(leftStart, rightStart);
				var unionEnd = Math.Max(leftEnd, rightEnd);

				if (unionStart != unionEnd)
				{
					survivor.Selection.Update(unionStart, unionEnd);
					survivor.Move(unionEnd);
				}
				else
				{
					survivor.Selection.Reset(unionStart);
					survivor.Move(unionStart);
				}

				_carets.Remove(other);
				if (!_carets.Contains(survivor))
				{
					_carets.Add(survivor);
				}

				merged = true;
				break;
			}
		}
	}

	/// <summary>
	/// Moves carets and selections after offset by delta. The source caret is skipped
	/// (it was already moved for this edit).
	/// </summary>
	public void ShiftAfter(int offset, int delta, Caret source)
	{
		if (delta == 0)
		{
			return;
		}

		var length = _viewModel.DocumentLength;
		for (var i = 0; i < _carets.Count; i++)
		{
			var caret = _carets[i];
			if (caret == source)
			{
				continue;
			}

			var nextOffset = caret.Offset;
			if (nextOffset > offset)
			{
				nextOffset += delta;
			}

			IntegerExtensions.EnsureRange(ref nextOffset, 0, length);
			if (nextOffset != caret.Offset)
			{
				caret.Move(nextOffset);
			}

			var start = caret.Selection.StartOffset;
			var end = caret.Selection.EndOffset;
			if (start > offset)
			{
				start += delta;
			}
			if (end > offset)
			{
				end += delta;
			}

			IntegerExtensions.EnsureRange(ref start, 0, length);
			IntegerExtensions.EnsureRange(ref end, 0, length);
			if ((start != caret.Selection.StartOffset) || (end != caret.Selection.EndOffset))
			{
				caret.Selection.Update(start, end);
			}
		}
	}

	public void Remove(Caret caret)
	{
		if ((caret == null) || (caret == _primary) || (_carets.Count <= 1))
		{
			return;
		}

		Unwire(caret);
		_carets.Remove(caret);
		OnCaretsChanged();
	}

	/// <summary>
	/// Snapshot of every caret for compound undo.
	/// </summary>
	public CaretSnapshot[] Capture()
	{
		var states = new CaretSnapshot[_carets.Count];
		for (var i = 0; i < _carets.Count; i++)
		{
			var caret = _carets[i];
			states[i] = new CaretSnapshot(
				caret.Offset,
				caret.Selection.StartOffset,
				caret.Selection.EndOffset,
				caret == _primary);
		}

		return states;
	}

	public void EnsureAllInDocument()
	{
		for (var i = 0; i < _carets.Count; i++)
		{
			_carets[i].EnsureInDocument();
		}
	}

	public void UpdateVisualLayouts()
	{
		for (var i = 0; i < _carets.Count; i++)
		{
			_carets[i].UpdateVisualLayout();
		}
	}

	/// <summary>
	/// Replaces the caret set with a captured snapshot. Primary object is kept.
	/// </summary>
	public void Restore(CaretSnapshot[] states)
	{
		if ((states == null) || (states.Length == 0))
		{
			return;
		}

		CollapseToPrimary();

		var primaryIndex = 0;
		for (var i = 0; i < states.Length; i++)
		{
			if (!states[i].IsPrimary)
			{
				continue;
			}

			primaryIndex = i;
			break;
		}

		ApplySnapshot(_primary, states[primaryIndex]);

		for (var i = 0; i < states.Length; i++)
		{
			if (i == primaryIndex)
			{
				continue;
			}

			var caret = CreateCaretAt(states[i].Offset);
			if (caret == null)
			{
				continue;
			}

			ApplySnapshot(caret, states[i]);
		}
	}

	/// <summary>
	/// Carets from low document offset to high (clipboard join / paste mapping).
	/// </summary>
	public List<Caret> DocumentOrder()
	{
		var list = new List<Caret>(_carets);
		list.Sort(CompareDocumentOrder);
		return list;
	}

	/// <summary>
	/// Carets from high document offset to low, for buffer mutations.
	/// </summary>
	public List<Caret> ReverseDocumentOrder()
	{
		var list = DocumentOrder();
		list.Reverse();
		return list;
	}

	private static void ApplySnapshot(Caret caret, CaretSnapshot state)
	{
		caret.Move(state.Offset);
		caret.Selection.Update(state.SelectionStart, state.SelectionEnd);
	}

	/// <summary>
	/// Adds a caret at offset if none is already there. Does not toggle.
	/// </summary>
	private Caret EnsureAt(int offset)
	{
		offset = ClampOffset(offset);
		for (var i = 0; i < _carets.Count; i++)
		{
			var existing = _carets[i];
			if ((existing.Offset == offset) && (existing.Selection.Length <= 0))
			{
				return existing;
			}
		}

		if (_carets.Count >= MaxCarets)
		{
			return null;
		}

		return CreateCaretAt(offset);
	}

	private Caret GetExtremalCaret(int lineDelta)
	{
		var best = _primary;
		var bestLine = GetLineIndex(best);
		for (var i = 0; i < _carets.Count; i++)
		{
			var caret = _carets[i];
			var lineIndex = GetLineIndex(caret);
			if (lineDelta > 0)
			{
				if (lineIndex > bestLine)
				{
					best = caret;
					bestLine = lineIndex;
				}
			}
			else if (lineIndex < bestLine)
			{
				best = caret;
				bestLine = lineIndex;
			}
		}

		return best;
	}

	private int GetLineIndex(Caret caret)
	{
		var line = caret.Line ?? _viewModel.Lines.GetLineFromOffset(caret.Offset);
		return line == null ? 0 : line.LineNumber - 1;
	}

	private Caret CreateCaretAt(int offset)
	{
		if (_carets.Count >= MaxCarets)
		{
			return null;
		}

		var caret = new Caret(_viewModel);
		caret.Move(offset);
		Wire(caret);
		_carets.Add(caret);
		OnCaretsChanged();
		return caret;
	}

	private void OnCaretMoved(object sender, EventArgs e)
	{
		CaretMoved?.Invoke(sender, e);
	}

	private void OnCaretsChanged()
	{
		CaretsChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnSelectionUpdated(object sender, EventArgs e)
	{
		SelectionUpdated?.Invoke(sender, e);
	}

	private void Unwire(Caret caret)
	{
		caret.CaretMoved -= OnCaretMoved;
		caret.Selection.Updated -= OnSelectionUpdated;
	}

	private void Wire(Caret caret)
	{
		caret.CaretMoved += OnCaretMoved;
		caret.Selection.Updated += OnSelectionUpdated;
	}

	private static int CompareDocumentOrder(Caret left, Caret right)
	{
		GetRange(left, out var leftStart, out _);
		GetRange(right, out var rightStart, out _);
		var start = leftStart.CompareTo(rightStart);
		if (start != 0)
		{
			return start;
		}

		return left.Offset.CompareTo(right.Offset);
	}

	private int ClampOffset(int offset)
	{
		var length = _viewModel.DocumentLength;
		if (offset < 0)
		{
			return 0;
		}

		return offset > length ? length : offset;
	}

	private static void GetRange(Caret caret, out int start, out int end)
	{
		if (caret.Selection.Length > 0)
		{
			start = Math.Min(caret.Selection.StartOffset, caret.Selection.EndOffset);
			end = Math.Max(caret.Selection.StartOffset, caret.Selection.EndOffset);
			return;
		}

		start = caret.Offset;
		end = caret.Offset;
	}

	#endregion

	#region Events

	public event EventHandler CaretMoved;

	public event EventHandler CaretsChanged;

	public event EventHandler SelectionUpdated;

	#endregion
}

/// <summary>
/// Offset and selection for one caret in an undo unit.
/// </summary>
public readonly struct CaretSnapshot
{
	public CaretSnapshot(int offset, int selectionStart, int selectionEnd, bool isPrimary)
	{
		Offset = offset;
		SelectionStart = selectionStart;
		SelectionEnd = selectionEnd;
		IsPrimary = isPrimary;
	}

	public bool IsPrimary { get; }

	public int Offset { get; }

	public int SelectionEnd { get; }

	public int SelectionStart { get; }
}
