#region References

using System;
using Cornerstone.Convert;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for enum types.
/// </summary>
public class EnumComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(object expected, object actual)
	{
		return expected?.GetType().IsEnum ?? false;
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		var expectedValue = (Enum) expected;

		if (!actual.TryConvertTo(expectedValue.GetType(), out var actualValue))
		{
			AddDifference(session, expectedValue.GetType().FullName, actualValue?.GetType().FullName, true, "Failed to convert actual to Enum type.");
			return CompareResult.NotEqual;
		}

		if (Equals(expectedValue, actualValue))
		{
			return CompareResult.AreEqual;
		}

		AddDifference(session, expectedValue.ToString(), actualValue.ToString(), true);
		return CompareResult.NotEqual;
	}

	#endregion
}