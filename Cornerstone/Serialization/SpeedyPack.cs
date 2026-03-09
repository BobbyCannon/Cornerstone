#region References

using System;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Serialization;

/// <summary>
/// A binary serializer.
/// </summary>
public class SpeedyPack
{
	#region Methods

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBoolean(SpeedyPacketDataTypes type)
	{
		return type is SpeedyPacketDataTypes.True or SpeedyPacketDataTypes.False;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsByte(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Byte and <= SpeedyPacketDataTypes.ByteTwo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsChar(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Char and <= SpeedyPacketDataTypes.CharMax;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDateOnly(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.DateOnly and <= SpeedyPacketDataTypes.DateOnlyWindowsEpoch;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDateTime(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.DateTime and <= SpeedyPacketDataTypes.DateTimeWindowsEpoch;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDateTimeOffset(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.DateTimeOffset and <= SpeedyPacketDataTypes.DateTimeOffsetWindowsEpoch;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDecimal(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Decimal and <= SpeedyPacketDataTypes.DecimalTwo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDouble(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Double and <= SpeedyPacketDataTypes.DoubleTwo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsFloat(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Float and <= SpeedyPacketDataTypes.FloatTwo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsGuid(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Guid and <= SpeedyPacketDataTypes.GuidAllBitsSet;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInt128(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Int128 and <= SpeedyPacketDataTypes.Int128Two;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInt16(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Int16 and <= SpeedyPacketDataTypes.Int16Two;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInt32(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Int32 and <= SpeedyPacketDataTypes.Int32Two;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInt64(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Int64 and <= SpeedyPacketDataTypes.Int64Two;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNullOrEmptyString(SpeedyPacketDataTypes type)
	{
		return type is SpeedyPacketDataTypes.String
			or SpeedyPacketDataTypes.StringOfEmpty;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSByte(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.SByte and <= SpeedyPacketDataTypes.SByteTwo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsTimeOnly(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.TimeOnly and <= SpeedyPacketDataTypes.TimeOnlyMax;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsTimeSpan(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.TimeSpan and <= SpeedyPacketDataTypes.TimeSpanZero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsUInt128(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.UInt128 and <= SpeedyPacketDataTypes.UInt128Two;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsUInt16(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.UInt16 and <= SpeedyPacketDataTypes.UInt16Two;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsUInt32(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.UInt32 and <= SpeedyPacketDataTypes.UInt32Two;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsUInt64(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.UInt64 and <= SpeedyPacketDataTypes.UInt64Two;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsVersion(SpeedyPacketDataTypes type)
	{
		return type is >= SpeedyPacketDataTypes.Version and <= SpeedyPacketDataTypes.VersionOneZeroZeroZero;
	}

	public static byte[] Pack(IPackable value)
	{
		return SpeedyPacket.Pack(value).ToArray();
	}

	public static T Unpack<T>(byte[] pack)
	{
		return (T) Unpack(pack, typeof(T));
	}

	public static object Unpack(byte[] pack, Type type)
	{
		return SpeedyPacket.Unpack(pack, type);
	}

	#endregion
}