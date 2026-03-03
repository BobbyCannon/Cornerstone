#region References

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

#pragma warning disable IL2067

namespace Cornerstone.Serialization;

public class SpeedyPacket : IReadOnlyList<object>
{
	#region Fields

	private static readonly SpeedyBufferPool<byte> _dataPool;
	private static readonly SpeedyBufferPool<SpeedyPacketDataTypes> _headerPool;
	private readonly List<object> _list;
	private static readonly Dictionary<Type, SerializerDelegate> _serializers;
	private static readonly Dictionary<Type, TypeMetadata> _typeMetadataCache;
	private static readonly Type _typeOfObject;

	#endregion

	#region Constructors

	public SpeedyPacket() : this([])
	{
	}

	public SpeedyPacket(object[] values)
	{
		_list = new List<object>(values);
	}

	static SpeedyPacket()
	{
		_typeMetadataCache = new();
		_dataPool = new();
		_headerPool = new();
		_typeOfObject = typeof(object);

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
				if (c == char.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.CharMin);
				}
				else if (c == char.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.CharMax);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.Char);
					NumberUInt16Write(c, d);
				}
			},
			[typeof(string)] = (v, h, d) =>
			{
				var s = (string) v;
				if (s == string.Empty)
				{
					h.Append(SpeedyPacketDataTypes.EmptyString);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.String);
					StringWrite(s, d);
				}
			},
			[typeof(byte)] = (v, h, d) =>
			{
				var b = (byte) v;
				if (b == byte.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.ByteMin);
				}
				else if (b == byte.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.ByteMax);
				}
				else if (b == 1)
				{
					h.Append(SpeedyPacketDataTypes.ByteOne);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.Byte);
					d.Append(b);
				}
			},
			[typeof(sbyte)] = (v, h, d) =>
			{
				var sb = (sbyte) v;
				if (sb == sbyte.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.SByteMin);
				}
				else if (sb == sbyte.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.SByteMax);
				}
				else if (sb == 0)
				{
					h.Append(SpeedyPacketDataTypes.SByteZero);
				}
				else if (sb == 1)
				{
					h.Append(SpeedyPacketDataTypes.SByteOne);
				}
				else if (sb == -1)
				{
					h.Append(SpeedyPacketDataTypes.SByteNegativeOne);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.SByte);
					d.Append((byte) sb);
				}
			},
			[typeof(short)] = (v, h, d) =>
			{
				var s = (short) v;
				if (s == short.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.Int16Min);
				}
				else if (s == short.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.Int16Max);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.Int16);
					NumberInt16Write(s, d);
				}
			},
			[typeof(ushort)] = (v, h, d) =>
			{
				var us = (ushort) v;
				if (us == ushort.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.UInt16Min);
				}
				else if (us == ushort.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.UInt16Max);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.UInt16);
					NumberUInt16Write(us, d);
				}
			},
			[typeof(int)] = (v, h, d) =>
			{
				var i = (int) v;
				if (i == int.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.Int32Min);
				}
				else if (i == int.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.Int32Max);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.Int32);
					NumberInt32Write(i, d);
				}
			},
			[typeof(uint)] = (v, h, d) =>
			{
				var ui = (uint) v;
				if (ui == uint.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.UInt32Min);
				}
				else if (ui == uint.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.UInt32Max);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.UInt32);
					NumberUInt32Write(ui, d);
				}
			},
			[typeof(long)] = (v, h, d) =>
			{
				var l = (long) v;
				if (l == long.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.Int64Min);
				}
				else if (l == long.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.Int64Max);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.Int64);
					NumberInt64Write(l, d);
				}
			},
			[typeof(ulong)] = (v, h, d) =>
			{
				var ul = (ulong) v;
				if (ul == ulong.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.UInt64Min);
				}
				else if (ul == ulong.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.UInt64Max);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.UInt64);
					NumberUInt64Write(ul, d);
				}
			},
			[typeof(Int128)] = (v, h, d) =>
			{
				var i128 = (Int128) v;
				if (i128 == Int128.MinValue)
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
				if (ui128 == UInt128.MinValue)
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
				if (dec == decimal.MinValue)
				{
					h.Append(SpeedyPacketDataTypes.DecimalMin);
				}
				else if (dec == decimal.MaxValue)
				{
					h.Append(SpeedyPacketDataTypes.DecimalMax);
				}
				else if (dec == decimal.Zero)
				{
					h.Append(SpeedyPacketDataTypes.DecimalZero);
				}
				else if (dec == decimal.One)
				{
					h.Append(SpeedyPacketDataTypes.DecimalOne);
				}
				else if (dec == decimal.MinusOne)
				{
					h.Append(SpeedyPacketDataTypes.DecimalMinusOne);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.Decimal);
					DecimalWrite(dec, d);
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
				else if (dt.DayNumber == DateTimeExtensions.UnixEpochDateOnlyDayNumber)
				{
					h.Append(SpeedyPacketDataTypes.DateOnlyUnixEpoch);
				}
				else if (dt.DayNumber == DateTimeExtensions.WindowsEpochDateOnlyDayNumber)
				{
					h.Append(SpeedyPacketDataTypes.DateOnlyWindowsEpoch);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.DateOnly);
					NumberInt32Write(dt.DayNumber, d);
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
				else if (dt.Ticks == DateTimeExtensions.UnixEpochDateTimeTicks)
				{
					h.Append(SpeedyPacketDataTypes.DateTimeUnixEpoch);
				}
				else if (dt.Ticks == DateTimeExtensions.WindowsEpochDateTimeTicks)
				{
					h.Append(SpeedyPacketDataTypes.DateTimeWindowsEpoch);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.DateTime);
					NumberInt64Write(dt.ToUtcDateTime().Ticks, d);
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
				else if (dto.Ticks == DateTimeExtensions.UnixEpochDateTimeTicks)
				{
					h.Append(SpeedyPacketDataTypes.DateTimeOffsetUnixEpoch);
				}
				else if (dto.Ticks == DateTimeExtensions.WindowsEpochDateTimeTicks)
				{
					h.Append(SpeedyPacketDataTypes.DateTimeOffsetWindowsEpoch);
				}
				else
				{
					h.Append(SpeedyPacketDataTypes.DateTimeOffset);
					NumberInt64Write(dto.Ticks, d);
					NumberInt64Write(dto.Offset.Ticks, d);
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
				h.Append(SpeedyPacketDataTypes.Version);
				NumberInt32Write(ver.Major, d);
				NumberInt32Write(ver.Minor, d);
				NumberInt32Write(ver.Build, d);
				NumberInt32Write(ver.Revision, d);
			},
			[typeof(SpeedyPacket)] = (v, h, d) =>
			{
				h.Append(SpeedyPacketDataTypes.Packet);
				PacketWrite(((SpeedyPacket) v).ToArray(), d);
			}
		};
	}

	#endregion

	#region Properties

	public int Count => _list.Count;

	public object this[int index] => _list[index];

	#endregion

	#region Methods

	public void FromByteArray(ReadOnlySpan<byte> data)
	{
		var packet = Unpack(data);
		_list.Clear();
		_list.AddRange(packet._list);
	}

	public IEnumerator<object> GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	public static ReadOnlySpan<byte> Pack(object value)
	{
		if (value is IPackable packable)
		{
			return Pack(packable.ToSpeedyPacket());
		}

		if (value
			is IEnumerable enumerable
			and not string)
		{
			return Pack(enumerable);
		}

		return Pack([value]);
	}

	public static ReadOnlySpan<byte> Pack(IEnumerable values)
	{
		return Pack(values.Cast<object>());
	}

	/// <summary>
	/// Convert the message to a byte array.
	/// </summary>
	/// <returns> The bytes that represents the message. </returns>
	public static ReadOnlySpan<byte> Pack<T>(IEnumerable<T> values)
	{
		var header = _headerPool.Rent(16384);
		var data = _dataPool.Rent(16384);

		try
		{
			foreach (var value in values)
			{
				object current = value;
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
				}
				else if (current is IPackable packable)
				{
					header.Append(SpeedyPacketDataTypes.Packet);
					PacketWrite(packable.ToSpeedyPacket(), data);
				}
				else if (current is byte[] byteArray)
				{
					header.Append(SpeedyPacketDataTypes.ByteArray);
					ByteArrayWrite(byteArray, data);
				}
				else if (current is IEnumerable enumerable
						&& (currentType != typeof(string)))
				{
					header.Append(SpeedyPacketDataTypes.Packet);
					PacketWrite(enumerable, data);
				}
				else
				{
					Debugger.Break();
					throw new NotImplementedException($"{currentType} is not supported.");
				}
			}

			header.Append(SpeedyPacketDataTypes.EndOfHeader);

			var response = new byte[header.Length + data.Length];
			var headerSpan = MemoryMarshal.Cast<SpeedyPacketDataTypes, byte>(header.AsSpan());
			var dataSpan = data.AsSpan();
			headerSpan.CopyTo(response.AsSpan(0, header.Length));
			dataSpan.CopyTo(response.AsSpan(header.Length, data.Length));

			return response;
		}
		finally
		{
			_headerPool.Return(header);
			_dataPool.Return(data);
		}
	}

	public ReadOnlySpan<byte> ToByteArray()
	{
		return Pack(_list);
	}

	public static object Unpack(ReadOnlySpan<byte> value, Type type)
	{
		var packet = Unpack(value);
		return ParseType(type, packet);
	}

	public static SpeedyPacket Unpack(ReadOnlySpan<byte> value)
	{
		var header = new SpeedyBuffer<SpeedyPacketDataTypes>();
		var index = 0;

		while ((index < value.Length)
				&& (value[index] != (byte) SpeedyPacketDataTypes.EndOfHeader))
		{
			header.Append((SpeedyPacketDataTypes) value[index++]);
		}

		index++;
		var response = new SpeedyPacket();

		foreach (var type in header.ToArray())
		{
			switch (type)
			{
				case SpeedyPacketDataTypes.ByteArray:
				{
					response.Add(ByteArrayRead(value, ref index).ToArray());
					break;
				}
				case SpeedyPacketDataTypes.Packet:
				{
					response.Add(PacketRead(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.Null:
				{
					response.Add(null);
					break;
				}
				case SpeedyPacketDataTypes.True:
				{
					response.Add(true);
					break;
				}
				case SpeedyPacketDataTypes.False:
				{
					response.Add(false);
					break;
				}
				case SpeedyPacketDataTypes.EmptyString:
				{
					response.Add(string.Empty);
					break;
				}
				case SpeedyPacketDataTypes.String:
				{
					response.Add(StringRead(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.CharMin:
				{
					response.Add(char.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.CharMax:
				{
					response.Add(char.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.Char:
				{
					response.Add((char) NumberUInt16Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.ByteMin:
				{
					response.Add(byte.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.ByteMax:
				{
					response.Add(byte.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.ByteOne:
				{
					response.Add((byte) 1);
					break;
				}
				case SpeedyPacketDataTypes.Byte:
				{
					response.Add(value[index++]);
					break;
				}
				case SpeedyPacketDataTypes.SByteMin:
				{
					response.Add(sbyte.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.SByteMax:
				{
					response.Add(sbyte.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.SByteZero:
				{
					response.Add((sbyte) 0);
					break;
				}
				case SpeedyPacketDataTypes.SByteOne:
				{
					response.Add((sbyte) 1);
					break;
				}
				case SpeedyPacketDataTypes.SByteNegativeOne:
				{
					response.Add((sbyte) -1);
					break;
				}
				case SpeedyPacketDataTypes.SByte:
				{
					response.Add((sbyte) value[index++]);
					break;
				}
				case SpeedyPacketDataTypes.Int16Max:
				{
					response.Add(short.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.Int16Min:
				{
					response.Add(short.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.Int16:
				{
					response.Add(NumberInt16Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.UInt16Max:
				{
					response.Add(ushort.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.UInt16Min:
				{
					response.Add(ushort.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.UInt16:
				{
					response.Add(NumberUInt16Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.Int32Max:
				{
					response.Add(int.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.Int32Min:
				{
					response.Add(int.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.Int32:
				{
					response.Add(NumberInt32Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.UInt32Min:
				{
					response.Add(uint.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.UInt32Max:
				{
					response.Add(uint.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.UInt32:
				{
					response.Add(NumberUInt32Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.Int64Min:
				{
					response.Add(long.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.Int64Max:
				{
					response.Add(long.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.Int64:
				{
					response.Add(NumberInt64Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.UInt64Min:
				{
					response.Add(ulong.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.UInt64Max:
				{
					response.Add(ulong.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.UInt64:
				{
					response.Add(NumberUInt64Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.Int128Min:
				{
					response.Add(Int128.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.Int128Max:
				{
					response.Add(Int128.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.Int128:
				{
					response.Add(NumberInt128Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.UInt128Min:
				{
					response.Add(UInt128.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.UInt128Max:
				{
					response.Add(UInt128.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.UInt128:
				{
					response.Add(NumberUInt128Read(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.FloatMin:
				{
					response.Add(float.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.FloatMax:
				{
					response.Add(float.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.FloatNegativeInfinity:
				{
					response.Add(float.NegativeInfinity);
					break;
				}
				case SpeedyPacketDataTypes.FloatPositiveInfinity:
				{
					response.Add(float.PositiveInfinity);
					break;
				}
				case SpeedyPacketDataTypes.FloatNaN:
				{
					response.Add(float.NaN);
					break;
				}
				case SpeedyPacketDataTypes.FloatNegativeZero:
				{
					response.Add(float.NegativeZero);
					break;
				}
				case SpeedyPacketDataTypes.FloatE:
				{
					response.Add(float.E);
					break;
				}
				case SpeedyPacketDataTypes.FloatEpsilon:
				{
					response.Add(float.Epsilon);
					break;
				}
				case SpeedyPacketDataTypes.FloatPi:
				{
					response.Add(float.Pi);
					break;
				}
				case SpeedyPacketDataTypes.FloatTau:
				{
					response.Add(float.Tau);
					break;
				}
				case SpeedyPacketDataTypes.Float:
				{
					response.Add(NumberFloatRead(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.DoubleMin:
				{
					response.Add(double.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.DoubleMax:
				{
					response.Add(double.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.DoubleNegativeInfinity:
				{
					response.Add(double.NegativeInfinity);
					break;
				}
				case SpeedyPacketDataTypes.DoublePositiveInfinity:
				{
					response.Add(double.PositiveInfinity);
					break;
				}
				case SpeedyPacketDataTypes.DoubleNaN:
				{
					response.Add(double.NaN);
					break;
				}
				case SpeedyPacketDataTypes.DoubleNegativeZero:
				{
					response.Add(double.NegativeZero);
					break;
				}
				case SpeedyPacketDataTypes.DoubleE:
				{
					response.Add(double.E);
					break;
				}
				case SpeedyPacketDataTypes.DoubleEpsilon:
				{
					response.Add(double.Epsilon);
					break;
				}
				case SpeedyPacketDataTypes.DoublePi:
				{
					response.Add(double.Pi);
					break;
				}
				case SpeedyPacketDataTypes.DoubleTau:
				{
					response.Add(double.Tau);
					break;
				}
				case SpeedyPacketDataTypes.Double:
				{
					response.Add(NumberDoubleRead(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.DecimalMin:
				{
					response.Add(decimal.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.DecimalMax:
				{
					response.Add(decimal.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.DecimalZero:
				{
					response.Add(decimal.Zero);
					break;
				}
				case SpeedyPacketDataTypes.DecimalOne:
				{
					response.Add(decimal.One);
					break;
				}
				case SpeedyPacketDataTypes.DecimalMinusOne:
				{
					response.Add(decimal.MinusOne);
					break;
				}
				case SpeedyPacketDataTypes.Decimal:
				{
					response.Add(DecimalRead(value, ref index));
					break;
				}
				case SpeedyPacketDataTypes.DateOnlyMin:
				{
					response.Add(DateOnly.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.DateOnlyMax:
				{
					response.Add(DateOnly.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.DateOnlyUnixEpoch:
				{
					response.Add(DateTimeExtensions.UnixEpochDateOnly);
					break;
				}
				case SpeedyPacketDataTypes.DateOnlyWindowsEpoch:
				{
					response.Add(DateTimeExtensions.WindowsEpochDateOnly);
					break;
				}
				case SpeedyPacketDataTypes.DateOnly:
				{
					response.Add(DateOnly.FromDayNumber(NumberInt32Read(value, ref index)));
					break;
				}
				case SpeedyPacketDataTypes.DateTimeMin:
				{
					response.Add(DateTime.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.DateTimeMax:
				{
					response.Add(DateTime.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.DateTimeUnixEpoch:
				{
					response.Add(DateTimeExtensions.UnixEpochDateTime);
					break;
				}
				case SpeedyPacketDataTypes.DateTimeWindowsEpoch:
				{
					response.Add(DateTimeExtensions.WindowsEpochDateTime);
					break;
				}
				case SpeedyPacketDataTypes.DateTime:
				{
					response.Add(new DateTime(NumberInt64Read(value, ref index), DateTimeKind.Utc));
					break;
				}
				case SpeedyPacketDataTypes.DateTimeOffsetMin:
				{
					response.Add(DateTimeOffset.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.DateTimeOffsetMax:
				{
					response.Add(DateTimeOffset.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.DateTimeOffsetUnixEpoch:
				{
					response.Add(DateTimeExtensions.UnixEpochDateTimeOffset);
					break;
				}
				case SpeedyPacketDataTypes.DateTimeOffsetWindowsEpoch:
				{
					response.Add(DateTimeExtensions.WindowsEpochDateTimeOffset);
					break;
				}
				case SpeedyPacketDataTypes.DateTimeOffset:
				{
					var ticks = NumberInt64Read(value, ref index);
					var offsetTicks = NumberInt64Read(value, ref index);
					response.Add(new DateTimeOffset(ticks, new TimeSpan(offsetTicks)));
					break;
				}
				case SpeedyPacketDataTypes.TimeOnlyMin:
				{
					response.Add(TimeOnly.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.TimeOnlyMax:
				{
					response.Add(TimeOnly.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.TimeOnly:
				{
					response.Add(new TimeOnly(NumberInt64Read(value, ref index)));
					break;
				}
				case SpeedyPacketDataTypes.TimeSpanMin:
				{
					response.Add(TimeSpan.MinValue);
					break;
				}
				case SpeedyPacketDataTypes.TimeSpanMax:
				{
					response.Add(TimeSpan.MaxValue);
					break;
				}
				case SpeedyPacketDataTypes.TimeSpanZero:
				{
					response.Add(TimeSpan.Zero);
					break;
				}
				case SpeedyPacketDataTypes.TimeSpan:
				{
					response.Add(new TimeSpan(NumberInt64Read(value, ref index)));
					break;
				}
				case SpeedyPacketDataTypes.GuidAllBitsSet:
				{
					response.Add(Guid.AllBitsSet);
					break;
				}
				case SpeedyPacketDataTypes.GuidEmpty:
				{
					response.Add(Guid.Empty);
					break;
				}
				case SpeedyPacketDataTypes.Guid:
				{
					var bytes = value.Slice(index, 16);
					index += 16;
					response.Add(new Guid(bytes, false));
					break;
				}
				case SpeedyPacketDataTypes.Version:
				{
					var major = NumberInt32Read(value, ref index);
					var minor = NumberInt32Read(value, ref index);
					var build = NumberInt32Read(value, ref index);
					var revision = NumberInt32Read(value, ref index);
					if (build == -1)
					{
						response.Add(new Version(major, minor));
					}
					else if (revision == -1)
					{
						response.Add(new Version(major, minor, build));
					}
					else
					{
						response.Add(new Version(major, minor, build, revision));
					}
					break;
				}
				default:
				{
					Debugger.Break();
					throw new NotImplementedException($"{type} is not supported.");
				}
			}
		}

		return response;
	}

	public T Unpack<T>()
		where T : IPackable, new()
	{
		var response = new T();
		response.FromSpeedyPacket(this);
		return response;
	}

	private void Add(object value)
	{
		_list.Add(value);
	}

	private static ReadOnlySpan<byte> ByteArrayRead(ReadOnlySpan<byte> data, ref int index)
	{
		var length = NumberInt32Read(data, ref index);
		var value = data.Slice(index, length);
		index += length;
		return value;
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

	private static decimal DecimalRead(ReadOnlySpan<byte> data, ref int index)
	{
		var span = data.Slice(index, 16); // decimal is 16 bytes
		Span<int> bits = stackalloc int[4];
		if (BitConverter.IsLittleEndian)
		{
			bits[0] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(0, 4));
			bits[1] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(4, 4));
			bits[2] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(8, 4));
			bits[3] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(12, 4));
		}
		else
		{
			Span<byte> buffer = stackalloc byte[16];
			span.CopyTo(buffer);
			buffer.Reverse();
			bits[0] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(0, 4));
			bits[1] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4, 4));
			bits[2] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(8, 4));
			bits[3] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(12, 4));
		}
		index += 16;
		return new decimal(bits);
	}

	private static void DecimalWrite(decimal value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[16]; // decimal is 16 bytes
		Span<int> bits = decimal.GetBits(value);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(0, 4), bits[0]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4, 4), bits[1]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(8, 4), bits[2]);
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(12, 4), bits[3]);
		data.Append(buffer);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
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

	private static double NumberDoubleRead(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadDoubleLittleEndian(data[index..]);
		index += 8;
		return value;
	}

	private static void NumberDoubleWrite(double value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[8];
		BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static float NumberFloatRead(ReadOnlySpan<byte> data, ref int index)
	{
		var span = data.Slice(index, 4);
		var value = BitConverter.IsLittleEndian
			? BitConverter.ToSingle(span)
			: BitConverter.ToSingle(span.ToArray().AsEnumerable().Reverse().ToArray());
		index += 4;
		return value;
	}

	private static void NumberFloatWrite(float value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[4];
		if (!BitConverter.TryWriteBytes(buffer, value))
		{
			throw new InvalidOperationException("Failed to write float.");
		}
		if (!BitConverter.IsLittleEndian)
		{
			buffer.Reverse();
		}
		data.Append(buffer);
	}

	private static Int128 NumberInt128Read(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadInt128LittleEndian(data.Slice(index));
		index += 16;
		return value;
	}

	private static void NumberInt128Write(Int128 value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[16];
		BinaryPrimitives.WriteInt128LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static short NumberInt16Read(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(index));
		index += 2;
		return value;
	}

	private static void NumberInt16Write(short value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[2];
		BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static int NumberInt32Read(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(index));
		index += 4;
		return value;
	}

	private static void NumberInt32Write(int value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[4];
		BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static long NumberInt64Read(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(index));
		index += 8;
		return value;
	}

	private static void NumberInt64Write(long value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[8];
		BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static UInt128 NumberUInt128Read(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadUInt128LittleEndian(data.Slice(index));
		index += 16;
		return value;
	}

	private static void NumberUInt128Write(UInt128 value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[16];
		BinaryPrimitives.WriteUInt128LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static ushort NumberUInt16Read(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(index));
		index += 2;
		return value;
	}

	private static void NumberUInt16Write(ushort value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[2];
		BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static uint NumberUInt32Read(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(index));
		index += 4;
		return value;
	}

	private static void NumberUInt32Write(uint value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static ulong NumberUInt64Read(ReadOnlySpan<byte> data, ref int index)
	{
		var value = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(index));
		index += 8;
		return value;
	}

	private static void NumberUInt64Write(ulong value, SpeedyBuffer<byte> data)
	{
		Span<byte> buffer = stackalloc byte[8];
		BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
		data.Append(buffer);
	}

	private static SpeedyPacket PacketRead(ReadOnlySpan<byte> data, ref int index)
	{
		var length = NumberInt32Read(data, ref index);
		var packetData = data.Slice(index, length);
		index += length;
		return Unpack(packetData);
	}

	private static void PacketWrite(IEnumerable value, SpeedyBuffer<byte> data)
	{
		var packetData = Pack(value);
		NumberInt32Write(packetData.Length, data);
		data.Append(packetData);
	}

	private static object ParseType(Type type, object value)
	{
		if (value == null)
		{
			return null;
		}

		// Check cache or compute metadata
		if (!_typeMetadataCache.TryGetValue(type, out var metadata))
		{
			metadata = new TypeMetadata(type);
			_typeMetadataCache[type] = metadata;
		}

		if ((metadata.Type == _typeOfObject)
			|| (value.GetType() == metadata.Type))
		{
			return value;
		}

		var packet = value as SpeedyPacket;
		if ((packet != null) && metadata.IsPackable)
		{
			var response = (IPackable) Activator.CreateInstance(type);
			response?.FromSpeedyPacket(packet);
			return response;
		}

		// Single value packet
		if (packet is { Count: 1 } && (packet[0].GetType() == type))
		{
			return packet[0];
		}

		if ((packet != null) && metadata.IsEnumerable)
		{
			var newCollection = SourceReflector.CreateCollectionInstance(type, metadata.ArrayType, packet.Count);

			switch (newCollection)
			{
				case Array newArray:
				{
					for (var i = 0; i < packet.Count; i++)
					{
						newArray.SetValue(ParseType(metadata.ArrayType, packet[i]), i);
					}
					return newArray;
				}
				case IList newList:
				{
					foreach (var element in packet)
					{
						newList.Add(ParseType(metadata.ArrayType, element));
					}
					return newList;
				}
				default:
				{
					throw new InvalidOperationException($"Unsupported collection type: {newCollection.GetType().Name}");
				}
			}
		}

		throw new NotImplementedException($"This type [{type}] is not supported.");
	}

	private static string StringDecode(string encoded)
	{
		if (string.IsNullOrEmpty(encoded) || !encoded.Contains("\\u"))
		{
			return encoded;
		}

		var span = encoded.AsSpan();
		using var rented = StringBuilderPool.Rent(encoded.Length);
		var builder = rented.Value;
		var i = 0;

		while (i < span.Length)
		{
			if (((i + 5) < span.Length) && (span[i] == '\\') && (span[i + 1] == 'u') &&
				int.TryParse(span.Slice(i + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
			{
				builder.Append((char) code);
				i += 6;
			}
			else
			{
				builder.Append(span[i]);
				i++;
			}
		}

		var response = builder.ToString();
		StringBuilderPool.Return(builder);
		return response;
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

	private static string StringRead(ReadOnlySpan<byte> data, ref int index)
	{
		var start = data[index] == AsciiCharacters.StartOfText ? index + 1 : -1;
		while ((start >= 0) && (++index < data.Length))
		{
			if (data[index] == AsciiCharacters.EndOfText)
			{
				var encoded = Encoding.UTF8.GetString(data.Slice(start, index - start));
				index++;
				return StringDecode(encoded);
			}
		}

		throw new InvalidDataContractException("Reading binary string failed.");
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

	#endregion

	#region Classes

	private class TypeMetadata
	{
		#region Constructors

		public TypeMetadata(Type type)
		{
			IsPackable = typeof(IPackable).IsAssignableFrom(type);
			IsEnumerable = typeof(IEnumerable).IsAssignableFrom(type) && (type != typeof(string));
			ArrayType = IsEnumerable ? Serializer.GetArrayType(type) : null;
			Type = type;

			// Handle Nullable<T>
			var typeGeneric = type?.IsGenericType == true ? type.GetGenericTypeDefinition() : null;
			if (typeGeneric == typeof(Nullable<>))
			{
				Type = Nullable.GetUnderlyingType(type) ?? type;
			}
		}

		#endregion

		#region Properties

		public Type ArrayType { get; }
		public bool IsEnumerable { get; }
		public bool IsPackable { get; }
		public Type Type { get; }

		#endregion
	}

	#endregion

	#region Delegates

	private delegate void SerializerDelegate(object value, SpeedyBuffer<SpeedyPacketDataTypes> header, SpeedyBuffer<byte> data);

	#endregion
}