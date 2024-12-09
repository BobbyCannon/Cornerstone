#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Fields

	public int OrdinalStringComparison;
	public MethodReference StringEquals;
	private Dictionary<string, MethodReference> _methodCache;

	#endregion

	#region Methods

	public void FindComparisonMethods()
	{
		_methodCache = new();

		var stringEquals = ModuleWeaver
			.TypeSystem
			.StringDefinition
			.Methods
			.First(_ => _.IsStatic &&
				(_.Name == "Equals") &&
				(_.Parameters.Count == 3) &&
				(_.Parameters[0].ParameterType.Name == "String") &&
				(_.Parameters[1].ParameterType.Name == "String") &&
				(_.Parameters[2].ParameterType.Name == "StringComparison"));
		StringEquals = ModuleDefinition.ImportReference(stringEquals);
		OrdinalStringComparison = (int) StringEquals
			.Parameters[2]
			.ParameterType
			.Resolve()
			.Fields
			.First(_ => _.Name == "Ordinal")
			.Constant;
	}

	public static MethodReference FindNamedMethod(TypeDefinition typeDefinition)
	{
		var equalsMethod = FindNamedMethod(typeDefinition, "Equals");
		if (equalsMethod == null)
		{
			return FindNamedMethod(typeDefinition, "op_Equality");
		}
		return equalsMethod;
	}

	public MethodReference FindTypeEquality(TypeReference typeDefinition)
	{
		var fullName = typeDefinition.FullName;
		if (_methodCache.TryGetValue(fullName, out var methodReference))
		{
			return methodReference;
		}

		var equality = GetEquality(typeDefinition);
		_methodCache.Add(fullName, equality);
		return equality;
	}

	private static MethodReference FindNamedMethod(TypeDefinition typeDefinition, string methodName)
	{
		return typeDefinition.Methods
			.FirstOrDefault(_ => (_.Name == methodName) &&
				_.IsStatic &&
				(_.ReturnType.Name == "Boolean") &&
				_.HasParameters &&
				(_.Parameters.Count == 2) &&
				(_.Parameters[0].ParameterType == typeDefinition) &&
				(_.Parameters[1].ParameterType == typeDefinition));
	}

	private MethodReference GetEquality(TypeReference typeDefinition)
	{
		if (typeDefinition.IsArray)
		{
			return null;
		}
		if (typeDefinition.IsGenericParameter)
		{
			return null;
		}
		if (typeDefinition.Namespace.StartsWith("System.Collections"))
		{
			return null;
		}
		if (typeDefinition.IsGenericInstance)
		{
			if (typeDefinition.FullName.StartsWith("System.Nullable"))
			{
				var typeWrappedByNullable = ((GenericInstanceType) typeDefinition).GenericArguments.First();
				if (typeWrappedByNullable.IsGenericParameter)
				{
					return null;
				}
				var genericInstanceMethod = new GenericInstanceMethod(NullableEqualsMethod);
				genericInstanceMethod.GenericArguments.Add(typeWrappedByNullable);
				return ModuleDefinition.ImportReference(genericInstanceMethod);
			}
		}
		var equality = GetStaticEquality(typeDefinition);
		if (equality != null)
		{
			return ModuleDefinition.ImportReference(equality);
		}
		return null;
	}

	private MethodReference GetStaticEquality(TypeReference typeReference)
	{
		var typeDefinition = Resolve(typeReference);
		if (typeDefinition.IsInterface)
		{
			return null;
		}

		return FindNamedMethod(typeDefinition);
	}

	#endregion
}