#region References

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Cornerstone.Extensions;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization;

/// <summary>
/// Zero-heap, stack-only ref struct reader that fully implements the SpeedyPacket binary format.
/// Header and data sections are separated at construction (O(n) scan, zero allocation).
/// </summary>
public ref struct SpeedyPackReader
{
	#region Fields

	private int _dataOffset;
	private int _headerOffset;

	#endregion

	#region Constructors

	public SpeedyPackReader(ReadOnlySpan<byte> buffer)
	{
		// One-time scan to locate EndOfHeader (zero allocation, just spans)
		var headerLen = 0;
		while ((headerLen < buffer.Length)
				&& (buffer[headerLen] != (byte) SpeedyPacketDataTypes.EndOfHeader))
		{
			headerLen++;
		}

		if (headerLen == buffer.Length)
		{
			throw new InvalidDataContractException("EndOfHeader marker (0xFF) not found in packet.");
		}

		Header = buffer[..headerLen];
		Data = buffer[(headerLen + 1)..];

		_headerOffset = 0;
		_dataOffset = 0;
	}

	#endregion

	#region Properties

	public ReadOnlySpan<byte> Data { get; }

	public ReadOnlySpan<byte> Header { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Peek the next header type. Returns false when header is exhausted.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryPeekHeaderType(out SpeedyPacketDataTypes type)
	{
		if (_headerOffset >= Header.Length)
		{
			type = default;
			return false;
		}

		type = (SpeedyPacketDataTypes) Header[_headerOffset];
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadByte(out byte value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.ByteMin:
				value = byte.MinValue;
				return true;
			case SpeedyPacketDataTypes.ByteMax:
				value = byte.MaxValue;
				return true;
			case SpeedyPacketDataTypes.ByteOne:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.ByteTwo:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.Byte:
				if (_dataOffset >= Data.Length)
				{
					value = 0;
					return false;
				}
				value = Data[_dataOffset++];
				return true;
			default:
				value = 0;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadChar(out char value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = '\0';
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.CharMin:
				value = char.MinValue;
				return true;
			case SpeedyPacketDataTypes.CharMax:
				value = char.MaxValue;
				return true;
			case SpeedyPacketDataTypes.Char:
				if ((_dataOffset + 2) > Data.Length)
				{
					value = '\0';
					return false;
				}
				value = (char) BinaryPrimitives.ReadUInt16LittleEndian(Data[_dataOffset..]);
				_dataOffset += 2;
				return true;
			default:
				value = '\0';
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadDateOnly(out DateOnly value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = DateOnly.MinValue;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.DateOnlyMin:
				value = DateOnly.MinValue;
				return true;
			case SpeedyPacketDataTypes.DateOnlyMax:
				value = DateOnly.MaxValue;
				return true;
			case SpeedyPacketDataTypes.DateOnlyUnixEpoch:
				value = DateTimeExtensions.UnixEpochDateOnly;
				return true;
			case SpeedyPacketDataTypes.DateOnlyWindowsEpoch:
				value = DateTimeExtensions.WindowsEpochDateOnly;
				return true;
			case SpeedyPacketDataTypes.DateOnly:
				if ((_dataOffset + 4) > Data.Length)
				{
					value = DateOnly.MinValue;
					return false;
				}
				value = DateOnly.FromDayNumber(BinaryPrimitives.ReadInt32LittleEndian(Data[_dataOffset..]));
				_dataOffset += 4;
				return true;
			default:
				value = DateOnly.MinValue;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadDateTime(out DateTime value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = DateTime.MinValue;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.DateTimeMin:
				value = DateTime.MinValue;
				return true;
			case SpeedyPacketDataTypes.DateTimeMax:
				value = DateTime.MaxValue;
				return true;
			case SpeedyPacketDataTypes.DateTimeUnixEpoch:
				value = DateTimeExtensions.UnixEpochDateTime;
				return true;
			case SpeedyPacketDataTypes.DateTimeWindowsEpoch:
				value = DateTimeExtensions.WindowsEpochDateTime;
				return true;
			case SpeedyPacketDataTypes.DateTime:
				if ((_dataOffset + 8) > Data.Length)
				{
					value = DateTime.MinValue;
					return false;
				}
				value = new DateTime(BinaryPrimitives.ReadInt64LittleEndian(Data[_dataOffset..]));
				_dataOffset += 8;
				return true;
			default:
				value = DateTime.MinValue;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadDateTimeOffset(out DateTimeOffset value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = DateTimeOffset.MinValue;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.DateTimeOffsetMin:
				value = DateTimeOffset.MinValue;
				return true;
			case SpeedyPacketDataTypes.DateTimeOffsetMax:
				value = DateTimeOffset.MaxValue;
				return true;
			case SpeedyPacketDataTypes.DateTimeOffsetUnixEpoch:
				value = DateTimeExtensions.UnixEpochDateTimeOffset;
				return true;
			case SpeedyPacketDataTypes.DateTimeOffsetWindowsEpoch:
				value = DateTimeExtensions.WindowsEpochDateTimeOffset;
				return true;
			case SpeedyPacketDataTypes.DateTimeOffset:
				if ((_dataOffset + 16) > Data.Length)
				{
					value = DateTimeOffset.MinValue;
					return false;
				}
				value = new DateTimeOffset(
					BinaryPrimitives.ReadInt64LittleEndian(Data[_dataOffset..]),
					new TimeSpan(BinaryPrimitives.ReadInt64LittleEndian(Data[(_dataOffset + 8)..]))
				);
				_dataOffset += 16;
				return true;
			default:
				value = DateTimeOffset.MinValue;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadDecimal(out decimal value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.DecimalMin:
				value = decimal.MinValue;
				return true;
			case SpeedyPacketDataTypes.DecimalMax:
				value = decimal.MaxValue;
				return true;
			case SpeedyPacketDataTypes.DecimalZero:
				value = 0m;
				return true;
			case SpeedyPacketDataTypes.DecimalOne:
				value = 1m;
				return true;
			case SpeedyPacketDataTypes.DecimalNegativeOne:
				value = -1m;
				return true;
			case SpeedyPacketDataTypes.Decimal:
				if ((_dataOffset + 16) > Data.Length)
				{
					value = 0;
					return false;
				}

				Span<int> bits = stackalloc int[4];
				var span = Data.Slice(_dataOffset, 16);

				if (BitConverter.IsLittleEndian)
				{
					bits[0] = BinaryPrimitives.ReadInt32LittleEndian(span[..4]);
					bits[1] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(4, 4));
					bits[2] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(8, 4));
					bits[3] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(12, 4));
				}
				else
				{
					Span<byte> buffer = stackalloc byte[16];
					span.CopyTo(buffer);
					buffer.Reverse();
					bits[0] = BinaryPrimitives.ReadInt32LittleEndian(buffer[..4]);
					bits[1] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4, 4));
					bits[2] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(8, 4));
					bits[3] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(12, 4));
				}

				value = new decimal(bits);
				_dataOffset += 16;
				return true;
			default:
				value = 0;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadDouble(out double value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0d;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.DoubleMin:
				value = double.MinValue;
				return true;
			case SpeedyPacketDataTypes.DoubleMax:
				value = double.MaxValue;
				return true;
			case SpeedyPacketDataTypes.DoubleNegativeInfinity:
				value = double.NegativeInfinity;
				return true;
			case SpeedyPacketDataTypes.DoublePositiveInfinity:
				value = double.PositiveInfinity;
				return true;
			case SpeedyPacketDataTypes.DoubleNaN:
				value = double.NaN;
				return true;
			case SpeedyPacketDataTypes.DoubleNegativeZero:
				value = -0.0d;
				return true;
			case SpeedyPacketDataTypes.DoubleNegativeOne:
				value = -1.0d;
				return true;
			case SpeedyPacketDataTypes.DoubleHalf:
				value = 0.5d;
				return true;
			case SpeedyPacketDataTypes.DoubleZero:
				value = 0d;
				return true;
			case SpeedyPacketDataTypes.DoubleOne:
				value = 1d;
				return true;
			case SpeedyPacketDataTypes.DoubleTwo:
				value = 2d;
				return true;
			case SpeedyPacketDataTypes.DoubleE:
				value = Math.E;
				return true;
			case SpeedyPacketDataTypes.DoubleEpsilon:
				value = double.Epsilon;
				return true;
			case SpeedyPacketDataTypes.DoublePi:
				value = Math.PI;
				return true;
			case SpeedyPacketDataTypes.DoubleTau:
				value = Math.Tau;
				return true;
			case SpeedyPacketDataTypes.Double:
				if ((_dataOffset + 8) > Data.Length)
				{
					value = 0d;
					return false;
				}
				value = BinaryPrimitives.ReadDoubleLittleEndian(Data[_dataOffset..]);
				_dataOffset += 8;
				return true;
			default:
				value = 0d;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadFloat(out float value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0f;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.FloatMin:
				value = float.MinValue;
				return true;
			case SpeedyPacketDataTypes.FloatMax:
				value = float.MaxValue;
				return true;
			case SpeedyPacketDataTypes.FloatNegativeInfinity:
				value = float.NegativeInfinity;
				return true;
			case SpeedyPacketDataTypes.FloatPositiveInfinity:
				value = float.PositiveInfinity;
				return true;
			case SpeedyPacketDataTypes.FloatNaN:
				value = float.NaN;
				return true;
			case SpeedyPacketDataTypes.FloatNegativeZero:
				value = -0.0f;
				return true;
			case SpeedyPacketDataTypes.FloatNegativeOne:
				value = -1.0f;
				return true;
			case SpeedyPacketDataTypes.FloatHalf:
				value = 0.5f;
				return true;
			case SpeedyPacketDataTypes.FloatZero:
				value = 0.0f;
				return true;
			case SpeedyPacketDataTypes.FloatOne:
				value = 1.0f;
				return true;
			case SpeedyPacketDataTypes.FloatTwo:
				value = 2.0f;
				return true;
			case SpeedyPacketDataTypes.FloatE:
				value = MathF.E;
				return true;
			case SpeedyPacketDataTypes.FloatEpsilon:
				value = float.Epsilon;
				return true;
			case SpeedyPacketDataTypes.FloatPi:
				value = MathF.PI;
				return true;
			case SpeedyPacketDataTypes.FloatTau:
				value = MathF.Tau;
				return true;
			case SpeedyPacketDataTypes.Float:
				if ((_dataOffset + 4) > Data.Length)
				{
					value = 0f;
					return false;
				}
				value = BinaryPrimitives.ReadSingleLittleEndian(Data.Slice(_dataOffset, 4));
				_dataOffset += 4;
				return true;
			default:
				value = 0f;
				return false;
		}
	}

	public bool TryReadGuid(out Guid value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = Guid.Empty;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.GuidEmpty:
				value = Guid.Empty;
				return true;
			case SpeedyPacketDataTypes.GuidAllBitsSet:
				value = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");
				return true;
			case SpeedyPacketDataTypes.Guid:
				if ((_dataOffset + 16) > Data.Length)
				{
					value = Guid.Empty;
					return false;
				}
				value = new Guid(Data.Slice(_dataOffset, 16), false);
				_dataOffset += 16;
				return true;
			default:
				value = Guid.Empty;
				return false;
		}
	}

	/// <summary>
	/// Reads the next header type. Returns false when header is exhausted.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadHeaderType(out SpeedyPacketDataTypes type)
	{
		if (_headerOffset >= Header.Length)
		{
			type = default;
			return false;
		}

		type = (SpeedyPacketDataTypes) Header[_headerOffset++];
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadInt128(out Int128 value)
	{
		// Your original implementation – kept exactly as you wrote it
		if (!TryReadHeaderType(out var type))
		{
			value = default;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.Int128NegativeOne:
				value = -1;
				return true;
			case SpeedyPacketDataTypes.Int128Zero:
				value = 0;
				return true;
			case SpeedyPacketDataTypes.Int128One:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.Int128Two:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.Int128Max:
				value = Int128.MaxValue;
				return true;
			case SpeedyPacketDataTypes.Int128Min:
				value = Int128.MinValue;
				return true;
			case SpeedyPacketDataTypes.Int128:
				if ((_dataOffset + 16) > Data.Length)
				{
					value = default;
					return false;
				}
				value = BinaryPrimitives.ReadInt128LittleEndian(Data.Slice(_dataOffset, 16));
				_dataOffset += 16;
				return true;
			default:
				value = default;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadInt16(out short value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.Int16Min:
				value = short.MinValue;
				return true;
			case SpeedyPacketDataTypes.Int16Max:
				value = short.MaxValue;
				return true;
			case SpeedyPacketDataTypes.Int16Zero:
				value = 0;
				return true;
			case SpeedyPacketDataTypes.Int16One:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.Int16Two:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.Int16NegativeOne:
				value = -1;
				return true;
			case SpeedyPacketDataTypes.Int16:
				if ((_dataOffset + 2) > Data.Length)
				{
					value = 0;
					return false;
				}
				value = BinaryPrimitives.ReadInt16LittleEndian(Data[_dataOffset..]);
				_dataOffset += 2;
				return true;
			default:
				value = 0;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadInt32(out int value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.Int32NegativeOne:
				value = -1;
				return true;
			case SpeedyPacketDataTypes.Int32Zero:
				value = 0;
				return true;
			case SpeedyPacketDataTypes.Int32One:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.Int32Two:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.Int32Min:
				value = int.MinValue;
				return true;
			case SpeedyPacketDataTypes.Int32Max:
				value = int.MaxValue;
				return true;
			case SpeedyPacketDataTypes.Int32:
				if ((_dataOffset + 4) > Data.Length)
				{
					value = 0;
					return false;
				}
				value = BinaryPrimitives.ReadInt32LittleEndian(Data[_dataOffset..]);
				_dataOffset += 4;
				return true;
			default:
				value = 0;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadInt64(out long value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.Int64NegativeOne:
			{
				value = -1L;
				return true;
			}
			case SpeedyPacketDataTypes.Int64Zero:
			{
				value = 0L;
				return true;
			}
			case SpeedyPacketDataTypes.Int64One:
			{
				value = 1L;
				return true;
			}
			case SpeedyPacketDataTypes.Int64Two:
			{
				value = 2L;
				return true;
			}
			case SpeedyPacketDataTypes.Int64Min:
			{
				value = long.MinValue;
				return true;
			}
			case SpeedyPacketDataTypes.Int64Max:
			{
				value = long.MaxValue;
				return true;
			}
			case SpeedyPacketDataTypes.Int64:
			{
				if ((_dataOffset + 8) > Data.Length)
				{
					value = 0;
					return false;
				}
				value = BinaryPrimitives.ReadInt64LittleEndian(Data[_dataOffset..]);
				_dataOffset += 8;
				return true;
			}
			default:
			{
				value = 0;
				return false;
			}
		}
	}

	/// <summary>
	/// Zero-copy length-prefixed payload (used by Packet and ByteArray).
	/// The header type (Packet or ByteArray) must be consumed first.
	/// </summary>
	public bool TryReadLengthPrefixedBytes(out ReadOnlySpan<byte> payload)
	{
		if ((_dataOffset + 4) > Data.Length)
		{
			payload = default;
			return false;
		}

		var len = BinaryPrimitives.ReadInt32LittleEndian(Data[_dataOffset..]);
		_dataOffset += 4;

		if ((len < 0) || ((_dataOffset + len) > Data.Length))
		{
			payload = default;
			return false;
		}

		payload = Data.Slice(_dataOffset, len);
		_dataOffset += len;
		return true;
	}

	/// <summary>
	/// Helper for callers who want a sub-reader for a nested packet (zero extra heap).
	/// Now correctly consumes the Packet header type.
	/// </summary>
	public bool TryReadNestedPacket(out SpeedyPackReader subReader)
	{
		if (!TryReadHeaderType(out var type) || (type != SpeedyPacketDataTypes.Packet))
		{
			subReader = default;
			return false;
		}

		if (!TryReadLengthPrefixedBytes(out var payload))
		{
			subReader = default;
			return false;
		}

		subReader = new SpeedyPackReader(payload);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadSByte(out sbyte value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.SByteMin:
				value = sbyte.MinValue;
				return true;
			case SpeedyPacketDataTypes.SByteMax:
				value = sbyte.MaxValue;
				return true;
			case SpeedyPacketDataTypes.SByteNegativeOne:
				value = -1;
				return true;
			case SpeedyPacketDataTypes.SByteZero:
				value = 0;
				return true;
			case SpeedyPacketDataTypes.SByteOne:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.SByteTwo:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.SByte:
				if (_dataOffset >= Data.Length)
				{
					value = 0;
					return false;
				}
				value = (sbyte) Data[_dataOffset++];
				return true;
			default:
				value = 0;
				return false;
		}
	}

	/// <summary>
	/// Returns raw UTF-8 bytes between StartOfText and EndOfText. Zero-copy.
	/// </summary>
	public bool TryReadString(out ReadOnlySpan<byte> utf8Bytes)
	{
		utf8Bytes = default;

		if (!TryReadHeaderType(out var type))
		{
			return false;
		}

		if (type == SpeedyPacketDataTypes.StringOfEmpty)
		{
			utf8Bytes = ReadOnlySpan<byte>.Empty;
			return true;
		}

		if (type != SpeedyPacketDataTypes.String)
		{
			return false;
		}

		if ((_dataOffset >= Data.Length) || (Data[_dataOffset] != AsciiCharacters.StartOfText))
		{
			return false;
		}

		var start = ++_dataOffset;

		while (_dataOffset < Data.Length)
		{
			if (Data[_dataOffset] == AsciiCharacters.EndOfText)
			{
				utf8Bytes = Data.Slice(start, _dataOffset - start);
				_dataOffset++;
				return true;
			}
			_dataOffset++;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadTimeOnly(out TimeOnly value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = TimeOnly.MinValue;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.TimeOnlyMin:
				value = TimeOnly.MinValue;
				return true;
			case SpeedyPacketDataTypes.TimeOnlyMax:
				value = TimeOnly.MaxValue;
				return true;
			case SpeedyPacketDataTypes.TimeOnly:
				if ((_dataOffset + 8) > Data.Length)
				{
					value = TimeOnly.MinValue;
					return false;
				}
				value = new TimeOnly(BinaryPrimitives.ReadInt64LittleEndian(Data[_dataOffset..]));
				_dataOffset += 8;
				return true;
			default:
				value = TimeOnly.MinValue;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadTimeSpan(out TimeSpan value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = TimeSpan.MinValue;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.TimeSpanMin:
				value = TimeSpan.MinValue;
				return true;
			case SpeedyPacketDataTypes.TimeSpanMax:
				value = TimeSpan.MaxValue;
				return true;
			case SpeedyPacketDataTypes.TimeSpan:
				if ((_dataOffset + 8) > Data.Length)
				{
					value = TimeSpan.MinValue;
					return false;
				}
				value = new TimeSpan(BinaryPrimitives.ReadInt64LittleEndian(Data[_dataOffset..]));
				_dataOffset += 8;
				return true;
			default:
				value = TimeSpan.MinValue;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadUInt128(out UInt128 value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = default;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.UInt128One:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.UInt128Two:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.UInt128Min:
				value = UInt128.MinValue;
				return true;
			case SpeedyPacketDataTypes.UInt128Max:
				value = UInt128.MaxValue;
				return true;
			case SpeedyPacketDataTypes.UInt128:
				if ((_dataOffset + 16) > Data.Length)
				{
					value = default;
					return false;
				}
				value = BinaryPrimitives.ReadUInt128LittleEndian(Data.Slice(_dataOffset, 16));
				_dataOffset += 16;
				return true;
			default:
				value = default;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadUInt16(out ushort value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.UInt16One:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.UInt16Two:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.UInt16Min:
				value = ushort.MinValue;
				return true;
			case SpeedyPacketDataTypes.UInt16Max:
				value = ushort.MaxValue;
				return true;
			case SpeedyPacketDataTypes.UInt16:
				if ((_dataOffset + 2) > Data.Length)
				{
					value = 0;
					return false;
				}
				value = BinaryPrimitives.ReadUInt16LittleEndian(Data[_dataOffset..]);
				_dataOffset += 2;
				return true;
			default:
				value = 0;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadUInt32(out uint value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.UInt32One:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.UInt32Two:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.UInt32Min:
				value = uint.MinValue;
				return true;
			case SpeedyPacketDataTypes.UInt32Max:
				value = uint.MaxValue;
				return true;
			case SpeedyPacketDataTypes.UInt32:
				if ((_dataOffset + 4) > Data.Length)
				{
					value = 0;
					return false;
				}
				value = BinaryPrimitives.ReadUInt32LittleEndian(Data[_dataOffset..]);
				_dataOffset += 4;
				return true;
			default:
				value = 0;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadUInt64(out ulong value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = 0;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.UInt64One:
				value = 1;
				return true;
			case SpeedyPacketDataTypes.UInt64Two:
				value = 2;
				return true;
			case SpeedyPacketDataTypes.UInt64Min:
				value = ulong.MinValue;
				return true;
			case SpeedyPacketDataTypes.UInt64Max:
				value = ulong.MaxValue;
				return true;
			case SpeedyPacketDataTypes.UInt64:
				if ((_dataOffset + 8) > Data.Length)
				{
					value = 0;
					return false;
				}
				value = BinaryPrimitives.ReadUInt64LittleEndian(Data[_dataOffset..]);
				_dataOffset += 8;
				return true;
			default:
				value = 0;
				return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadVersion(out Version value)
	{
		if (!TryReadHeaderType(out var type))
		{
			value = null;
			return false;
		}

		switch (type)
		{
			case SpeedyPacketDataTypes.VersionOneZero:
			{
				value = new Version(1, 0);
				return true;
			}
			case SpeedyPacketDataTypes.VersionOneZeroZero:
			{
				value = new Version(1, 0, 0);
				return true;
			}
			case SpeedyPacketDataTypes.VersionOneZeroZeroZero:
			{
				value = new Version(1, 0, 0, 0);
				return true;
			}
			case SpeedyPacketDataTypes.Version:
			{
				if ((_dataOffset + 16) > Data.Length)
				{
					value = null;
					return false;
				}
				var major = BinaryPrimitives.ReadInt32LittleEndian(Data[_dataOffset..]);
				var minor = BinaryPrimitives.ReadInt32LittleEndian(Data[(_dataOffset + 4)..]);
				var build = BinaryPrimitives.ReadInt32LittleEndian(Data[(_dataOffset + 8)..]);
				var revision = BinaryPrimitives.ReadInt32LittleEndian(Data[(_dataOffset + 12)..]);
				var ver = build == -1 ? new Version(major, minor) :
					revision == -1 ? new Version(major, minor, build) :
					new Version(major, minor, build, revision);
				value = ver;
				_dataOffset += 16;
				return true;
			}
			default:
			{
				value = null;
				return false;
			}
		}
	}

	#endregion
}