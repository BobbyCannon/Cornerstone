#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for number types.
/// </summary>
public class NumberComparer : BaseComparer
{
	#region Constructors

	/// <inheritdoc />
	public NumberComparer() : base(Activator.NumberTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		var expectedValue = EnsureNotPointer(expected);
		var actualValue = EnsureNotPointer(actual).ConvertTo(expectedValue.GetType());

		if (expectedValue is double dExpected && actualValue is double dActual)
		{
			if (dExpected.ComparePrecision(dActual, session.Settings.DoubleTolerance))
			{
				return CompareResult.AreEqual;
			}

			session.AddDifference(expected, actual, true, message);
			return CompareResult.NotEqual;
		}

		if (expectedValue is float fExpected && actualValue is float fActual)
		{
			if (fExpected.ComparePrecision(fActual, session.Settings.FloatTolerance))
			{
				return CompareResult.AreEqual;
			}

			session.AddDifference(expected, actual, true, message);
			return CompareResult.NotEqual;
		}

		var c = expectedValue as IComparable;
		if (c?.CompareTo(actualValue) == 0)
		{
			return CompareResult.AreEqual;
		}

		session.AddDifference(expected, actual, true, message);
		return CompareResult.NotEqual;
	}

	private object EnsureNotPointer(object expected)
	{
		return expected switch
		{
			IntPtr p => p.ToInt64(),
			UIntPtr p => p.ToUInt64(),
			_ => expected
		};
	}

	#endregion
}