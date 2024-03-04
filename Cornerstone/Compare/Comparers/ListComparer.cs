#region References

using System.Collections;
using System.Linq;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Represents a comparer for lists.
/// </summary>
public class ListComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(object expected, object actual)
	{
		return expected is IList || (expected?.GetType().IsArray == true);
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		try
		{
			session.Add(expected);
			session.Add(actual);

			var list1 = ((IEnumerable) expected).Cast<object>().ToArray();
			var list2 = ((IEnumerable) actual).Cast<object>().ToArray();
			var length = list1.Length;

			if (length != list2.Length)
			{
				AddDifference(session, length.ToString(), list2.Length.ToString(), true, $"The {message} collection lengths are different.");
				return CompareResult.NotEqual;
			}

			if (length == 0)
			{
				return CompareResult.AreEqual;
			}

			for (var i = 0; i < list1.Length; i++)
			{
				var expectedValue = list1[i];
				var actualValue = list2[i];

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
					return session.Result;
				}
			}

			// Return the current result.
			return session.Result;
		}
		finally
		{
			session.Remove(expected);
			session.Remove(actual);
		}
	}

	#endregion
}