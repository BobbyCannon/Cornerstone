#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Generators.ValueGenerators;

/// <inheritdoc />
public class BooleanValueGenerator : ValueGenerator
{
	#region Constructors

	/// <inheritdoc />
	public BooleanValueGenerator() : base(Activator.BooleanTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object GenerateValue(Type type)
	{
		return RandomGenerator.NextBool();
	}

	/// <inheritdoc />
	public override IList<object> GenerateValues(Type type)
	{
		if (type == typeof(bool))
		{
			return [true, false];
		}

		if (type == typeof(bool?))
		{
			return [null, true, false];
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	#endregion
}