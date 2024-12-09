using System;

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for String types. See <see cref="Activator.StringTypes" /> for the types supported.
/// </summary>
public class StringComparer : BaseComparer
{
	#region Constructors

	/// <inheritdoc />
	public StringComparer() : base(Activator.StringTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		Validate(expected, actual);

		var expectedValue = expected.ToString();
		var actualValue = actual.ToString();

		if (string.Equals(expectedValue, actualValue, session.Settings.StringComparison))
		{
			return CompareResult.AreEqual;
		}

		AddDifference(session, expectedValue, actualValue, true, message);
		return CompareResult.NotEqual;
	}

	#endregion
}