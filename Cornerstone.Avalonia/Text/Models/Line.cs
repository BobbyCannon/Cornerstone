#region References

using System;
using Avalonia;
using Cornerstone.Data;
using TextMetrics = Cornerstone.Avalonia.Text.Rendering.TextMetrics;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

public partial class Line : Notifiable
{
	#region Fields

	private readonly LineManager _lineManager;

	#endregion

	#region Constructors

	public Line(LineManager lineManager)
	{
		_lineManager = lineManager;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The exclusive offset (end) of the selection.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int EndOffset { get; set; }

	/// <summary>
	/// The length of the selection.
	/// </summary>
	public int Length => EndOffset - StartOffset;

	/// <summary>
	/// The amount of line ending characters.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int LineEndingLength { get; set; }

	/// <summary>
	/// The number of the line.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int LineNumber { get; set; }

	/// <summary>
	/// The inclusive offset (start) of the selection.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int StartOffset { get; set; }

	/// <summary>
	/// The inclusive offset (start) of the selection.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Rect VisualLayout { get; private set; }

	#endregion

	#region Methods

	public bool Contains(int offset)
	{
		return (offset >= StartOffset)
			&& (offset < EndOffset);
	}

	public override string ToString()
	{
		// todo: add more checks
		return Length > 0
			? _lineManager.Document.Buffer.SubString(StartOffset, Length)
			: string.Empty;
	}

	internal void UpdateLineMetrics(double offsetY, TextMetrics textMetrics, double? visualMaxWidth)
	{
		var maxWidth = 0.0;
		var currentWidth = 0.0;
		var lines = 1;
		var isFirst = true;
		var lastBreakIndex = -1;
		var lastBreakWidth = 0.0;
		var spans = _lineManager.Document.Buffer.GetTwoSpans(StartOffset, Length);

		if (!spans.BeforeGap.IsEmpty)
		{
			ProcessSpan(spans.BeforeGap);
		}
		if (!spans.AfterGap.IsEmpty)
		{
			ProcessSpan(spans.AfterGap);
		}

		if (maxWidth == 0)
		{
			maxWidth = currentWidth;
		}

		VisualLayout = new Rect(0, offsetY, maxWidth, lines * textMetrics.CharacterHeight);
		return;

		void ProcessSpan(ReadOnlySpan<char> span)
		{
			for (var i = 0; i < span.Length; i++)
			{
				var c = span[i];
				if ((currentWidth == 0) && !isFirst)
				{
					lastBreakIndex = -1;
					lastBreakWidth = 0;
					lines++;
				}

				isFirst = false;

				var advance = c switch
				{
					'\r' => 0,
					'\n' => 0,
					'\t' => textMetrics.CharacterWidth * 4,
					_ when c <= 0x7F => textMetrics.CharacterWidth, // ASCII
					_ when c <= 0xFFFF => textMetrics.CharacterWidth, // BMP (most non-ASCII letters)
					_ => textMetrics.CharacterWidth * 2
				};

				if (c is ' ' or '\t')
				{
					lastBreakIndex = i;
					lastBreakWidth = currentWidth + advance;
				}

				currentWidth += advance;

				if (c == '\r')
				{
					if (((i + 1) < span.Length) && (span[i + 1] == '\n'))
					{
						i++;
					}

					maxWidth = Math.Max(maxWidth, currentWidth - advance);
					currentWidth = 0;
					continue;
				}

				if (c == '\n')
				{
					maxWidth = Math.Max(maxWidth, currentWidth - advance);
					currentWidth = 0;
					continue;
				}

				if (!visualMaxWidth.HasValue
					|| !((currentWidth + advance) > visualMaxWidth.Value))
				{
					// Line does not need to break so continue
					continue;
				}

				if (lastBreakIndex >= 0)
				{
					maxWidth = Math.Max(maxWidth, lastBreakWidth);
					currentWidth -= lastBreakWidth;
					lastBreakIndex = -1;
					lastBreakWidth = 0;

					if (currentWidth > 0)
					{
						// Only add new line if we have remaining wrapping lines
						lines++;
					}
				}
				else
				{
					maxWidth = Math.Max(maxWidth, currentWidth);
					currentWidth = 0;
				}
			}
		}
	}

	#endregion
}