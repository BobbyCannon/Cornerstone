#region References

using System;
using System.Buffers;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// Provides high-performance utilities for processing text in an IStringBuffer (including gap buffers).
/// </summary>
[SourceReflection]
public class TextService
{
	#region Fields

	public readonly IStringBuffer Buffer;
	public static readonly SearchValues<char> Indentation;
	public static readonly SearchValues<char> Newlines;
	public static readonly SearchValues<char> Whitespace;

	#endregion

	#region Constructors

	public TextService(IStringBuffer buffer)
	{
		Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
	}

	static TextService()
	{
		Indentation = SearchValues.Create(" \t");
		Newlines = SearchValues.Create("\r\n");
		Whitespace = SearchValues.Create(" \t\r\n\f\v");
	}

	#endregion

	#region Properties

	public int Position { get; protected set; }

	#endregion

	#region Methods

	/// <summary>
	/// Returns the position after skipping all indentation starting from the given position.
	/// </summary>
	public int CalculatePastIndentation(int position)
	{
		var count = Buffer.Count;
		while ((position < count) && Indentation.Contains(Buffer[position]))
		{
			position++;
		}
		return position;
	}

	/// <summary>
	/// Returns the position after skipping all whitespace starting from the given position.
	/// </summary>
	public int CalculatePastWhitespace(int position)
	{
		var count = Buffer.Count;
		while ((position < count) && Whitespace.Contains(Buffer[position]))
		{
			position++;
		}
		return position;
	}

	/// <summary>
	/// Returns the position of the next newline (\r or \n), or the end of the buffer.
	/// Does NOT consume anything.
	/// </summary>
	public int CalculateUntilEndOfLine(int position)
	{
		var count = Buffer.Count;
		while (position < count)
		{
			if (Newlines.Contains(Buffer[position]))
			{
				return position;
			}

			position++;
		}
		return position;
	}
	
	/// <summary>
	/// Returns the position of the next value after the newline (\r or \n), or the end of the buffer.
	/// Does NOT consume anything.
	/// </summary>
	public int CalculatePastEndOfLine(int position)
	{
		var count = Buffer.Count;
		while (position < count)
		{
			if (!Newlines.Contains(Buffer[position]))
			{
				return position;
			}

			position++;
		}
		return position;
	}

	/// <summary>
	/// Returns the first position where the character is NOT equal to the given value.
	/// </summary>
	public int CalculateUntilNot(int position, char value)
	{
		var count = Buffer.Count;
		while ((position < count) && (Buffer[position] == value))
		{
			position++;
		}
		return position;
	}

	public int ConsumeCharacters(char value)
	{
		Position = CalculateUntilNot(Position, value);
		return Position;
	}

	public int ConsumeNewLines()
	{
		var count = Buffer.Count;
		while ((Position < count) && Newlines.Contains(Buffer[Position]))
		{
			Position++;
		}
		return Position;
	}

	/// <summary>
	/// Consumes all characters until (but not including) the next newline.
	/// Stops at \r or \n. Returns the final Position.
	/// </summary>
	public int ConsumeRestOfLine()
	{
		var count = Buffer.Count;
		while (Position < count)
		{
			if (Newlines.Contains(Buffer[Position]))
			{
				return Position;
			}

			Position++;
		}
		return Position;
	}

	public int ConsumeWhitespace()
	{
		var count = Buffer.Count;
		while ((Position < count) && Whitespace.Contains(Buffer[Position]))
		{
			Position++;
		}
		return Position;
	}

	public bool TryMatch(int start, string expected)
	{
		return ((start + expected.Length) <= Buffer.Count)
			&& Buffer.Equals(start, expected);
	}

	public bool TryPeekNext(char expected)
	{
		return ((Position + 1) < Buffer.Count) && (Buffer[Position + 1] == expected);
	}

	/// <summary>
	/// Helper method to detect a delimited token (startPattern + content + endPattern).
	/// Does NOT consume the token — only calculates the end offset.
	/// </summary>
	protected bool TryProcessDelimitedToken(
		string startPattern,
		string endPattern,
		int tokenType,
		out int endOffset)
	{
		endOffset = Position;

		if (!TryMatch(Position, startPattern))
		{
			return false;
		}

		var position = Position + startPattern.Length;
		var count = Buffer.Count;

		while (position < count)
		{
			if (TryMatch(position, endPattern))
			{
				position += endPattern.Length;
				endOffset = position;
				return true;
			}

			position++;
		}

		return false;
	}

	#endregion
}