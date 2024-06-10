#region References

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace Cornerstone.Text.Buffers;

/// <summary>
/// TextReader implementation that reads text from a buffer.
/// </summary>
public sealed class StringGapBufferTextReader : TextReader
{
	#region Fields

	private readonly StringGapBuffer _buffer;
	private int _bufferIndex;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new StringGapBufferTextReader.
	/// </summary>
	public StringGapBufferTextReader(IEnumerable<char> value)
		: this(new StringGapBuffer(value))
	{
	}

	/// <summary>
	/// Creates a new StringGapBufferTextReader.
	/// </summary>
	public StringGapBufferTextReader(StringGapBuffer buffer)
	{
		_buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
		_bufferIndex = 0;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override int Peek()
	{
		if ((_bufferIndex < 0)
			|| (_bufferIndex >= _buffer.Count))
		{
			return -1;
		}

		return _buffer[_bufferIndex];
	}

	/// <inheritdoc />
	public override int Read()
	{
		if ((_bufferIndex < 0)
			|| (_bufferIndex >= _buffer.Count))
		{
			return -1;
		}

		return _buffer[_bufferIndex++];
	}

	/// <inheritdoc />
	public override int Read(char[] buffer, int index, int count)
	{
		return _buffer.Read(buffer, index, count);
	}

	#endregion
}