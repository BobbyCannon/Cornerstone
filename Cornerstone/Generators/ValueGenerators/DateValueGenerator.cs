#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Protocols.Osc;

#endregion

namespace Cornerstone.Generators.ValueGenerators;

/// <inheritdoc />
public class DateValueGenerator : ValueGenerator
{
	#region Constructors

	/// <inheritdoc />
	public DateValueGenerator() : base(Activator.DateTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object GenerateValue(Type type)
	{
		var next = RandomGenerator.NextDateTime();

		#if !NETSTANDARD
		if ((type == typeof(DateOnly)) || (type == typeof(DateOnly?)))
		{
			return new DateOnly(next.Year, next.Month, next.Day);
		}
		#endif

		if ((type == typeof(DateTime)) || (type == typeof(DateTime?)))
		{
			return next;
		}

		if ((type == typeof(DateTimeOffset)) || (type == typeof(DateTimeOffset?)))
		{
			return new DateTimeOffset(next, RandomGenerator.NextTimeSpan());
		}

		if ((type == typeof(IsoDateTime)) || (type == typeof(IsoDateTime?)))
		{
			return new IsoDateTime(next, RandomGenerator.NextTimeSpan());
		}

		if ((type == typeof(OscTimeTag)) || (type == typeof(OscTimeTag?)))
		{
			return new OscTimeTag(next);
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	/// <inheritdoc />
	public override IList<object> GenerateValues(Type type)
	{
		var response = new List<object>();

		if (type.IsNullableType())
		{
			response.Add(null);
		}

		#if !NETSTANDARD
		if ((type == typeof(DateOnly)) || (type == typeof(DateOnly?)))
		{
			return response.AddRange(
				DateOnly.MinValue,
				DateOnly.MaxValue,
				new DateOnly(2023, 10, 31)
			);
		}
		#endif

		if ((type == typeof(DateTime)) || (type == typeof(DateTime?)))
		{
			return response.AddRange(
				DateTime.MinValue,
				DateTime.MaxValue,
				new DateTime(2023, 10, 31, 12, 01, 02, DateTimeKind.Local),
				new DateTime(2023, 10, 31, 12, 01, 03, DateTimeKind.Utc),
				new DateTime(2023, 10, 31, 12, 01, 04, DateTimeKind.Unspecified)
			);
		}

		if ((type == typeof(DateTimeOffset)) || (type == typeof(DateTimeOffset?)))
		{
			return response.AddRange(
				DateTimeOffset.MinValue,
				DateTimeOffset.MaxValue,
				new DateTimeOffset(2023, 10, 31, 12, 01, 02, new TimeSpan(1, 2, 0))
			);
		}

		if ((type == typeof(IsoDateTime)) || (type == typeof(IsoDateTime?)))
		{
			return response.AddRange(
				IsoDateTime.MinValue,
				IsoDateTime.MaxValue,
				new IsoDateTime(new DateTime(2023, 10, 31, 12, 01, 02, DateTimeKind.Local), new TimeSpan(1, 2, 3)),
				new IsoDateTime(new DateTime(2023, 10, 31, 12, 01, 03, DateTimeKind.Utc), new TimeSpan(4, 5, 6))
			);
		}

		if ((type == typeof(OscTimeTag)) || (type == typeof(OscTimeTag?)))
		{
			return response.AddRange(
				OscTimeTag.MinValue,
				OscTimeTag.MaxValue,
				new OscTimeTag(new DateTime(2023, 10, 31, 12, 01, 02, DateTimeKind.Utc))
			);
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	#endregion
}