#region References

using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization;

public static class SpeedyPackWriter
{
	#region Fields

	private static readonly SpeedyListPool<byte> _dataPool;
	private static readonly SpeedyListPool<SpeedyPacketDataTypes> _headerPool;
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
				h.Add(b ? SpeedyPacketDataTypes.True : SpeedyPacketDataTypes.False);
			},
			[typeof(char)] = (v, h, d) =>
			{
				var c = (char) v;
				switch (c)
				{
					case char.MinValue:
					{
						h.Add(SpeedyPacketDataTypes.CharMin);
						break;
					}
					case char.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.CharMax);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.Char);
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
						h.Add(SpeedyPacketDataTypes.Null);
						break;
					}
					case "":
					{
						h.Add(SpeedyPacketDataTypes.StringOfEmpty);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.String);
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
						h.Add(SpeedyPacketDataTypes.ByteMin);
						break;
					}
					case byte.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.ByteMax);
						break;
					}
					case 1:
					{
						h.Add(SpeedyPacketDataTypes.ByteOne);
						break;
					}
					case 2:
					{
						h.Add(SpeedyPacketDataTypes.ByteTwo);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.Byte);
						d.Add(b);
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
						h.Add(SpeedyPacketDataTypes.SByteMin);
						break;
					}
					case sbyte.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.SByteMax);
						break;
					}
					case 0:
					{
						h.Add(SpeedyPacketDataTypes.SByteZero);
						break;
					}
					case 1:
					{
						h.Add(SpeedyPacketDataTypes.SByteOne);
						break;
					}
					case 2:
					{
						h.Add(SpeedyPacketDataTypes.SByteTwo);
						break;
					}
					case -1:
					{
						h.Add(SpeedyPacketDataTypes.SByteNegativeOne);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.SByte);
						d.Add((byte) sb);
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
						h.Add(SpeedyPacketDataTypes.Int16Min);
						break;
					}
					case short.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.Int16Max);
						break;
					}
					case -1:
					{
						h.Add(SpeedyPacketDataTypes.Int16NegativeOne);
						break;
					}
					case 0:
					{
						h.Add(SpeedyPacketDataTypes.Int16Zero);
						break;
					}
					case 1:
					{
						h.Add(SpeedyPacketDataTypes.Int16One);
						break;
					}
					case 2:
					{
						h.Add(SpeedyPacketDataTypes.Int16Two);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.Int16);
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
						h.Add(SpeedyPacketDataTypes.UInt16Min);
						break;
					}
					case ushort.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.UInt16Max);
						break;
					}
					case 1:
					{
						h.Add(SpeedyPacketDataTypes.UInt16One);
						break;
					}
					case 2:
					{
						h.Add(SpeedyPacketDataTypes.UInt16Two);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.UInt16);
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
						h.Add(SpeedyPacketDataTypes.Int32Min);
						break;
					}
					case int.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.Int32Max);
						break;
					}
					case -1:
					{
						h.Add(SpeedyPacketDataTypes.Int32NegativeOne);
						break;
					}
					case 0:
					{
						h.Add(SpeedyPacketDataTypes.Int32Zero);
						break;
					}
					case 1:
					{
						h.Add(SpeedyPacketDataTypes.Int32One);
						break;
					}
					case 2:
					{
						h.Add(SpeedyPacketDataTypes.Int32Two);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.Int32);
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
						h.Add(SpeedyPacketDataTypes.UInt32Min);
						break;
					}
					case uint.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.UInt32Max);
						break;
					}
					case 1:
					{
						h.Add(SpeedyPacketDataTypes.UInt32One);
						break;
					}
					case 2:
					{
						h.Add(SpeedyPacketDataTypes.UInt32Two);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.UInt32);
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
						h.Add(SpeedyPacketDataTypes.Int64Zero);
						break;
					}
					case -1:
					{
						h.Add(SpeedyPacketDataTypes.Int64NegativeOne);
						break;
					}
					case long.MinValue:
					{
						h.Add(SpeedyPacketDataTypes.Int64Min);
						break;
					}
					case long.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.Int64Max);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.Int64);
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
						h.Add(SpeedyPacketDataTypes.UInt64One);
						break;
					}
					case 2:
					{
						h.Add(SpeedyPacketDataTypes.UInt64Two);
						break;
					}
					case ulong.MinValue:
					{
						h.Add(SpeedyPacketDataTypes.UInt64Min);
						break;
					}
					case ulong.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.UInt64Max);
						break;
					}
					default:
					{
						h.Add(SpeedyPacketDataTypes.UInt64);
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
					h.Add(SpeedyPacketDataTypes.Int128NegativeOne);
				}
				else if (i128 == 0)
				{
					h.Add(SpeedyPacketDataTypes.Int128Zero);
				}
				else if (i128 == 1)
				{
					h.Add(SpeedyPacketDataTypes.Int128One);
				}
				else if (i128 == 2)
				{
					h.Add(SpeedyPacketDataTypes.Int128Two);
				}
				else if (i128 == Int128.MinValue)
				{
					h.Add(SpeedyPacketDataTypes.Int128Min);
				}
				else if (i128 == Int128.MaxValue)
				{
					h.Add(SpeedyPacketDataTypes.Int128Max);
				}
				else
				{
					h.Add(SpeedyPacketDataTypes.Int128);
					NumberInt128Write(i128, d);
				}
			},
			[typeof(UInt128)] = (v, h, d) =>
			{
				var ui128 = (UInt128) v;
				if (ui128 == 1)
				{
					h.Add(SpeedyPacketDataTypes.UInt128One);
				}
				else if (ui128 == 2)
				{
					h.Add(SpeedyPacketDataTypes.UInt128Two);
				}
				else if (ui128 == UInt128.MinValue)
				{
					h.Add(SpeedyPacketDataTypes.UInt128Min);
				}
				else if (ui128 == UInt128.MaxValue)
				{
					h.Add(SpeedyPacketDataTypes.UInt128Max);
				}
				else
				{
					h.Add(SpeedyPacketDataTypes.UInt128);
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
						h.Add(SpeedyPacketDataTypes.FloatNegativeOne);
						break;
					}
					case 0.5f:
					{
						h.Add(SpeedyPacketDataTypes.FloatHalf);
						break;
					}
					case 1f:
					{
						h.Add(SpeedyPacketDataTypes.FloatOne);
						break;
					}
					case 2f:
					{
						h.Add(SpeedyPacketDataTypes.FloatTwo);
						break;
					}
					case float.MinValue:
					{
						h.Add(SpeedyPacketDataTypes.FloatMin);
						break;
					}
					case float.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.FloatMax);
						break;
					}
					case (float) Math.E:
					{
						h.Add(SpeedyPacketDataTypes.FloatE);
						break;
					}
					case (float) Math.PI:
					{
						h.Add(SpeedyPacketDataTypes.FloatPi);
						break;
					}
					case (float) (2 * Math.PI):
					{
						h.Add(SpeedyPacketDataTypes.FloatTau);
						break;
					}
					default:
					{
						if (float.IsNegativeInfinity(f))
						{
							h.Add(SpeedyPacketDataTypes.FloatNegativeInfinity);
						}
						else if (float.IsPositiveInfinity(f))
						{
							h.Add(SpeedyPacketDataTypes.FloatPositiveInfinity);
						}
						else if (float.IsNaN(f))
						{
							h.Add(SpeedyPacketDataTypes.FloatNaN);
						}
						else if (float.IsNegative(f) && (f == 0f))
						{
							h.Add(SpeedyPacketDataTypes.FloatNegativeZero);
						}
						else
						{
							h.Add(SpeedyPacketDataTypes.Float);
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
						h.Add(SpeedyPacketDataTypes.DoubleNegativeOne);
						break;
					}
					case 0.5d:
					{
						h.Add(SpeedyPacketDataTypes.DoubleHalf);
						break;
					}
					case 1.0d:
					{
						h.Add(SpeedyPacketDataTypes.DoubleOne);
						break;
					}
					case 2.0d:
					{
						h.Add(SpeedyPacketDataTypes.DoubleTwo);
						break;
					}
					case double.NegativeZero:
					{
						h.Add(SpeedyPacketDataTypes.DoubleNegativeZero);
						break;
					}
					case double.MinValue:
					{
						h.Add(SpeedyPacketDataTypes.DoubleMin);
						break;
					}
					case double.MaxValue:
					{
						h.Add(SpeedyPacketDataTypes.DoubleMax);
						break;
					}
					case Math.E:
					{
						h.Add(SpeedyPacketDataTypes.DoubleE);
						break;
					}
					case Math.PI:
					{
						h.Add(SpeedyPacketDataTypes.DoublePi);
						break;
					}
					case 2 * Math.PI:
					{
						h.Add(SpeedyPacketDataTypes.DoubleTau);
						break;
					}
					default:
					{
						if (double.IsNegativeInfinity(db))
						{
							h.Add(SpeedyPacketDataTypes.DoubleNegativeInfinity);
						}
						else if (double.IsPositiveInfinity(db))
						{
							h.Add(SpeedyPacketDataTypes.DoublePositiveInfinity);
						}
						else if (double.IsNaN(db))
						{
							h.Add(SpeedyPacketDataTypes.DoubleNaN);
						}
						else if (double.IsNegative(db) && (db == 0d))
						{
							h.Add(SpeedyPacketDataTypes.DoubleNegativeZero);
						}
						else
						{
							h.Add(SpeedyPacketDataTypes.Double);
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
						h.Add(SpeedyPacketDataTypes.DecimalMin);
						break;
					case decimal.MaxValue:
						h.Add(SpeedyPacketDataTypes.DecimalMax);
						break;
					case decimal.Zero:
						h.Add(SpeedyPacketDataTypes.DecimalZero);
						break;
					case decimal.One:
						h.Add(SpeedyPacketDataTypes.DecimalOne);
						break;
					case decimal.MinusOne:
						h.Add(SpeedyPacketDataTypes.DecimalNegativeOne);
						break;
					default:
						h.Add(SpeedyPacketDataTypes.Decimal);
						DecimalWrite(dec, d);
						break;
				}
			},
			[typeof(DateOnly)] = (v, h, d) =>
			{
				var dt = (DateOnly) v;
				if (dt == DateOnly.MinValue)
				{
					h.Add(SpeedyPacketDataTypes.DateOnlyMin);
				}
				else if (dt == DateOnly.MaxValue)
				{
					h.Add(SpeedyPacketDataTypes.DateOnlyMax);
				}
				else
				{
					switch (dt.DayNumber)
					{
						case DateTimeExtensions.UnixEpochDateOnlyDayNumber:
							h.Add(SpeedyPacketDataTypes.DateOnlyUnixEpoch);
							break;
						case DateTimeExtensions.WindowsEpochDateOnlyDayNumber:
							h.Add(SpeedyPacketDataTypes.DateOnlyWindowsEpoch);
							break;
						default:
							h.Add(SpeedyPacketDataTypes.DateOnly);
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
					h.Add(SpeedyPacketDataTypes.DateTimeMin);
				}
				else if (dt == DateTime.MaxValue)
				{
					h.Add(SpeedyPacketDataTypes.DateTimeMax);
				}
				else
				{
					switch (dt.Ticks)
					{
						case DateTimeExtensions.UnixEpochDateTimeTicks:
							h.Add(SpeedyPacketDataTypes.DateTimeUnixEpoch);
							break;
						case DateTimeExtensions.WindowsEpochDateTimeTicks:
							h.Add(SpeedyPacketDataTypes.DateTimeWindowsEpoch);
							break;
						default:
							h.Add(SpeedyPacketDataTypes.DateTime);
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
					h.Add(SpeedyPacketDataTypes.DateTimeOffsetMin);
				}
				else if (dto == DateTimeOffset.MaxValue)
				{
					h.Add(SpeedyPacketDataTypes.DateTimeOffsetMax);
				}
				else
				{
					switch (dto.Ticks)
					{
						case DateTimeExtensions.UnixEpochDateTimeTicks:
							h.Add(SpeedyPacketDataTypes.DateTimeOffsetUnixEpoch);
							break;
						case DateTimeExtensions.WindowsEpochDateTimeTicks:
							h.Add(SpeedyPacketDataTypes.DateTimeOffsetWindowsEpoch);
							break;
						default:
							h.Add(SpeedyPacketDataTypes.DateTimeOffset);
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
					h.Add(SpeedyPacketDataTypes.TimeOnlyMin);
				}
				else if (to == TimeOnly.MaxValue)
				{
					h.Add(SpeedyPacketDataTypes.TimeOnlyMax);
				}
				else
				{
					h.Add(SpeedyPacketDataTypes.TimeOnly);
					NumberInt64Write(to.Ticks, d);
				}
			},
			[typeof(TimeSpan)] = (v, h, d) =>
			{
				var ts = (TimeSpan) v;
				if (ts == TimeSpan.MinValue)
				{
					h.Add(SpeedyPacketDataTypes.TimeSpanMin);
				}
				else if (ts == TimeSpan.MaxValue)
				{
					h.Add(SpeedyPacketDataTypes.TimeSpanMax);
				}
				else if (ts == TimeSpan.Zero)
				{
					h.Add(SpeedyPacketDataTypes.TimeSpanZero);
				}
				else
				{
					h.Add(SpeedyPacketDataTypes.TimeSpan);
					NumberInt64Write(ts.Ticks, d);
				}
			},
			[typeof(Guid)] = (v, h, d) =>
			{
				var g = (Guid) v;
				if (g == Guid.Empty)
				{
					h.Add(SpeedyPacketDataTypes.GuidEmpty);
				}
				else if (g == Guid.AllBitsSet)
				{
					h.Add(SpeedyPacketDataTypes.GuidAllBitsSet);
				}
				else
				{
					h.Add(SpeedyPacketDataTypes.Guid);
					GuidWrite(g, d);
				}
			},
			[typeof(Version)] = (v, h, d) =>
			{
				var ver = (Version) v;
				switch (ver.Major)
				{
					case 1 when (ver.Minor == 0) && (ver.Build == -1) && (ver.Revision == -1):
						h.Add(SpeedyPacketDataTypes.VersionOneZero);
						return;
					case 1 when (ver.Minor == 0) && (ver.Build == 0) && (ver.Revision == -1):
						h.Add(SpeedyPacketDataTypes.VersionOneZeroZero);
						return;
					case 1 when (ver.Minor == 0) && (ver.Build == 0) && (ver.Revision == 0):
						h.Add(SpeedyPacketDataTypes.VersionOneZeroZeroZero);
						return;
				}

				h.Add(SpeedyPacketDataTypes.Version);
				NumberInt32Write(ver.Major, d);
				NumberInt32Write(ver.Minor, d);
				NumberInt32Write(ver.Build, d);
				NumberInt32Write(ver.Revision, d);
			},
			[typeof(SpeedyPacket)] = (v, h, d) =>
			{
				h.Add(SpeedyPacketDataTypes.Packet);
				WriteInternal(true, ((SpeedyPacket) v).ToArray(), d);
			}
		};
	}

	#endregion

	#region Methods

	public static int Write(IEnumerable<object> values, SpeedyList<byte> destination)
	{
		return WriteInternal(false, values, destination);
	}

	internal static void SerializeToBuffers(
		IEnumerable<object> values,
		SpeedyList<SpeedyPacketDataTypes> header,
		SpeedyList<byte> data)
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
				header.Add(SpeedyPacketDataTypes.Null);
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
				{
					header.Add(SpeedyPacketDataTypes.Packet);
					WriteInternal(true, packable.ToSpeedyPacket(), data);
					continue;
				}
				case byte[] byteArray:
				{
					header.Add(SpeedyPacketDataTypes.ByteArray);
					ByteArrayWrite(byteArray, data);
					continue;
				}
				case IEnumerable enumerable when currentType != typeof(string):
				{
					header.Add(SpeedyPacketDataTypes.Packet);
					WriteInternal(true, enumerable.Cast<object>(), data);
					continue;
				}
				default:
				{
					#if DEBUG
					if (Debugger.IsAttached)
					{
						Debugger.Break();
					}
					#endif
					throw new NotImplementedException($"{currentType} is not supported.");
				}
			}
		}
	}

	private static void ByteArrayWrite(ReadOnlySpan<byte> value, SpeedyList<byte> data)
	{
		NumberInt32Write(value.Length, data);
		data.Add(value);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void DecimalWrite(decimal value, SpeedyList<byte> data)
	{
		var buffer = data.GetWriteSpan(16);
		Span<int> bits = decimal.GetBits(value);
		BinaryPrimitives.WriteInt32LittleEndian(buffer[..4], bits[0]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4, 4), bits[1]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(8, 4), bits[2]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(12, 4), bits[3]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void GuidWrite(Guid value, SpeedyList<byte> data)
	{
		value.TryWriteBytes(data.GetWriteSpan(16), false, out _);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool NeedsEncoding(char c)
	{
		return ((c < 32) && (c != '\n') && (c != '\r') && (c != '\t')) || (c > 127);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberDoubleWrite(double value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteDoubleLittleEndian(data.GetWriteSpan(8), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberFloatWrite(float value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteSingleLittleEndian(data.GetWriteSpan(4), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberInt128Write(Int128 value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteInt128LittleEndian(data.GetWriteSpan(16), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberInt16Write(short value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteInt16LittleEndian(data.GetWriteSpan(2), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberInt32Write(int value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteInt32LittleEndian(data.GetWriteSpan(4), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberInt64Write(long value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteInt64LittleEndian(data.GetWriteSpan(8), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberUInt128Write(UInt128 value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteUInt128LittleEndian(data.GetWriteSpan(16), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberUInt16Write(ushort value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteUInt16LittleEndian(data.GetWriteSpan(2), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberUInt32Write(uint value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteUInt32LittleEndian(data.GetWriteSpan(4), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NumberUInt64Write(ulong value, SpeedyList<byte> data)
	{
		BinaryPrimitives.WriteUInt64LittleEndian(data.GetWriteSpan(8), value);
	}

	private static void StringWrite(string value, SpeedyList<byte> data)
	{
		data.Add(AsciiCharacters.StartOfText);
		var span = value.AsSpan();

		// Fast path: if entirely ASCII-safe, bulk-copy without per-char branching
		if (!ContainsNonAsciiOrControl(span))
		{
			var dest = data.GetWriteSpan(span.Length);
			for (var i = 0; i < span.Length; i++)
			{
				dest[i] = (byte) span[i];
			}
		}
		else
		{
			// Slow path: escape non-ASCII / control characters
			for (var i = 0; i < span.Length; i++)
			{
				var c = span[i];
				if (NeedsEncoding(c))
				{
					data.Add((byte) '\\');
					data.Add((byte) 'u');
					var code = (int) c;
					for (var shift = 12; shift >= 0; shift -= 4)
					{
						var digit = (code >> shift) & 0xF;
						data.Add((byte) (digit < 10 ? '0' + digit : ('A' + digit) - 10));
					}
				}
				else
				{
					data.Add((byte) c);
				}
			}
		}

		data.Add(AsciiCharacters.EndOfText);
	}

	private static int WriteInternal(bool prefixLength, IEnumerable<object> values, SpeedyList<byte> destination)
	{
		var header = _headerPool.Rent(16384);
		var data = _dataPool.Rent(16384);

		try
		{
			SerializeToBuffers(values, header, data);
			header.Add(SpeedyPacketDataTypes.EndOfHeader);

			var length = header.Count + data.Count;

			if (prefixLength)
			{
				NumberInt32Write(length, destination);
				length += 4;
			}

			var headerBytes = MemoryMarshal.Cast<SpeedyPacketDataTypes, byte>(header.AsSpan());

			destination.Add(headerBytes);
			destination.Add(data.AsSpan());

			return length;
		}
		finally
		{
			_headerPool.Return(header);
			_dataPool.Return(data);
		}
	}

	#endregion

	#region Delegates

	private delegate void SerializerDelegate(object value, SpeedyList<SpeedyPacketDataTypes> header, SpeedyList<byte> data);

	#endregion
}