#region References

using System;

#endregion

namespace Cornerstone.Data.Bytes;

public readonly partial struct ByteSize
{
	public static ByteSize FromBits(byte value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(byte? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(sbyte value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(sbyte? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(short value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(short? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(ushort value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(ushort? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(int value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(int? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(uint value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(uint? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(long value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(long? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(ulong value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(ulong? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(IntPtr value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(IntPtr? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(UIntPtr value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(UIntPtr? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(decimal value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(decimal? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(double value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(double? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(float value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(float? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBytes(byte value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(byte? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(sbyte value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(sbyte? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(short value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(short? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(ushort value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(ushort? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(int value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(int? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(uint value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(uint? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(long value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(long? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(ulong value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(ulong? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(IntPtr value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(IntPtr? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(UIntPtr value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(UIntPtr? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(decimal value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(decimal? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(double value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(double? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(float value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(float? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromKilobits(byte value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(byte? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(sbyte value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(sbyte? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(short value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(short? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(ushort value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(ushort? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(int value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(int? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(uint value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(uint? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(long value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(long? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(ulong value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(ulong? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(IntPtr value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(IntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(UIntPtr value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(UIntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(decimal value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(decimal? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(double value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(double? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(float value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(float? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobytes(byte value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(byte? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(sbyte value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(sbyte? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(short value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(short? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(ushort value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(ushort? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(int value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(int? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(uint value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(uint? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(long value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(long? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(ulong value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(ulong? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(IntPtr value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(IntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(UIntPtr value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(UIntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(decimal value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(decimal? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(double value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(double? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(float value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(float? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromMegabits(byte value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(byte? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(sbyte value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(sbyte? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(short value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(short? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(ushort value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(ushort? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(int value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(int? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(uint value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(uint? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(long value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(long? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(ulong value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(ulong? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(IntPtr value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(IntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(UIntPtr value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(UIntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(decimal value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(decimal? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(double value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(double? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(float value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(float? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabytes(byte value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(byte? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(sbyte value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(sbyte? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(short value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(short? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(ushort value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(ushort? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(int value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(int? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(uint value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(uint? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(long value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(long? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(ulong value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(ulong? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(IntPtr value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(IntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(UIntPtr value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(UIntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(decimal value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(decimal? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(double value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(double? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(float value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(float? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromGigabits(byte value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(byte? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(sbyte value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(sbyte? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(short value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(short? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(ushort value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(ushort? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(int value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(int? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(uint value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(uint? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(long value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(long? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(ulong value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(ulong? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(IntPtr value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(IntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(UIntPtr value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(UIntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(decimal value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(decimal? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(double value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(double? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(float value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(float? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabytes(byte value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(byte? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(sbyte value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(sbyte? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(short value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(short? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(ushort value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(ushort? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(int value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(int? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(uint value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(uint? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(long value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(long? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(ulong value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(ulong? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(IntPtr value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(IntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(UIntPtr value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(UIntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(decimal value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(decimal? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(double value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(double? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(float value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(float? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromTerabits(byte value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(byte? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(sbyte value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(sbyte? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(short value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(short? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(ushort value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(ushort? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(int value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(int? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(uint value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(uint? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(long value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(long? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(ulong value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(ulong? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(IntPtr value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(IntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(UIntPtr value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(UIntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(decimal value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(decimal? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(double value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(double? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(float value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(float? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabytes(byte value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(byte? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(sbyte value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(sbyte? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(short value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(short? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(ushort value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(ushort? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(int value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(int? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(uint value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(uint? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(long value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(long? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(ulong value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(ulong? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(IntPtr value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(IntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(UIntPtr value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(UIntPtr? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(decimal value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(decimal? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(double value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(double? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(float value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(float? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	
	public static ByteSize FromBits(Int128 value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(Int128? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBits(UInt128 value)
	{
		return new ByteSize((decimal) value / BitsInByte);
	}
	public static ByteSize FromBits(UInt128? value)
	{
		return new ByteSize((decimal?) value / BitsInByte);
	}
	public static ByteSize FromBytes(Int128 value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(Int128? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromBytes(UInt128 value)
	{
		return new ByteSize((decimal) value);
	}
	public static ByteSize FromBytes(UInt128? value)
	{
		return new ByteSize((decimal?) value);
	}
	public static ByteSize FromKilobits(Int128 value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(Int128? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(UInt128 value)
	{
		return new ByteSize((decimal) value * BytesInKilobit);
	}
	public static ByteSize FromKilobits(UInt128? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobit);
	}
	public static ByteSize FromKilobytes(Int128 value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(Int128? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(UInt128 value)
	{
		return new ByteSize((decimal) value * BytesInKilobyte);
	}
	public static ByteSize FromKilobytes(UInt128? value)
	{
		return new ByteSize((decimal?) value * BytesInKilobyte);
	}
	public static ByteSize FromMegabits(Int128 value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(Int128? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(UInt128 value)
	{
		return new ByteSize((decimal) value * BytesInMegabit);
	}
	public static ByteSize FromMegabits(UInt128? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabit);
	}
	public static ByteSize FromMegabytes(Int128 value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(Int128? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(UInt128 value)
	{
		return new ByteSize((decimal) value * BytesInMegabyte);
	}
	public static ByteSize FromMegabytes(UInt128? value)
	{
		return new ByteSize((decimal?) value * BytesInMegabyte);
	}
	public static ByteSize FromGigabits(Int128 value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(Int128? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(UInt128 value)
	{
		return new ByteSize((decimal) value * BytesInGigabit);
	}
	public static ByteSize FromGigabits(UInt128? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabit);
	}
	public static ByteSize FromGigabytes(Int128 value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(Int128? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(UInt128 value)
	{
		return new ByteSize((decimal) value * BytesInGigabyte);
	}
	public static ByteSize FromGigabytes(UInt128? value)
	{
		return new ByteSize((decimal?) value * BytesInGigabyte);
	}
	public static ByteSize FromTerabits(Int128 value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(Int128? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(UInt128 value)
	{
		return new ByteSize((decimal) value * BytesInTerabit);
	}
	public static ByteSize FromTerabits(UInt128? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabit);
	}
	public static ByteSize FromTerabytes(Int128 value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(Int128? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(UInt128 value)
	{
		return new ByteSize((decimal) value * BytesInTerabyte);
	}
	public static ByteSize FromTerabytes(UInt128? value)
	{
		return new ByteSize((decimal?) value * BytesInTerabyte);
	}
}