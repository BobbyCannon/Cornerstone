#region References

using System.Collections;
using System.Collections.Generic;
using Cornerstone.Avalonia.HexEditor.Document;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

internal sealed class VisualBytesLinesBuffer : IReadOnlyList<VisualBytesLine>
{
	#region Fields

	private readonly List<VisualBytesLine> _activeLines = new();
	private readonly HexView _owner;
	private readonly Stack<VisualBytesLine> _pool = new();

	#endregion

	#region Constructors

	public VisualBytesLinesBuffer(HexView owner)
	{
		_owner = owner;
	}

	#endregion

	#region Properties

	public int Count => _activeLines.Count;

	public VisualBytesLine this[int index] => _activeLines[index];

	#endregion

	#region Methods

	public void Clear()
	{
		foreach (var instance in _activeLines)
		{
			Return(instance);
		}
		_activeLines.Clear();
	}

	public IEnumerator<VisualBytesLine> GetEnumerator()
	{
		return _activeLines.GetEnumerator();
	}

	public VisualBytesLine GetOrCreateVisualLine(BitRange range)
	{
		VisualBytesLine newLine = null;

		// Find existing line or create a new one, while keeping the list of visual lines ordered by range.
		for (var i = 0; i < _activeLines.Count; i++)
		{
			// Exact match?
			var currentLine = _activeLines[i];
			if (currentLine.Range.Start == range.Start)
			{
				// Edge-case: if our range is not exactly right, the line's range is outdated (e.g., as a result of
				// inserting or removing a character at the end of the document).
				if (currentLine.Range.End != range.End)
				{
					_activeLines[i] = currentLine = Rent(range);
				}

				return currentLine;
			}

			// If the next line is further than the requested start, the line does not exist.
			if (currentLine.Range.Start > range.Start)
			{
				newLine = Rent(range);
				_activeLines.Insert(i, newLine);
				break;
			}
		}

		// We didn't find any line for the location, add it to the end.
		if (newLine is null)
		{
			newLine = Rent(range);
			_activeLines.Add(newLine);
		}

		return newLine;
	}

	public VisualBytesLine GetVisualLineByLocation(BitLocation location)
	{
		for (var i = 0; i < _activeLines.Count; i++)
		{
			var line = _activeLines[i];
			if (line.VirtualRange.Contains(location))
			{
				return line;
			}

			if (line.Range.Start > location)
			{
				return null;
			}
		}

		return null;
	}

	public IEnumerable<VisualBytesLine> GetVisualLinesByRange(BitRange range)
	{
		for (var i = 0; i < _activeLines.Count; i++)
		{
			var line = _activeLines[i];
			if (line.VirtualRange.OverlapsWith(range))
			{
				yield return line;
			}

			if (line.Range.Start >= range.End)
			{
				yield break;
			}
		}
	}

	public void RemoveOutsideOfRange(BitRange range)
	{
		for (var i = 0; i < _activeLines.Count; i++)
		{
			var line = _activeLines[i];
			if (!range.Contains(line.VirtualRange.Start))
			{
				Return(line);
				_activeLines.RemoveAt(i--);
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private VisualBytesLine GetPooledLine()
	{
		while (_pool.TryPop(out var line))
		{
			if (line.Data.Length == _owner.ActualBytesPerLine)
			{
				return line;
			}
		}

		return new VisualBytesLine(_owner);
	}

	private VisualBytesLine Rent(BitRange virtualRange)
	{
		var line = GetPooledLine();

		line.VirtualRange = virtualRange;
		line.Range = _owner.Document is { ValidRanges.EnclosingRange: var enclosingRange }
			? virtualRange.Clamp(enclosingRange)
			: BitRange.Empty;

		line.Invalidate();

		return line;
	}

	private void Return(VisualBytesLine line)
	{
		_pool.Push(line);
	}

	#endregion
}