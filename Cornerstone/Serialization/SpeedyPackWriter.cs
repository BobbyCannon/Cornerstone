#region References

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization;

public static class SpeedyPackWriter
{
	#region Fields

	private static readonly SpeedyBufferPool<byte> _dataPool;
	private static readonly SpeedyBufferPool<SpeedyPacketDataTypes> _headerPool;
	private static readonly Dictionary<Type, SerializerDelegate> _serializers;

	#endregion

	#region Constructors

	static SpeedyPackWriter()
	{
		_dataPool = new();
		_headerPool = new();
		_serializers = new Dictionary<Type, SerializerDelegate>
		{
			[typeof(bool)] = (v, h, _) =>
			{
				var b = (bool) v;
				h.Append(b ? SpeedyPacketDataTypes.True : SpeedyPacketDataTypes.False);
			},
			[typeof(char)] = (v, h, d) =>
			{
				var c = (char) v;
				switch (c)
				{
					case char.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.CharMin);
						break;
					}
					case char.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.CharMax);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.Char);
						NumberUInt16Write(c, d);
						break;
					}
				}
			},
			[typeof(string)] = (v, h, d) =>
			{
				var s = (string) v;
				switch (s)
				{
					case null:
					{
						h.Append(SpeedyPacketDataTypes.Null);
						break;
					}
					case "":
					{
						h.Append(SpeedyPacketDataTypes.StringOfEmpty);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.String);
						StringWrite(s, d);
						break;
					}
				}
			},
			[typeof(byte)] = (v, h, d) =>
			{
				var b = (byte) v;
				switch (b)
				{
					case byte.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.ByteMin);
						break;
					}
					case byte.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.ByteMax);
						break;
					}
					case 1:
					{
						h.Append(SpeedyPacketDataTypes.ByteOne);
						break;
					}
					case 2:
					{
						h.Append(SpeedyPacketDataTypes.ByteTwo);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.Byte);
						d.Append(b);
						break;
					}
				}
			},
			[typeof(sbyte)] = (v, h, d) =>
			{
				var sb = (sbyte) v;
				switch (sb)
				{
					case sbyte.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.SByteMin);
						break;
					}
					case sbyte.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.SByteMax);
						break;
					}
					case 0:
					{
						h.Append(SpeedyPacketDataTypes.SByteZero);
						break;
					}
					case 1:
					{
						h.Append(SpeedyPacketDataTypes.SByteOne);
						break;
					}
					case 2:
					{
						h.Append(SpeedyPacketDataTypes.SByteTwo);
						break;
					}
					case -1:
					{
						h.Append(SpeedyPacketDataTypes.SByteNegativeOne);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.SByte);
						d.Append((byte) sb);
						break;
					}
				}
			},
			[typeof(short)] = (v, h, d) =>
			{
				var s = (short) v;
				switch (s)
				{
					case short.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.Int16Min);
						break;
					}
					case short.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.Int16Max);
						break;
					}
					case -1:
					{
						h.Append(SpeedyPacketDataTypes.Int16NegativeOne);
						break;
					}
					case 0:
					{
						h.Append(SpeedyPacketDataTypes.Int16Zero);
						break;
					}
					case 1:
					{
						h.Append(SpeedyPacketDataTypes.Int16One);
						break;
					}
					case 2:
					{
						h.Append(SpeedyPacketDataTypes.Int16Two);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.Int16);
						NumberInt16Write(s, d);
						break;
					}
				}
			},
			[typeof(ushort)] = (v, h, d) =>
			{
				var us = (ushort) v;
				switch (us)
				{
					case ushort.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.UInt16Min);
						break;
					}
					case ushort.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.UInt16Max);
						break;
					}
					case 1:
					{
						h.Append(SpeedyPacketDataTypes.UInt16One);
						break;
					}
					case 2:
					{
						h.Append(SpeedyPacketDataTypes.UInt16Two);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.UInt16);
						NumberUInt16Write(us, d);
						break;
					}
				}
			},
			[typeof(int)] = (v, h, d) =>
			{
				var i = (int) v;
				switch (i)
				{
					case int.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.Int32Min);
						break;
					}
					case int.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.Int32Max);
						break;
					}
					case -1:
					{
						h.Append(SpeedyPacketDataTypes.Int32NegativeOne);
						break;
					}
					case 0:
					{
						h.Append(SpeedyPacketDataTypes.Int32Zero);
						break;
					}
					case 1:
					{
						h.Append(SpeedyPacketDataTypes.Int32One);
						break;
					}
					case 2:
					{
						h.Append(SpeedyPacketDataTypes.Int32Two);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.Int32);
						NumberInt32Write(i, d);
						break;
					}
				}
			},
			[typeof(uint)] = (v, h, d) =>
			{
				var ui = (uint) v;
				switch (ui)
				{
					case uint.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.UInt32Min);
						break;
					}
					case uint.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.UInt32Max);
						break;
					}
					case 1:
					{
						h.Append(SpeedyPacketDataTypes.UInt32One);
						break;
					}
					case 2:
					{
						h.Append(SpeedyPacketDataTypes.UInt32Two);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.UInt32);
						NumberUInt32Write(ui, d);
						break;
					}
				}
			},
			[typeof(long)] = (v, h, d) =>
			{
				var l = (long) v;
				switch (l)
				{
					case 0:
					{
						h.Append(SpeedyPacketDataTypes.Int64Zero);
						break;
					}
					case -1:
					{
						h.Append(SpeedyPacketDataTypes.Int64NegativeOne);
						break;
					}
					case long.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.Int64Min);
						break;
					}
					case long.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.Int64Max);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.Int64);
						NumberInt64Write(l, d);
						break;
					}
				}
			},
			[typeof(ulong)] = (v, h, d) =>
			{
				var ul = (ulong) v;
				switch (ul)
				{
					case 1:
					{
						h.Append(SpeedyPacketDataTypes.UInt64One);
						break;
					}
					case 2:
					{
						h.Append(SpeedyPacketDataTypes.UInt64Two);
						break;
					}
					case ulong.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.UInt64Min);
						break;
					}
					case ulong.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.UInt64Max);
						break;
					}
					default:
					{
						h.Append(SpeedyPacketDataTypes.UInt64);
						NumberUInt64Write(ul, d);
						break;
					}
				}
			},
			[typeof(Int128)] = (v, h, d) =>
			{
				var i128 = (Int128) v;
				if (i128 == -1)
				{
					h.Append(SpeedyPacketDataTypes.Int128NegativeOne);
				}
				else if (i128 == 0)
				{
					h.Append(SpeedyPacketDataTypes.Int128Zero);
				}
				else if (i128 == 1)
				{
					h.Append(SpeedyPacketDataTypes.Int128One);
				}
				else if (i128 == 2)
				{
					h.Append(SpeedyPacketDataTypes.Int128Two);
				}
				else if (i128 == Int128.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.Int128Min);
				}
				else if (i128 == Int128.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.Int128Max);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.Int128);
					NumberInt128Write(i128, d);
				}
			},
			[typeof(UInt128)] = (v, h, d) =>
			{
				var ui128 = (UInt128) v;
				if (ui128 == 1)
				{
					h.Append(SpeedyPacketDataTypes.UInt128One);
				}
				else if (ui128 == 2)
				{
					h.Append(SpeedyPacketDataTypes.UInt128Two);
				}
				else if (ui128 == UInt128.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.UInt128Min);
				}
				else if (ui128 == UInt128.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.UInt128Max);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.UInt128);
					NumberUInt128Write(ui128, d);
				}
			},
			[typeof(float)] = (v, h, d) =>
			{
				var f = (float) v;
				switch (f)
				{
					case -1.0f:
					{
						h.Append(SpeedyPacketDataTypes.FloatNegativeOne);
						break;
					}
					case 0.5f:
					{
						h.Append(SpeedyPacketDataTypes.FloatHalf);
						break;
					}
					case 1f:
					{
						h.Append(SpeedyPacketDataTypes.FloatOne);
						break;
					}
					case 2f:
					{
						h.Append(SpeedyPacketDataTypes.FloatTwo);
						break;
					}
					case float.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.FloatMin);
						break;
					}
					case float.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.FloatMax);
						break;
					}
					case (float) Math.E:
					{
						h.Append(SpeedyPacketDataTypes.FloatE);
						break;
					}
					case (float) Math.PI:
					{
						h.Append(SpeedyPacketDataTypes.FloatPi);
						break;
					}
					case (float) (2 * Math.PI):
					{
						h.Append(SpeedyPacketDataTypes.FloatTau);
						break;
					}
					default:
					{
						if (float.IsNegativeInfinity(f))
						{
							h.Append(SpeedyPacketDataTypes.FloatNegativeInfinity);
						}
						else if (float.IsPositiveInfinity(f))
						{
							h.Append(SpeedyPacketDataTypes.FloatPositiveInfinity);
						}
						else if (float.IsNaN(f))
						{
							h.Append(SpeedyPacketDataTypes.FloatNaN);
						}
						else if (float.IsNegative(f) && (f == 0f))
						{
							h.Append(SpeedyPacketDataTypes.FloatNegativeZero);
						}
						else
						{
							h.Append(SpeedyPacketDataTypes.Float);
							NumberFloatWrite(f, d);
						}
						break;
					}
				}
			},
			[typeof(double)] = (v, h, d) =>
			{
				var db = (double) v;
				switch (db)
				{
					case -1.0d:
					{
						h.Append(SpeedyPacketDataTypes.DoubleNegativeOne);
						break;
					}
					case 0.5d:
					{
						h.Append(SpeedyPacketDataTypes.DoubleHalf);
						break;
					}
					case 1.0d:
					{
						h.Append(SpeedyPacketDataTypes.DoubleOne);
						break;
					}
					case 2.0d:
					{
						h.Append(SpeedyPacketDataTypes.DoubleTwo);
						break;
					}
					case double.NegativeZero:
					{
						h.Append(SpeedyPacketDataTypes.DoubleNegativeZero);
						break;
					}
					case double.MinValue:
					{
						h.Append(SpeedyPacketDataTypes.DoubleMin);
						break;
					}
					case double.MaxValue:
					{
						h.Append(SpeedyPacketDataTypes.DoubleMax);
						break;
					}
					case Math.E:
					{
						h.Append(SpeedyPacketDataTypes.DoubleE);
						break;
					}
					case Math.PI:
					{
						h.Append(SpeedyPacketDataTypes.DoublePi);
						break;
					}
					case 2 * Math.PI:
					{
						h.Append(SpeedyPacketDataTypes.DoubleTau);
						break;
					}
					default:
					{
						if (double.IsNegativeInfinity(db))
						{
							h.Append(SpeedyPacketDataTypes.DoubleNegativeInfinity);
						}
						else if (double.IsPositiveInfinity(db))
						{
							h.Append(SpeedyPacketDataTypes.DoublePositiveInfinity);
						}
						else if (double.IsNaN(db))
						{
							h.Append(SpeedyPacketDataTypes.DoubleNaN);
						}
						else if (double.IsNegative(db) && (db == 0d))
						{
							h.Append(SpeedyPacketDataTypes.DoubleNegativeZero);
						}
						else
						{
							h.Append(SpeedyPacketDataTypes.Double);
							NumberDoubleWrite(db, d);
						}
						break;
					}
				}
			},
			[typeof(decimal)] = (v, h, d) =>
			{
				var dec = (decimal) v;
				switch (dec)
				{
					case decimal.MinValue:
						h.Append(SpeedyPacketDataTypes.DecimalMin);
						break;
					case decimal.MaxValue:
						h.Append(SpeedyPacketDataTypes.DecimalMax);
						break;
					case decimal.Zero:
						h.Append(SpeedyPacketDataTypes.DecimalZero);
						break;
					case decimal.One:
						h.Append(SpeedyPacketDataTypes.DecimalOne);
						break;
					case decimal.MinusOne:
						h.Append(SpeedyPacketDataTypes.DecimalNegativeOne);
						break;
					default:
						h.Append(SpeedyPacketDataTypes.Decimal);
						DecimalWrite(dec, d);
						break;
				}
			},
			[typeof(DateOnly)] = (v, h, d) =>
			{
				var dt = (DateOnly) v;
				if (dt == DateOnly.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.DateOnlyMin);
				}
				else if (dt == DateOnly.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.DateOnlyMax);
				}
				else
				{
					switch (dt.DayNumber)
					{
						case DateTimeExtensions.UnixEpochDateOnlyDayNumber:
							h.Append(SpeedyPacketDataTypes.DateOnlyUnixEpoch);
							break;
						case DateTimeExtensions.WindowsEpochDateOnlyDayNumber:
							h.Append(SpeedyPacketDataTypes.DateOnlyWindowsEpoch);
							break;
						default:
							h.Append(SpeedyPacketDataTypes.DateOnly);
							NumberInt32Write(dt.DayNumber, d);
							break;
					}
				}
			},
			[typeof(DateTime)] = (v, h, d) =>
			{
				var dt = (DateTime) v;
				if (dt == DateTime.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.DateTimeMin);
				}
				else if (dt == DateTime.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.DateTimeMax);
				}
				else
				{
					switch (dt.Ticks)
					{
						case DateTimeExtensions.UnixEpochDateTimeTicks:
							h.Append(SpeedyPacketDataTypes.DateTimeUnixEpoch);
							break;
						case DateTimeExtensions.WindowsEpochDateTimeTicks:
							h.Append(SpeedyPacketDataTypes.DateTimeWindowsEpoch);
							break;
						default:
							h.Append(SpeedyPacketDataTypes.DateTime);
							NumberInt64Write(dt.ToUtcDateTime().Ticks, d);
							break;
					}
				}
			},
			[typeof(DateTimeOffset)] = (v, h, d) =>
			{
				var dto = (DateTimeOffset) v;
				if (dto == DateTimeOffset.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.DateTimeOffsetMin);
				}
				else if (dto == DateTimeOffset.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.DateTimeOffsetMax);
				}
				else
				{
					switch (dto.Ticks)
					{
						case DateTimeExtensions.UnixEpochDateTimeTicks:
							h.Append(SpeedyPacketDataTypes.DateTimeOffsetUnixEpoch);
							break;
						case DateTimeExtensions.WindowsEpochDateTimeTicks:
							h.Append(SpeedyPacketDataTypes.DateTimeOffsetWindowsEpoch);
							break;
						default:
							h.Append(SpeedyPacketDataTypes.DateTimeOffset);
							NumberInt64Write(dto.Ticks, d);
							NumberInt64Write(dto.Offset.Ticks, d);
							break;
					}
				}
			},
			[typeof(TimeOnly)] = (v, h, d) =>
			{
				var to = (TimeOnly) v;
				if (to == TimeOnly.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.TimeOnlyMin);
				}
				else if (to == TimeOnly.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.TimeOnlyMax);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.TimeOnly);
					NumberInt64Write(to.Ticks, d);
				}
			},
			[typeof(TimeSpan)] = (v, h, d) =>
			{
				var ts = (TimeSpan) v;
				if (ts == TimeSpan.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.TimeSpanMin);
				}
				else if (ts == TimeSpan.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.TimeSpanMax);
				}
				else if (ts == TimeSpan.Zero)
				{
					h.Append(SpeedyPacketDataTypes.TimeSpanZero);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.TimeSpan);
					NumberInt64Write(ts.Ticks, d);
				}
			},
			[typeof(Guid)] = (v, h, d) =>
			{
				var g = (Guid) v;
				if (g == Guid.Empty)
				{
					h.Append(SpeedyPacketDataTypes.GuidEmpty);
				}
				else if (g == Guid.AllBitsSet)
				{
					h.Append(SpeedyPacketDataTypes.GuidAllBitsSet);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.Guid);
					GuidWrite(g, d);
				}
			},
			[typeof(Version)] = (v, h, d) =>
			{
				var ver = (Version) v;
				switch (ver.Major)
				{
					case 1 when (ver.Minor == 0) && (ver.Build == -1) && (ver.Revision == -1):
						h.Append(SpeedyPacketDataTypes.VersionOneZero);
						return;
					case 1 when (ver.Minor == 0) && (ver.Build == 0) && (ver.Revision == -1):
						h.Append(SpeedyPacketDataTypes.VersionOneZeroZero);
						return;
					case 1 when (ver.Minor == 0) && (ver.Build == 0) && (ver.Revision == 0):
						h.Append(SpeedyPacketDataTypes.VersionOneZeroZeroZero);
						return;
				}

				h.Append(SpeedyPacketDataTypes.Version);
				NumberInt32Write(ver.Major, d);
				NumberInt32Write(ver.Minor, d);
				NumberInt32Write(ver.Build, d);
				NumberInt32Write(ver.Revision, d);
			},
			[typeof(SpeedyPacket)] = (v, h, d) =>
			{
				h.Append(SpeedyPacketDataTypes.Packet);
				WriteInternal(true, ((SpeedyPacket) v).ToArray(), d);
			}
		};
	}

	#endregion

	#region Methods

	public static int Write(IEnumerable<object> values, SpeedyBuffer<byte> destination)
	{
		return WriteInternal(false, values, destination);
	}

	internal static void SerializeToBuffers(
		IEnumerable<object> values,
		SpeedyBuffer<SpeedyPacketDataTypes> header,
		SpeedyBuffer<byte> data)
	{
		foreach (var value in values)
		{
			var current = value;
			if (current is Enum eValue)
			{
				current = ConvertToUnderlyingType(eValue);
			}

			if (current == null)
			{
				header.Append(SpeedyPacketDataTypes.Null);
				continue;
			}

			var currentType = current.GetType();
			if (_serializers.TryGetValue(currentType, out var serializer))
			{
				serializer(current, header, data);
				continue;
			}

			switch (current)
			{
				case IPackable packable:
					header.Append(SpeedyPacketDataTypes.Packet);
					WriteInternal(true, packable.ToSpeedyPacket(), data);
					continue;
				case byte[] byteArray:
					header.Append(SpeedyPacketDataTypes.ByteArray);
					ByteArrayWrite(byteArray, data);
					continue;
				case IEnumerable enumerable when currentType != typeof(string):
					header.Append(SpeedyPacketDataTypes.Packet);
					WriteInternal(true, enumerable.Cast<object>(), data);
					continue;
				default:
					Debugger.Break();
					throw new NotImplementedException($"{currentType} is not supported.");
			}
		}
	}

	private static void ByteArrayWrite(ReadOnlySpan<byte> value, SpeedyBuffer<byte> data)
	{
		NumberInt32Write(value.Length, data);
		data.Append(value);
	}

	private static bool ContainsNonAsciiOrControl(ReadOnlySpan<char> span)
	{
		for (var i = 0; i < span.Length; i++)
		{
			var c = span[i];
			if (NeedsEncoding(c))
			{
				return true;
			}
		}
		return false;
	}

	private static object ConvertToUnderlyingType<T>(T enumValue) where T : Enum
	{
		return Type.GetTypeCode(Enum.GetUnderlyingType(enumValue.GetType())) switch
		{
			TypeCode.Byte => (byte) (object) enumValue,
			TypeCode.SByte => (sbyte) (object) enumValue,
			TypeCode.Int16 => (short) (object) enumValue,
			TypeCode.UInt16 => (ushort) (object) enumValue,
			TypeCode.Int32 => (int) (object) enumValue,
			TypeCode.UInt32 => (uint) (object) enumValue,
			TypeCode.Int64 => (long) (object) enumValue,
			TypeCode.UInt64 => (ulong) (object) enumValue,
			_ => throw new NotSupportedException($"Unsupported enum underlying type: {Enum.GetUnderlyingType(typeof(T))}")
		};
	}

	private static void DecimalWrite(decimal value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[16]; // decimal is 16 bytes
		Span<int> bits = decimal.GetBits(value);
		BinaryPrimitives.WriteInt32LittleEndian(buffer[..4], bits[0]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4, 4), bits[1]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(8, 4), bits[2]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(12, 4), bits[3]);
		data.Append(buffer);
	}

	private static void GuidWrite(Guid value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[16];
		value.TryWriteBytes(buffer, false, out _);
		data.Append(buffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool NeedsEncoding(char c)
	{
		return ((c < 32) && (c != '\n') && (c != '\r') && (c != '\t')) || (c > 127);
	}

	private static void NumberDoubleWrite(double value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[8];
		BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberFloatWrite(float value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[4];
		BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberInt128Write(Int128 value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[16];
		BinaryPrimitives.WriteInt128LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberInt16Write(short value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[2];
		BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberInt32Write(int value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[4];
		BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberInt64Write(long value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[8];
		BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberUInt128Write(UInt128 value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[16];
		BinaryPrimitives.WriteUInt128LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberUInt16Write(ushort value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[2];
		BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberUInt32Write(uint value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static void NumberUInt64Write(ulong value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[8];
		BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static string StringEncode(string input)
	{
		var span = input.AsSpan();
		if (string.IsNullOrEmpty(input) || !ContainsNonAsciiOrControl(span))
		{
			return input;
		}

		var length = Encoding.UTF8.GetByteCount(span);
		using var rented = StringBuilderPool.Rent(length);
		var builder = rented.Value;

		for (var i = 0; i < span.Length; i++)
		{
			var c = span[i];
			if (NeedsEncoding(c))
			{
				builder.Append($"\\u{(int) c:X4}");
			}
			else
			{
				builder.Append(c);
			}
		}
		var response = builder.ToString();
		StringBuilderPool.Return(builder);
		return response;
	}

	private static void StringWrite(string value, SpeedyBuffer<byte> data)
	{
		// Must have enough room for encoding \u1234 per character
		var buffer = ArrayPool<byte>.Shared.Rent(value.Length * 6);
		try
		{
			var encoded = StringEncode(value);
			var bytesWritten = Encoding.UTF8.GetBytes(encoded, buffer);
			data.Append(AsciiCharacters.StartOfText);
			data.Append(buffer.AsSpan(0, bytesWritten));
			data.Append(AsciiCharacters.EndOfText);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private static int WriteInternal(bool prefixLength, IEnumerable<object> values, SpeedyBuffer<byte> destination)
	{
		destination.Clear();

		using var header = _headerPool.Rent(16384);
		using var data = _dataPool.Rent(16384);

		SerializeToBuffers(values, header, data);
		header.Append(SpeedyPacketDataTypes.EndOfHeader);

		var length = header.Length + data.Length;

		if (prefixLength)
		{
			NumberInt32Write(length, destination);
			length += 4;
		}

		var headerBytes = MemoryMarshal.Cast<SpeedyPacketDataTypes, byte>(header.AsSpan());

		destination.Append(headerBytes);
		destination.Append(data.AsSpan());

		return length;
	}

	#endregion

	#region Delegates

	private delegate void SerializerDelegate(object value, SpeedyBuffer<SpeedyPacketDataTypes> header, SpeedyBuffer<byte> data);

	#endregion
}