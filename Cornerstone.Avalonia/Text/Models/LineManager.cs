#region References

using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Profiling;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public class LineManager : Notifiable, IEnumerable<Line>, IQueue<Line>
{
	#region Fields

	private readonly IList<Line> _lines;
	private readonly IQueue<Line> _pool;

	#endregion

	#region Constructors

	internal LineManager(TextEditorViewModel viewModel)
	{
		ViewModel = viewModel;

		_pool = new SpeedyQueue<Line>(65536);
		_lines = new SpeedyList<Line>(isLongLivedBuffer: true, clearOnCleanup: false);
	}

	#endregion

	#region Properties

	public int Count => _lines.Count;

	public Line this[int index] => _lines[index];

	public int LineRebuildIndex { get; private set; }

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

		// If we get here, offset lies between lines or after last line
		// Because we already checked the after-last-line case, this means:
		// between line[right] and line[right+1]
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

	public Line LastOrDefault()
	{
		return _lines.Count == 0 ? null : _lines[_lines.Count - 1];
	}

	public Size Measure(Size availableSize, bool wordWrap)
	{
		var offsetY = 0.0;
		var documentWidth = 0.0;
		double? maxWidth = wordWrap ? availableSize.Width : null;

		foreach (var line in _lines)
		{
			line.UpdateLineMetrics(offsetY, maxWidth);

			if (line.VisualLayout.Width > documentWidth)
			{
				documentWidth = line.VisualLayout.Width;
			}
			offsetY += line.VisualLayout.Height;
		}

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

	internal void Rebuild(TextDocumentChangedArgs args)
	{
		using var _ = ProfilerExtensions.Start(ViewModel.Profiler, "LineManager.Rebuild");

		LineRebuildIndex = GetLineOffsetForDocumentOffset(args.Offset);

		var lineNumber = Count > 0 ? _lines[LineRebuildIndex]?.LineNumber ?? 1 : 1;
		var index = Count > 0 ? _lines[LineRebuildIndex]?.StartOffset ?? 0 : 0;

		while (NextLine(lineNumber, ref index) is { } line)
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

		NotifyOfPropertyChanged(nameof(Count));
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private Line NextLine(int lineNumber, ref int index)
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
			return StartNewLine(lineNumber, index++);
		}

		// We are exactly at end, create empty line if previous line ended in new line
		if (index == bufferCount)
		{
			var prev = ViewModel.Buffer[index - 1];
			return prev is '\n' or '\r'
				? StartNewLine(lineNumber, index++)
				: null;
		}

		var line = StartNewLine(lineNumber, index);

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
					return line;
				}
				case '\n':
				{
					line.LineEndingLength = 1;
					line.EndOffset = index;
					return line;
				}
			}
		}

		// Reached natural end of buffer without newline
		line.EndOffset = index;
		return line;
	}

	private Line StartNewLine(int lineNumber, int startOffset)
	{
		var line = TryDequeue(out var p) ? p : new Line(this);
		line.Reset(lineNumber, startOffset);
		return line;
	}

	#endregion
}