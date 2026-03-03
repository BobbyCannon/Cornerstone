#region References

using System;
using System.Collections.Immutable;
using Cornerstone.Extensions;

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
		FromTypes = [..fromTypes];
		ToTypes = includeFromTypesInToTypes
			? [..ArrayExtensions.CombineArrays(fromTypes, toTypes)]
			: [..toTypes];
	}

	#endregion

	#region Properties

	/// <summary>
	/// Types this comparer can compare from.
	/// </summary>
	public ImmutableArray<Type> FromTypes { get; }

	/// <summary>
	/// Types this comparer can compare to;
	/// </summary>
	public ImmutableArray<Type> ToTypes { get; }

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
			session.AddDifference(expected, actual, true, message);
			return session.Result;
		}

		//if (!session.Settings.IgnoreObjectTypes
		//	&& !AreSameType(session, expected, actual))
		//{
		//	session.AddDifference(expected.GetType().FullName, actual.GetType().FullName, true, message ?? (() => "Data types do not match."));
		//	return session.Result;
		//}

		return CompareValues(session, expected, actual, message);
	}

	/// <summary>
	/// Check to see if this comparer supports the supplied value.
	/// </summary>
	/// <param name="session"> </param>
	/// <param name="expected"> The expected value. </param>
	/// <param name="actual"> The actual value. </param>
	/// <returns> True if the comparer supports the value otherwise false. </returns>
	public virtual bool IsSupported(CompareSession session, object expected, object actual)
	{
		return TryValidate(expected, actual, out _);
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