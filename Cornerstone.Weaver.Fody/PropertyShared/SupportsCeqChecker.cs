#region References

using System;
using System.Collections.Generic;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public static class SupportsCeqChecker
{
	#region Fields

	private static readonly List<string> CeqStructNames;

	#endregion

	#region Constructors

	static SupportsCeqChecker()
	{
		CeqStructNames =
		[
			nameof(Int32),
			nameof(UInt32),
			nameof(Int64),
			nameof(UInt64),
			nameof(Single),
			nameof(Double),
			nameof(Boolean),
			nameof(Int16),
			nameof(UInt16),
			nameof(Byte),
			nameof(SByte),
			nameof(Char)
		];
	}

	#endregion

	#region Methods

	public static bool SupportsCeq(this TypeReference typeReference)
	{
		if (CeqStructNames.Contains(typeReference.Name))
		{
			return true;
		}

		if (typeReference.IsArray)
		{
			return false;
		}

		if (typeReference.ContainsGenericParameter)
		{
			return false;
		}

		var typeDefinition = typeReference.Resolve();
		if (typeDefinition == null)
		{
			throw new($"Could not resolve '{typeReference.FullName}'.");
		}

		if (typeDefinition.IsEnum)
		{
			return true;
		}

		return !typeDefinition.IsValueType;
	}

	#endregion
}