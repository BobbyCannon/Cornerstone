﻿#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public static class CecilExtensions
{
	#region Methods

	public static bool ContainsAttribute(this IEnumerable<CustomAttribute> attributes, string attributeName)
	{
		return attributes.Any(attribute => attribute.Constructor.DeclaringType.FullName == attributeName);
	}

	public static CustomAttribute GetAttribute(this IEnumerable<CustomAttribute> attributes, string attributeName)
	{
		return attributes.FirstOrDefault(attribute => attribute.Constructor.DeclaringType.FullName == attributeName);
	}

	public static FieldReference GetGeneric(this FieldDefinition definition)
	{
		if (definition.DeclaringType.HasGenericParameters)
		{
			var declaringType = new GenericInstanceType(definition.DeclaringType);
			foreach (var parameter in definition.DeclaringType.GenericParameters)
			{
				declaringType.GenericArguments.Add(parameter);
			}

			return new(definition.Name, definition.FieldType, declaringType);
		}

		return definition;
	}

	public static MethodReference GetGeneric(this MethodReference reference)
	{
		if (reference.DeclaringType.HasGenericParameters)
		{
			var declaringType = new GenericInstanceType(reference.DeclaringType);
			foreach (var parameter in reference.DeclaringType.GenericParameters)
			{
				declaringType.GenericArguments.Add(parameter);
			}

			var methodReference = new MethodReference(reference.Name, reference.MethodReturnType.ReturnType, declaringType);
			foreach (var parameterDefinition in reference.Parameters)
			{
				methodReference.Parameters.Add(parameterDefinition);
			}

			methodReference.HasThis = reference.HasThis;
			return methodReference;
		}

		return reference;
	}

	public static string GetName(this PropertyDefinition propertyDefinition)
	{
		return $"{propertyDefinition.DeclaringType.FullName}.{propertyDefinition.Name}";
	}

	public static bool IsCall(this OpCode opCode)
	{
		return opCode.Code is Code.Call or Code.Callvirt;
	}

	public static bool IsCallToMethod(this Instruction instruction, string methodName, out int propertyNameIndex)
	{
		propertyNameIndex = 1;
		if (!instruction.OpCode.IsCall())
		{
			return false;
		}

		if (instruction.Operand is not MethodReference methodReference)
		{
			return false;
		}

		if (methodReference.Name != methodName)
		{
			return false;
		}

		var parameterDefinition = methodReference.Parameters.FirstOrDefault(_ => _.Name == "propertyName");
		if (parameterDefinition != null)
		{
			propertyNameIndex = methodReference.Parameters.Count - parameterDefinition.Index;
		}

		return true;
	}

	#endregion
}