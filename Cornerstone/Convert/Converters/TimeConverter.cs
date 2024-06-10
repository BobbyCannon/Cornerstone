#region References

using System;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Convert.Converters;

/// <inheritdoc />
public class TimeConverter : BaseConverter
{
	#region Constructors

	/// <inheritdoc />
	public TimeConverter() : base(
		new Guid("F14C39A5-5503-4D0F-917E-8C8A4606798C"),
		Activator.TimeTypes,
		ArrayExtensions.CombineArrays(
			Activator.TimeTypes,
			Activator.StringTypes
		))
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Convert to TimeSpan.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The TimeSpan value. </returns>
	public static TimeSpan ToTimeSpan(object value)
	{
		return value.ConvertTo<TimeSpan>();
	}

	/// <inheritdoc />
	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterOptions options = null)
	{
		if (from == null)
		{
			value = null;
			return toType.IsNullable();
		}

		TimeSpan span;

		var toString = Activator.StringTypes.Contains(toType);

		switch (from)
		{
			#if (NET6_0_OR_GREATER)
			case TimeOnly o:
			{
				if (toString)
				{
					value = o.ToString("O");
					return true;
				}

				span = o.ToTimeSpan();
				break;
			}
			#endif
			case TimeSpan s:
			{
				if (toString)
				{
					value = s.ToString("G");
					return true;
				}

				span = s;
				break;
			}
			default:
			{
				// Should never get here, code coverage won't like this.
				return base.TryConvertTo(from, fromType, toType, out value, options);
			}
		}

		#if (NET6_0_OR_GREATER)
		if ((toType == typeof(TimeOnly)) || (toType == typeof(TimeOnly?)))
		{
			value = new TimeOnly(span.Ticks);
			return true;
		}
		#endif
		if ((toType == typeof(TimeSpan)) || (toType == typeof(TimeSpan?)))
		{
			value = new TimeSpan(span.Ticks);
			return true;
		}

		// Should never get here, code coverage won't like this.
		return base.TryConvertTo(from, fromType, toType, out value, options);
	}

	#endregion
}