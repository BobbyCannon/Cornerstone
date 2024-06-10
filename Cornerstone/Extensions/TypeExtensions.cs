#region References

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Data;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for <see cref="Type" />.
/// </summary>
public static class TypeExtensions
{
	#region Fields

	private static readonly Type _enumerableType;
	private static readonly Type _stringType;

	private static readonly ConcurrentDictionary<Type, string> _typeAssemblyNames;

	#endregion

	#region Constructors

	static TypeExtensions()
	{
		_enumerableType = typeof(IEnumerable);
		_stringType = typeof(string);
		_typeAssemblyNames = new ConcurrentDictionary<Type, string>();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Add a nullable version of each value type in the provided list.
	/// </summary>
	/// <param name="types"> The types to include nullables. </param>
	/// <returns> The complete list of types including nullable versions. </returns>
	public static Type[] AddNullables(Type[] types)
	{
		var response = new List<Type>(types.Length * 2);
		foreach (var type in types)
		{
			response.Add(type);

			if (!type.IsValueType)
			{
				continue;
			}

			var nullableType = type.ToNullableType();
			if (response.Contains(nullableType))
			{
				continue;
			}

			response.Add(nullableType);
		}
		return response.ToArray();
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
	/// Gets the default included properties.
	/// Defaults to an empty collection for sync actions.
	/// Defaults to all properties for Updateable and Property Change Tracking.
	/// All entities must override this method to provide included (white listing)
	/// of properties to be updated based on the provided action.
	/// </summary>
	/// <param name="type"> The type to get the default included properties. </param>
	/// <param name="action"> The properties to include for the action. </param>
	/// <returns> The properties to be included for the action. </returns>
	public static HashSet<string> GetDefaultIncludedProperties(this Type type, UpdateableAction action)
	{
		switch (action)
		{
			case UpdateableAction.PartialUpdate:
			case UpdateableAction.PropertyChangeTracking:
			case UpdateableAction.Updateable:
			{
				var properties = Cache.GetPropertyDictionary(type.GetRealTypeUsingReflection());
				return [..properties.Keys];
			}
			case UpdateableAction.UnwrapProxyEntity:
			{
				var realType = type.GetRealTypeUsingReflection();
				var properties = Cache.GetPropertyDictionary(realType);
				var virtuals = realType.GetVirtualPropertyNames();
				return [..properties.Keys.Except(virtuals)];
			}
			case UpdateableAction.Unknown:
			case UpdateableAction.SyncIncomingAdd:
			case UpdateableAction.SyncIncomingModified:
			case UpdateableAction.SyncOutgoing:
			default:
			{
				return [];
			}
		}
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
	/// Returns true if the object is a descendant of the provided generic type
	/// </summary>
	/// <typeparam name="T"> The parent type. </typeparam>
	/// <param name="value"> The value to check. </param>
	/// <returns> </returns>
	/// <exception cref="ArgumentNullException"> The value was null and could not be processed. </exception>
	public static bool ImplementsType<T>(this object value)
	{
		return value is Type vType
			? ImplementsType<T>(vType)
			: ImplementsType<T>(value.GetType());
	}

	/// <summary>
	/// Returns true if the object implements the provided type.
	/// </summary>
	/// <param name="valueType"> The value type to check. </param>
	/// <param name="parentType"> The parent type. </param>
	/// <returns> </returns>
	/// <exception cref="ArgumentNullException"> The valueType was null and could not be processed. </exception>
	public static bool ImplementsType(this object valueType, Type parentType)
	{
		return ImplementsType(valueType.GetType(), parentType);
	}

	/// <summary>
	/// Returns true if the object implements the provided type.
	/// </summary>
	/// <param name="valueType"> The value type to check. </param>
	/// <param name="parentType"> The parent type. </param>
	/// <returns> </returns>
	/// <exception cref="ArgumentNullException"> The valueType was null and could not be processed. </exception>
	public static bool ImplementsType(this Type valueType, Type parentType)
	{
		if ((valueType == null) || (parentType == null))
		{
			return false;
		}

		if (parentType.IsGenericType)
		{
			return IsSubclassOfRawGeneric(valueType, parentType);
		}

		if (parentType.IsInterface)
		{
			return parentType.IsAssignableFrom(valueType);
		}

		return valueType.IsSubclassOf(parentType);
	}

	/// <summary>
	/// Returns true if the object is a descendant of the provided generic type
	/// </summary>
	/// <typeparam name="T"> The parent type. </typeparam>
	/// <param name="value"> The value to check. </param>
	/// <returns> </returns>
	/// <exception cref="ArgumentNullException"> The value was null and could not be processed. </exception>
	public static bool ImplementsType<T>(this Type value)
	{
		return (value != null) && value.ImplementsType(typeof(T));
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
			return GetDirectInterfaces(valueType).Contains(parent);
		}

		return parent.IsGenericType
			? valueType.IsSubClassOfGeneric(parent, true)
			: valueType.BaseType == parent;
	}

	/// <summary>
	/// Determine if the provided type is an IEnumerable type.
	/// </summary>
	/// <param name="type"> The type to be checked. </param>
	/// <returns> Returns true if the type is an IEnumerable false otherwise. </returns>
	/// <remarks> Ignores the following types "string". </remarks>
	public static bool IsEnumerable(this Type type)
	{
		return _enumerableType.IsAssignableFrom(type)
			&& (_stringType != type);
	}

	/// <summary>
	/// Determine if the type is an enum with the flags attribute.
	/// </summary>
	/// <param name="type"> The type to be tested. </param>
	/// <returns> True if the enum type is flagged otherwise false. </returns>
	public static bool IsFlaggedEnum(this Type type)
	{
		return type.IsEnum && type.GetCustomAttributes<FlagsAttribute>().Any();
	}

	/// <summary>
	/// Determines if a instance is nullable.
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

	/// <summary>
	/// Determine if a type is a struct.
	/// </summary>
	/// <param name="type"> The type to be checked. </param>
	/// <returns> True if the type is a struct otherwise false. </returns>
	public static bool IsStruct(this Type type)
	{
		return type is { IsValueType: true, IsEnum: false };
	}

	/// <summary>
	/// Return true if the type is supported in full framework (classic).
	/// </summary>
	/// <param name="type"> The type to be tested. </param>
	/// <returns> </returns>
	public static bool IsSupportedInFullFramework(this Type type)
	{
		#if NET6_0_OR_GREATER
		return (type != typeof(DateOnly))
			&& (type != typeof(TimeOnly));
		#else
		return true;
		#endif
	}

	/// <summary>
	/// Converts the type to an assembly name. Does not include version. Ex. System.String,mscorlib
	/// </summary>
	/// <param name="type"> The type to get the assembly name for. </param>
	/// <returns> The assembly name for the provided type. </returns>
	public static string ToAssemblyName(this Type type)
	{
		return _typeAssemblyNames.GetOrAdd(type, $"{type.FullName},{type.Assembly.GetName().Name}");
	}

	/// <summary>
	/// Converts data type to the code simplified type. Ex. Int16 to short, Single to float
	/// </summary>
	/// <param name="type"> The type to get C# code for. </param>
	/// <returns> The type in C# code format. </returns>
	public static string ToCSharpCode(this Type type)
	{
		return CSharpCodeWriter.GetCodeTypeName(type);
	}

	/// <summary>
	/// Convert type to <see cref="Nullable{T}" /> type.
	/// </summary>
	/// <param name="type"> The type to convert. </param>
	/// <returns> The type as Nullable. </returns>
	public static Type ToNullableType(this Type type)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;
		return type.IsValueType ? typeof(Nullable<>).GetCachedMakeGenericType(type) : type;
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
					if (child.GetInterfaces()
						.Where(i => GetFullTypeDefinition(parent) == GetFullTypeDefinition(i))
						.Any(item => VerifyGenericArguments(parent, item)))
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

	private static bool IsSubclassOfRawGeneric(Type valueType, Type parentGeneric)
	{
		while ((valueType != null) && (valueType != typeof(object)))
		{
			if (valueType == parentGeneric)
			{
				return true;
			}

			//
			// Convert the value to type definition if the parent is a definition
			//
			var current = valueType.IsGenericType
				&& parentGeneric.IsGenericTypeDefinition
					? valueType.GetGenericTypeDefinition()
					: valueType;

			if (parentGeneric == current)
			{
				return true;
			}

			if (parentGeneric.IsInterface)
			{
				var interfaceTypes = current.GetInterfaces();

				foreach (var interfaceType in interfaceTypes)
				{
					if (parentGeneric.IsGenericTypeDefinition
						&& interfaceType.IsGenericType
						&& !interfaceType.IsGenericTypeDefinition)
					{
						//
						// Convert the value to type definition if the parent is a definition
						//
						var interfaceTypeDefinition = interfaceType.GetGenericTypeDefinition();
						if (interfaceTypeDefinition == parentGeneric)
						{
							return true;
						}
					}

					if (interfaceType == parentGeneric)
					{
						return true;
					}
				}
			}

			valueType = valueType.BaseType;
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

		return !childArguments
			.Where((t, i) => ((t.Assembly != parentArguments[i].Assembly)
					|| (t.Name != parentArguments[i].Name)
					|| (t.Namespace != parentArguments[i].Namespace))
				&& !t.IsSubclassOf(parentArguments[i])
			).Any();
	}

	#endregion
}