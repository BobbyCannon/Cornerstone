#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for String types. See <see cref="SourceTypes.StringTypes" /> for the types supported.
/// </summary>
public class StringComparer : BaseComparer
{
	#region Constructors

	public StringComparer() : base(SourceTypes.StringTypes)
	{
	}

	#endregion

	#region Methods

	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		Validate(expected, actual);

		var expectedValue = expected.ToString();
		var actualValue = actual.ToString();

		if (string.Equals(expectedValue, actualValue, session.Settings.StringComparison))
		{
			return CompareResult.AreEqual;
		}

		session.AddDifference(expectedValue, actualValue, true, message);
		return CompareResult.NotEqual;
	}

	#endregion
}