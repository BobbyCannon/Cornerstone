#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Represents a comparer for class object.
/// </summary>
public class ObjectComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(object expected, object actual)
	{
		var expectedType = expected?.GetType();
		var actualType = actual?.GetType();

		return (expectedType != null)
			&& (expectedType.IsClass || expectedType.IsStruct())
			&& (actualType != null)
			&& (actualType.IsClass || actualType.IsStruct());
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		try
		{
			session.Add(expected);
			session.Add(actual);

			var expectedProperties = expected.GetCachedPropertyDictionary();
			var actualProperties = actual.GetCachedPropertyDictionary();

			if ((expectedProperties.Count == 0) && (actualProperties.Count == 0))
			{
				// There's nothing to compare.
				return CompareResult.AreEqual;
			}

			var propertiesToExclude = session.Options.PropertiesToIgnore?.TryGetValue(expected.GetType(), out var p) == true ? p : Array.Empty<string>();

			// Cycle through properties
			foreach (var expectedProperty in expectedProperties)
			{
				if (!actualProperties.TryGetValue(expectedProperty.Key, out var actualProperty))
				{
					// failed to get property on actual
					if (session.Options.IgnoreMissingProperties)
					{
						// This compare allows missing properties so just continue
						continue;
					}

					session.AppendDifference($"Expected [{expectedProperty.Key}] property is was not found on the actual value.");
					return CompareResult.NotEqual;
				}

				if (!expectedProperty.Value.CanRead
					|| propertiesToExclude.Contains(expectedProperty.Key, System.StringComparer.OrdinalIgnoreCase))
				{
					continue;
				}

				var expectedValue = expectedProperty.Value.GetValue(expected);
				var actualValue = actualProperty.GetValue(actual);

				// Values cannot be previous process or references to themselves.
				if (session.CheckReference(expected, expectedValue, actual, actualValue))
				{
					// Recursive support
					continue;
				}

				CompareSession.InternalProcess(session, expectedValue, actualValue, expectedProperty.Key);

				// See if we have hit an issue.
				if (session.Result != CompareResult.AreEqual)
				{
					return session.Result;
				}
			}

			// Return the current result.
			return session.Result;
		}
		catch (Exception ex)
		{
			session.AppendDifference($"{expected.GetType()} failed. Exception: {ex.Message}");
			return CompareResult.Inconclusive;
		}
		finally
		{
			session.Remove(expected);
			session.Remove(actual);
		}
	}

	#endregion
}