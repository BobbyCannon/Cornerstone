#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Generators.ValueGenerators;

/// <inheritdoc />
public class VersionValueGenerator : ValueGenerator
{
	#region Constructors

	/// <inheritdoc />
	public VersionValueGenerator() : base(typeof(Version))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object GenerateValue(Type type)
	{
		return new Version(1, 2, 3, 4);
	}

	/// <inheritdoc />
	public override IList<object> GenerateValues(Type type)
	{
		return new List<object>
		{
			new Version(),
			new Version(1, 2),
			new Version(1, 2, 3),
			new Version(1, 2, 3, 4),
			new Version(12345, 12345, 12345, 12345),
			new Version(99999, 99999, 99999, 99999)
		};
	}

	#endregion
}