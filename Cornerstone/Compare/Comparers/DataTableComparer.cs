#region References

using System.Data;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Represents a comparer for a DataTable.
/// </summary>
public class DataTableComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(object expected, object actual)
	{
		return expected is DataTable
			&& actual is DataTable;
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		try
		{
			session.Add(expected);
			session.Add(actual);

			var expectedValue = (DataTable) expected;
			var actualValue = (DataTable) actual;

			Compare(session, expectedValue, actualValue);

			// Return the current result.
			return session.Result;
		}
		finally
		{
			session.Remove(expected);
			session.Remove(actual);
		}
	}

	internal static void Compare(CompareSession session, DataTable expected, DataTable actual)
	{
		if (expected.Rows.Count != actual.Rows.Count)
		{
			AddDifference(session, expected.Rows.Count.ToString(), actual.Rows.Count.ToString(),
				true, "The data table row counts are different."
			);
			return;
		}

		for (var i = 0; i < expected.Rows.Count; i++)
		{
			var expectedValue = expected.Rows[i];
			var actualValue = actual.Rows[i];

			// Values cannot be previous process or references to themselves.
			if (session.CheckReference(expected, expectedValue, actual, actualValue))
			{
				// Recursive support
				continue;
			}

			Compare(session, expectedValue, actualValue);

			// See if we have hit an issue.
			if (session.Result == CompareResult.NotEqual)
			{
				return;
			}
		}
	}

	internal static void Compare(CompareSession session, DataRow expected, DataRow actual)
	{
		if (expected.ItemArray.Length != actual.ItemArray.Length)
		{
			AddDifference(session, expected.ItemArray.Length.ToString(), actual.ItemArray.Length.ToString(),
				true, "The data table row column lengths are different."
			);
			return;
		}

		for (var i = 0; i < expected.ItemArray.Length; i++)
		{
			var expectedValue = expected.ItemArray[i];
			var actualValue = actual.ItemArray[i];

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