#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization;

public class SpeedyPacket : IReadOnlyList<object>
{
	#region Fields

	private readonly List<object> _list;
	private static Dictionary<Type, TypeMetadata> _typeMetadataCache;
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
		_typeOfObject = typeof(object);
	}

	#endregion

	#region Properties

	public int Count => _list.Count;

	public object this[int index] => _list[index];

	#endregion

	#region Methods

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

	public static byte[] Pack(IEnumerable<object> values)
	{
		using var buffer = new SpeedyList<byte>(16384);
		SpeedyPackWriter.Write(values, buffer);
		return buffer.ToArray();
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
		var reader = new SpeedyPackReader(value);
		return Unpack(ref reader);
	}

	public T Unpack<T>()
		where T : IPackable, new()
	{
		var response = new T();
		response.FromSpeedyPacket(this);
		return response;
	}

	internal void Add(object value)
	{
		_list.Add(value);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private static object ParseType(Type type, object value)
	{
		if (value == null)
		{
			return null;
		}

		// Lock-free read: the dictionary reference is always a complete, valid snapshot
		var cache = Volatile.Read(ref _typeMetadataCache)!;
		if (!cache.TryGetValue(type, out var metadata))
		{
			metadata = new TypeMetadata(type);
			// Copy-on-write: create a new dictionary with the added entry and swap it in
			var updated = new Dictionary<Type, TypeMetadata>(cache) { [type] = metadata };
			// If another thread won the race, that's fine — we still use our local metadata
			Interlocked.CompareExchange(ref _typeMetadataCache, updated, cache);
		}

		if ((metadata.Type == _typeOfObject)
			|| (value.GetType() == metadata.Type))
		{
			return value;
		}

		var packet = value as SpeedyPacket;
		if ((packet != null) && metadata.IsPackable)
		{
			var response = (IPackable) SourceReflector.CreateInstance(type);
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

	private static SpeedyPacket Unpack(ref SpeedyPackReader reader)
	{
		var response = new SpeedyPacket();

		while (reader.TryPeekHeaderType(out var type))
		{
			switch (type)
			{
				case SpeedyPacketDataTypes.Null: response.Add(null); break;
				case SpeedyPacketDataTypes.True: response.Add(true); break;
				case SpeedyPacketDataTypes.False: response.Add(false); break;
				case SpeedyPacketDataTypes.StringOfEmpty: response.Add(string.Empty); break;
				case SpeedyPacketDataTypes.String:
				{
					if (!reader.TryReadString(out var utf8Bytes))
					{
						throw new InvalidDataContractException("Failed to read string.");
					}
					var encoded = Encoding.UTF8.GetString(utf8Bytes);
					response.Add(StringDecode(encoded));
					continue;
				}
				case SpeedyPacketDataTypes.ByteArray:
				{
					if (!reader.TryReadLengthPrefixedBytes(out var payload))
					{
						throw new InvalidDataContractException("Failed to read byte array.");
					}
					response.Add(payload.ToArray());
					break;
				}
				case SpeedyPacketDataTypes.Packet:
				{
					if (!reader.TryReadNestedPacket(out var subReader))
					{
						throw new InvalidDataContractException("Failed to read nested packet.");
					}
					response.Add(Unpack(ref subReader));
					continue;
				}
				case SpeedyPacketDataTypes.CharMin:
				case SpeedyPacketDataTypes.CharMax:
				case SpeedyPacketDataTypes.Char:
				{
					if (!reader.TryReadChar(out var c))
					{
						throw new InvalidDataContractException("Failed to read Char.");
					}
					response.Add(c);
					continue;
				}
				case SpeedyPacketDataTypes.ByteMin:
				case SpeedyPacketDataTypes.ByteMax:
				case SpeedyPacketDataTypes.ByteOne:
				case SpeedyPacketDataTypes.ByteTwo:
				case SpeedyPacketDataTypes.Byte:
				{
					if (!reader.TryReadByte(out var b))
					{
						throw new InvalidDataContractException("Failed to read Byte.");
					}
					response.Add(b);
					continue;
				}
				case SpeedyPacketDataTypes.SByteMin:
				case SpeedyPacketDataTypes.SByteMax:
				case SpeedyPacketDataTypes.SByteNegativeOne:
				case SpeedyPacketDataTypes.SByteZero:
				case SpeedyPacketDataTypes.SByteOne:
				case SpeedyPacketDataTypes.SByteTwo:
				case SpeedyPacketDataTypes.SByte:
				{
					if (!reader.TryReadSByte(out var sb))
					{
						throw new InvalidDataContractException("Failed to read SByte.");
					}
					response.Add(sb);
					continue;
				}
				case SpeedyPacketDataTypes.Int16Min:
				case SpeedyPacketDataTypes.Int16Max:
				case SpeedyPacketDataTypes.Int16NegativeOne:
				case SpeedyPacketDataTypes.Int16Zero:
				case SpeedyPacketDataTypes.Int16One:
				case SpeedyPacketDataTypes.Int16Two:
				case SpeedyPacketDataTypes.Int16:
				{
					if (!reader.TryReadInt16(out var v))
					{
						throw new InvalidDataContractException("Failed to read Int16.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.UInt16Min:
				case SpeedyPacketDataTypes.UInt16Max:
				case SpeedyPacketDataTypes.UInt16One:
				case SpeedyPacketDataTypes.UInt16Two:
				case SpeedyPacketDataTypes.UInt16:
				{
					if (!reader.TryReadUInt16(out var v))
					{
						throw new InvalidDataContractException("Failed to read UInt16.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.Int32Min:
				case SpeedyPacketDataTypes.Int32Max:
				case SpeedyPacketDataTypes.Int32NegativeOne:
				case SpeedyPacketDataTypes.Int32Zero:
				case SpeedyPacketDataTypes.Int32One:
				case SpeedyPacketDataTypes.Int32Two:
				case SpeedyPacketDataTypes.Int32:
				{
					if (!reader.TryReadInt32(out var v))
					{
						throw new InvalidDataContractException("Failed to read Int32.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.UInt32Min:
				case SpeedyPacketDataTypes.UInt32Max:
				case SpeedyPacketDataTypes.UInt32One:
				case SpeedyPacketDataTypes.UInt32Two:
				case SpeedyPacketDataTypes.UInt32:
				{
					if (!reader.TryReadUInt32(out var v))
					{
						throw new InvalidDataContractException("Failed to read UInt32.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.Int64Min:
				case SpeedyPacketDataTypes.Int64Max:
				case SpeedyPacketDataTypes.Int64NegativeOne:
				case SpeedyPacketDataTypes.Int64Zero:
				case SpeedyPacketDataTypes.Int64One:
				case SpeedyPacketDataTypes.Int64Two:
				case SpeedyPacketDataTypes.Int64:
				{
					if (!reader.TryReadInt64(out var v))
					{
						throw new InvalidDataContractException("Failed to read Int64.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.UInt64Min:
				case SpeedyPacketDataTypes.UInt64Max:
				case SpeedyPacketDataTypes.UInt64One:
				case SpeedyPacketDataTypes.UInt64Two:
				case SpeedyPacketDataTypes.UInt64:
				{
					if (!reader.TryReadUInt64(out var v))
					{
						throw new InvalidDataContractException("Failed to read UInt64.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.Int128Min:
				case SpeedyPacketDataTypes.Int128Max:
				case SpeedyPacketDataTypes.Int128NegativeOne:
				case SpeedyPacketDataTypes.Int128Zero:
				case SpeedyPacketDataTypes.Int128One:
				case SpeedyPacketDataTypes.Int128Two:
				case SpeedyPacketDataTypes.Int128:
				{
					if (!reader.TryReadInt128(out var v))
					{
						throw new InvalidDataContractException("Failed to read Int128.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.UInt128Min:
				case SpeedyPacketDataTypes.UInt128Max:
				case SpeedyPacketDataTypes.UInt128One:
				case SpeedyPacketDataTypes.UInt128Two:
				case SpeedyPacketDataTypes.UInt128:
				{
					if (!reader.TryReadUInt128(out var v))
					{
						throw new InvalidDataContractException("Failed to read UInt128.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.FloatMin:
				case SpeedyPacketDataTypes.FloatMax:
				case SpeedyPacketDataTypes.FloatNegativeInfinity:
				case SpeedyPacketDataTypes.FloatPositiveInfinity:
				case SpeedyPacketDataTypes.FloatNaN:
				case SpeedyPacketDataTypes.FloatNegativeZero:
				case SpeedyPacketDataTypes.FloatZero:
				case SpeedyPacketDataTypes.FloatE:
				case SpeedyPacketDataTypes.FloatEpsilon:
				case SpeedyPacketDataTypes.FloatPi:
				case SpeedyPacketDataTypes.FloatTau:
				case SpeedyPacketDataTypes.FloatHalf:
				case SpeedyPacketDataTypes.FloatNegativeOne:
				case SpeedyPacketDataTypes.FloatOne:
				case SpeedyPacketDataTypes.FloatTwo:
				case SpeedyPacketDataTypes.Float:
				{
					if (!reader.TryReadFloat(out var v))
					{
						throw new InvalidDataContractException("Failed to read Float.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.DoubleMin:
				case SpeedyPacketDataTypes.DoubleMax:
				case SpeedyPacketDataTypes.DoubleNegativeInfinity:
				case SpeedyPacketDataTypes.DoublePositiveInfinity:
				case SpeedyPacketDataTypes.DoubleNaN:
				case SpeedyPacketDataTypes.DoubleNegativeZero:
				case SpeedyPacketDataTypes.DoubleZero:
				case SpeedyPacketDataTypes.DoubleE:
				case SpeedyPacketDataTypes.DoubleEpsilon:
				case SpeedyPacketDataTypes.DoublePi:
				case SpeedyPacketDataTypes.DoubleTau:
				case SpeedyPacketDataTypes.DoubleHalf:
				case SpeedyPacketDataTypes.DoubleNegativeOne:
				case SpeedyPacketDataTypes.DoubleOne:
				case SpeedyPacketDataTypes.DoubleTwo:
				case SpeedyPacketDataTypes.Double:
				{
					if (!reader.TryReadDouble(out var v))
					{
						throw new InvalidDataContractException("Failed to read Double.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.DecimalMin:
				case SpeedyPacketDataTypes.DecimalMax:
				case SpeedyPacketDataTypes.DecimalNegativeOne:
				case SpeedyPacketDataTypes.DecimalZero:
				case SpeedyPacketDataTypes.DecimalOne:
				case SpeedyPacketDataTypes.DecimalTwo:
				case SpeedyPacketDataTypes.Decimal:
				{
					if (!reader.TryReadDecimal(out var v))
					{
						throw new InvalidDataContractException("Failed to read Decimal.");
					}
					response.Add(v);
					continue;
				}
				case SpeedyPacketDataTypes.DateOnlyMin:
				case SpeedyPacketDataTypes.DateOnlyMax:
				case SpeedyPacketDataTypes.DateOnlyUnixEpoch:
				case SpeedyPacketDataTypes.DateOnlyWindowsEpoch:
				case SpeedyPacketDataTypes.DateOnly:
				{
					if (!reader.TryReadDateOnly(out var date))
					{
						throw new InvalidDataContractException("Failed to read DateOnly.");
					}
					response.Add(date);
					continue;
				}

				case SpeedyPacketDataTypes.DateTimeMin:
				case SpeedyPacketDataTypes.DateTimeMax:
				case SpeedyPacketDataTypes.DateTimeUnixEpoch:
				case SpeedyPacketDataTypes.DateTimeWindowsEpoch:
				case SpeedyPacketDataTypes.DateTime:
				{
					if (!reader.TryReadDateTime(out var dateTime))
					{
						throw new InvalidDataContractException("Failed to read DateTime.");
					}
					response.Add(dateTime);
					continue;
				}
				case SpeedyPacketDataTypes.DateTimeOffsetMin:
				case SpeedyPacketDataTypes.DateTimeOffsetMax:
				case SpeedyPacketDataTypes.DateTimeOffsetUnixEpoch:
				case SpeedyPacketDataTypes.DateTimeOffsetWindowsEpoch:
				case SpeedyPacketDataTypes.DateTimeOffset:
				{
					if (!reader.TryReadDateTimeOffset(out var dateTimeOffset))
					{
						throw new InvalidDataContractException("Failed to read DateTimeOffset.");
					}
					response.Add(dateTimeOffset);
					continue;
				}
				case SpeedyPacketDataTypes.TimeOnlyMin:
				case SpeedyPacketDataTypes.TimeOnlyMax:
				case SpeedyPacketDataTypes.TimeOnly:
				{
					if (!reader.TryReadTimeOnly(out var timeOnly))
					{
						throw new InvalidDataContractException("Failed to read TimeOnly.");
					}
					response.Add(timeOnly);
					continue;
				}
				case SpeedyPacketDataTypes.TimeSpanMin:
				case SpeedyPacketDataTypes.TimeSpanMax:
				case SpeedyPacketDataTypes.TimeSpanZero:
				case SpeedyPacketDataTypes.TimeSpan:
				{
					if (!reader.TryReadTimeSpan(out var timespan))
					{
						throw new InvalidDataContractException("Failed to read TimeSpan.");
					}
					response.Add(timespan);
					continue;
				}
				case SpeedyPacketDataTypes.GuidEmpty:
				case SpeedyPacketDataTypes.GuidAllBitsSet:
				case SpeedyPacketDataTypes.Guid:
				{
					if (!reader.TryReadGuid(out var guid))
					{
						throw new InvalidDataContractException("Failed to read Guid.");
					}
					response.Add(guid);
					continue;
				}
				case SpeedyPacketDataTypes.Version:
				case SpeedyPacketDataTypes.VersionOneZero:
				case SpeedyPacketDataTypes.VersionOneZeroZero:
				case SpeedyPacketDataTypes.VersionOneZeroZeroZero:
				{
					if (!reader.TryReadVersion(out var version))
					{
						throw new InvalidDataContractException("Failed to read Version.");
					}
					response.Add(version);
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
					throw new NotImplementedException($"{type} is not supported.");
				}
			}

			// Consume the header value for non-reader values
			reader.TryReadHeaderType(out type);
		}

		return response;
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
}