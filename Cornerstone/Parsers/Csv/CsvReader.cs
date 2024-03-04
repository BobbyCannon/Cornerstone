#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Parsers.Csv;

/// <summary>
/// Reader for CSV file format.
/// </summary>
/// <remarks>
/// https://csv-spec.org/
/// https://datatracker.ietf.org/doc/html/rfc4180#section-2
/// </remarks>
public class CsvReader
{
	#region Fields

	private char[] _buffer;
	private int _bufferLength;
	private int _bufferLoadThreshold;
	private int _currentBufferLength;
	private List<CsvParsedField> _fields;
	private string[] _headerColumns;
	private int _linesRead;
	private int _lineStartPosition;
	private readonly TextReader _reader;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the CSV parser.
	/// </summary>
	public CsvReader(TextReader reader, CsvOptions options)
	{
		_reader = reader;

		BufferSize = 32768;
		Options = options;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Size of the circular buffer. Buffer size limits max length of the CSV line that can be processed.
	/// </summary>
	/// <remarks> Default buffer size is 32kb. </remarks>
	public int BufferSize { get; set; }

	/// <summary>
	/// The count of fields in the current record.
	/// </summary>
	public int FieldsCount { get; private set; }

	/// <summary>
	/// Access a field by the index.
	/// </summary>
	/// <param name="idx"> The field index. </param>
	/// <returns> The field content or null if the index is out of range. </returns>
	public string this[int idx]
	{
		get
		{
			if ((idx >= 0) && (idx < FieldsCount))
			{
				var f = _fields[idx];
				return f.GetValue(_buffer);
			}
			return null;
		}
	}

	/// <summary>
	/// Access a field by the header name.
	/// </summary>
	/// <param name="header"> The header name. </param>
	/// <returns> The field contents. </returns>
	/// <exception cref="InvalidOperationException"> Headers has not been read. </exception>
	public string this[string header]
	{
		get
		{
			if (_headerColumns == null)
			{
				throw new InvalidOperationException("Headers has not been read.");
			}

			var indexOf = Array.IndexOf(_headerColumns, header);
			return indexOf == -1 ? null : this[indexOf];
		}
	}

	/// <summary>
	/// Options for the CSV parser.
	/// </summary>
	public CsvOptions Options { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Convert the read line as an array.
	/// </summary>
	/// <returns> The array in a readonly form. </returns>
	public ReadOnlySet<string> LineAsArray()
	{
		return new ReadOnlySet<string>(_fields.Select(x => x.GetValue(_buffer)));
	}

	/// <summary>
	/// Reads a record from the CSV parser.
	/// </summary>
	/// <returns> </returns>
	public bool Read()
	{
		if (_fields == null)
		{
			_fields = new List<CsvParsedField>();
			FieldsCount = 0;
		}

		if (_buffer == null)
		{
			_bufferLoadThreshold = Math.Min(BufferSize, 8192);
			_bufferLength = BufferSize + _bufferLoadThreshold;
			_buffer = new char[_bufferLength];
			_lineStartPosition = 0;
			_currentBufferLength = 0;
		}

		var eof = FillBuffer();

		FieldsCount = 0;

		if (_currentBufferLength <= 0)
		{
			return false; // no more data
		}

		_linesRead++;

		var maximumPosition = _lineStartPosition + _currentBufferLength;
		var characterPosition = _lineStartPosition;
		var currentField = GetOrAddField(characterPosition);
		var ignoreQuote = false;

		for (; characterPosition < maximumPosition; characterPosition++)
		{
			var bufferIndex = GetBufferIndex(characterPosition);
			var character = _buffer[bufferIndex];

			switch (character)
			{
				case '\"':
				{
					if (ignoreQuote)
					{
						currentField.End = characterPosition;
					}
					else if (!currentField.EmptyOrSpace && (currentField.Quoted || (currentField.Length > 0)))
					{
						// current field already is quoted = lets treat quotes as usual chars
						currentField.End = characterPosition;
						currentField.Quoted = false;
						ignoreQuote = true;
					}
					else
					{
						var endQuotePos = ReadQuotedFieldToEnd(characterPosition + 1, maximumPosition, eof, ref currentField.EscapedQuotesCount);
						currentField.Start = characterPosition;
						currentField.End = endQuotePos;
						currentField.Quoted = true;
						characterPosition = endQuotePos;

						if (currentField.EmptyOrSpace)
						{
							// skip all space(s)
							var index = GetBufferIndex(characterPosition);
							while (((index + 1) < maximumPosition) && (_buffer[index + 1] == ' '))
							{
								characterPosition++;
								index = GetBufferIndex(characterPosition);
							}
						}
					}
					break;
				}
				case '\r':
				{
					if (((characterPosition + 1) < maximumPosition) && (_buffer[(characterPosition + 1) % _bufferLength] == '\n'))
					{
						// \r\n handling
						characterPosition++;
					}
					// in some files only \r used as line separator - lets allow that
					characterPosition++;
					goto LineEnded;
				}
				case '\n':
				{
					characterPosition++;
					goto LineEnded;
				}
				default:
				{
					if (character == Options.Delimiter)
					{
						currentField = GetOrAddField(characterPosition + 1);
						ignoreQuote = false;
						continue;
					}

					// space
					if (character == ' ')
					{
						if (Options.TrimFields)
						{
							// do nothing
							continue;
						}
					}
					else
					{
						currentField.EmptyOrSpace = false;
					}

					// content char
					if (currentField.Length == 0)
					{
						currentField.Start = characterPosition;
					}

					if (currentField.Quoted)
					{
						// non-space content after quote = treat quotes as part of content
						currentField.Quoted = false;
						ignoreQuote = true;
					}
					currentField.End = characterPosition;
					break;
				}
			}
		}

		if (!eof)
		{
			// line is not finished, but whole buffer was processed and not EOF
			throw new InvalidDataException(GetLineTooLongMsg());
		}

		LineEnded:

		_currentBufferLength -= characterPosition - _lineStartPosition;
		_lineStartPosition = characterPosition % _bufferLength;

		if ((FieldsCount == 1) && (_fields[0].Length == 0))
		{
			// skip empty lines
			return Read();
		}

		if (Options.HasHeader && (_headerColumns == null))
		{
			ParseFieldsAsHeaders();
		}

		return true;
	}

	/// <summary>
	/// Read content from a CSV file using default options.
	/// </summary>
	/// <typeparam name="T"> The type to be returned. </typeparam>
	/// <param name="content"> The CSV content in string format. </param>
	/// <param name="mapper"> The mapper to map a record to the type. </param>
	/// <returns> A list of types from the CSV records. </returns>
	/// <exception cref="Exception"> Failed to parse line {line number}. </exception>
	public static IList<T> ReadContent<T>(string content, Func<T, CsvReader, bool> mapper) where T : new()
	{
		return ReadContent(content, new CsvOptions(), mapper);
	}

	/// <summary>
	/// Read content from a CSV file.
	/// </summary>
	/// <typeparam name="T"> The type to be returned. </typeparam>
	/// <param name="content"> The CSV content in string format. </param>
	/// <param name="options"> The options for parsing. </param>
	/// <param name="mapper"> The mapper to map a record to the type. </param>
	/// <returns> A list of types from the CSV records. </returns>
	/// <exception cref="Exception"> Failed to parse line {line number}. </exception>
	public static IList<T> ReadContent<T>(string content, CsvOptions options, Func<T, CsvReader, bool> mapper) where T : new()
	{
		using var reader = new StringReader(content);
		var parser = new CsvReader(reader, options);
		var response = new List<T>();
		var line = 0;

		if (options.HasHeader)
		{
			parser.Read();
		}

		while (parser.Read())
		{
			var t = new T();
			line++;

			if (!mapper(t, parser))
			{
				throw new Exception($"Failed to parse line {line}");
			}

			response.Add(t);
		}

		return response;
	}

	private bool FillBuffer()
	{
		var eof = false;
		var toRead = _bufferLength - _currentBufferLength;
		if (toRead >= _bufferLoadThreshold)
		{
			var freeStart = (_lineStartPosition + _currentBufferLength) % _buffer.Length;
			if (freeStart >= _lineStartPosition)
			{
				_currentBufferLength += ReadBlockAndCheckEof(_buffer, freeStart, _buffer.Length - freeStart, ref eof);
				if (_lineStartPosition > 0)
				{
					_currentBufferLength += ReadBlockAndCheckEof(_buffer, 0, _lineStartPosition, ref eof);
				}
			}
			else
			{
				_currentBufferLength += ReadBlockAndCheckEof(_buffer, freeStart, toRead, ref eof);
			}
		}
		return eof;
	}

	private int GetBufferIndex(int characterPosition)
	{
		return characterPosition < _bufferLength ? characterPosition : characterPosition % _bufferLength;
	}

	private string GetLineTooLongMsg()
	{
		return $"CSV line #{_linesRead} length exceeds buffer size ({BufferSize})";
	}

	private CsvParsedField GetOrAddField(int startIdx)
	{
		FieldsCount++;
		while (FieldsCount > _fields.Count)
		{
			_fields.Add(new CsvParsedField());
		}
		var f = _fields[FieldsCount - 1];
		f.Reset(startIdx);
		return f;
	}

	private void ParseFieldsAsHeaders()
	{
		_headerColumns = _fields.Select(x => x.GetValue(_buffer)).ToArray();
	}

	private int ReadBlockAndCheckEof(char[] buffer, int start, int len, ref bool eof)
	{
		if (len == 0)
		{
			return 0;
		}
		var read = _reader.ReadBlock(buffer, start, len);
		if (read < len)
		{
			eof = true;
		}
		return read;
	}

	private int ReadQuotedFieldToEnd(int start, int maxPosition, bool endOfFile, ref int escapedQuotesCount)
	{
		int pos;

		for (pos = start; pos < maxPosition; pos++)
		{
			var characterIndex = pos < _bufferLength ? pos : pos % _bufferLength;
			var character = _buffer[characterIndex];

			if (character == '\"')
			{
				var hasNextCh = (pos + 1) < maxPosition;
				if (hasNextCh && (_buffer[(pos + 1) % _bufferLength] == '\"'))
				{
					// double quote inside quote = just a content
					pos++;
					escapedQuotesCount++;
				}
				else
				{
					return pos;
				}
			}
		}

		if (endOfFile)
		{
			// this is incorrect CSV as quote is not closed
			// but in case of EOF lets ignore that
			return pos - 1;
		}

		throw new InvalidDataException(GetLineTooLongMsg());
	}

	#endregion
}