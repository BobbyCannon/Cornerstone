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
	Unknown = 0x0,
	Packet = 0x1,
	True = 0x2,
	False = 0x3,
	Null = 0x4,
	String = 0x5,
	StringOfEmpty = 0x6,

	Char = 0x7,
	CharMin = 0x8,
	CharMax = 0x9,

	Byte = 0xA,
	ByteMin = 0xB,
	ByteMax = 0xC,
	ByteOne = 0xD,
	ByteTwo = 0xE,
	ByteArray = 0xF,

	SByte = 0x10,
	SByteMin = 0x11,
	SByteMax = 0x12,
	SByteNegativeOne = 0x13,
	SByteZero = 0x14,
	SByteOne = 0x15,
	SByteTwo = 0x16,

	Int16 = 0x17,
	Int16Min = 0x18,
	Int16Max = 0x19,
	Int16NegativeOne = 0x1A,
	Int16Zero = 0x1B,
	Int16One = 0x1C,
	Int16Two = 0x1D,

	UInt16 = 0x1E,
	UInt16Min = 0x1F,
	UInt16Max = 0x20,
	UInt16One = 0x21,
	UInt16Two = 0x22,

	Int32 = 0x23,
	Int32Min = 0x24,
	Int32Max = 0x25,
	Int32NegativeOne = 0x26,
	Int32Zero = 0x27,
	Int32One = 0x28,
	Int32Two = 0x29,

	UInt32 = 0x2A,
	UInt32Min = 0x2B,
	UInt32Max = 0x2C,
	UInt32One = 0x2D,
	UInt32Two = 0x2E,

	Int64 = 0x2F,
	Int64Min = 0x30,
	Int64Max = 0x31,
	Int64NegativeOne = 0x32,
	Int64Zero = 0x33,
	Int64One = 0x34,
	Int64Two = 0x35,

	UInt64 = 0x36,
	UInt64Min = 0x37,
	UInt64Max = 0x38,
	UInt64One = 0x39,
	UInt64Two = 0x3A,

	Int128 = 0x3B,
	Int128Min = 0x3C,
	Int128Max = 0x3D,
	Int128NegativeOne = 0x3E,
	Int128Zero = 0x3F,
	Int128One = 0x40,
	Int128Two = 0x41,

	UInt128 = 0x42,
	UInt128Min = 0x43,
	UInt128Max = 0x44,
	UInt128One = 0x45,
	UInt128Two = 0x46,

	IntPtr = 0x47,
	IntPtrMin = 0x48,
	IntPtrMax = 0x49,

	UIntPtr = 0x4A,
	UIntPtrMin = 0x4B,
	UIntPtrMax = 0x4C,

	Float = 0x4D,
	FloatMin = 0x4E,
	FloatMax = 0x4F,
	FloatNegativeInfinity = 0x50,
	FloatPositiveInfinity = 0x51,
	FloatNaN = 0x52,
	FloatNegativeZero = 0x53,
	FloatZero = 0x54,
	FloatE = 0x55,
	FloatEpsilon = 0x56,
	FloatPi = 0x57,
	FloatTau = 0x58,
	FloatHalf = 0x59,
	FloatNegativeOne = 0x5A,
	FloatOne = 0x5B,
	FloatTwo = 0x5C,

	Double = 0x5D,
	DoubleMin = 0x5E,
	DoubleMax = 0x5F,
	DoubleNegativeInfinity = 0x60,
	DoublePositiveInfinity = 0x61,
	DoubleNaN = 0x62,
	DoubleNegativeZero = 0x63,
	DoubleZero = 0x64,
	DoubleE = 0x65,
	DoubleEpsilon = 0x66,
	DoublePi = 0x67,
	DoubleTau = 0x68,
	DoubleHalf = 0x69,
	DoubleNegativeOne = 0x6A,
	DoubleOne = 0x6B,
	DoubleTwo = 0x6C,

	Decimal = 0x6D,
	DecimalMin = 0x6E,
	DecimalMax = 0x6F,
	DecimalNegativeOne = 0x70,
	DecimalZero = 0x71,
	DecimalOne = 0x72,
	DecimalTwo = 0x73,

	DateOnly = 0x74,
	DateOnlyMin = 0x75,
	DateOnlyMax = 0x76,
	DateOnlyUnixEpoch = 0x77,
	DateOnlyWindowsEpoch = 0x78,

	DateTime = 0x79,
	DateTimeMin = 0x7A,
	DateTimeMax = 0x7B,
	DateTimeUnixEpoch = 0x7C,
	DateTimeWindowsEpoch = 0x7D,

	DateTimeOffset = 0x7E,
	DateTimeOffsetMin = 0x7F,
	DateTimeOffsetMax = 0x80,
	DateTimeOffsetUnixEpoch = 0x81,
	DateTimeOffsetWindowsEpoch = 0x82,

	TimeOnly = 0x83,
	TimeOnlyMin = 0x84,
	TimeOnlyMax = 0x85,

	TimeSpan = 0x86,
	TimeSpanMin = 0x87,
	TimeSpanMax = 0x88,
	TimeSpanZero = 0x89,

	Guid = 0x8A,
	GuidEmpty = 0x8B,
	GuidAllBitsSet = 0x8C,

	Version = 0x8D,
	VersionOneZero = 0x8E,
	VersionOneZeroZero = 0x8F,
	VersionOneZeroZeroZero = 0x90,

	EndOfHeader = 0xFF
}