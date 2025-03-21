#region References

using System;
using Cornerstone.Data;
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
	public override bool IsSupported(CompareSession session, object expected, object actual)
	{
		var expectedType = expected?.GetType();
		var actualType = actual?.GetType();

		return (expectedType != null)
			&& (expectedType.IsClass || expectedType.IsStruct())
			&& (actualType != null)
			&& (actualType.IsClass || actualType.IsStruct());
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		try
		{
			session.AddReference(expected);
			session.AddReference(actual);

			var expectedProperties = expected.GetCachedPropertyDictionary();
			var actualProperties = actual.GetCachedPropertyDictionary();

			if ((expectedProperties.Count == 0) && (actualProperties.Count == 0))
			{
				// There's nothing to compare.
				return CompareResult.AreEqual;
			}

			var expectedType = expected.GetType();
			var globalIncludeExcludeSettings = session.Settings.GlobalIncludeExcludeSettings ?? IncludeExcludeSettings.Empty;
			var typePropertiesToExclude = session.Settings.TypeIncludeExcludeSettings?.TryGetValue(expectedType, out var p) == true ? p : IncludeExcludeSettings.Empty;

			// Cycle through properties
			foreach (var expectedProperty in expectedProperties)
			{
				if (!typePropertiesToExclude.ShouldProcessProperty(expectedProperty.Key)
					|| !globalIncludeExcludeSettings.ShouldProcessProperty(expectedProperty.Key))
				{
					continue;
				}

				if (!actualProperties.TryGetValue(expectedProperty.Key, out var actualProperty))
				{
					// failed to get property on actual
					if (session.Settings.IgnoreMissingProperties)
					{
						// This compare allows missing properties so just continue
						continue;
					}

					session.AddDifference($"Expected [{expectedProperty.Key}] property is was not found on the actual value.");
					return CompareResult.NotEqual;
				}

				if (!expectedProperty.Value.CanRead)
				{
					continue;
				}

				if (expectedProperty.Value.IsIndexer())
				{
					// Property is an indexer
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

				session.Path.Push(expectedProperty.Key);
				CompareSession.InternalProcess(session, expectedValue, actualValue, message);
				session.Path.Pop();

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
			session.AddDifference($"{expected.GetType()} failed. Exception: {ex.ToDetailedString()}");
			return CompareResult.Inconclusive;
		}
		finally
		{
			session.RemoveReference(expected);
			session.RemoveReference(actual);
		}
	}

	#endregion
}