#region References

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Enumerable = System.Linq.Enumerable;

#endregion

namespace Cornerstone.Convert.Converters;

public class DateConverter : BaseConverter
{
	#region Fields

	private static readonly Dictionary<string, Func<string, object>> _parsers;

	#endregion

	#region Constructors

	public DateConverter() : base(
		new Guid("AB09FECB-5056-42E7-86AC-E9C79D07977E"),
		SourceTypes.DateTypes,
		ArrayExtensions.CombineArrays(
			SourceTypes.DateTypes,
			SourceTypes.StringTypes
		))
	{
	}

	static DateConverter()
	{
		NumberTypes = [typeof(long), typeof(ulong)];

		_parsers = new Dictionary<string, Func<string, object>>
		{
			#if NET6_0_OR_GREATER
			{ typeof(DateOnly?).FullName, x => ToDateOnly(x) },
			{ typeof(DateOnly).FullName, x => ToDateOnly(x) },
			{ typeof(TimeOnly?).FullName, x => ToTimeOnly(x) },
			{ typeof(TimeOnly).FullName, x => ToTimeOnly(x) },
			#endif

			{ typeof(DateTime?).FullName, x => ToDateTime(x) },
			{ typeof(DateTime).FullName, x => ToDateTime(x) },
			{ typeof(DateTimeOffset?).FullName, x => ToDateTimeOffset(x) },
			{ typeof(DateTimeOffset).FullName, x => ToDateTimeOffset(x) },
		};
	}

	#endregion

	#region Properties

	/// <summary>
	/// Numbers that can be converted to DateTime.
	/// </summary>

	// ReSharper disable once CollectionNeverUpdated.Global
	public static ImmutableArray<Type> NumberTypes { get; }

	#endregion

	#region Methods

	public override bool CanConvert(Type fromType, Type toType)
	{
		return base.CanConvert(fromType, toType)
			|| (FromTypes.Contains(fromType)
				&& NumberTypes.Contains(toType));
	}

	/// <summary>
	/// Convert to DateOnly.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The converted value. </returns>
	public static DateOnly ToDateOnly(string value)
	{
		return DateOnly.TryParse(value, out var dateOnly) ? dateOnly : default;
	}

	/// <summary>
	/// Convert to DateTime.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The DateTime value. </returns>
	public static DateTime ToDateTime(string value)
	{
		return DateTime.TryParse(value, out var dateTime) ? dateTime : throw new ConversionException();
	}

	/// <summary>
	/// Convert to DateTimeOffset.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The DateTimeOffset value. </returns>
	public static DateTimeOffset ToDateTimeOffset(string value)
	{
		return DateTimeOffset.TryParse(value, out var dateTime) ? dateTime : throw new ConversionException();
	}

	/// <summary>
	/// Convert to TimeOnly.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The converted value. </returns>
	public static TimeOnly ToTimeOnly(string value)
	{
		return TimeOnly.TryParse(value, out var timeOnly) ? timeOnly : default;
	}

	/// <inheritdoc />
	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
	{
		return TryConvert(from, toType, out value)
			|| base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	/// <summary>
	/// Try to create the date from the tick value.
	/// </summary>
	/// <param name="toType"> The type to convert to. </param>
	/// <param name="ticks"> The date in tick format. </param>
	/// <param name="value"> The value if parsed. </param>
	/// <returns> True if the date could be created by ticks otherwise false. </returns>
	public static bool TryFromTicks(Type toType, ulong ticks, out object value)
	{
		if ((toType == typeof(DateTime)) || (toType == typeof(DateTime?)))
		{
			value = new DateTime((long) ticks);
			return true;
		}

		value = default;
		return false;
	}

	private static bool TryConvert(object from, Type toType, out object value)
	{
		if (from == null)
		{
			value = null;
			return toType.IsNullable();
		}

		DateTime dateTime;
		var timeSpan = TimeSpan.Zero;
		var fromType = from.GetType();
		var toString = Enumerable.Contains(SourceTypes.StringTypes, toType);

		if (Enumerable.Contains(SourceTypes.StringTypes, fromType))
		{
			var response = _parsers.TryGetValue(toType.FullName, out var parser);
			value = parser?.Invoke(from.ToString());
			return response;
		}

		switch (from)
		{
			case DateTimeOffset d:
			{
				if (toString)
				{
					value = d.ToString("O");
					return true;
				}
				dateTime = d.DateTime.ToUtcDateTime();
				timeSpan = d.Offset;
				break;
			}
			case DateTime d:
			{
				if (toString)
				{
					value = d.ToString("O");
					return true;
				}
				dateTime = d.ToUtcDateTime();
				break;
			}
			case DateOnly d:
			{
				if (toString)
				{
					value = d.ToString("O");
					return true;
				}
				dateTime = new DateTime(d.Year, d.Month, d.Day);
				break;
			}
			default:
			{
				value = null;
				return false;
			}
		}

		if ((toType == typeof(DateTimeOffset)) || (toType == typeof(DateTimeOffset?)))
		{
			value = new DateTimeOffset(dateTime, timeSpan);
			return true;
		}
		if ((toType == typeof(DateTime)) || (toType == typeof(DateTime?)))
		{
			value = timeSpan == TimeSpan.Zero ? dateTime : dateTime.Add(timeSpan);
			return true;
		}
		if ((toType == typeof(DateOnly)) || (toType == typeof(DateOnly?)))
		{
			value = new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
			return true;
		}
		value = null;
		return false;
	}

	#endregion
}