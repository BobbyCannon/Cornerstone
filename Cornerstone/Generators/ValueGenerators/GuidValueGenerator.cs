#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Generators.ValueGenerators;

/// <inheritdoc />
public class GuidValueGenerator : ValueGenerator
{
	#region Constructors

	/// <inheritdoc />
	public GuidValueGenerator() : base(Activator.GuidTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object GenerateValue(Type type)
	{
		return Guid.NewGuid();
	}

	/// <inheritdoc />
	public override IList<object> GenerateValues(Type type)
	{
		var response = new List<object>();

		if (type.IsNullableType())
		{
			response.Add(null);
		}

		if ((type == typeof(Guid)) || (type == typeof(Guid?)))
		{
			return response.AddRange(
				Guid.Empty,
				Guid.Parse("6DCEFB3F-4B1C-40FD-827E-58D31767E4A8"),
				Guid.Parse("00000000-0000-0000-0000-000000000001"),
				Guid.Parse("10000000-0000-0000-0000-000000000000")
			);
		}

		if ((type == typeof(ShortGuid)) || (type == typeof(ShortGuid?)))
		{
			return response.AddRange(
				ShortGuid.Empty,
				ShortGuid.Parse("6DCEFB3F-4B1C-40FD-827E-58D31767E4A8"),
				ShortGuid.Parse("00000000-0000-0000-0000-000000000001"),
				ShortGuid.Parse("10000000-0000-0000-0000-000000000000")
			);
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	#endregion
}