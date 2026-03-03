#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

#pragma warning disable IL2055
#pragma warning disable IL2070

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for reflection based actions.
/// </summary>
public static class ReflectionExtensions
{
	#region Constants

	public const BindingFlags DeclaredOnlyLookup =
		BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.Instance
		| BindingFlags.Static
		| BindingFlags.DeclaredOnly;

	#endregion

	#region Methods

	public static MethodInfo FindGenericMethod(this Type type, string name, int typeParameterCount, string[] parameterTypes)
	{
		foreach (var method in type.GetMethods(DeclaredOnlyLookup))
		{
			if (!method.IsGenericMethod)
			{
				continue;
			}

			if (method.Name != name)
			{
				continue;
			}

			if (method.GetGenericArguments().Length != typeParameterCount)
			{
				continue;
			}

			var parameters = method.GetParameters();
			if (parameters.Length != parameterTypes.Length)
			{
				continue;
			}

			for (var i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].ParameterType.ToString() != parameterTypes[i])
				{
				}
			}

			return method;
		}

		return null!;
	}

	/// <summary>
	/// Convert type to <see cref="Nullable{T}" /> type.
	/// </summary>
	/// <param name="type"> The type to convert. </param>
	/// <returns> The type as Nullable. </returns>
	public static Type FromNullableType(this Type type)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;
		return type;
	}

	/// <summary>
	/// Get direct implemented interfaces for a type.
	/// </summary>
	/// <param name="type"> The type to get the interfaces for. </param>
	/// <returns> The direct interfaces for a type. </returns>
	public static IEnumerable<Type> GetDirectInterfaces(this Type type)
	{
		var interfaces = type.GetInterfaces();
		var baseType = type.BaseType;
		var result = new HashSet<Type>(interfaces);
		foreach (var i in interfaces)
		{
			result.ExceptWith(i.GetInterfaces());
			if (baseType != null)
			{
				result.ExceptWith(baseType.GetInterfaces());
				baseType = baseType.BaseType;
			}
		}
		return result;
	}

	/// <summary>
	/// Returns true if the object is a direct descendant of the provided generic type
	/// </summary>
	/// <param name="parent"> The parent type. </param>
	/// <param name="valueType"> The value type to check. </param>
	/// <returns> </returns>
	/// <exception cref="ArgumentNullException"> The valueType was null and could not be processed. </exception>
	public static bool IsDirectDescendantOf(this Type valueType, Type parent)
	{
		if ((valueType == null) || (parent == null))
		{
			return false;
		}

		if (parent.IsInterface)
		{
			return valueType.GetDirectInterfaces().Contains(parent);
		}

		return parent.IsGenericType
			? valueType.IsSubClassOfGeneric(parent, true)
			: valueType.BaseType == parent;
	}

	/// <summary>
	/// Determines if an instance is nullable.
	/// </summary>
	/// <param name="type"> The type to be checked. </param>
	/// <returns> True if the type instance is nullable otherwise false. </returns>
	public static bool IsNullable(this Type type)
	{
		return type.IsClass
			|| !type.IsValueType
			|| (Nullable.GetUnderlyingType(type) != null);
	}

	/// <summary>
	/// Determines if a type is nullable.
	/// </summary>
	/// <param name="type"> The type to be checked. </param>
	/// <returns> True if the type is nullable otherwise false. </returns>
	public static bool IsNullableType(this Type type)
	{
		return type.ImplementsType(typeof(Nullable<>))
			&& (Nullable.GetUnderlyingType(type) != null);
	}

	private static Type GetFullTypeDefinition(Type type)
	{
		return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
	}

	/// <summary>
	/// Determines if the child is a subclass of the parent.
	/// </summary>
	/// <param name="child"> The type to be tested. </param>
	/// <param name="parent"> The type of the parent. </param>
	/// <param name="directDescendantOnly"> Direct base class only. </param>
	/// <returns> True if the child implements the parent otherwise false. </returns>
	private static bool IsSubClassOfGeneric(this Type child, Type parent, bool directDescendantOnly)
	{
		if (child == parent)
		{
			return false;
		}

		if (child.IsSubclassOf(parent))
		{
			return true;
		}

		var parameters = parent.GetGenericArguments();
		var isParameterLessGeneric = !((parameters.Length > 0)
			&& ((parameters[0].Attributes & TypeAttributes.BeforeFieldInit) == TypeAttributes.BeforeFieldInit));

		while ((child != null) && (child != typeof(object)))
		{
			if (child.BaseType == null)
			{
				return false;
			}

			var cur = GetFullTypeDefinition(child.BaseType);
			if ((parent == cur) || (isParameterLessGeneric && cur.GetInterfaces().Select(GetFullTypeDefinition).Contains(GetFullTypeDefinition(parent))))
			{
				return true;
			}
			if (!isParameterLessGeneric)
			{
				if ((GetFullTypeDefinition(parent) == cur) && !cur.IsInterface)
				{
					if (VerifyGenericArguments(GetFullTypeDefinition(parent), cur))
					{
						if (VerifyGenericArguments(parent, child))
						{
							return true;
						}
					}
				}
				else
				{
					if (child.GetInterfaces().Where(i => GetFullTypeDefinition(parent) == GetFullTypeDefinition(i)).Any(item => VerifyGenericArguments(parent, item)))
					{
						return true;
					}
				}
			}

			if (directDescendantOnly)
			{
				return false;
			}

			child = child.BaseType;
		}

		return false;
	}

	private static bool VerifyGenericArguments(Type parent, Type child)
	{
		var childArguments = child.GetGenericArguments();
		var parentArguments = parent.GetGenericArguments();

		if (childArguments.Length != parentArguments.Length)
		{
			return true;
		}

		return !childArguments.Where((t, i) => ((t.Assembly != parentArguments[i].Assembly)
				|| (t.Name != parentArguments[i].Name)
				|| (t.Namespace != parentArguments[i].Namespace))
			&& !t.IsSubclassOf(parentArguments[i])
		).Any();
	}

	#endregion
}