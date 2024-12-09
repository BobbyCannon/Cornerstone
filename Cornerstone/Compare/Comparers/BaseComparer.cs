#region References

using System;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Represents a comparer for a type.
/// </summary>
public abstract class BaseComparer
{
	#region Constructors

	/// <summary>
	/// Initialize the type comparer.
	/// </summary>
	/// <param name="types"> The types this comparer can compare from and to. </param>
	protected BaseComparer(params Type[] types)
		: this(types, types, false)
	{
	}

	/// <summary>
	/// Initialize the type comparer.
	/// </summary>
	/// <param name="fromTypes"> The types this comparer can compare from. </param>
	/// <param name="toTypes"> The types this comparer can compare to. </param>
	/// <param name="includeFromTypesInToTypes"> Include the fromTypes in the final ToTypes. </param>
	protected BaseComparer(Type[] fromTypes, Type[] toTypes, bool includeFromTypesInToTypes)
	{
		FromTypes = new ReadOnlySet<Type>(fromTypes);
		ToTypes = includeFromTypesInToTypes
			? new ReadOnlySet<Type>(fromTypes, toTypes)
			: new ReadOnlySet<Type>(toTypes);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Types this comparer can compare from.
	/// </summary>
	public ReadOnlySet<Type> FromTypes { get; }

	/// <summary>
	/// Types this comparer can compare to;
	/// </summary>
	public ReadOnlySet<Type> ToTypes { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Check to see if this type comparer can compare the provided types.
	/// </summary>
	/// <param name="fromType"> The type to compare from. </param>
	/// <param name="toType"> The type to compare to. </param>
	/// <returns> True if the comparer can compare from the type otherwise false. </returns>
	public bool CanCompare(Type fromType, Type toType)
	{
		return FromTypes.Contains(fromType)
			&& ToTypes.Contains(toType);
	}

	/// <summary>
	/// Compare the provided two objects.
	/// </summary>
	/// <param name="session"> The session calling this comparer. </param>
	/// <param name="expected"> The expected value. </param>
	/// <param name="actual"> The value to be tested. </param>
	/// <param name="message"> Message to prefix if compare fails. </param>
	/// <returns> The result of the compare. </returns>
	public CompareResult Compare(CompareSession session, object expected, object actual, Func<string> message = null)
	{
		if ((expected == null) && (actual == null))
		{
			session.UpdateResult(CompareResult.AreEqual);
			return session.Result;
		}

		if ((expected == null) || (actual == null))
		{
			AddDifference(session, expected?.ToString() ?? "null", actual?.ToString() ?? "null", true, message);
			return session.Result;
		}

		if (!AreSameType(session, expected, actual))
		{
			AddDifference(session, expected.GetType().FullName, actual.GetType().FullName, true, message ?? (() => "Data types do not match."));
			return session.Result;
		}

		return CompareValues(session, expected, actual, message);
	}

	/// <summary>
	/// Check to see if this comparer supports the supplied value.
	/// </summary>
	/// <param name="expected"> The expected value. </param>
	/// <param name="actual"> The actual value. </param>
	/// <returns> True if the comparer supports the value otherwise false. </returns>
	public virtual bool IsSupported(object expected, object actual)
	{
		return TryValidate(expected, actual, out _);
	}

	/// <summary>
	/// Build an error message for the comparer
	/// </summary>
	/// <param name="session"> The session to append the error message. </param>
	/// <param name="expected"> The expected value in string format. </param>
	/// <param name="actual"> The actual value in string format. </param>
	/// <param name="shouldEqual"> True if the values should have been equal otherwise false. </param>
	/// <param name="message"> On optional message to include. </param>
	/// <returns> The error message. </returns>
	protected static void AddDifference(CompareSession session, string expected, string actual, bool shouldEqual, Func<string> message = null)
	{
		session.UpdateResult(CompareResult.NotEqual);

		var actualMessage = message?.Invoke();

		if (!string.IsNullOrWhiteSpace(actualMessage))
		{
			session.AppendDifference(actualMessage);
			session.AppendDifference(string.Empty);
		}

		if (shouldEqual)
		{
			session.AppendDifference(" ");
			session.AppendDifference(expected);
			session.AppendDifference(" != ");
			session.AppendDifference(actual);
			return;
		}

		session.AppendDifference("Should not have equaled the value below");
		session.AppendDifference(actual);
	}

	/// <summary>
	/// Check to see if the types match and are the same type.
	/// </summary>
	/// <param name="session"> The session calling this comparer. </param>
	/// <param name="expected"> The expected value. </param>
	/// <param name="actual"> The value to be tested. </param>
	/// <returns> True if the expected and actual objects are the same type otherwise false. </returns>
	protected bool AreSameType(CompareSession session, object expected, object actual)
	{
		if (session.Settings.IgnoreObjectTypes)
		{
			return true;
		}

		var expectedType = expected.GetType();
		var actualType = actual.GetType();
		return expectedType == actualType;
	}

	/// <summary>
	/// Final compare the provided two objects.
	/// </summary>
	/// <param name="session"> The session calling this comparer. </param>
	/// <param name="expected"> The expected value. </param>
	/// <param name="actual"> The value to be tested. </param>
	/// <param name="message"> Message to prefix if compare fails. </param>
	/// <returns> The result of the compare. </returns>
	protected abstract CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message);

	/// <summary>
	/// Validates the types are supported.
	/// </summary>
	/// <param name="from"> The [from] object. </param>
	/// <param name="to"> The [to] object. </param>
	/// <param name="message"> The message if validate false. </param>
	/// <returns> True if from and to are supported otherwise false. </returns>
	protected bool TryValidate(object from, object to, out string message)
	{
		if (from == null)
		{
			message = "The from value is null.";
			return false;
		}

		if (to == null)
		{
			message = "The to value is null.";
			return false;
		}

		var fromType = from.GetType();
		var toType = to.GetType();

		if (!CanCompare(fromType, toType))
		{
			message = "The types are not supported by this comparer.";
			return false;
		}

		message = string.Empty;
		return true;
	}

	/// <summary>
	/// Validates the types are supported.
	/// </summary>
	/// <param name="from"> The [from] object. </param>
	/// <param name="to"> The [to] object. </param>
	protected void Validate(object from, object to)
	{
		if (!TryValidate(from, to, out var message))
		{
			throw new NotSupportedException(message);
		}
	}

	#endregion
}