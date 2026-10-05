#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Uses a type's overridden Equals instead of walking properties.
/// </summary>
public class EqualsComparer : BaseComparer
{
	#region Methods

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "GetType() cannot flow DynamicallyAccessedMembers; Equals override is inspected on the runtime type.")]
	public override bool IsSupported(CompareSession session, object expected, object actual)
	{
		if ((expected == null) || (actual == null))
		{
			return false;
		}

		var type = expected.GetType();
		if (!type.IsClass)
		{
			return false;
		}

		return OverridesEquals(type);
	}

	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		if (expected.Equals(actual))
		{
			return CompareResult.AreEqual;
		}

		session.AddDifference(expected, actual, true, message);
		return CompareResult.NotEqual;
	}

	private static bool OverridesEquals([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
	{
		var method = type.GetMethod(nameof(Equals), BindingFlags.Public | BindingFlags.Instance, null, [typeof(object)], null);
		return (method != null) && (method.DeclaringType != typeof(object));
	}

	#endregion
}
