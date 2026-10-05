#region References

using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Presentation.Controls.Text.Models;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Folding;

/// <summary>
/// Fold ranges for a text document. Collapse is view state; the buffer is unchanged.
/// </summary>
public class FoldingManager
{
	#region Constants

	public const int NearbyRestoreLineWindow = 32;

	#endregion

	#region Fields

	private readonly List<FoldingSection> _sections;
	private IFoldingStrategy _strategy;
	private readonly TextEditorViewModel _viewModel;
	private bool _isFirstUpdate;

	#endregion

	#region Constructors

	internal FoldingManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
		_sections = [];
		_isFirstUpdate = true;
	}

	#endregion

	#region Properties

	public IReadOnlyList<FoldingSection> AllFoldings => _sections;

	public bool HasStrategy => _strategy != null;

	/// <summary>
	/// Optional fold discovery. Set by the host (for example PowerShell regions).
	/// </summary>
	public IFoldingStrategy Strategy
	{
		get => _strategy;
		set
		{
			var hadStrategy = _strategy != null;
			_strategy = value;
			if (_strategy != null)
			{
				Refresh();
			}
			else if (hadStrategy)
			{
				Clear();
			}
		}
	}

	#endregion

	#region Methods

	public int[] CaptureCollapsedStartOffsets()
	{
		var count = 0;
		foreach (var section in _sections)
		{
			if (section.IsFolded)
			{
				count++;
			}
		}

		if (count == 0)
		{
			return [];
		}

		var offsets = new int[count];
		var index = 0;
		foreach (var section in _sections)
		{
			if (section.IsFolded)
			{
				offsets[index++] = section.StartOffset;
			}
		}

		return offsets;
	}

	public CollapsedFoldSnapshot[] CaptureCollapsedFolds()
	{
		var count = 0;
		foreach (var section in _sections)
		{
			if (section.IsFolded)
			{
				count++;
			}
		}

		if (count == 0)
		{
			return [];
		}

		var snapshots = new CollapsedFoldSnapshot[count];
		var index = 0;
		foreach (var section in _sections)
		{
			if (!section.IsFolded)
			{
				continue;
			}

			snapshots[index++] = new CollapsedFoldSnapshot
			{
				Offset = section.StartOffset,
				Line = GetStartLineNumber(section),
				Hint = GetHint(section),
				Path = GetFoldPath(section)
			};
		}

		return snapshots;
	}

	/// <summary>
	/// Collapse sections that match the snapshots (offset, nearby hint, or unique path).
	/// Other sections are expanded. No-op when snapshots is null.
	/// </summary>
	public void ApplyCollapsedFolds(CollapsedFoldSnapshot[] snapshots)
	{
		if (snapshots == null)
		{
			return;
		}

		var collapsed = new HashSet<FoldingSection>();
		foreach (var snapshot in snapshots)
		{
			if (snapshot == null)
			{
				continue;
			}

			var match = FindRestoreMatch(snapshot, collapsed);
			if (match != null)
			{
				collapsed.Add(match);
			}
		}

		var changed = false;
		foreach (var section in _sections)
		{
			if (section.SetFoldedCore(collapsed.Contains(section)))
			{
				changed = true;
			}
		}

		if (changed)
		{
			NotifyFoldingChanged();
		}
	}

	public int AdjustCaretOffset(int previousOffset, int newOffset)
	{
		var section = GetCollapsingSection(newOffset);
		if (section == null)
		{
			return newOffset;
		}

		var previousCollapsed = GetCollapsingSection(previousOffset) == section;
		if (previousCollapsed && (newOffset >= previousOffset))
		{
			return section.EndOffset;
		}

		if (IsOnHeaderLine(section, previousOffset) && (newOffset >= previousOffset))
		{
			var headerEnd = GetHeaderContentEnd(section);
			if (previousOffset >= headerEnd)
			{
				return section.EndOffset;
			}
		}

		return GetHeaderContentEnd(section);
	}

	/// <summary>
	/// Collapse sections whose start offset is in <paramref name="offsets"/>.
	/// Other sections are expanded. No-op when <paramref name="offsets"/> is null.
	/// </summary>
	public void ApplyCollapsedStartOffsets(int[] offsets)
	{
		if (offsets == null)
		{
			return;
		}

		var collapsed = new HashSet<int>(offsets);
		var changed = false;
		foreach (var section in _sections)
		{
			if (section.SetFoldedCore(collapsed.Contains(section.StartOffset)))
			{
				changed = true;
			}
		}

		if (changed)
		{
			NotifyFoldingChanged();
		}
	}

	public void ApplyDocumentChange(TextDocumentChangedArgs args)
	{
		if (args.Type == TextDocumentChangeType.Reset)
		{
			// Strategy Refresh rematches by start offset and keeps IsFolded.
			// Without a strategy, offsets are stale after a full buffer replace.
			if (_strategy == null)
			{
				_sections.Clear();
				_isFirstUpdate = true;
			}
			return;
		}

		var length = args.Text?.Length ?? 0;
		if (length == 0)
		{
			return;
		}

		var delta = args.Type == TextDocumentChangeType.Remove ? -length : length;
		if (delta == 0)
		{
			return;
		}

		for (var i = _sections.Count - 1; i >= 0; i--)
		{
			var section = _sections[i];
			if (section.StartOffset >= args.Offset)
			{
				section.StartOffset += delta;
				section.EndOffset += delta;
			}
			else if (section.EndOffset > args.Offset)
			{
				section.EndOffset += delta;
			}

			if ((section.StartOffset < 0)
				|| (section.EndOffset <= section.StartOffset)
				|| (section.EndOffset > _viewModel.DocumentLength))
			{
				_sections.RemoveAt(i);
			}
		}
	}

	public void CollapseAll()
	{
		SetAllFolded(true);
	}

	public void ExpandAll()
	{
		SetAllFolded(false);
	}

	public void Clear()
	{
		if (_sections.Count == 0)
		{
			return;
		}

		_sections.Clear();
		NotifyFoldingChanged();
	}

	public FoldingSection CreateFolding(int startOffset, int endOffset)
	{
		if (startOffset >= endOffset)
		{
			throw new ArgumentException("startOffset must be less than endOffset");
		}

		var section = new FoldingSection(this, startOffset, endOffset);
		var index = 0;
		while ((index < _sections.Count) && (_sections[index].StartOffset <= startOffset))
		{
			index++;
		}

		_sections.Insert(index, section);
		NotifyFoldingChanged();
		return section;
	}

	public FoldingSection CreateFoldingForLines(int startLineNumber, int endLineNumber)
	{
		if (!_viewModel.Lines.TryGetLine(startLineNumber, out var startLine)
			|| !_viewModel.Lines.TryGetLine(endLineNumber, out var endLine))
		{
			throw new ArgumentOutOfRangeException();
		}

		return CreateFolding(startLine.StartOffset, endLine.EndOffset);
	}

	public FoldingSection GetCollapsingSection(int offset)
	{
		FoldingSection result = null;
		foreach (var section in _sections)
		{
			if (!section.IsFolded || !section.Contains(offset))
			{
				continue;
			}

			if (IsOnHeaderLine(section, offset))
			{
				continue;
			}

			if ((result == null) || (section.StartOffset < result.StartOffset))
			{
				result = section;
			}
		}

		return result;
	}

	public IReadOnlyList<FoldingSection> GetFoldingsContaining(int offset)
	{
		var result = new List<FoldingSection>();
		foreach (var section in _sections)
		{
			if (section.Contains(offset) || (section.StartOffset == offset))
			{
				result.Add(section);
			}
		}

		return result;
	}

	public FoldingSection GetFoldingStartingOnLine(Text.Models.Line line)
	{
		if ((line == null) || (_sections.Count == 0) || IsLineCollapsed(line))
		{
			return null;
		}

		var index = FindFirstIndexAtOrAfter(line.StartOffset);
		if (index >= _sections.Count)
		{
			return null;
		}

		var section = _sections[index];
		var onLine = line.Contains(section.StartOffset)
			|| (section.StartOffset == line.StartOffset);
		return onLine ? section : null;
	}

	public FoldingSection GetNextFolding(int startOffset)
	{
		var index = FindFirstIndexAtOrAfter(startOffset);
		return index < _sections.Count ? _sections[index] : null;
	}

	public bool IsLineCollapsed(Text.Models.Line line)
	{
		if (line == null)
		{
			return false;
		}

		foreach (var section in _sections)
		{
			if (!section.IsFolded)
			{
				continue;
			}

			if ((section.StartOffset < line.StartOffset) && (line.StartOffset < section.EndOffset))
			{
				return true;
			}
		}

		return false;
	}

	public void Refresh()
	{
		if (_strategy == null)
		{
			return;
		}

		try
		{
			UpdateFoldings(_strategy.CreateNewFoldings(_viewModel));
		}
		catch
		{
			// Fold discovery is derived; keep existing sections.
		}
	}

	public void RemoveFolding(FoldingSection section)
	{
		if (section == null)
		{
			return;
		}

		section.IsFolded = false;
		_sections.Remove(section);
		NotifyFoldingChanged();
	}

	public void UpdateFoldings(IEnumerable<NewFolding> newFoldings, int firstErrorOffset = -1)
	{
		if (newFoldings == null)
		{
			newFoldings = [];
		}

		if (firstErrorOffset < 0)
		{
			firstErrorOffset = int.MaxValue;
		}

		var oldFoldings = _sections.ToArray();
		var oldFoldingIndex = 0;
		var previousStartOffset = 0;
		var changed = false;

		foreach (var newFolding in newFoldings)
		{
			if (newFolding.StartOffset < previousStartOffset)
			{
				throw new ArgumentException("newFoldings must be sorted by start offset");
			}

			previousStartOffset = newFolding.StartOffset;
			if (newFolding.StartOffset == newFolding.EndOffset)
			{
				continue;
			}

			while ((oldFoldingIndex < oldFoldings.Length) && (newFolding.StartOffset > oldFoldings[oldFoldingIndex].StartOffset))
			{
				_sections.Remove(oldFoldings[oldFoldingIndex++]);
				changed = true;
			}

			FoldingSection section;
			if ((oldFoldingIndex < oldFoldings.Length) && (newFolding.StartOffset == oldFoldings[oldFoldingIndex].StartOffset))
			{
				section = oldFoldings[oldFoldingIndex++];
				section.EndOffset = newFolding.EndOffset;
			}
			else
			{
				section = new FoldingSection(this, newFolding.StartOffset, newFolding.EndOffset);
				var index = 0;
				while ((index < _sections.Count) && (_sections[index].StartOffset <= section.StartOffset))
				{
					index++;
				}

				_sections.Insert(index, section);
				if (_isFirstUpdate)
				{
					section.IsFolded = newFolding.DefaultClosed;
				}

				changed = true;
			}

			section.Title = newFolding.Name;
		}

		_isFirstUpdate = false;

		while (oldFoldingIndex < oldFoldings.Length)
		{
			var oldSection = oldFoldings[oldFoldingIndex++];
			if (oldSection.StartOffset >= firstErrorOffset)
			{
				break;
			}

			_sections.Remove(oldSection);
			changed = true;
		}

		if (changed)
		{
			NotifyFoldingChanged();
		}
	}

	internal int GetHeaderContentEnd(FoldingSection section)
	{
		var line = _viewModel.Lines.GetLineFromOffset(section.StartOffset);
		if (line == null)
		{
			return section.StartOffset;
		}

		return line.EndOffset - line.LineEndingLength;
	}

	internal void NotifyFoldingChanged()
	{
		ClampCaretIfCollapsed();
		_viewModel.Lines.InvalidateLayout();
		FoldingsChanged?.Invoke(this, EventArgs.Empty);
	}

	private int FindFirstIndexAtOrAfter(int startOffset)
	{
		var left = 0;
		var right = _sections.Count - 1;
		var result = _sections.Count;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			if (_sections[mid].StartOffset >= startOffset)
			{
				result = mid;
				right = mid - 1;
			}
			else
			{
				left = mid + 1;
			}
		}

		return result;
	}

	private void SetAllFolded(bool folded)
	{
		var changed = false;
		foreach (var section in _sections)
		{
			if (section.SetFoldedCore(folded))
			{
				changed = true;
			}
		}

		if (changed)
		{
			NotifyFoldingChanged();
		}
	}

	private void ClampCaretIfCollapsed()
	{
		var caret = _viewModel.Caret;
		if (caret == null)
		{
			return;
		}

		var section = GetCollapsingSection(caret.Offset);
		if (section == null)
		{
			return;
		}

		var target = GetHeaderContentEnd(section);
		if (caret.Offset != target)
		{
			caret.Move(target);
		}
	}

	private bool IsOnHeaderLine(FoldingSection section, int offset)
	{
		var line = _viewModel.Lines.GetLineFromOffset(section.StartOffset);
		if (line == null)
		{
			return offset == section.StartOffset;
		}

		return (offset >= line.StartOffset) && (offset < line.EndOffset);
	}

	private FoldingSection FindRestoreMatch(CollapsedFoldSnapshot snapshot, HashSet<FoldingSection> alreadyMatched)
	{
		var exact = GetFoldingAtStartOffset(snapshot.Offset);
		if ((exact != null) && !alreadyMatched.Contains(exact) && HintAllows(snapshot, exact))
		{
			return exact;
		}

		var nearby = FindNearbyHintMatch(snapshot, alreadyMatched);
		if (nearby != null)
		{
			return nearby;
		}

		return FindUniquePathMatch(snapshot, alreadyMatched);
	}

	private FoldingSection GetFoldingAtStartOffset(int startOffset)
	{
		foreach (var section in _sections)
		{
			if (section.StartOffset == startOffset)
			{
				return section;
			}
		}

		return null;
	}

	private FoldingSection FindNearbyHintMatch(CollapsedFoldSnapshot snapshot, HashSet<FoldingSection> alreadyMatched)
	{
		if (string.IsNullOrEmpty(snapshot.Hint))
		{
			return null;
		}

		FoldingSection best = null;
		var bestDistance = int.MaxValue;
		foreach (var section in _sections)
		{
			if (alreadyMatched.Contains(section))
			{
				continue;
			}

			if (!HintsEqual(snapshot.Hint, GetHint(section)))
			{
				continue;
			}

			var line = GetStartLineNumber(section);
			var distance = Math.Abs(line - snapshot.Line);
			if (distance > NearbyRestoreLineWindow)
			{
				continue;
			}

			if ((best == null) || (distance < bestDistance) || ((distance == bestDistance) && (section.StartOffset < best.StartOffset)))
			{
				best = section;
				bestDistance = distance;
			}
		}

		return best;
	}

	private FoldingSection FindUniquePathMatch(CollapsedFoldSnapshot snapshot, HashSet<FoldingSection> alreadyMatched)
	{
		if (string.IsNullOrEmpty(snapshot.Path))
		{
			return null;
		}

		FoldingSection match = null;
		foreach (var section in _sections)
		{
			if (alreadyMatched.Contains(section))
			{
				continue;
			}

			if (!string.Equals(GetFoldPath(section), snapshot.Path, StringComparison.Ordinal))
			{
				continue;
			}

			if (match != null)
			{
				return null;
			}

			match = section;
		}

		return match;
	}

	private bool HintAllows(CollapsedFoldSnapshot snapshot, FoldingSection section)
	{
		if (string.IsNullOrEmpty(snapshot.Hint))
		{
			return true;
		}

		return HintsEqual(snapshot.Hint, GetHint(section));
	}

	private static bool HintsEqual(string left, string right)
	{
		return string.Equals(left?.Trim(), right?.Trim(), StringComparison.Ordinal);
	}

	private int GetStartLineNumber(FoldingSection section)
	{
		var line = _viewModel.Lines.GetLineFromOffset(section.StartOffset);
		return line?.LineNumber ?? 0;
	}

	private string GetHint(FoldingSection section)
	{
		if (!string.IsNullOrEmpty(section.Title))
		{
			return section.Title.Trim();
		}

		var line = _viewModel.Lines.GetLineFromOffset(section.StartOffset);
		if (line == null)
		{
			return string.Empty;
		}

		return line.ToString()?.Trim() ?? string.Empty;
	}

	private string GetFoldPath(FoldingSection section)
	{
		var builder = new StringBuilder();
		foreach (var candidate in _sections)
		{
			if (candidate.StartOffset >= section.StartOffset)
			{
				break;
			}

			if (!candidate.Contains(section.StartOffset))
			{
				continue;
			}

			AppendPathSegment(builder, GetHint(candidate));
		}

		AppendPathSegment(builder, GetHint(section));
		return builder.ToString();
	}

	private static void AppendPathSegment(StringBuilder builder, string segment)
	{
		if (string.IsNullOrEmpty(segment))
		{
			return;
		}

		if (builder.Length > 0)
		{
			builder.Append('/');
		}

		builder.Append(segment);
	}

	#endregion

	#region Events

	public event EventHandler FoldingsChanged;

	#endregion
}
