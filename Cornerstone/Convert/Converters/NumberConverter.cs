#region References

using System;
using System.Globalization;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Convert.Converters;

/// <inheritdoc />
public class NumberConverter : BaseConverter
{
	#region Constructors

	/// <inheritdoc />
	public NumberConverter() : base(
		new Guid("D1892654-D142-4FAA-9304-E4CDBD4556E7"),
		Activator.NumberTypes,
		ArrayExtensions.CombineArrays(
			Activator.NumberTypes,
			Activator.StringTypes,
			Activator.GuidTypes,
			[typeof(JsonNumber)]
		))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool CanConvert(Type fromType, Type toType)
	{
		return base.CanConvert(fromType, toType)
			|| (DateConverter.NumberTypes.Contains(fromType)
				&& Activator.DateTypes.Contains(toType));
	}

	/// <summary>
	/// Convert to Byte.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The byte value. </returns>
	public static byte ToByte(object value)
	{
		return value.ConvertTo<byte>();
	}

	/// <summary>
	/// Convert to Decimal.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The decimal value. </returns>
	public static decimal ToDecimal(object value)
	{
		return value.ConvertTo<decimal>();
	}

	/// <summary>
	/// Convert to Double.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The double value. </returns>
	public static double ToDouble(object value)
	{
		return value.ConvertTo<double>();
	}

	/// <summary>
	/// Convert to Int16 (short).
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The short value. </returns>
	public static short ToInt16(object value)
	{
		return value.ConvertTo<short>();
	}

	/// <summary>
	/// Convert to Int32 (int).
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The int value. </returns>
	public static int ToInt32(object value)
	{
		return value.ConvertTo<int>();
	}

	/// <summary>
	/// Convert to Int64 (ulong).
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The long value. </returns>
	public static long ToInt64(object value)
	{
		return value.ConvertTo<long>();
	}

	/// <summary>
	/// Convert to SByte.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The sbyte value. </returns>
	public static sbyte ToSByte(object value)
	{
		return value.ConvertTo<sbyte>();
	}

	/// <summary>
	/// Convert to Single (float).
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The float value. </returns>
	public static float ToSingle(object value)
	{
		return value.ConvertTo<float>();
	}

	/// <summary>
	/// Convert to UInt16 (ushort).
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The ushort value. </returns>
	public static ushort ToUInt16(object value)
	{
		return value.ConvertTo<ushort>();
	}

	/// <summary>
	/// Convert to UInt32 (uint).
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The uint value. </returns>
	public static uint ToUInt32(object value)
	{
		return value.ConvertTo<uint>();
	}

	/// <summary>
	/// Convert to UInt64 (ulong).
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The ulong value. </returns>
	public static ulong ToUInt64(object value)
	{
		return value.ConvertTo<ulong>();
	}

	/// <inheritdoc />
	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterOptions options = null)
	{
		if (from == null)
		{
			value = null;
			return toType.IsNullable();
		}

		if (!CanConvert(fromType, toType))
		{
			return base.TryConvertTo(from, fromType, toType, out value, options);
		}

		var fromValue = from switch
		{
			IntPtr intPtr => intPtr.ToInt64(),
			UIntPtr longPtr => longPtr.ToUInt64(),
			JsonNumber sValue => sValue.Value,
			_ => from
		};

		if (Activator.GuidTypes.Contains(toType))
		{
			var number = fromValue.ConvertTo<ulong>();
			var bytes = new byte[16];
			var hexValue = ulong.Parse(number.ToString(), NumberStyles.HexNumber);
			BitConverter.GetBytes(hexValue).CopyTo(bytes, 0);
			bytes = bytes.Reverse().ToArray();
			var guid = new Guid(bytes);
			value = toType == typeof(ShortGuid) ? new ShortGuid(guid) : guid;
			return true;
		}

		if (Activator.DateTypes.Contains(toType))
		{
			var number = fromValue.ConvertTo<ulong>();
			return DateConverter.TryFromTicks(toType, number, out value);
		}

		if (Activator.StringTypes.Contains(toType))
		{
			value = toType.CreateInstance(fromValue.ToString());
			return true;
		}

		if (toType == typeof(JsonNumber))
		{
			value = new JsonNumber(fromValue);
			return true;
		}

		if ((typeof(byte) == toType) || (typeof(byte?) == toType))
		{
			value = System.Convert.ToByte(fromValue);
			return true;
		}
		if ((typeof(sbyte) == toType) || (typeof(sbyte?) == toType))
		{
			value = System.Convert.ToSByte(fromValue);
			return true;
		}
		if ((typeof(short) == toType) || (typeof(short?) == toType))
		{
			value = System.Convert.ToInt16(fromValue);
			return true;
		}
		if ((typeof(ushort) == toType) || (typeof(ushort?) == toType))
		{
			value = System.Convert.ToUInt16(fromValue);
			return true;
		}
		// int at the bottom
		if ((typeof(uint) == toType) || (typeof(uint?) == toType))
		{
			value = System.Convert.ToUInt32(fromValue);
			return true;
		}
		if ((typeof(nuint) == toType) || (typeof(nuint?) == toType))
		{
			value = System.Convert.ToUInt32(fromValue);
			return true;
		}
		if ((typeof(long) == toType) || (typeof(long?) == toType))
		{
			value = System.Convert.ToInt64(fromValue);
			return true;
		}
		if ((typeof(ulong) == toType) || (typeof(ulong?) == toType))
		{
			value = System.Convert.ToUInt64(fromValue);
			return true;
		}
		if ((typeof(decimal) == toType) || (typeof(decimal?) == toType))
		{
			if (fromValue is double dValue)
			{
				if (dValue.Equals(0.0d))
				{
					value = decimal.Zero;
					return true;
				}

				if (dValue.GreaterThanOrEqualTo(79228162514264337593543950335.0))
				{
					value = decimal.MaxValue;
					return true;
				}

				if (dValue.LessThanOrEqualTo(-79228162514264337593543950335.0))
				{
					value = decimal.MinValue;
					return true;
				}
			}

			value = System.Convert.ToDecimal(fromValue);
			return true;
		}
		if ((typeof(double) == toType) || (typeof(double?) == toType))
		{
			value = fromValue switch
			{
				IntPtr p => System.Convert.ToDouble(p.ToInt64()),
				UIntPtr p => System.Convert.ToDouble(p.ToUInt64()),
				_ => System.Convert.ToDouble(fromValue)
			};

			return true;
		}
		if ((typeof(float) == toType) || (typeof(float?) == toType))
		{
			if (fromValue is double dValue)
			{
				if (dValue.GreaterThanOrEqualTo(float.MaxValue))
				{
					value = float.MaxValue;
					return true;
				}

				if (dValue.LessThanOrEqualTo(float.MinValue))
				{
					value = float.MinValue;
					return true;
				}
			}

			value = fromValue switch
			{
				IntPtr p => System.Convert.ToSingle(p.ToInt64()),
				UIntPtr p => System.Convert.ToSingle(p.ToUInt64()),
				_ => System.Convert.ToSingle(fromValue)
			};
			return true;
		}

		value = System.Convert.ToInt32(fromValue);
		return true;
	}

	#endregion
}