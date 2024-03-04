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
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		Validate(expected, actual);

		var expectedValue = expected.ToString();
		var actualValue = actual.ToString();

		if (string.Equals(expectedValue, actualValue, session.Options.StringComparison))
		{
			return CompareResult.AreEqual;
		}

		AddDifference(session, expectedValue, actualValue, true);
		return CompareResult.NotEqual;
	}

	#endregion
}