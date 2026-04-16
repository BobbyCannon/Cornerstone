#region References

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceMethodInfo : Reflection.SourceMethodInfo
{
	#region Properties

	public new Accessibility Accessibility { get; set; }
	public new List<SourceAttributeInfo> Attributes { get; } = [];
	public bool IsPartial { get; set; }
	public new SourceParameterInfo[] Parameters { get; set; }
	public NullableAnnotation ReturnNullableAnnotation { get; set; }
	public new string ReturnType { get; set; }
	public SourceTypeParameterInfo[] TypeParameters { get; set; }

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