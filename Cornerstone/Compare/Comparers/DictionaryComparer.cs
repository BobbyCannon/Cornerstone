#region References

using System.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Represents a comparer for class object.
/// </summary>
public class DictionaryComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(object expected, object actual)
	{
		return expected.ImplementsType<IDictionary>()
			&& actual.ImplementsType<IDictionary>();
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		try
		{
			session.AddReference(expected);
			session.AddReference(actual);

			var expectedValue = (IDictionary) expected;
			var actualValue = (IDictionary) actual;

			CompareDictionaries(session, expectedValue, actualValue);

			// Return the current result.
			return session.Result;
		}
		finally
		{
			session.RemoveReference(expected);
			session.RemoveReference(actual);
		}
	}

	internal static void CompareDictionaries(CompareSession session, IDictionary expected, IDictionary actual)
	{
		var keys = expected.Keys;
		var length = expected.Keys.Count;

		if (length != actual.Keys.Count)
		{
			AddDifference(session, length.ToString(), actual.Count.ToString(),
				true, "The dictionary lengths are different."
			);
			return;
		}

		if (length == 0)
		{
			session.UpdateResult(CompareResult.AreEqual);
			return;
		}

		foreach (var key in keys)
		{
			if (!actual.Contains(key))
			{
				AddDifference(session, key.ToString(), string.Empty, true, "Key missing in actual dictionary.");
				return;
			}

			var expectedValue = expected[key];
			var actualValue = actual[key];

			// Values cannot be previous process or references to themselves.
			if (session.CheckReference(expected, expectedValue, actual, actualValue))
			{
				// Recursive support
				continue;
			}

			CompareSession.InternalProcess(session, expectedValue, actualValue);

			// See if we have hit an issue.
			if (session.Result == CompareResult.NotEqual)
			{
				return;
			}
		}
	}

	#endregion
}