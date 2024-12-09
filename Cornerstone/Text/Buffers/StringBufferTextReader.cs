#region References

using System;
using System.IO;

#endregion

namespace Cornerstone.Text.Buffers;

public class StringBufferTextReader : TextReader
{
	#region Fields

	private readonly IStringBuffer _buffer;
	private int _bufferIndex;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new StringBufferReader.
	/// </summary>
	public StringBufferTextReader(string value)
		: this(new StringGapBuffer(value))
	{
	}

	/// <summary>
	/// Creates a new StringBufferReader.
	/// </summary>
	public StringBufferTextReader(IStringBuffer buffer)
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
		if ((_bufferIndex < 0)
			|| (_bufferIndex >= _buffer.Count))
		{
			return -1;
		}

		var response = _buffer.Read(_bufferIndex, buffer, index, count);
		_bufferIndex += response;
		return response;
	}

	/// <inheritdoc />
	public override int ReadBlock(char[] buffer, int index, int count)
	{
		if ((_bufferIndex < 0)
			|| (_bufferIndex >= _buffer.Count))
		{
			return -1;
		}

		var response = _buffer.Read(_bufferIndex, buffer, index, count);
		_bufferIndex += response;
		return response;
	}

	/// <inheritdoc />
	public override string ReadToEnd()
	{
		var response = _buffer.Read(_bufferIndex, _buffer.Count);
		return new string(response);
	}

	#endregion
}