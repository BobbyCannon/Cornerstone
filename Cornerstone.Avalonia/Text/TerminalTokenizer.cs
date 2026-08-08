#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Avalonia.Media;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Text;

public class TerminalTokenizer : Tokenizer
{
	#region Fields

	public static readonly Regex ControlSequenceRegex;
	private static readonly SpeedyQueue<Token> _sharedTokenPool;

	#endregion

	#region Constructors

	public TerminalTokenizer()
		: base(new StringBuffer(), _sharedTokenPool)
	{
	}

	static TerminalTokenizer()
	{
		_sharedTokenPool = new SpeedyQueue<Token>();

		ControlSequenceRegex = new Regex(
			@"(\x1b\[[0-9;]*m)|([^\x1b]+)",
			RegexOptions.Compiled | RegexOptions.CultureInvariant
		);
	}

	#endregion

	#region Methods

	internal void ProcessAnsiText(Terminal terminal, string text)
	{
		var matches = ControlSequenceRegex.Matches(text);
		var lastPos = 0;
		bool? bold = null;
		bool? italic = null;
		bool? strikethrough = null;
		Color? foregroundColor = null;
		Color? backgroundColor = null;

		foreach (Match m in matches)
		{
			if (m.Groups[1].Success)
			{
				var seq = m.Groups[1].ValueSpan;
				var paramsSpan = seq[2..^1];

				if (paramsSpan.IsEmpty)
				{
					foregroundColor = null;
					backgroundColor = null;
					bold = false;
					italic = false;
					strikethrough = false;
				}
				else
				{
					ProcessAnsiTextPattern(paramsSpan.ToString(), ref foregroundColor, ref backgroundColor, ref bold, ref italic, ref strikethrough);
				}
			}
			else
			{
				terminal.AppendTextWithColor(m.Value, foregroundColor, backgroundColor, bold, italic, strikethrough);
			}

			lastPos = m.Index + m.Length;
		}

		// Trailing text
		if (lastPos < text.Length)
		{
			terminal.AppendTextWithColor(text.Substring(lastPos), foregroundColor, backgroundColor, bold, italic, strikethrough);
		}
	}

	/// <summary>
	/// Converts ANSI 256 color index (0-255) to Avalonia Color.
	/// </summary>
	private Color Get256Color(int index)
	{
		if (index is < 0 or > 255)
		{
			return Colors.Transparent;
		}

		// 0-15: Standard colors (reuse existing map when possible)
		if (index < 16)
		{
			if (ColorExtensions.ControlColorMap.TryGetValue(index.ToString(), out var color))
			{
				return color;
			}

			// Fallback standard 16 colors
			return index switch
			{
				0 => Color.FromRgb(0, 0, 0),
				1 => Color.FromRgb(128, 0, 0),
				2 => Color.FromRgb(0, 128, 0),
				3 => Color.FromRgb(128, 128, 0),
				4 => Color.FromRgb(0, 0, 128),
				5 => Color.FromRgb(128, 0, 128),
				6 => Color.FromRgb(0, 128, 128),
				7 => Color.FromRgb(192, 192, 192),
				8 => Color.FromRgb(128, 128, 128),
				9 => Color.FromRgb(255, 0, 0),
				10 => Color.FromRgb(0, 255, 0),
				11 => Color.FromRgb(255, 255, 0),
				12 => Color.FromRgb(0, 0, 255),
				13 => Color.FromRgb(255, 0, 255),
				14 => Color.FromRgb(0, 255, 255),
				_ => Color.FromRgb(255, 255, 255)
			};
		}

		// 16-231: 6×6×6 color cube
		if (index < 232)
		{
			var offset = index - 16;
			var r = (offset / 36) * 51;
			var g = ((offset % 36) / 6) * 51;
			var b = (offset % 6) * 51;
			return Color.FromRgb((byte) r, (byte) g, (byte) b);
		}

		// 232-255: Grayscale ramp
		var gray = 8 + ((index - 232) * 10);
		return Color.FromRgb((byte) gray, (byte) gray, (byte) gray);
	}

	private void ProcessAnsiTextPattern(string parameters, ref Color? foregroundColor, ref Color? backgroundColor, ref bool? bold, ref bool? italic, ref bool? strikethrough)
	{
		if (string.IsNullOrWhiteSpace(parameters))
		{
			ResetAll(ref foregroundColor, ref backgroundColor, ref bold, ref italic, ref strikethrough);
			return;
		}

		var parts = parameters.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var i = 0;

		while (i < parts.Length)
		{
			if (!int.TryParse(parts[i], out var code))
			{
				i++;
				continue;
			}

			switch (code)
			{
				case 0: // Reset all attributes
				{
					ResetAll(ref foregroundColor, ref backgroundColor, ref bold, ref italic, ref strikethrough);
					break;
				}
				case 1: // Bold / bright
				{
					bold = true;
					break;
				}
				case 22: // Normal intensity (not bold)
				{
					bold = false;
					break;
				}
				case 3: // Italic
				{
					italic = true;
					break;
				}
				case 23: // Not italic
				{
					italic = false;
					break;
				}
				case 9: // Strikethrough
				{
					strikethrough = true;
					break;
				}
				case 29: // Not strikethrough
				{
					strikethrough = false;
					break;
				}
				case 39: // Default foreground color
				{
					foregroundColor = null;
					break;
				}
				case 49: // Default background color
				{
					backgroundColor = null;
					break;
				}
				case 38:
				{
					// 256-color foreground: 38;5;n
					if (((i + 2) < parts.Length) && (parts[i + 1] == "5") && int.TryParse(parts[i + 2], out var fgIndex))
					{
						foregroundColor = Get256Color(fgIndex);
						i += 2; // Skip the next two parameters
					}
					break;
				}
				case 48:
				{
					// 256-color background: 48;5;n
					if (((i + 2) < parts.Length) && (parts[i + 1] == "5") && int.TryParse(parts[i + 2], out var bgIndex))
					{
						backgroundColor = Get256Color(bgIndex);
						i += 2; // Skip the next two parameters
					}
					break;
				}
				default:
				{
					// Basic 16 colors + bright colors (keep your existing map for compatibility)
					if (ColorExtensions.ControlColorMap.TryGetValue(code.ToString(), out var color))
					{
						if (code is >= 30 and <= 37 or >= 90 and <= 97)
						{
							foregroundColor = color;
						}
						else if (code is >= 40 and <= 47 or >= 100 and <= 107)
						{
							backgroundColor = color;
						}
					}
					break;
				}
			}

			i++;
		}
		return;
	}

	[SuppressMessage("ReSharper", "RedundantAssignment")]
	private static void ResetAll(ref Color? foregroundColor, ref Color? backgroundColor, ref bool? bold, ref bool? italic, ref bool? strikethrough)
	{
		foregroundColor = null;
		backgroundColor = null;
		bold = false;
		italic = false;
		strikethrough = false;
	}

	#endregion
}