namespace Cornerstone.Serialization;

/// <summary>
/// Packet header data types. This is the values listed in the header that describes
/// the data in the rest of the packet.
/// </summary>
/// <remarks>
/// Epoch means a specific point in time used as a reference for measuring time in a system, often the
/// starting point for timestamps (e.g., Unix Epoch, 1970-01-01).
/// </remarks>
public enum SpeedyPacketDataTypes : byte
{
	Unknown = 0x00,
	Packet = 0x01,
	True = 0x02,
	False = 0x03,
	Null = 0x04,
	String = 0x05,
	EmptyString = 0x06,
	Char = 0x07,

	/// <summary>
	/// 0
	/// </summary>
	CharMin = 0x08,

	/// <summary>
	/// 65535, 0xFFFF
	/// </summary>
	CharMax = 0x09,
	Byte = 0x0A,

	/// <summary>
	/// 0
	/// </summary>
	ByteMin = 0x0B,

	/// <summary>
	/// 255, 0xFF
	/// </summary>
	ByteMax = 0x0C,
	ByteOne = 0x0D,
	ByteArray = 0x0E,
	SByte = 0x0F,

	/// <summary>
	/// -128, 0x80
	/// </summary>
	SByteMin = 0x10,

	/// <summary>
	/// 127, 0x7F
	/// </summary>
	SByteMax = 0x11,
	SByteZero = 0x12,
	SByteOne = 0x13,
	SByteNegativeOne = 0x14,
	Int16 = 0x15,

	/// <summary>
	/// -32768, 0x8000
	/// </summary>
	Int16Min = 0x16,

	/// <summary>
	/// 32767, 0x7FFF
	/// </summary>
	Int16Max = 0x17,
	Int16Zero = 0x18,
	Int16One = 0x19,
	Int16NegativeOne = 0x1A,
	UInt16 = 0x1B,

	/// <summary>
	/// 0
	/// </summary>
	UInt16Min = 0x1C,

	/// <summary>
	/// 65535, 0xFFFF
	/// </summary>
	UInt16Max = 0x1D,
	Int32 = 0x1E,

	/// <summary>
	/// -2147483648, 0x80000000
	/// </summary>
	Int32Min = 0x1F,

	/// <summary>
	/// 2147483647, 0x7FFFFFFF
	/// </summary>
	Int32Max = 0x20,
	UInt32 = 0x21,
	UInt32Min = 0x22,

	/// <summary>
	/// 4294967295, 0xFFFFFFFF
	/// </summary>
	UInt32Max = 0x23,
	Int64 = 0x24,

	/// <summary>
	/// -9223372036854775808, 0x8000000000000000
	/// </summary>
	Int64Min = 0x25,

	/// <summary>
	/// 9223372036854775807, 0x7FFFFFFFFFFFFFFF
	/// </summary>
	Int64Max = 0x26,
	UInt64 = 0x27,
	UInt64Min = 0x28,

	/// <summary>
	/// 18446744073709551615, 0xFFFFFFFFFFFFFFFF
	/// </summary>
	UInt64Max = 0x29,
	Int128 = 0x2A,

	/// <summary>
	/// 170141183460469231731687303715884105727
	/// 7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF
	/// </summary>
	Int128Min = 0x2B,

	/// <summary>
	/// -170141183460469231731687303715884105728
	/// 80000000000000000000000000000000
	/// </summary>
	Int128Max = 0x2C,
	UInt128 = 0x2D,
	UInt128Min = 0x2E,

	/// <summary>
	/// 340282366920938463463374607431768211455
	/// 0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF
	/// </summary>
	UInt128Max = 0x2F,
	IntPtr = 0x30,

	/// <summary>
	/// -9223372036854775808
	/// 0x8000000000000000
	/// </summary>
	IntPtrMin = 0x31,

	/// <summary>
	/// 9223372036854775807
	/// 0x7FFFFFFFFFFFFFFF
	/// </summary>
	IntPtrMax = 0x32,
	UIntPtr = 0x33,
	UIntPtrMin = 0x34,

	/// <summary>
	/// 18446744073709551615
	/// 0xFFFFFFFFFFFFFFFF
	/// </summary>
	UIntPtrMax = 0x35,
	Float = 0x36,

	/// <summary>
	/// -3.4028235E+38
	/// </summary>
	FloatMin = 0x37,

	/// <summary>
	/// 3.4028235E+38
	/// </summary>
	FloatMax = 0x38,
	FloatNegativeInfinity = 0x39,
	FloatPositiveInfinity = 0x3A,
	FloatNaN = 0x3B,
	FloatNegativeZero = 0x3C,
	FloatE = 0x3D,
	FloatEpsilon = 0x3E,
	FloatPi = 0x3F,
	FloatTau = 0x40,
	Double = 0x41,

	/// <summary>
	/// -1.7976931348623157E+308
	/// </summary>
	DoubleMin = 0x42,

	/// <summary>
	/// 1.7976931348623157E+308
	/// </summary>
	DoubleMax = 0x43,
	DoubleZero = 0x44,
	DoubleNegativeInfinity = 0x45,
	DoublePositiveInfinity = 0x46,
	DoubleNaN = 0x47,
	DoubleNegativeZero = 0x48,
	DoubleE = 0x49,
	DoubleEpsilon = 0x4A,
	DoublePi = 0x4B,
	DoubleTau = 0x4C,
	Decimal = 0x4D,

	/// <summary>
	/// -79228162514264337593543950335
	/// </summary>
	DecimalMin = 0x4E,

	/// <summary>
	/// 79228162514264337593543950335
	/// </summary>
	DecimalMax = 0x4F,
	DecimalZero = 0x50,
	DecimalOne = 0x51,
	DecimalMinusOne = 0x52,
	DateOnly = 0x53,

	/// <summary>
	/// 1/1/0001
	/// </summary>
	DateOnlyMin = 0x54,

	/// <summary>
	/// 12/31/9999
	/// </summary>
	DateOnlyMax = 0x55,

	/// <summary>
	/// 1970-01-01 UTC
	/// </summary>
	DateOnlyUnixEpoch = 0x56,

	/// <summary>
	/// 1601-01-01 UTC
	/// </summary>
	DateOnlyWindowsEpoch = 0x57,
	DateTime = 0x58,

	/// <summary>
	/// 1/1/0001 12:00:00 AM
	/// </summary>
	DateTimeMin = 0x59,

	/// <summary>
	/// 12/31/9999 11:59:59 PM
	/// </summary>
	DateTimeMax = 0x5A,
	DateTimeUnixEpoch = 0x5B,
	DateTimeWindowsEpoch = 0x5C,
	DateTimeOffset = 0x5D,

	/// <summary>
	/// 1/1/0001 12:00:00 AM +00:00
	/// </summary>
	DateTimeOffsetMin = 0x5E,

	/// <summary>
	/// 12/31/9999 11:59:59 PM +00:00
	/// </summary>
	DateTimeOffsetMax = 0x5F,
	DateTimeOffsetUnixEpoch = 0x60,
	DateTimeOffsetWindowsEpoch = 0x61,
	TimeOnly = 0x62,

	/// <summary>
	/// 12:00 AM
	/// </summary>
	TimeOnlyMin = 0x63,

	/// <summary>
	/// 11:59 PM
	/// </summary>
	TimeOnlyMax = 0x64,
	TimeSpan = 0x65,

	/// <summary>
	/// -10675199.02:48:05.4775808
	/// </summary>
	TimeSpanMin = 0x66,

	/// <summary>
	/// 10675199.02:48:05.4775807
	/// </summary>
	TimeSpanMax = 0x67,
	TimeSpanZero = 0x68,
	Guid = 0x69,

	/// <summary>
	/// 00000000-0000-0000-0000-000000000000
	/// </summary>
	GuidEmpty = 0x6A,

	/// <summary>
	/// FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF
	/// </summary>
	GuidAllBitsSet = 0x6B,

	/// <summary>
	/// Version number 1.2, 1.2.3, 1.2.3.4
	/// </summary>
	Version = 0x6C,

	/// <summary>
	/// End of the header
	/// </summary>
	EndOfHeader = 0xFF
}