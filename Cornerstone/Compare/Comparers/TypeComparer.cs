#region References

using System;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for date types.
/// </summary>
public class TypeComparer : BaseComparer
{
	#region Methods

	public override bool IsSupported(CompareSession session, object expected, object actual)
	{
		return expected is Type
			&& actual is Type;
	}

	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		if (expected == actual)
		{
			return CompareResult.AreEqual;
		}

		var expectedValue = expected as Type;
		var actualValue = actual as Type;

		session.AddDifference(expectedValue?.FullName, actualValue?.FullName, true);
		return CompareResult.NotEqual;
	}

	#endregion
}