#region References

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Cornerstone.Attributes;
using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Internal;
using Cornerstone.Location;
using Cornerstone.Protocols.Osc;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for <see cref="Type" />.
/// </summary>
public static class TypeExtensions
{
	#region Fields

	private static readonly Type _enumerableType;
	private static readonly ReadOnlyDictionary<Type, string> _simplifiedTypeNames;
	private static readonly Type _stringType;
	private static readonly ConcurrentDictionary<Type, string> _typeAssemblyNames;

	#endregion

	#region Constructors

	static TypeExtensions()
	{
		_enumerableType = typeof(IEnumerable);
		_stringType = typeof(string);
		_simplifiedTypeNames = new ReadOnlyDictionary<Type, string>(
			new Dictionary<Type, string>
			{
				{ typeof(char), "char" },
				{ typeof(bool), "bool" },
				{ typeof(byte), "byte" },
				{ typeof(sbyte), "sbyte" },
				{ typeof(short), "short" },
				{ typeof(ushort), "ushort" },
				{ typeof(int), "int" },
				{ typeof(uint), "uint" },
				{ typeof(long), "long" },
				{ typeof(ulong), "ulong" },
				{ typeof(decimal), "decimal" },
				{ typeof(float), "float" },
				{ typeof(double), "double" },
				{ typeof(string), "string" }
			}
		);
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
	/// Get a non default value for data type.
	/// </summary>
	/// <param name="info"> The info of the property to get the value for. </param>
	/// <param name="arguments"> The arguments for the constructing the instance. </param>
	/// <param name="customTypeFactory"> An optional custom factory method to create unknown types. </param>
	/// <returns> The new instances of the type. </returns>
	public static object CreateInstanceOfNonDefaultValue(this PropertyInfo info, object[] arguments = null,
		Func<Type, object[], object> customTypeFactory = null)
	{
		var requiredRange = info.GetCustomAttribute<RangeAttribute>();
		return CreateInstanceOfNonDefaultValue(info.PropertyType, arguments, requiredRange, customTypeFactory);
	}

	/// <summary>
	/// Get a non default value for data type.
	/// </summary>
	/// <param name="type"> The type of object to get the value for. </param>
	/// <param name="requiredRange"> An optional required range. </param>
	/// <param name="arguments"> The arguments for the constructing the instance. </param>
	/// <param name="customTypeFactory"> An optional custom factory method to create unknown types. </param>
	/// <returns> The new instances of the type. </returns>
	public static object CreateInstanceOfNonDefaultValue(this Type type, object[] arguments = null,
		RangeAttribute requiredRange = null, Func<Type, object[], object> customTypeFactory = null)
	{
		if (type.IsDelegate())
		{
			// todo: how do we create a new instance of a delegate?
			return null;
		}

		if (type.IsEnum)
		{
			var details = EnumExtensions.GetAllEnumDetails(type).Values.ToList();
			return details.Count >= 2
				? details[1].Value
				: details.FirstOrDefault().Value;
		}
		if ((type == typeof(bool)) || (type == typeof(bool?)))
		{
			return true;
		}
		if ((type == typeof(byte)) || (type == typeof(byte?)))
		{
			var value = RandomGenerator.NextByte();
			return (byte) (value == 0 ? 1 : value);
		}
		if ((type == typeof(char)) || (type == typeof(char?)))
		{
			var value = RandomGenerator.NextChar();
			return (char) (value == 0 ? 1 : value);
		}
		if ((type == typeof(DateTime)) || (type == typeof(DateTime?)))
		{
			return DateTimeProvider.RealTime.UtcNow;
		}
		if ((type == typeof(DateTimeOffset)) || (type == typeof(DateTimeOffset?)))
		{
			return DateTimeOffset.UtcNow;
		}
		if ((type == typeof(OscTimeTag)) || (type == typeof(OscTimeTag?)))
		{
			return OscTimeTag.UtcNow;
		}
		if ((type == typeof(TimeSpan)) || (type == typeof(TimeSpan?)))
		{
			var ticks = RandomGenerator.NextLong(1, TimeSpan.FromDays(30).Ticks);
			return TimeSpan.FromTicks(ticks == 0 ? 1 : ticks);
		}
		if ((type == typeof(double)) || (type == typeof(double?)))
		{
			var value = RandomGenerator.NextDouble(-100000, 100000);
			return value == 0 ? 1.0 : value;
		}
		if ((type == typeof(float)) || (type == typeof(float?)))
		{
			return (float) RandomGenerator.NextDouble(-100000, 100000);
		}
		if ((type == typeof(decimal)) || (type == typeof(decimal?)))
		{
			return RandomGenerator.NextDecimal(-100000, 100000);
		}
		if ((type == typeof(Guid)) || (type == typeof(Guid?)))
		{
			return Guid.NewGuid();
		}
		if ((type == typeof(ShortGuid)) || (type == typeof(ShortGuid?)))
		{
			return ShortGuid.NewGuid();
		}
		if ((type == typeof(byte)) || (type == typeof(byte?)))
		{
			return (byte) RandomGenerator.NextInteger(1, byte.MaxValue);
		}
		if ((type == typeof(sbyte)) || (type == typeof(sbyte?)))
		{
			var value = RandomGenerator.NextInteger(sbyte.MinValue, sbyte.MaxValue);
			return (sbyte) (value == 0 ? 1 : value);
		}
		if ((type == typeof(IntPtr)) || (type == typeof(IntPtr?)))
		{
			var value = RandomGenerator.NextInteger(int.MinValue + 1, int.MaxValue - 1);
			return new IntPtr(value == 0 ? 1 : value);
		}
		if ((type == typeof(UIntPtr)) || (type == typeof(UIntPtr?)))
		{
			var value = (ulong) RandomGenerator.NextLong(uint.MinValue + 1, uint.MaxValue - 1);
			return new UIntPtr(value == 0 ? 1u : value);
		}
		if ((type == typeof(int)) || (type == typeof(int?)))
		{
			var value = RandomGenerator.NextInteger(
				requiredRange?.Minimum.ConvertTo<int>() ?? int.MinValue + 1,
				requiredRange?.Maximum.ConvertTo<int>() ?? int.MaxValue - 1
			);
			return value == 0 ? 1 : value;
		}
		if ((type == typeof(uint)) || (type == typeof(uint?)))
		{
			return (uint) RandomGenerator.NextInteger(1, int.MaxValue / 4);
		}
		if ((type == typeof(long)) || (type == typeof(long?)))
		{
			var value = RandomGenerator.NextLong(long.MinValue / 4, long.MaxValue / 4);
			return value == 0 ? 1 : value;
		}
		if ((type == typeof(ulong)) || (type == typeof(ulong?)))
		{
			return (ulong) RandomGenerator.NextLong(
				requiredRange?.Minimum.ConvertTo<long>() ?? 1,
				requiredRange?.Maximum.ConvertTo<long>() ?? long.MaxValue / 4
			);
		}
		if ((type == typeof(short)) || (type == typeof(short?)))
		{
			var value = RandomGenerator.NextInteger(short.MinValue / 4, short.MaxValue / 4);
			return (short) (value == 0 ? 1 : value);
		}
		if ((type == typeof(ushort)) || (type == typeof(ushort?)))
		{
			return (ushort) RandomGenerator.NextInteger(1, ushort.MaxValue / 4);
		}
		if (type == typeof(string))
		{
			return Guid.NewGuid().ToString();
		}

		if (type == typeof(Rectangle))
		{
			return new Rectangle(1, 2, 3, 4);
		}

		if (type == typeof(IHorizontalLocation))
		{
			return new HorizontalLocation();
		}

		if (type == typeof(IVerticalLocation))
		{
			return new VerticalLocation();
		}

		return customTypeFactory?.Invoke(type, arguments ?? [])
			?? type.CreateInstance(arguments ?? []);
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
	/// Converts data type to the code simplified type. Ex. Int16 to short, Single to float
	/// </summary>
	/// <param name="value"> The type to get C# code for. </param>
	/// <returns> The type in C# code format. </returns>
	public static string GetCodeTypeName(this object value)
	{
		return value switch
		{
			MethodInfo sValue => CSharpCodeWriter.GetCodeTypeName(sValue),
			_ => CSharpCodeWriter.GenerateCode(value?.GetType())
		};
	}

	/// <summary>
	/// Converts data type to the code simplified type. Ex. Int16 to short, Single to float
	/// </summary>
	/// <param name="type"> The type to get C# code for. </param>
	/// <returns> The type in C# code format. </returns>
	public static string GetCodeTypeName(this Type type)
	{
		if (_simplifiedTypeNames.TryGetValue(type, out var value))
		{
			var isNullableType = type.IsNullableType();
			return isNullableType ? $"{value}?" : value;
		}

		if (type.ImplementsType(typeof(Nullable<>)))
		{
			var baseType = type.FromNullableType();
			var name = GetCodeTypeName(baseType);
			return name + "?";
		}

		if (!type.IsGenericType)
		{
			return type.Name;
		}

		var typeName = type.Name;
		var index = typeName.IndexOf("`");
		if (index >= 0)
		{
			typeName = typeName.Substring(0, index);
		}

		var genericArguments = type.GetGenericArguments();
		return $"{typeName}<{string.Join(", ", genericArguments.Select(GetCodeTypeName))}>";
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
				var realType = type.GetRealTypeUsingReflection();
				var properties = Cache.GetPropertyDictionary(realType);
				return [..properties.Keys];
			}
			case UpdateableAction.UnwrapProxyEntity:
			{
				var realType = type.GetRealTypeUsingReflection();
				var properties = Cache.GetPropertyDictionary(realType);
				var virtuals = realType.GetVirtualPropertyNames();
				return [..properties.Keys.Except(virtuals)];
			}
			case UpdateableAction.SyncIncomingAdd:
			case UpdateableAction.SyncIncomingUpdate:
			case UpdateableAction.SyncOutgoing:
			{
				var realType = type.GetRealTypeUsingReflection();
				var properties = Cache
					.GetPropertyDictionary(realType)
					.Where(x =>
					{
						var a = x.Value.GetCustomAttribute<SyncPropertyAttribute>();
						return (a != null) && a.Supports(action);
					})
					.Select(x => x.Key)
					.ToList();
				return [..properties];
			}
			case UpdateableAction.None:
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

	public static Type[] GetGenericArgumentsRecursive(this Type type)
	{
		while (type != null)
		{
			if (type.IsGenericType)
			{
				return type.GenericTypeArguments;
			}

			if (type.IsGenericTypeDefinition)
			{
				var definition = type.GetGenericTypeDefinition();
				return definition.GenericTypeArguments;
			}

			type = type.BaseType;
		}

		return null;
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

	/// <summary>
	/// Determines whether [is numeric type] [the specified type].
	/// </summary>
	/// <param name="type"> The type. </param>
	/// <returns> <c> true </c> if [is numeric type] [the specified type]; otherwise, <c> false </c>. </returns>
	public static bool IsNumericType(this Type type)
	{
		switch (Type.GetTypeCode(type))
		{
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Decimal:
			case TypeCode.Double:
			case TypeCode.Single:
			{
				return true;
			}
			default:
			{
				return false;
			}
		}
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
	/// Convert type to <see cref="Nullable{T}" /> type.
	/// </summary>
	/// <param name="type"> The type to convert. </param>
	/// <returns> The type as Nullable. </returns>
	public static Type ToNullableType(this Type type)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;
		return type.IsValueType ? typeof(Nullable<>).GetCachedMakeGenericType(type) : type;
	}

	/// <summary>
	/// Try to convert the assembly name string into a type. Does not include version. Ex. System.String,mscorlib
	/// </summary>
	/// <param name="value"> The type assembly name string. </param>
	/// <param name="type"> The type to get the assembly name for. </param>
	/// <returns> True if the value was a valid type otherwise false. </returns>
	public static bool TryGetType(this string value, out Type type)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			type = null;
			return false;
		}

		try
		{
			type = Type.GetType(value);
			return type != null;
		}
		catch
		{
			type = null;
			return false;
		}
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