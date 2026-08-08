#region References

using System;

#endregion

namespace Cornerstone.Agent.Header;

public enum ModelDataTypeEnum : uint
{
	// The value is an 8-bit unsigned integer.
	GgufMetadataValueTypeUint8 = 0,

	// The value is an 8-bit signed integer.
	GgufMetadataValueTypeInt8 = 1,

	// The value is a 16-bit unsigned little-endian integer.
	GgufMetadataValueTypeUint16 = 2,

	// The value is a 16-bit signed little-endian integer.
	GgufMetadataValueTypeInt16 = 3,

	// The value is a 32-bit unsigned little-endian integer.
	GgufMetadataValueTypeUint32 = 4,

	// The value is a 32-bit signed little-endian integer.
	GgufMetadataValueTypeInt32 = 5,

	// The value is a 32-bit IEEE754 floating point number.
	GgufMetadataValueTypeFloat32 = 6,

	// The value is a boolean.
	// 1-byte value where 0 is false and 1 is true.
	// Anything else is invalid, and should be treated as either the model being invalid or the reader being buggy.
	GgufMetadataValueTypeBool = 7,

	// The value is a UTF-8 non-null-terminated string, with length prepended.
	GgufMetadataValueTypeString = 8,

	// The value is an array of other values, with the length and type prepended.
	///

	// Arrays can be nested, and the length of the array is the number of elements in the array, not the number of bytes.
	GgufMetadataValueTypeArray = 9,

	// The value is a 64-bit unsigned little-endian integer.
	GgufMetadataValueTypeUint64 = 10,

	// The value is a 64-bit signed little-endian integer.
	GgufMetadataValueTypeInt64 = 11,

	// The value is a 64-bit IEEE754 floating point number.
	GgufMetadataValueTypeFloat64 = 12
}

public static class GgufDataTypeEnumHelper
{
	#region Methods

	public static int GetDataTypeSize(this ModelDataTypeEnum dateType)
	{
		return dateType switch
		{
			ModelDataTypeEnum.GgufMetadataValueTypeUint8 => 1,
			ModelDataTypeEnum.GgufMetadataValueTypeInt8 => 1,
			ModelDataTypeEnum.GgufMetadataValueTypeUint16 => 2,
			ModelDataTypeEnum.GgufMetadataValueTypeInt16 => 2,
			ModelDataTypeEnum.GgufMetadataValueTypeUint32 => 4,
			ModelDataTypeEnum.GgufMetadataValueTypeInt32 => 4,
			ModelDataTypeEnum.GgufMetadataValueTypeFloat32 => 4,
			ModelDataTypeEnum.GgufMetadataValueTypeBool => 1,
			ModelDataTypeEnum.GgufMetadataValueTypeString => -1,
			ModelDataTypeEnum.GgufMetadataValueTypeArray => -1,
			ModelDataTypeEnum.GgufMetadataValueTypeUint64 => 8,
			ModelDataTypeEnum.GgufMetadataValueTypeInt64 => 8,
			ModelDataTypeEnum.GgufMetadataValueTypeFloat64 => 8,
			_ => throw new ArgumentOutOfRangeException(nameof(dateType), dateType, null)
		};
	}

	#endregion
}