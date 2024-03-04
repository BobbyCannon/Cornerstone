#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Generators.ValueGenerators;

/// <inheritdoc />
public class TimeValueGenerator : ValueGenerator
{
	#region Constructors

	/// <inheritdoc />
	public TimeValueGenerator() : base(Activator.TimeTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object GenerateValue(Type type)
	{
		#if !NETSTANDARD
		if ((type == typeof(TimeOnly)) || (type == typeof(TimeOnly?)))
		{
			return new TimeOnly(RandomGenerator.NextTimeSpan().Ticks);
		}
		#endif

		if ((type == typeof(TimeSpan)) || (type == typeof(TimeSpan?)))
		{
			return RandomGenerator.NextTimeSpan();
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	/// <inheritdoc />
	public override IList<object> GenerateValues(Type type)
	{
		#if !NETSTANDARD
		if ((type == typeof(TimeOnly)) || (type == typeof(TimeOnly?)))
		{
			return new object[]
			{
				TimeOnly.MinValue,
				TimeOnly.MaxValue,
				#if NET7_0_OR_GREATER
				new TimeOnly(11, 01, 02, 03, 04)
				#else
				new TimeOnly(11, 01, 02, 03)
				#endif
			};
		}
		#endif

		if ((type == typeof(TimeSpan)) || (type == typeof(TimeSpan?)))
		{
			return new object[]
			{
				TimeSpan.MinValue,
				TimeSpan.MaxValue,
				TimeSpan.Zero,
				#if NET7_0_OR_GREATER
				new TimeSpan(01, 02, 03, 04, 05, 06)
				#else
				new TimeSpan(01, 02, 03, 04, 05)
				#endif
			};
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	#endregion
}