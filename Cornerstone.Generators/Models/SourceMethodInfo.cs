#region References

using System.Linq;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceMethodInfo : SourceMemberInfo
{
	#region Properties

	public Accessibility Accessibility { get; init; }
	public string FullyQualifiedTypeName { get; init; }
	public bool IsAbstract { get; init; }
	public bool IsGenericMethod { get; init; }
	public bool IsOverride { get; init; }
	public bool IsPartial { get; init; }
	public bool IsStatic { get; init; }
	public bool IsVirtual { get; init; }
	public SourceParameterInfo[] Parameters { get; init; }
	public NullableAnnotation ReturnNullableAnnotation { get; init; }
	public string ReturnType { get; init; }
	public SourceTypeParameterInfo[] TypeParameters { get; init; }

	#endregion

	#region Methods

	public bool CanInvoke()
	{
		if (Accessibility is not Accessibility.Public)
		{
			return false;
		}

		if (Parameters.Any(x => x.IsRef || x.IsOut || x.IsParameterTypeRefLike || x.IsParameterTypePointer))
		{
			return false;
		}

		if (IsGenericMethod)
		{
			if (TypeParameters.Any(x =>
					x.HasUnmanagedTypeConstraint
					|| x.HasValueTypeConstraint
					|| x.HasTypeParameterInConstraintTypes
					|| (x.ConstraintTypes?.Length > 1)))
			{
				return false;
			}

			if (Parameters.Any(x => !x.IsTypeParameter && x.HasNestedTypeParameter))
			{
				return false;
			}

			if (!TypeParameters.All(t => Parameters.Any(x => x.ParameterType == t.Name)))
			{
				return false;
			}
		}

		return true;
	}

	#endregion
}