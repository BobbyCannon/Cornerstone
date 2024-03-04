#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Convert.Converters;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Convert;

/// <summary>
/// Service for comparing objects.
/// </summary>
public static class Converter
{
	#region Fields

	private static readonly LookupCollection<Type, BaseConverter> _converters;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the compare service.
	/// </summary>
	static Converter()
	{
		_converters = new LookupCollection<Type, BaseConverter>();

		RegisterConverter(new BooleanConverter());
		RegisterConverter(new DateConverter());
		RegisterConverter(new EnumConverter());
		RegisterConverter(new GuidConverter());
		RegisterConverter(new JsonValueConverter());
		RegisterConverter(new NumberConverter());
		RegisterConverter(new TimeConverter());
		RegisterConverter(new CharConverter());
		RegisterConverter(new StringConverter());
	}

	#endregion

	#region Methods

	/// <summary>
	/// Try to convert from one type to another.
	/// </summary>
	/// <typeparam name="T"> The type to convert to. </typeparam>
	/// <param name="from"> The original type. </param>
	/// <param name="options"> The options for converting. </param>
	/// <returns> The new type. </returns>
	public static T ConvertTo<T>(this object from, IConverterOptions options = null)
	{
		return (T) ConvertTo(from, typeof(T), options);
	}

	/// <summary>
	/// Try to convert from one type to another.
	/// </summary>
	/// <param name="from"> The original type. </param>
	/// <param name="toType"> The type to convert to. </param>
	/// <param name="options"> The options for converting. </param>
	/// <returns> The new type. </returns>
	public static object ConvertTo(this object from, Type toType, IConverterOptions options = null)
	{
		if (toType == typeof(object))
		{
			return from;
		}

		if (from == null)
		{
			if (toType.IsNullable())
			{
				return null;
			}

			throw new ArgumentNullException(nameof(from));
		}

		if (from.TryConvertTo(toType, out var value, options))
		{
			return value;
		}

		var fromType = from.GetType();
		throw new ConversionException(fromType, toType,
			$"Failed to convert [{fromType?.FullName}] to [{toType.FullName}] type.");
	}

	/// <summary>
	/// Try to convert from one type to another.
	/// </summary>
	/// <typeparam name="T"> The type to convert to. </typeparam>
	/// <param name="from"> The original type. </param>
	/// <param name="to"> The new type. </param>
	/// <returns> True if the object was converted otherwise false. </returns>
	public static bool TryConvertTo<T>(this object from, out T to)
	{
		if (TryConvertTo(from, typeof(T), out var value))
		{
			to = (T) value;
			return true;
		}

		to = default;
		return false;
	}

	/// <summary>
	/// Try to convert from one type to another.
	/// </summary>
	/// <param name="from"> The original type. </param>
	/// <param name="toType"> The type to convert to. </param>
	/// <param name="to"> The new type. </param>
	/// <param name="options"> The options for converting. </param>
	/// <returns> True if the object was converted otherwise false. </returns>
	public static bool TryConvertTo(this object from, Type toType, out object to, IConverterOptions options = null)
	{
		if ((from != null) && (from.GetType() == toType))
		{
			to = from;
			return true;
		}

		if (from == null)
		{
			if (toType.IsNullable())
			{
				// Type was nullable so just assign null
				to = null;
				return true;
			}

			// Type was not nullable so fail.
			to = default;
			return false;
		}

		var fromType = from.GetType();
		if (fromType == toType)
		{
			to = from;
			return true;
		}

		// Get the converter then try to convert
		if (_converters.TryGetValue(fromType, toType, x => x.CanConvert(fromType, toType), out var converter))
		{
			return converter.TryConvertTo(from, fromType, toType, out to, options);
		}

		// From / To converter not found
		to = default;
		return false;
	}

	/// <summary>
	/// Registers a converter.
	/// </summary>
	/// <param name="converter"> The converter to process. </param>
	internal static void RegisterConverter(BaseConverter converter)
	{
		var fromTypes = converter.FromTypes;
		var toTypes = converter.ToTypes;
		var methodInfo = converter.GetType().GetMethod(nameof(BaseConverter.CanConvert));

		if ((methodInfo != null) && (methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType))
		{
			_converters.TryAdd(converter);
			return;
		}

		foreach (var fromType in fromTypes)
		{
			foreach (var toType in toTypes)
			{
				if (converter.CanConvert(fromType, toType))
				{
					_converters.TryAdd(fromType, toType, converter);
				}

				var nonNullableFrom = fromType.FromNullableType();
				var nonNullableTo = toType.FromNullableType();

				if ((nonNullableFrom == nonNullableTo)
					|| !converter.CanConvert(toType, fromType))
				{
					continue;
				}

				_converters.TryAdd(toType, fromType, converter);
			}
		}
	}

	#endregion
}