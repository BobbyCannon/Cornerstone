#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Text.Buffers;

#endregion

namespace Cornerstone.Text;

public class TextDocument : ITextRange
{
	#region Constants

	/// <summary>
	/// Represents an invalid character. This character is returned when attempting to read a character at an invalid index.
	/// </summary>
	public const char NullChar = '\0';

	#endregion

	#region Fields

	private readonly StringGapBuffer _buffer;
	private int _readIndex;

	#endregion

	#region Constructors

	protected TextDocument(TextDocumentSettings settings = null)
	{
		_buffer = new StringGapBuffer();

		Lines = new SpeedyList<TextRange>();
		Settings = settings ?? new TextDocumentSettings();

		Reset();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The current column number of the <see cref="ReadIndex" />.
	/// </summary>
	public int ColumnNumber { get; private set; }

	/// <inheritdoc />
	public int EndIndex => _buffer.Count;

	/// <summary>
	/// Returns true if the current position is at the end of the text being parsed.
	/// Otherwise, false.
	/// </summary>
	public bool EndOfText => ReadIndex >= _buffer.Count;

	/// <summary>
	/// Gets the character at the specified index. Returns <see cref="NullChar" /> if <paramref name="index" />
	/// is not valid.
	/// </summary>
	/// <param name="index"> 0-based position of the character to return. </param>
	public char this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (index >= 0) && (index < _buffer.Count) ? _buffer[index] : NullChar;
	}

	/// <inheritdoc />
	public int Length => _buffer.Count;

	/// <summary>
	/// The current line number of the <see cref="ReadIndex" />.
	/// </summary>
	public int LineNumber { get; private set; }

	/// <summary>
	/// The lines of the document. Each line is separated by the NewLine (\n) character.
	/// </summary>
	public SpeedyList<TextRange> Lines { get; }

	/// <summary>
	/// The current character index.
	/// </summary>
	public int ReadIndex
	{
		get => _readIndex;
		private set =>
			_readIndex = value < 0
				? 0
				: value > _buffer.Count
					? _buffer.Count
					: value;
	}

	/// <inheritdoc />
	public int Remaining => _buffer.Count - ReadIndex;

	/// <summary>
	/// The text document options.
	/// </summary>
	public TextDocumentSettings Settings { get; }

	/// <inheritdoc />
	public int StartIndex => 0;

	#endregion

	#region Methods

	public void Add(IStringBuffer content)
	{
		_buffer.Add(content);
	}

	public void Add(string content)
	{
		_buffer.Add(content);
	}

	/// <summary>
	/// Find the first character in the expected list.
	/// </summary>
	/// <param name="startIndex"> The inclusive start index to start at. </param>
	/// <param name="endIndex"> The exclusive end index to start at. </param>
	/// <param name="expected"> The characters to look for. </param>
	/// <param name="index"> The index where any character was found. </param>
	/// <returns> True if a character was found otherwise false. </returns>
	public bool FindAnyCharacter(int startIndex, int endIndex, char[] expected, out int index)
	{
		var i = Math.Max(startIndex, StartIndex);
		var until = Math.Min(endIndex, EndIndex);

		while (i < until)
		{
			if (expected.Any(e => e == this[i]))
			{
				index = i;
				return true;
			}

			i++;
		}

		index = -1;
		return false;
	}

	/// <summary>
	/// Find the first character not in the except list.
	/// </summary>
	/// <param name="startIndex"> The inclusive start index to start at. </param>
	/// <param name="endIndex"> The exclusive end index to stop at. </param>
	/// <param name="except"> The characters to skip. </param>
	/// <param name="index"> The index where the character was found. </param>
	/// <returns> True if a character was found otherwise false. </returns>
	public bool FindAnyCharacterExcept(int startIndex, int endIndex, char[] except, out int index)
	{
		var i = Math.Max(startIndex, StartIndex);
		var until = Math.Min(endIndex, EndIndex);

		while (i < until)
		{
			if (except.Any(e => e == this[i]))
			{
				i++;
				continue;
			}

			index = i;
			return true;
		}

		index = -1;
		return false;
	}

	/// <summary>
	/// Find the first character in the expected list but in reverse order.
	/// </summary>
	/// <param name="startIndex"> The inclusive start index to stop at. </param>
	/// <param name="endIndex"> The exclusive end index to start at. </param>
	/// <param name="except"> The characters to skip. </param>
	/// <param name="index"> The index where any character was found. </param>
	/// <returns> True if a character was found otherwise false. </returns>
	public bool FindAnyCharacterExceptInReverse(int startIndex, int endIndex, char[] except, out int index)
	{
		var i = Math.Min(endIndex, EndIndex);
		var until = Math.Max(startIndex, StartIndex);

		while (i >= until)
		{
			if (except.Any(e => e == this[i]))
			{
				i--;
				continue;
			}

			index = i;
			return true;
		}

		index = -1;
		return false;
	}

	/// <summary>
	/// Find the first character in the expected list but in reverse order.
	/// </summary>
	/// <param name="startIndex"> The inclusive start index to stop at. </param>
	/// <param name="endIndex"> The inclusive end index to start at. </param>
	/// <param name="expected"> The characters to look for. </param>
	/// <param name="index"> The index where any character was found. </param>
	/// <returns> True if a character was found otherwise false. </returns>
	public bool FindAnyCharacterInReverse(int startIndex, int endIndex, char[] expected, out int index)
	{
		var i = Math.Min(endIndex, EndIndex);
		var until = Math.Max(startIndex, StartIndex);

		while (i >= until)
		{
			if (expected.Any(e => e == this[i]))
			{
				index = i;
				return true;
			}

			i--;
		}

		index = -1;
		return false;
	}

	/// <summary>
	/// Match characters provided in the order requested.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="expected"> The characters to look for. </param>
	/// <param name="ignore"> The optional character to ignore (skip). </param>
	/// <param name="until"> The characters that will stop processing if found. </param>
	/// <returns> The array of indexes of the expected characters. </returns>
	/// <remarks>
	/// .                      01234546789
	/// Ex. #{} would match on ### { red } Header
	/// - result [0,4,9]
	/// </remarks>
	public int[] FindCharactersIndexes(int index, char[] expected, char[] ignore = null, char[] until = null)
	{
		var response = new int[expected.Length];
		var i = index;
		var expectedIndex = 0;

		ignore ??= [];
		until ??= [NullChar];

		while (i < EndIndex)
		{
			if (ignore.Any(e => e == this[i]))
			{
				i++;
				continue;
			}

			if (this[i] == expected[expectedIndex])
			{
				// Capture first index
				response[expectedIndex] = i;

				// Skip duplicate characters
				while ((i < EndIndex) && (this[i] == expected[expectedIndex]))
				{
					i++;
				}

				expectedIndex++;
			}

			if (expectedIndex == response.Length)
			{
				return response;
			}

			if (until.Any(e => e == this[i]))
			{
				return response;
			}

			i++;
		}

		return response.SubArray(0, expectedIndex);
	}

	/// <summary>
	/// Match characters provided in the order requested.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="expected"> The characters to look for. </param>
	/// <param name="except"> The characters that will stop processing if found. </param>
	/// <returns> The array of indexes of the expected characters. </returns>
	/// <remarks>
	/// Ex. []() would match on [Test](http://test.com)
	/// </remarks>
	public int[] FindCharactersPattern(int index, char[] expected, char[] except = null)
	{
		var response = new int[expected.Length];
		var i = index;
		var foundIndex = 0;

		except ??= [NullChar];

		while (i < EndIndex)
		{
			if (this[i] == expected[foundIndex])
			{
				response[foundIndex++] = i;
			}

			if (foundIndex == expected.Length)
			{
				return response;
			}

			if (except.Any(e => e == this[i]))
			{
				return [];
			}

			i++;
		}

		return response;
	}

	public static TextDocument Load(string content, TextDocumentSettings settings = null)
	{
		var response = new TextDocument(settings);
		response.Add(content);
		response.Rebuild();
		return response;
	}

	public static TextDocument Load(IStringBuffer content, TextDocumentSettings settings = null)
	{
		var response = new TextDocument(settings);
		response.Add(content);
		response.Rebuild();
		return response;
	}

	public bool Match(string value, StringComparison comparison = StringComparison.CurrentCulture)
	{
		return _buffer.Match(ReadIndex, value, comparison);
	}

	public bool Match(int index, string value, StringComparison comparison = StringComparison.CurrentCulture)
	{
		return _buffer.Match(index, value, comparison);
	}

	public bool MatchAny(char[] values, out int length, StringComparison comparison = StringComparison.CurrentCulture)
	{
		return _buffer.MatchAny(ReadIndex, values, comparison, out length);
	}

	/// <summary>
	/// Count to see how many characters match the provided value.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="expected"> The character to look for. </param>
	/// <param name="maxValue"> The maximum value </param>
	/// <returns> </returns>
	public int MatchCharacter(int index, char expected, int maxValue = int.MaxValue)
	{
		var response = 0;

		while (((index + response) < EndIndex)
				&& (response < maxValue)
				&& (this[index + response] == expected))
		{
			response++;
		}

		return response;
	}

	/// <summary>
	/// Match string provided in the order requested.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="expected"> The strings to look for. </param>
	/// <param name="except"> The characters that will stop processing if found. </param>
	/// <returns> The array of indexes of the expected strings. </returns>
	public int[] MatchStrings(int index, string[] expected, char[] except = null)
	{
		var response = new int[expected.Length];
		var i = index;
		var foundIndex = 0;

		except ??= [NullChar];

		while (i < EndIndex)
		{
			if (Match(i, expected[foundIndex]))
			{
				response[foundIndex++] = i;
				i += expected.Length;
			}

			if (foundIndex == expected.Length)
			{
				return response;
			}

			if (except.Any(e => e == this[i]))
			{
				return [];
			}

			i++;
		}

		return [];
	}

	/// <summary>
	/// Moves the current index ahead the specified number of characters.
	/// </summary>
	/// <param name="count">
	/// The number of characters to move ahead. Use negative numbers
	/// to move backwards.
	/// </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Move(int count)
	{
		ReadIndex = _readIndex + count;
	}

	/// <summary>
	/// Moves the current index to a specific index.
	/// </summary>
	/// <param name="index">
	/// The specific index to move to.
	/// </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveTo(int index)
	{
		ReadIndex = index;
	}

	/// <summary>
	/// Move the current index ahead one character.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextDocument operator --(TextDocument document)
	{
		document.Move(-1);
		return document;
	}

	/// <summary>
	/// Move the current index ahead one character.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextDocument operator ++(TextDocument document)
	{
		document.Move(1);
		return document;
	}

	public void Remove(int index, int length)
	{
		_buffer.Remove(index, length);

		//if (Ranges.TryFind(index, out var value))
		//{
		//	value -= length;

		//	if (value.Length <= 0)
		//	{
		//		Ranges.Remove(value);

		//		// Rebuild the connection
		//		if (value.Previous != null)
		//		{
		//			value.Previous.Next = value.Next;
		//		}
		//	}
		//}

		Rebuild();
	}

	/// <summary>
	/// Reset the reader
	/// </summary>
	public void Reset()
	{
		ColumnNumber = 1;
		LineNumber = 1;
		ReadIndex = 0;
	}

	public string SubString(int index, int length)
	{
		return _buffer.SubString(index, length);
	}

	public string SubStringUsingAbsoluteIndexes(int startIndex, int endIndex, bool inclusive)
	{
		var start = inclusive ? startIndex : startIndex + 1;
		var length = inclusive ? (endIndex - startIndex) + 1 : endIndex - startIndex - 1;
		if (length <= 0)
		{
			return string.Empty;
		}

		return SubString(start, length) ?? string.Empty;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return _buffer.ToString();
	}

	/// <summary>
	/// Gets the next character in the buffer.
	/// </summary>
	/// <returns> The next character. </returns>
	protected char NextChar()
	{
		if (ReadIndex >= _buffer.Count)
		{
			return (char) 0;
		}

		var c = this[ReadIndex++];

		ColumnNumber++;

		if (c == '\n')
		{
			LineNumber++;
			ColumnNumber = 1;
		}

		return c;
	}

	/// <summary>
	/// Peek the next char.
	/// </summary>
	/// <returns> The character if found otherwise null character (0). </returns>
	protected char PeekChar()
	{
		return this[ReadIndex];
	}

	protected virtual void Rebuild()
	{
		// todo: rebuild ranges completely
		//Ranges.Load(ReadRanges());
		Lines.Load(ReadLines());
	}

	private IEnumerable<TextRange> ReadLines()
	{
		Reset();

		TextRange previous = null;

		while (!EndOfText)
		{
			var index = _buffer.IndexOf('\n', ReadIndex);
			var length = index == -1
				? Length - ReadIndex
				: (previous == null ? index : index - ReadIndex) + 1;
			var next = new TextRange(this, previous, length);
			previous = next;

			ReadIndex = next.EndIndex;
			LineNumber++;

			yield return previous;

			if (EndOfText)
			{
				break;
			}
		}
	}

	#endregion
}