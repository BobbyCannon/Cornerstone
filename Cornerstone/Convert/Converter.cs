#region References

using System;
using System.Dynamic;
using Cornerstone.Convert.Converters;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Convert;

/// <summary>
/// Service for comparing objects.
/// </summary>
public static class Converter
{
	#region Fields

	private static readonly ConverterCollection<Type, BaseConverter> _converters;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the compare service.
	/// </summary>
	static Converter()
	{
		_converters = new ConverterCollection<Type, BaseConverter>();

		RegisterConverter(new BooleanConverter());
		RegisterConverter(new DateConverter());
		RegisterConverter(new EnumConverter());
		RegisterConverter(new GuidConverter());
		RegisterConverter(new NumberConverter());
		RegisterConverter(new TimeConverter());
		RegisterConverter(new CharConverter());
		RegisterConverter(new StringConverter());
		RegisterConverter(new UpdateableConverter());
		RegisterConverter(new DictionaryConverter());
		RegisterConverter(new CollectionConverter());
	}

	#endregion

	#region Methods

	/// <summary>
	/// Try to convert from one type to another.
	/// </summary>
	/// <typeparam name="T"> The type to convert to. </typeparam>
	/// <param name="from"> The original type. </param>
	/// <param name="settings"> The options for converting. </param>
	/// <returns> The new type. </returns>
	public static T ConvertTo<T>(this object from, IConverterSettings settings = null)
	{
		return (T) from.ConvertTo(typeof(T), settings);
	}

	/// <summary>
	/// Try to convert from one type to another.
	/// </summary>
	/// <param name="from"> The original type. </param>
	/// <param name="toType"> The type to convert to. </param>
	/// <param name="settings"> The options for converting. </param>
	/// <returns> The new type. </returns>
	public static object ConvertTo(this object from, Type toType, IConverterSettings settings = null)
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

		if (from.TryConvertTo(toType, out var value, settings))
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
		if (from.TryConvertTo(typeof(T), out var value))
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
	/// <param name="settings"> The options for converting. </param>
	/// <returns> True if the object was converted otherwise false. </returns>
	public static bool TryConvertTo(this object from, Type toType, out object to, IConverterSettings settings = null)
	{
		to = null;

		// Fast identity check
		if ((from != null) && (from.GetType() == toType))
		{
			to = from;
			return true;
		}

		// Null handling
		if (from == null)
		{
			if (toType.IsNullable())
			{
				to = null;
				return true;
			}
			return false;
		}

		var fromType = from.GetType();

		// Try registered specific converters
		if (_converters.TryGetValue(fromType, toType, x => x.CanConvert(fromType, toType), out var converter))
		{
			return converter.TryConvertTo(from, fromType, toType, out to, settings);
		}

		// Built-in .NET fallback (for primitives, strings, enums, etc.)
		if (TrySystemConvert(from, toType, out to))
		{
			return true;
		}

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

		// Check to see if this converter has a custom CanConvert method.
		if ((methodInfo != null) && (methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType))
		{
			//
			// Requires use of the overridden CanConvert method instead of relying on type values.
			//
			_converters.AddConverter(converter);
			return;
		}

		//
		// This converter can just use from / to types.
		//
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

	private static bool TrySystemConvert(object from, Type toType, out object result)
	{
		result = null;

		try
		{
			// System.Convert handles many common cases efficiently
			result = System.Convert.ChangeType(from, toType);
			return true;
		}
		catch
		{
			// Fall through - not all conversions are supported by System.Convert
		}

		// Additional common cases not covered well by ChangeType
		if (toType == typeof(string))
		{
			result = from?.ToString();
			return true;
		}

		if (from is IConvertible convertible)
		{
			try
			{
				result = convertible.ToType(toType, null);
				return true;
			}
			catch
			{
			}
		}

		return false;
	}

	#endregion
}