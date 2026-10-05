#region References

using System;
using System.Collections;
using System.Collections.Generic;
using Cornerstone.Presentation;
using Cornerstone.Collections;
using Cornerstone.Profiling;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Models;

[SourceReflection]
public class LineManager : CornerstoneObject, IEnumerable<Line>, IQueue<Line>
{
	#region Fields

	private double _cachedDocumentHeight;
	private double _cachedDocumentWidth;
	private bool _hasMeasureCache;
	private double _lastCharacterHeight;
	private double _lastCharacterWidth;
	private double _lastMeasureMaxWidth;
	private bool _lastWordWrap;
	private int _layoutFromIndex;
	private bool _layoutIncremental;
	private readonly IList<Line> _lines;
	private readonly IQueue<Line> _pool;
	private int _rebindOldLength;
	private int _rebindOldStart = -1;

	#endregion

	#region Constructors

	internal LineManager(TextEditorViewModel viewModel)
	{
		ViewModel = viewModel;

		_pool = new SpeedyQueue<Line>(65536);
		_lines = new SpeedyList<Line>(isLongLivedBuffer: true, clearOnCleanup: false);
		_layoutFromIndex = 0;
	}

	#endregion

	#region Properties

	public int Count => _lines.Count;

	public Line this[int index] => _lines[index];

	public int LineRebuildIndex { get; private set; }

	/// <summary>
	/// True when the last edit stayed on the last line and that line's height did not change.
	/// Consumers should repaint only — skip measure, margin layout, and scroll-to-end.
	/// </summary>
	internal bool LastEditNeedsPaintOnly { get; private set; }

	/// <summary>
	/// True when the last Measure recomputed line metrics, shifted Y, or changed extent.
	/// False on a wrap-off cache hit (resize with the same character metrics).
	/// </summary>
	internal bool LastMeasureChangedLayout { get; private set; }

	internal TextEditorViewModel ViewModel { get; }

	#endregion

	#region Methods

	public void Add(Line line)
	{
		_lines.Add(line);
	}

	public void Clear()
	{
		// Not implemented
	}

	public void Enqueue(Line value)
	{
		_pool.Enqueue(value);
	}

	public void Enqueue(Line[] values)
	{
		_pool.Enqueue(values);
	}

	public void Enqueue(ReadOnlySpan<Line> values)
	{
		_pool.Enqueue(values);
	}

	public IEnumerator<Line> GetEnumerator()
	{
		return _lines.GetEnumerator();
	}

	public Line GetLineFromOffset(int offset)
	{
		if (_lines.Count == 0)
		{
			return null;
		}

		if (_lines.Count == 1)
		{
			return _lines[0];
		}

		// Fast path for very likely case: offset at or beyond end
		if (offset >= _lines[^1].EndOffset)
		{
			return _lines[^1];
		}

		var left = 0;
		var right = _lines.Count - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			var line = _lines[mid];

			if (line.Contains(offset))
			{
				return line;
			}

			if (offset < line.StartOffset)
			{
				right = mid - 1;
			}
			else
			{
				left = mid + 1;
			}
		}

		// Offset lies between lines (or before the first). Never index -1.
		if (right < 0)
		{
			return _lines[0];
		}

		if (right >= _lines.Count)
		{
			return _lines[_lines.Count - 1];
		}

		return _lines[right];
	}

	public int GetLineOffsetForDocumentOffset(int offset)
	{
		if (_lines.Count == 0)
		{
			return 0;
		}

		if (_lines.Count == 1)
		{
			return 0;
		}

		// Fast path for very likely case: offset at or beyond end
		if (offset >= _lines[^1].EndOffset)
		{
			return _lines.Count - 1;
		}

		var left = 0;
		var right = _lines.Count - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			var line = _lines[mid];

			if (line.Contains(offset))
			{
				return mid;
			}

			if (offset < line.StartOffset)
			{
				right = mid - 1;
			}
			else
			{
				left = mid + 1;
			}
		}

		// If we get here, offset lies between lines or after last line
		// Because we already checked the after-last-line case, this means:
		// between line[right] and line[right+1]
		return right;
	}

	/// <summary>
	/// Lines whose visual rect intersects [topY, bottomY). Binary-searches the first
	/// candidate so paint does not walk the whole document.
	/// </summary>
	public IEnumerable<Line> GetVisibleLines(double topY, double bottomY)
	{
		var index = GetFirstVisibleLineIndex(topY);
		while (index < _lines.Count)
		{
			var line = _lines[index++];
			if (line.VisualLayout.Top >= bottomY)
			{
				yield break;
			}

			if ((line.VisualLayout.Height <= 0) || (line.VisualLayout.Bottom <= topY))
			{
				continue;
			}

			yield return line;
		}
	}

	public Line LastOrDefault()
	{
		return _lines.Count == 0 ? null : _lines[_lines.Count - 1];
	}

	public Size Measure(Size availableSize, bool wordWrap)
	{
		var metrics = ViewModel.ViewMetrics;
		var offsetY = 0.0;
		var documentWidth = 0.0;

		// Word-wrap only when width is finite; infinite available width means
		// unconstrained measure — use natural (unwrapped) line widths.
		double? maxWidth = wordWrap && double.IsFinite(availableSize.Width)
			? availableSize.Width
			: null;

		var wrapWidth = maxWidth ?? double.NaN;
		var metricsChanged = !_hasMeasureCache
			|| !metrics.CharacterHeight.Equals(_lastCharacterHeight)
			|| !metrics.CharacterWidth.Equals(_lastCharacterWidth);
		var wrapChanged = !_hasMeasureCache
			|| (wordWrap != _lastWordWrap)
			|| !AreSameWidth(wrapWidth, _lastMeasureMaxWidth);

		if (!metricsChanged && !wrapChanged && !_layoutIncremental && _hasMeasureCache && HasReusableVisualCache())
		{
			LastMeasureChangedLayout = false;
			_layoutIncremental = false;
			_layoutFromIndex = 0;
			return new Size(_cachedDocumentWidth, _cachedDocumentHeight);
		}

		var start = 0;
		if (!metricsChanged && _layoutIncremental && !wrapChanged && (_layoutFromIndex > 0) && (_layoutFromIndex < _lines.Count)
			&& (_lines[_layoutFromIndex - 1].VisualLayout.Height > 0))
		{
			start = _layoutFromIndex;
			offsetY = _lines[start - 1].VisualLayout.Bottom;
			documentWidth = _cachedDocumentWidth;
		}

		LastMeasureChangedLayout = true;

		for (var i = start; i < _lines.Count; i++)
		{
			var line = _lines[i];
			if (ViewModel.FoldingManager.IsLineCollapsed(line))
			{
				line.CollapseVisual(offsetY);
				continue;
			}

			var canShift = !wrapChanged
				&& !metricsChanged
				&& (i > start)
				&& (line.VisualLayout.Height > 0);
			if (canShift)
			{
				line.ShiftVisualY(offsetY);
			}
			else
			{
				line.UpdateLineMetrics(offsetY, maxWidth);
			}

			if (line.VisualLayout.Width > documentWidth)
			{
				documentWidth = line.VisualLayout.Width;
			}
			offsetY += line.VisualLayout.Height;
		}

		_lastWordWrap = wordWrap;
		_lastMeasureMaxWidth = wrapWidth;
		_lastCharacterHeight = metrics.CharacterHeight;
		_lastCharacterWidth = metrics.CharacterWidth;
		_hasMeasureCache = true;
		_layoutIncremental = false;
		_layoutFromIndex = 0;

		if (!double.IsFinite(documentWidth) || (documentWidth < 0))
		{
			documentWidth = 0;
		}
		if (!double.IsFinite(offsetY) || (offsetY < 0))
		{
			offsetY = 0;
		}

		_cachedDocumentWidth = documentWidth;
		_cachedDocumentHeight = offsetY;
		return new Size(documentWidth, offsetY);
	}

	public bool TryDequeue(out Line value)
	{
		if ((LineRebuildIndex >= 0) && (LineRebuildIndex < Count))
		{
			value = this[LineRebuildIndex];
			return true;
		}

		return _pool.TryDequeue(out value);
	}

	public bool TryGetLine(int lineNumber, out Line line)
	{
		if ((lineNumber <= 0) || (lineNumber > _lines.Count))
		{
			line = null;
			return false;
		}

		line = _lines[lineNumber - 1];
		return true;
	}

	/// <summary>
	/// Fast lookup: returns the line that contains the given document offset.
	/// Uses binary search (O(log N)) on the ordered _lines collection.
	/// </summary>
	public bool TryGetLineForOffset(int offset, out Line line)
	{
		line = null;
		if ((offset < 0) || (_lines.Count == 0))
		{
			return false;
		}

		// Binary search
		var left = 0;
		var right = _lines.Count - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) / 2);
			var current = _lines[mid];

			if (offset < current.StartOffset)
			{
				right = mid - 1;
			}
			else if (offset >= current.EndOffset)
			{
				left = mid + 1;
			}
			else
			{
				line = current;
				return true;
			}
		}

		// Edge case: caret exactly at the very start or end of the document
		if ((offset == ViewModel.Buffer.Count) && (_lines.Count > 0))
		{
			line = _lines[^1];
			return true;
		}

		return false;
	}

	/// <summary>
	/// Fast lookup: returns the line that contains the given document visual X/Y.
	/// Uses binary search (O(log N)) on the ordered _lines collection based on VisualLayout.
	/// Clamps to the first/last line (standard behavior in every major text editor for mouse clicks).
	/// </summary>
	public bool TryGetLineForOffset(double visualX, double visualY, out Line line)
	{
		line = null;

		if (_lines.Count == 0)
		{
			return false;
		}

		// Binary search on cumulative Y position
		var left = 0;
		var right = _lines.Count - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			var rect = _lines[mid].VisualLayout;

			if (visualY < rect.Y)
			{
				right = mid - 1;
			}
			else if (visualY >= rect.Bottom)
			{
				left = mid + 1;
			}
			else
			{
				line = _lines[mid];
				return true;
			}
		}

		// Clamp to nearest edge (essential for mouse hit-testing)
		line = visualY < _lines[0].VisualLayout.Y ? _lines[0] : _lines[^1];
		return true;
	}

	public bool TryPeek(out Line value)
	{
		return _pool.TryPeek(out value);
	}

	internal void InvalidateLayout()
	{
		_hasMeasureCache = false;
		_layoutIncremental = false;
		_layoutFromIndex = 0;
	}

	internal void Rebuild(TextDocumentChangedArgs args)
	{
		using var _ = ProfilerExtensions.Start(ViewModel.Profiler, "LineManager.Rebuild");

		LastEditNeedsPaintOnly = false;
		LineRebuildIndex = GetLineOffsetForDocumentOffset(args.Offset);

		// Measure is coalesced (many Rebuilds per layout pass). Keep the earliest
		// dirty line so later appends cannot skip unmeasured rows (blank terminal).
		if (args.Type != TextDocumentChangeType.Add)
		{
			// Reset/Remove drop VisualLayout on reused lines. Do not keep the
			// wrap-off measure cache or the next Measure would skip UpdateLineMetrics
			// and paint an empty first frame.
			_layoutIncremental = false;
			_layoutFromIndex = 0;
			_hasMeasureCache = false;
		}
		else if (!_layoutIncremental)
		{
			_layoutIncremental = true;
			_layoutFromIndex = LineRebuildIndex;
		}
		else if (LineRebuildIndex < _layoutFromIndex)
		{
			_layoutFromIndex = LineRebuildIndex;
		}

		var rebindVisual = args.Type != TextDocumentChangeType.Reset;
		var documentDelta = 0;
		if (args.Type == TextDocumentChangeType.Add)
		{
			documentDelta = args.Text?.Length ?? 0;
		}
		else if (args.Type == TextDocumentChangeType.Remove)
		{
			documentDelta = -(args.Text?.Length ?? 0);
		}

		var lineNumber = Count > 0 ? _lines[LineRebuildIndex]?.LineNumber ?? 1 : 1;
		var index = Count > 0 ? _lines[LineRebuildIndex]?.StartOffset ?? 0 : 0;

		while (NextLine(lineNumber, ref index, rebindVisual, documentDelta) is { } line)
		{
			if (LineRebuildIndex++ < Count)
			{
				// An existing line was updated so just continue
				lineNumber++;
				continue;
			}

			Add(line);
			lineNumber++;
		}

		while (LineRebuildIndex < Count)
		{
			// Pool the remaining lines.
			var lineToPool = this[Count - 1];
			_lines.RemoveAt(Count - 1);
			_pool.Enqueue(lineToPool);
		}

		LineRebuildIndex = -1;

		NotifyComputedPropertyChanged(nameof(Count));
	}

	/// <summary>
	/// Apply an insert or delete that stays on the last line (no line breaks).
	/// Skips a full line scan and keeps the existing visual Y.
	/// </summary>
	internal bool TryApplyLastLineEdit(TextDocumentChangedArgs args)
	{
		LastEditNeedsPaintOnly = false;
		if (string.IsNullOrEmpty(args.Text)
			|| (_lines.Count == 0)
			|| !_hasMeasureCache)
		{
			return false;
		}

		if (args.Text.AsSpan().IndexOfAny('\r', '\n') >= 0)
		{
			return false;
		}

		var last = _lines[_lines.Count - 1];
		if (last.VisualLayout.Height <= 0)
		{
			return false;
		}

		var length = args.Text.Length;
		if (args.Type == TextDocumentChangeType.Add)
		{
			if ((args.Offset < last.StartOffset) || (args.Offset > last.EndOffset))
			{
				return false;
			}

			last.EndOffset += length;
		}
		else if (args.Type == TextDocumentChangeType.Remove)
		{
			if ((args.Offset < last.StartOffset)
				|| ((args.Offset + length) > last.EndOffset))
			{
				return false;
			}

			last.EndOffset -= length;
		}
		else
		{
			return false;
		}

		var oldHeight = last.VisualLayout.Height;
		double? wrapWidth = null;
		if (!double.IsNaN(_lastMeasureMaxWidth))
		{
			wrapWidth = _lastMeasureMaxWidth;
		}

		last.UpdateLineMetrics(last.VisualLayout.Y, wrapWidth);

		if (last.VisualLayout.Width > _cachedDocumentWidth)
		{
			_cachedDocumentWidth = last.VisualLayout.Width;
		}

		_cachedDocumentHeight = last.VisualLayout.Bottom;
		ViewModel.ViewMetrics.DocumentSize = new Size(_cachedDocumentWidth, _cachedDocumentHeight);

		if (Math.Abs(last.VisualLayout.Height - oldHeight) > 0.01)
		{
			_layoutIncremental = true;
			_layoutFromIndex = _lines.Count - 1;
			return true;
		}

		LastEditNeedsPaintOnly = true;
		return true;
	}

	private bool HasReusableVisualCache()
	{
		for (var i = 0; i < _lines.Count; i++)
		{
			var line = _lines[i];
			if (line.VisualLayout.Height > 0)
			{
				return true;
			}
		}

		return false;
	}

	private static bool AreSameWidth(double left, double right)
	{
		if (double.IsNaN(left) && double.IsNaN(right))
		{
			return true;
		}

		return left.Equals(right);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	internal int GetFirstVisibleLineIndex(double topY)
	{
		if (_lines.Count == 0)
		{
			return 0;
		}

		var left = 0;
		var right = _lines.Count - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			var rect = _lines[mid].VisualLayout;

			if (topY < rect.Y)
			{
				right = mid - 1;
			}
			else if (topY >= rect.Bottom)
			{
				left = mid + 1;
			}
			else
			{
				return mid;
			}
		}

		if (topY < _lines[0].VisualLayout.Y)
		{
			return 0;
		}

		if (topY >= _lines[^1].VisualLayout.Bottom)
		{
			return _lines.Count;
		}

		return left;
	}

	private Line NextLine(int lineNumber, ref int index, bool rebindVisual, int documentDelta)
	{
		var bufferCount = ViewModel.Buffer.Count;

		// Already past end, only allow one empty line at very beginning
		if (index > bufferCount)
		{
			return null;
		}

		// Document is completely empty, create a single empty line
		if ((index == 0) && (bufferCount == 0))
		{
			var empty = StartNewLine(lineNumber, index++, rebindVisual);
			FinishRebind(empty, rebindVisual, documentDelta);
			return empty;
		}

		// We are exactly at end, create empty line if previous line ended in new line
		if (index == bufferCount)
		{
			var prev = ViewModel.Buffer[index - 1];
			if (prev is not ('\n' or '\r'))
			{
				return null;
			}

			var trailing = StartNewLine(lineNumber, index++, rebindVisual);
			FinishRebind(trailing, rebindVisual, documentDelta);
			return trailing;
		}

		var line = StartNewLine(lineNumber, index, rebindVisual);

		while (index < bufferCount)
		{
			switch (ViewModel.Buffer[index++])
			{
				case '\r':
				{
					if ((index < bufferCount) && (ViewModel.Buffer[index] == '\n'))
					{
						index++;
						line.LineEndingLength = 2;
					}
					else
					{
						line.LineEndingLength = 1;
					}
					line.EndOffset = index;
					FinishRebind(line, rebindVisual, documentDelta);
					return line;
				}
				case '\n':
				{
					line.LineEndingLength = 1;
					line.EndOffset = index;
					FinishRebind(line, rebindVisual, documentDelta);
					return line;
				}
			}
		}

		// Reached natural end of buffer without newline
		line.EndOffset = index;
		FinishRebind(line, rebindVisual, documentDelta);
		return line;
	}

	private Line StartNewLine(int lineNumber, int startOffset, bool rebindVisual)
	{
		var reusing = rebindVisual && (LineRebuildIndex >= 0) && (LineRebuildIndex < Count);
		var line = TryDequeue(out var p) ? p : new Line(this);
		if (reusing)
		{
			_rebindOldStart = line.StartOffset;
			_rebindOldLength = line.Length;
			line.Rebind(lineNumber, startOffset);
		}
		else
		{
			_rebindOldStart = -1;
			line.Reset(lineNumber, startOffset);
		}

		return line;
	}

	private void FinishRebind(Line line, bool rebindVisual, int documentDelta)
	{
		if (!rebindVisual || (_rebindOldStart < 0))
		{
			return;
		}

		line.PreserveOrClearVisual(_rebindOldStart, _rebindOldLength, documentDelta);
		_rebindOldStart = -1;
	}

	#endregion
}