#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Validation;

/// <summary>
/// Provides some basic argument validations.
/// </summary>
public static class ArgumentValidator
{
	#region Methods

	/// <summary>
	/// Throws ArgumentOutOfRangeException if the value is less than or equal to the given lower limit.
	/// </summary>
	/// <param name="parameterName"> The name of the parameter being validated. </param>
	/// <param name="valueToCheck"> The value of the parameter being validated. </param>
	/// <param name="lowerLimit"> The lower limit which the value should be greater than. </param>
	public static void IsGreaterThan(string parameterName, int valueToCheck, int lowerLimit)
	{
		if (valueToCheck < lowerLimit)
		{
			throw new ArgumentOutOfRangeException(parameterName, valueToCheck,
				$"Value is less than or equal to {lowerLimit}"
			);
		}
	}

	/// <summary>
	/// Throws ArgumentOutOfRangeException if the value is greater than or equal
	/// to the given upper limit.
	/// </summary>
	/// <param name="parameterName"> The name of the parameter being validated. </param>
	/// <param name="valueToCheck"> The value of the parameter being validated. </param>
	/// <param name="upperLimit"> The upper limit which the value should be less than. </param>
	public static void IsLessThan(string parameterName, int valueToCheck, int upperLimit)
	{
		if (valueToCheck >= upperLimit)
		{
			throw new ArgumentOutOfRangeException(parameterName, valueToCheck,
				$"Value is greater than or equal to {upperLimit}"
			);
		}
	}

	/// <summary>
	/// Throws ArgumentException if the value is equal to the undesired value.
	/// </summary>
	/// <typeparam name="TValue"> The type of value to be validated. </typeparam>
	/// <param name="parameterName"> The name of the parameter being validated. </param>
	/// <param name="undesiredValue"> The value that valueToCheck should not equal. </param>
	/// <param name="valueToCheck"> The value of the parameter being validated. </param>
	public static void IsNotEqual<TValue>(string parameterName, TValue valueToCheck, TValue undesiredValue)
	{
		if (EqualityComparer<TValue>.Default.Equals(valueToCheck, undesiredValue))
		{
			throw new ArgumentException($"The given value '{valueToCheck}' should not equal '{undesiredValue}'", parameterName);
		}
	}

	/// <summary>
	/// Throws ArgumentOutOfRangeException if the value is negative.
	/// </summary>
	/// <param name="parameterName"> The name of the parameter being validated. </param>
	/// <param name="valueToCheck"> The value of the parameter being validated. </param>
	public static void IsNotNegative(string parameterName, int valueToCheck)
	{
		if (valueToCheck < 0)
		{
			throw new ArgumentOutOfRangeException(parameterName, valueToCheck, "Value cannot be negative");
		}
	}

	/// <summary>
	/// Throws ArgumentNullException if value is null.
	/// </summary>
	/// <param name="parameterName"> The name of the parameter being validated. </param>
	/// <param name="valueToCheck"> The value of the parameter being validated. </param>
	#if NETSTANDARD2_0
	public static T IsNotNull<T>(string parameterName, T valueToCheck)
		#else
	public static T IsNotNull<T>(string parameterName, [NotNull] T valueToCheck)
	#endif
	{
		if (valueToCheck == null)
		{
			throw new ArgumentNullException(parameterName);
		}

		return valueToCheck;
	}

	/// <summary>
	/// Throws ArgumentException if the value is null, an empty string,
	/// or a string containing only whitespace.
	/// </summary>
	/// <param name="parameterName"> The name of the parameter being validated. </param>
	/// <param name="valueToCheck"> The value of the parameter being validated. </param>
	public static void IsNotNullOrEmptyString(string parameterName, string valueToCheck)
	{
		if (string.IsNullOrWhiteSpace(valueToCheck))
		{
			throw new ArgumentException("Parameter contains a null, empty, or whitespace string.", parameterName);
		}
	}

	/// <summary>
	/// Throws ArgumentOutOfRangeException if the value is outside
	/// of the given lower and upper limits.
	/// </summary>
	/// <param name="parameterName"> The name of the parameter being validated. </param>
	/// <param name="valueToCheck"> The value of the parameter being validated. </param>
	/// <param name="lowerLimit"> The lower limit which the value should not be less than. </param>
	/// <param name="upperLimit"> The upper limit which the value should not be greater than. </param>
	public static void IsWithinRangeExclusive(string parameterName, int valueToCheck, int lowerLimit, int upperLimit)
	{
		// TODO: Debug assert here if lowerLimit >= upperLimit

		if ((valueToCheck < lowerLimit) || (valueToCheck > upperLimit))
		{
			throw new ArgumentOutOfRangeException(parameterName, valueToCheck,
				$"Value is not between {lowerLimit} and {upperLimit}"
			);
		}
	}

	/// <summary>
	/// Throws ArgumentOutOfRangeException if the value is outside
	/// of the given lower and upper limits.
	/// </summary>
	/// <param name="parameterName"> The name of the parameter being validated. </param>
	/// <param name="valueToCheck"> The value of the parameter being validated. </param>
	/// <param name="lowerLimit"> The lower limit which the value should not be less than. </param>
	/// <param name="upperLimit"> The upper limit which the value should not be greater than. </param>
	public static void IsWithinRangeInclusive(string parameterName, int valueToCheck, int lowerLimit, int upperLimit)
	{
		// TODO: Debug assert here if lowerLimit >= upperLimit

		if ((valueToCheck <= lowerLimit) || (valueToCheck >= upperLimit))
		{
			throw new ArgumentOutOfRangeException(parameterName, valueToCheck,
				$"Value is not between {lowerLimit} and {upperLimit}"
			);
		}
	}

	#endregion
}