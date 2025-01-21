#region References

using System;
using System.Collections.Generic;
using System.IO;
using Cornerstone.Collections;
using Cornerstone.Text.Buffers;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Implements the ITextSource interface using a buffer.
/// </summary>
public sealed class StringGapBufferTextSource : ITextSource
{
	#region Fields

	private readonly StringGapBuffer _buffer;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new RopeTextSource.
	/// </summary>
	public StringGapBufferTextSource(IEnumerable<char> buffer)
		: this(new StringGapBuffer(buffer))
	{
	}

	/// <summary>
	/// Creates a new RopeTextSource.
	/// </summary>
	public StringGapBufferTextSource(StringGapBuffer buffer)
	{
		_buffer = buffer?.ShallowClone() ?? throw new ArgumentNullException(nameof(buffer));
	}

	/// <summary>
	/// Creates a new RopeTextSource.
	/// </summary>
	public StringGapBufferTextSource(StringGapBuffer buffer, ITextSourceVersion version)
	{
		_buffer = buffer?.ShallowClone() ?? throw new ArgumentNullException(nameof(buffer));
		Version = version;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public string Text => _buffer.ToString();

	/// <inheritdoc />
	public int TextLength => _buffer.Count;

	/// <inheritdoc />
	public ITextSourceVersion Version { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public TextReader CreateReader()
	{
		return new StringBufferTextReader(_buffer);
	}

	/// <inheritdoc />
	public TextReader CreateReader(int offset, int length)
	{
		return new StringBufferTextReader(_buffer.SubString(offset, length));
	}

	/// <inheritdoc />
	public ITextSource CreateSnapshot()
	{
		return this;
	}

	/// <inheritdoc />
	public ITextSource CreateSnapshot(int offset, int length)
	{
		return new StringGapBufferTextSource(_buffer.SubString(offset, length));
	}

	/// <inheritdoc />
	public char GetCharAt(int offset)
	{
		return _buffer[offset];
	}

	/// <inheritdoc />
	public string GetText(int offset, int length)
	{
		return _buffer.SubString(offset, length);
	}

	/// <inheritdoc />
	public string GetText(IRange range)
	{
		return _buffer.SubString(range.StartIndex, range.Length);
	}

	/// <inheritdoc />
	public int IndexOf(char c, int startIndex, int count)
	{
		return _buffer.IndexOf(c, startIndex, count);
	}

	/// <inheritdoc />
	public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
	{
		return _buffer.IndexOf(searchText, startIndex, count, comparisonType);
	}

	/// <inheritdoc />
	public int IndexOfAny(char[] anyOf, int startIndex, int count)
	{
		return _buffer.IndexOfAny(anyOf, startIndex, count);
	}

	/// <inheritdoc />
	public int LastIndexOf(char c, int startIndex, int count)
	{
		return _buffer.LastIndexOf(c, startIndex, count);
	}

	/// <inheritdoc />
	public int LastIndexOf(string searchText, StringComparison comparisonType)
	{
		return _buffer.LastIndexOf(searchText, 0, TextLength, comparisonType);
	}

	/// <inheritdoc />
	public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
	{
		return _buffer.LastIndexOf(searchText, startIndex, count, comparisonType);
	}

	/// <inheritdoc />
	public void WriteTextTo(TextWriter writer)
	{
		_buffer.WriteTo(writer, 0, _buffer.Count);
	}

	/// <inheritdoc />
	public void WriteTextTo(TextWriter writer, int offset, int length)
	{
		_buffer.WriteTo(writer, offset, length);
	}

	#endregion
}