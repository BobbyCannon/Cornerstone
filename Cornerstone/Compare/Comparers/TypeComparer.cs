#region References

using System;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for date types.
/// </summary>
public class TypeComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(object expected, object actual)
	{
		return expected is Type
			&& actual is Type;
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		if (expected == actual)
		{
			return CompareResult.AreEqual;
		}

		var expectedValue = expected as Type;
		var actualValue = actual as Type;

		AddDifference(session, expectedValue?.FullName, actualValue?.FullName, true);
		return CompareResult.NotEqual;
	}

	#endregion
}