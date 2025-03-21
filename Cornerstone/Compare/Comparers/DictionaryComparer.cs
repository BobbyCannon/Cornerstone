#region References

using System;
using System.Collections;
using System.Collections.Generic;
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
	public override bool IsSupported(CompareSession session, object expected, object actual)
	{
		return (expected.ImplementsType<IDictionary>()
				|| expected.ImplementsType(typeof(IReadOnlyDictionary<,>)))
			&& (actual.ImplementsType<IDictionary>()
				|| actual.ImplementsType(typeof(IReadOnlyDictionary<,>)));
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		try
		{
			session.AddReference(expected);
			session.AddReference(actual);

			CompareDictionaries(session, expected, actual, message);

			// Return the current result.
			return session.Result;
		}
		finally
		{
			session.RemoveReference(expected);
			session.RemoveReference(actual);
		}
	}

	internal static void CompareDictionaries(CompareSession session, object expected, object actual, Func<string> message)
	{
		var expectedKeys = GetKeys(expected).IterateList();
		var actualKeys = GetKeys(actual).IterateList();

		if (expectedKeys.Count != actualKeys.Count)
		{
			session.AddDifference(expectedKeys.Count.ToString(), actualKeys.Count.ToString(), true,
				() => message == null
					? "The dictionary lengths are different."
					: $"{message.Invoke()} The dictionary lengths are different."
			);
			return;
		}

		if (expectedKeys.Count == 0)
		{
			session.UpdateResult(CompareResult.AreEqual);
			return;
		}

		foreach (var key in expectedKeys)
		{
			if (!actualKeys.Contains(key)
				&& !session.Settings.IgnoreMissingDictionaryEntries)
			{
				session.Path.Push(key.ToString());
				session.AddDifference(key.ToString(), string.Empty, true, () => "Key missing in actual dictionary.");
				return;
			}

			var expectedValue = GetValue(expected, key);
			var actualValue = GetValue(actual, key);

			// Values cannot be previous process or references to themselves.
			if (session.CheckReference(expected, expectedValue, actual, actualValue))
			{
				// Recursive support
				continue;
			}

			session.Path.Push(key.ToString());
			CompareSession.InternalProcess(session, expectedValue, actualValue, message);
			session.Path.Pop();

			// See if we have hit an issue.
			if (session.Result == CompareResult.NotEqual)
			{
				return;
			}
		}
	}

	private static IEnumerable GetKeys(object dict)
	{
		var keysProperty = dict.GetType().GetCachedProperty("Keys");
		var keysValue = keysProperty?.GetValue(dict);
		return keysValue as IEnumerable;
	}

	private static object GetValue(object dict, object key)
	{
		var indexer = dict.GetType().GetProperty("Item", [key.GetType()]);
		return indexer?.GetValue(dict, [key]);
	}

	#endregion
}