#region References

using System;
using System.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Represents a comparer for Tuple / ValueTuple.
/// </summary>
public class TupleComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(object expected, object actual)
	{
		return (expected.IsTuple() || expected.IsValueTuple())
			&& (actual.IsTuple() || actual.IsValueTuple());
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		try
		{
			session.AddReference(expected);
			session.AddReference(actual);

			var values1 = expected.GetValueTupleItemDictionary();
			var values2 = actual.GetValueTupleItemDictionary();

			DictionaryComparer.CompareDictionaries(session, values1, values2, message);

			// Return the current result.
			return session.Result;
		}
		finally
		{
			session.RemoveReference(expected);
			session.RemoveReference(actual);
		}
	}

	#endregion
}