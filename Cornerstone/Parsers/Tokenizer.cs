#region References

using System.IO;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers;

/// <summary>
/// Represents a tokenizer for a file.
/// </summary>
/// <typeparam name="T"> The data for a token. </typeparam>
/// <typeparam name="T2"> The type for the token. </typeparam>
public abstract class Tokenizer<T, T2>
	where T : ITokenData<T2>
{
	#region Constants

	private const int _bufferSize = 1024;

	#endregion

	#region Fields

	internal T CurrentToken;
	internal T NextToken;
	private readonly char[] _buffer;
	private readonly TextReader _textReader;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the tokenizer.
	/// </summary>
	/// <param name="reader"> The reader for text. </param>
	protected Tokenizer(TextReader reader)
	{
		_buffer = new char[_bufferSize];
		BufferEnd = _bufferSize + 1;
		TemporaryBuffer = new TextBuilder();
		_textReader = reader;

		var read = _textReader.ReadBlock(_buffer, 0, _bufferSize);
		if (read < _bufferSize)
		{
			BufferEnd = read;
		}
	}

	#endregion

	#region Properties

	/// <summary>
	/// The end of the buffer.
	/// </summary>
	protected int BufferEnd { get; private set; }

	/// <summary>
	/// The index of the column for the current line.
	/// </summary>
	protected int ColumnIndex { get; private set; }

	/// <summary>
	/// The current character index.
	/// </summary>
	protected int CurrentIndex { get; private set; }

	/// <summary>
	/// The current number of a line.
	/// </summary>
	protected int LineNumber { get; private set; } = 1;

	/// <summary>
	/// Represents a temporary buffer to use during tokenizing.
	/// </summary>
	protected TextBuilder TemporaryBuffer { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Move to the next token.
	/// </summary>
	public abstract void MoveNext();

	/// <summary>
	/// Expect the next characters should match the provided expected values.
	/// </summary>
	/// <param name="expected"> The expected data. </param>
	protected void Expect(string expected)
	{
		for (var i = 1; i < expected.Length; i++)
		{
			var c = NextChar();

			if ((c == 0) && (CurrentIndex >= BufferEnd))
			{
				throw new ParserException($"Unexpected end of stream reached while '{expected}' was expected in line {LineNumber} at position {ColumnIndex}.", LineNumber, ColumnIndex);
			}

			if (c != expected[i])
			{
				throw new ParserException($"Expected '{expected}' at position {ColumnIndex} in line {LineNumber}, but found '{expected.Substring(0, i) + c}'.", LineNumber, ColumnIndex);
			}
		}
	}

	/// <summary>
	/// Checks to see if the character is whitespace.
	/// </summary>
	protected bool IsWhitespace(char c)
	{
		return c is ' ' or '\t' or '\r' or '\n';
	}

	/// <summary>
	/// Gets the next character in the buffer.
	/// </summary>
	/// <returns> The next character. </returns>
	protected char NextChar()
	{
		if (CurrentIndex >= BufferEnd)
		{
			return (char) 0;
		}

		var c = _buffer[CurrentIndex++];

		ColumnIndex++;

		if (CurrentIndex == _bufferSize)
		{
			ReadNextChunk();
		}

		if (c == '\n')
		{
			LineNumber++;
			ColumnIndex = 0;
		}

		return c;
	}

	/// <summary>
	/// Parse a number from the character.
	/// </summary>
	/// <param name="character"> The character to process. </param>
	/// <param name="multiplier"> The multiplier to shift the value. </param>
	/// <returns> The number. </returns>
	protected uint ParseSingleNumber(char character, uint multiplier)
	{
		uint response = character switch
		{
			>= '0' and <= '9' => (uint) (character - '0') * multiplier,
			>= 'A' and <= 'F' => (uint) ((character - 'A') + 10) * multiplier,
			>= 'a' and <= 'f' => (uint) ((character - 'a') + 10) * multiplier,
			_ => 0
		};
		return response;
	}

	/// <summary>
	/// Parse a string out of the buffer.
	/// </summary>
	/// <param name="quote"> The quote character. </param>
	/// <returns> The string parsed. </returns>
	protected abstract string ParseString(char quote);

	/// <summary>
	/// Peek the next char.
	/// </summary>
	/// <returns> The character if found otherwise null character (0). </returns>
	protected char PeekChar()
	{
		if (CurrentIndex >= BufferEnd)
		{
			return (char) 0;
		}

		return _buffer[CurrentIndex];
	}

	/// <summary>
	/// Refill the buffer with more data.
	/// </summary>
	protected void ReadNextChunk()
	{
		CurrentIndex = 0;
		var read = _textReader.ReadBlock(_buffer, 0, _bufferSize);
		if (read < _bufferSize)
		{
			BufferEnd = read;
		}
	}

	#endregion
}