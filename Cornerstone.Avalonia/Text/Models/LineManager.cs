#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Cornerstone.Avalonia.Text.Rendering;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

public class LineManager : Notifiable, IEnumerable<Line>
{
	#region Fields

	private readonly List<Line> _lines;
	private readonly Queue<Line> _pool;

	#endregion

	#region Constructors

	internal LineManager(TextDocument document)
	{
		Document = document;
		_pool = new Queue<Line>(256);
		_lines = new List<Line>(1024);
	}

	#endregion

	#region Properties

	public int Count => _lines.Count;

	public Line this[int index] => _lines[index];

	internal TextDocument Document { get; }

	#endregion

	#region Methods

	public void Add(Line line)
	{
		_lines.Add(line);
	}

	public IEnumerator<Line> GetEnumerator()
	{
		return _lines.GetEnumerator();
	}

	public Line GetLineForOffset(int offset)
	{
		foreach (var line in _lines)
		{
			if (line.Contains(offset))
			{
				return line;
			}
		}

		return _lines.LastOrDefault();
	}

	public Line LastOrDefault()
	{
		return _lines.LastOrDefault();
	}

	public Size Measure(Size availableSize, bool wordWrap, TextMetrics textMetrics)
	{
		var offsetY = 0.0;
		var documentWidth = 0.0;
		double? maxWidth = wordWrap ? availableSize.Width : null;

		foreach (var line in _lines)
		{
			line.UpdateLineMetrics(offsetY, textMetrics, maxWidth);

			if (line.VisualLayout.Width > documentWidth)
			{
				documentWidth = line.VisualLayout.Width;
			}
			offsetY += line.VisualLayout.Height;
		}

		return new Size(documentWidth, offsetY);
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

	internal void Rebuild(int startOffset)
	{
		var index = startOffset;
		var existingLine = GetLineForOffset(startOffset);
		var documentWidth = 0;

		if (existingLine != null)
		{
			// Pool all lines that are past this line
			var keepCount = existingLine.LineNumber - 1;
			while (_lines.Count > keepCount)
			{
				var lineToPool = _lines[_lines.Count - 1];
				_lines.RemoveAt(_lines.Count - 1);
				_pool.Enqueue(lineToPool);
			}
		}

		foreach (var x in _lines)
		{
			if (x.Length > documentWidth)
			{
				documentWidth = x.Length;
			}
		}

		var line = StartNewLine(
			existingLine?.LineNumber ?? 1,
			Math.Min(index, existingLine?.StartOffset ?? int.MaxValue)
		);

		while ((line != null) && (index < Document.Buffer.Count))
		{
			switch (Document.Buffer[index++])
			{
				case '\r':
				{
					if ((index < Document.Buffer.Count)
						&& (Document.Buffer[index] == '\n'))
					{
						index++;
						line.LineEndingLength = 2;
					}
					else
					{
						line.LineEndingLength = 1;
					}
					break;
				}
				case '\n':
				{
					line.LineEndingLength = 1;
					break;
				}
				default:
				{
					continue;
				}
			}

			line.EndOffset = index;

			if (line.Length > documentWidth)
			{
				documentWidth = line.Length;
			}

			_lines.Add(line);

			line = (line.LineEndingLength > 0) || (index < Document.Buffer.Count)
				? StartNewLine(line.LineNumber + 1, line.EndOffset)
				: null;
		}

		if (line?.StartOffset <= Document.Buffer.Count)
		{
			line.EndOffset = Document.Buffer.Count;
			line.LineEndingLength = 0;

			_lines.Add(line);

			if (line.Length > documentWidth)
			{
				documentWidth = line.Length;
			}
		}

		// Add 1 character for width.
		Document.DocumentWidth = documentWidth + 1;
		NotifyOfPropertyChanged(nameof(Count));
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private Line StartNewLine(int lineNumber, int startOffset)
	{
		var line = _pool.TryDequeue(out var p) ? p : new Line(this);
		line.LineNumber = lineNumber;
		line.StartOffset = startOffset;
		line.EndOffset = startOffset;
		line.LineEndingLength = 0;
		return line;
	}

	#endregion
}