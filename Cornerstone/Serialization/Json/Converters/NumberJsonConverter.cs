#region References

using System;
using System.Globalization;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all number types. Supports the following types:
/// byte, short, ushort, int, uint, long, ulong
/// </summary>
public class NumberJsonConverter : JsonConverter
{
	#region Constructors

	/// <inheritdoc />
	public NumberJsonConverter() : base(
		ArrayExtensions.CombineArrays(
			Activator.NumberTypes,
			[typeof(JsonNumber)]
		))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationSettings settings)
	{
		var sValue = GetJsonString(value, settings);
		consumer.WriteRawString(sValue);
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		return jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonNumber x => x.Value.ConvertTo(type),
			JsonString x => x.Value.ConvertTo(type),
			_ => throw new NotImplementedException()
		};
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationSettings settings)
	{
		var sValue = value switch
		{
			char cValue => cValue.ToString(),
			byte cValue => cValue.ToString(),
			sbyte cValue => cValue.ToString(),
			short cValue => cValue.ToString(),
			ushort cValue => cValue.ToString(),
			int cValue => cValue.ToString(),
			uint cValue => cValue.ToString(),
			long cValue => cValue.ToString(),
			ulong cValue => cValue.ToString(),
			#if NET7_0_OR_GREATER
			Int128 cValue => ToJson(cValue),
			UInt128 cValue => ToJson(cValue),
			#endif
			decimal cValue => ToJson(cValue),
			double cValue => ToJson(cValue),
			float cValue => ToJson(cValue),
			IntPtr cValue => cValue.ToInt64().ToString(),
			UIntPtr cValue => cValue.ToUInt64().ToString(),
			JsonNumber cValue => GetJsonString(cValue.Value, settings),
			_ => throw new NotImplementedException($"{value.GetType()} not supported")
		};
		return sValue;
	}

	internal static string ToJson(IntPtr ptr)
	{
		return ptr.ToInt64().ToString();
	}

	internal static string ToJson(UIntPtr ptr)
	{
		return ptr.ToUInt64().ToString();
	}

	#if NET7_0_OR_GREATER

	internal static string ToJson(Int128 cValue)
	{
		return cValue.ToString();
	}

	internal static string ToJson(UInt128 cValue)
	{
		return cValue.ToString();
	}

	#endif

	private static string ToJson(decimal value)
	{
		var text = value.ToString("G", CultureInfo.InvariantCulture);
		return text.IndexOfAny(['.', 'E', 'N', 'I']) >= 0 ? text : text + ".0";
	}

	private static string ToJson(float value)
	{
		if (float.IsNaN(value))
		{
			return "\"NaN\"";
		}
		if (float.IsInfinity(value))
		{
			return float.IsNegativeInfinity(value) ? "\"-Infinity\"" : "\"Infinity\"";
		}
		#if NET7_0_OR_GREATER
		if (float.NegativeZero.IsEqual(value))
		{
			return "-0.0";
		}
		#endif

		var text = value.ToString("R", CultureInfo.InvariantCulture);
		return text.IndexOfAny(['.', 'E', 'N']) >= 0 ? text : text + ".0";
	}

	private static string ToJson(double value)
	{
		if (double.IsNaN(value))
		{
			return "\"NaN\"";
		}

		if (double.IsInfinity(value))
		{
			return double.IsNegativeInfinity(value) ? "\"-Infinity\"" : "\"Infinity\"";
		}

		#if NET7_0_OR_GREATER
		if (double.NegativeZero.IsEqual(value))
		{
			return "-0.0";
		}
		#endif

		var text = value.ToString("R", CultureInfo.InvariantCulture);
		return text.IndexOfAny(['.', 'E', 'N']) >= 0 ? text : text + ".0";
	}

	#endregion
}