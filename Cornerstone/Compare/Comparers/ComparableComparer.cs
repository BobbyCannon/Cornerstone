#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Represents a comparer for IComparable objects.
/// </summary>
public class ComparableComparer : BaseComparer
{
	#region Methods

	/// <inheritdoc />
	public override bool IsSupported(object expected, object actual)
	{
		return expected is IComparable
			|| ((expected != null)
				&& expected.ImplementsType(typeof(IComparable<>))
			);
	}

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		var actualValue = actual.ConvertTo(expected.GetType());

		if (expected is string eString)
		{
			var aString = (string) actualValue;
			if (eString.Length != aString.Length)
			{
				session.AddDifference(expected, actual, true);
				return CompareResult.NotEqual;
			}

			if (string.Compare(eString, (string) actualValue, session.Settings.StringComparison) == 0)
			{
				return CompareResult.AreEqual;
			}
		}
		else if (expected is IComparable expectedValue)
		{
			if (expectedValue.CompareTo(actualValue) == 0)
			{
				return CompareResult.AreEqual;
			}
		}
		else if (expected.ImplementsType(typeof(IComparable<>)))
		{
			// todo: optimize
			var type = typeof(IComparable<>);
			var typed = type.MakeGenericType(expected.GetType());
			var method = typed.GetMethod(nameof(IComparable.CompareTo));
			var result = method?.Invoke(expected, [actualValue]);

			if (result is 0)
			{
				return CompareResult.AreEqual;
			}
		}

		session.AddDifference(expected, actual, true, message);

		return CompareResult.NotEqual;
	}

	#endregion
}