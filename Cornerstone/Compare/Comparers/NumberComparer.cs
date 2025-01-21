#region References

using System;
using Cornerstone.Convert;

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
			if (CompareDoublePrecision(dExpected, dActual, session.Settings.DoubleTolerance))
			{
				return CompareResult.AreEqual;
			}

			session.AddDifference(expected, actual, true, message);
			return CompareResult.NotEqual;
		}

		if (expectedValue is float fExpected && actualValue is float fActual)
		{
			if (CompareFloatPrecision(fExpected, fActual, session.Settings.FloatTolerance))
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

	private bool CompareDoublePrecision(double a, double b, double epsilon)
	{
		var absA = Math.Abs(a);
		var absB = Math.Abs(b);
		var diff = Math.Abs(a - b);

		if (a.Equals(b))
		{
			// shortcut, handles infinities and NaN
			return true;
		}
		if ((a == 0) || (b == 0) || ((absA + absB) < double.MinValue))
		{
			// a or b is zero or both are extremely close to it
			// relative error is less meaningful here
			return diff < (epsilon * double.MinValue);
		}

		// use relative error
		return (diff / (absA + absB)) < epsilon;
	}

	private bool CompareFloatPrecision(float a, float b, float epsilon)
	{
		var absA = Math.Abs(a);
		var absB = Math.Abs(b);
		var diff = Math.Abs(a - b);

		if (a.Equals(b))
		{
			// shortcut, handles infinities and NaN
			return true;
		}
		if ((a == 0) || (b == 0) || ((absA + absB) < float.MinValue))
		{
			// a or b is zero or both are extremely close to it
			// relative error is less meaningful here
			return diff < (epsilon * float.MinValue);
		}

		// use relative error
		return (diff / (absA + absB)) < epsilon;
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