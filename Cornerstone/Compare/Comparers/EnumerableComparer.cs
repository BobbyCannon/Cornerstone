#region References

using System;
using System.Collections;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Represents a comparer for enumerable.
/// </summary>
public class EnumerableComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(CompareSession session, object expected, object actual)
	{
		return expected is IEnumerable
			&& actual is IEnumerable;
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		try
		{
			session.AddReference(expected);
			session.AddReference(actual);

			var expectedEnumerator = ((IEnumerable) expected).GetEnumerator();
			var actualEnumerator = ((IEnumerable) actual).GetEnumerator();

			while (true)
			{
				var expectedMoved = expectedEnumerator.MoveNext();
				var actualMoved = actualEnumerator.MoveNext();

				if (!expectedMoved && !actualMoved)
				{
					// Both collections are done, are equal.
					return CompareResult.AreEqual;
				}

				if (!expectedMoved || !actualMoved)
				{
					// One of the collection failed to move.
					return CompareResult.NotEqual;
				}

				var expectedValue = expectedEnumerator.Current;
				var actualValue = actualEnumerator.Current;

				// Values cannot be previous process or references to themselves.
				if (session.CheckReference(expected, expectedValue, actual, actualValue))
				{
					// Recursive support
					continue;
				}

				CompareSession.InternalProcess(session, expectedValue, actualValue);

				if (session.Result == CompareResult.NotEqual)
				{
					return CompareResult.NotEqual;
				}
			}
		}
		finally
		{
			session.RemoveReference(expected);
			session.RemoveReference(actual);
		}
	}

	#endregion
}