#region References

using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators.ValueGenerators;

/// <inheritdoc />
public class StringValueGenerator : ValueGenerator
{
	#region Constructors

	/// <inheritdoc />
	public StringValueGenerator() : base(
		ArrayExtensions.CombineArrays(
			Activator.StringTypes,
			Activator.CharTypes
		))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object GenerateValue(Type type)
	{
		return RandomGenerator.NextString(12);
	}

	/// <inheritdoc />
	public override IList<object> GenerateValues(Type type)
	{
		if ((type == typeof(char)) || (type == typeof(char?)))
		{
			return new object[] { char.MinValue, char.MaxValue };
		}

		if (type == typeof(string))
		{
			return new object[] { string.Empty, "", " " };
		}

		if (type == typeof(GapBuffer<char>))
		{
			return new object[] { new GapBuffer<char>() };
		}

		if (type == typeof(RopeBuffer<char>))
		{
			return new object[] { new RopeBuffer<char>() };
		}

		if (type == typeof(StringBuilder))
		{
			return new object[] { new StringBuilder() };
		}

		if (type == typeof(TextBuilder))
		{
			return new object[] { new TextBuilder() };
		}

		if (type == typeof(JsonString))
		{
			return new object[] { new JsonString() };
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	#endregion
}