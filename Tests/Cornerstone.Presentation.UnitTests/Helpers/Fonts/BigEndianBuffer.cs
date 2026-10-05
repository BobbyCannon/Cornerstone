#region References

using System;
using System.Buffers.Binary;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers.Fonts;

/// <summary>
/// A growable big-endian byte writer for hand-crafting OpenType table sub-structures
/// (ItemVariationStores, cmap subtables, COLR paint graphs, ...) in tests.
/// </summary>
/// <remarks>
/// OpenType is big-endian and pervasively offset-based: a header field holds the byte
/// offset of a sub-table that is written later. <see cref="ReserveOffset16" /> /
/// <see cref="ReserveOffset32" /> write a placeholder and return its position so the
/// real value can be back-patched with <see cref="PatchUInt16" /> / <see cref="PatchUInt32" />
/// once the target's position is known (via <see cref="Position" />). All multi-byte
/// writes are big-endian.
/// </remarks>
public sealed class BigEndianBuffer
{
	#region Fields

	private byte[] _buffer;

	#endregion

	#region Constructors

	public BigEndianBuffer(int initialCapacity = 64)
	{
		_buffer = new byte[Math.Max(4, initialCapacity)];
	}

	#endregion

	#region Properties

	/// <summary> The number of bytes written so far — also the offset the next write lands at. </summary>
	public int Position { get; private set; }

	#endregion

	#region Methods

	public BigEndianBuffer Bytes(ReadOnlySpan<byte> bytes)
	{
		EnsureCapacity(bytes.Length);
		bytes.CopyTo(_buffer.AsSpan(Position));
		Position += bytes.Length;
		return this;
	}

	/// <summary> Writes an F2DOT14 fixed-point value (the variation-coordinate / region format). </summary>
	public BigEndianBuffer F2Dot14(double value)
	{
		return Int16((int) Math.Round(value * 16384.0));
	}

	/// <summary> Writes a 16.16 fixed-point value. </summary>
	public BigEndianBuffer Fixed(double value)
	{
		return Int32((int) Math.Round(value * 65536.0));
	}

	public BigEndianBuffer Int16(int value)
	{
		EnsureCapacity(2);
		BinaryPrimitives.WriteInt16BigEndian(_buffer.AsSpan(Position), checked((short) value));
		Position += 2;
		return this;
	}

	public BigEndianBuffer Int32(int value)
	{
		EnsureCapacity(4);
		BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(Position), value);
		Position += 4;
		return this;
	}

	public BigEndianBuffer Int8(int value)
	{
		EnsureCapacity(1);
		_buffer[Position++] = unchecked((byte) (sbyte) value);
		return this;
	}

	/// <summary> Back-patches a big-endian <c> uint16 </c> at a previously reserved position. </summary>
	public BigEndianBuffer PatchUInt16(int position, int value)
	{
		BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(position, 2), checked((ushort) value));
		return this;
	}

	/// <summary> Back-patches a big-endian <c> uint32 </c> at a previously reserved position. </summary>
	public BigEndianBuffer PatchUInt32(int position, uint value)
	{
		BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(position, 4), value);
		return this;
	}

	/// <summary> Writes a placeholder big-endian <c> uint16 </c> and returns its position for later patching. </summary>
	public int ReserveOffset16()
	{
		var pos = Position;
		UInt16(0);
		return pos;
	}

	/// <summary> Writes a placeholder big-endian <c> uint32 </c> and returns its position for later patching. </summary>
	public int ReserveOffset32()
	{
		var pos = Position;
		UInt32(0);
		return pos;
	}

	/// <summary> Writes a 4-character tag (space-padded / truncated to 4 bytes). </summary>
	public BigEndianBuffer Tag(string tag)
	{
		Span<byte> bytes = stackalloc byte[4] { 0x20, 0x20, 0x20, 0x20 };
		var n = Math.Min(4, tag.Length);
		for (var i = 0; i < n; i++)
		{
			bytes[i] = (byte) tag[i];
		}

		return Bytes(bytes);
	}

	public byte[] ToArray()
	{
		return _buffer.AsSpan(0, Position).ToArray();
	}

	public BigEndianBuffer UInt16(int value)
	{
		EnsureCapacity(2);
		BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(Position), checked((ushort) value));
		Position += 2;
		return this;
	}

	public BigEndianBuffer UInt24(int value)
	{
		EnsureCapacity(3);
		_buffer[Position++] = (byte) ((value >> 16) & 0xFF);
		_buffer[Position++] = (byte) ((value >> 8) & 0xFF);
		_buffer[Position++] = (byte) (value & 0xFF);
		return this;
	}

	public BigEndianBuffer UInt32(uint value)
	{
		EnsureCapacity(4);
		BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(Position), value);
		Position += 4;
		return this;
	}

	public BigEndianBuffer UInt8(int value)
	{
		EnsureCapacity(1);
		_buffer[Position++] = checked((byte) value);
		return this;
	}

	/// <summary> Writes <paramref name="count" /> zero bytes (padding / placeholder data). </summary>
	public BigEndianBuffer Zeros(int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count), count, "count must be non-negative.");
		}

		EnsureCapacity(count);
		Position += count; // already zero-initialized
		return this;
	}

	private void EnsureCapacity(int additional)
	{
		var required = Position + additional;
		if (required <= _buffer.Length)
		{
			return;
		}

		var newCapacity = _buffer.Length * 2;
		while (newCapacity < required)
		{
			newCapacity *= 2;
		}

		Array.Resize(ref _buffer, newCapacity);
	}

	#endregion
}