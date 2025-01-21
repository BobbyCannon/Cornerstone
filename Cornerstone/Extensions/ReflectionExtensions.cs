#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for reflection based actions.
/// </summary>
public static class ReflectionExtensions
{
	#region Constants

	/// <summary>
	/// Default flags for cached access.
	/// </summary>
	public const BindingFlags DefaultPrivateFlags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic;

	/// <summary>
	/// Default flags for cached access.
	/// </summary>
	public const BindingFlags DefaultPublicFlags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public;

	/// <summary>
	/// Flags for direct member only.
	/// </summary>
	public const BindingFlags DirectMemberFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;

	#endregion

	#region Fields

	private static readonly ConcurrentDictionary<CacheKey, MethodInfo> _genericMethod;
	private static readonly ConcurrentDictionary<CacheKey, MethodInfo> _makeGenericMethods;
	private static readonly ConcurrentDictionary<CacheKey, Type> _makeGenericTypes;
	private static readonly ConcurrentDictionary<CacheKey, ParameterInfo[]> _methodParameters;
	private static readonly ConcurrentDictionary<CacheKey, Type[]> _methodsGenericArgumentInfos;
	private static readonly ConcurrentDictionary<CacheKey, ConstructorInfo> _typeConstructors;
	private static readonly ConcurrentDictionary<CacheKey, ConstructorInfo[]> _typeConstructorsAll;
	private static readonly ConcurrentDictionary<CacheKey, Attribute> _typeCustomAttribute;
	private static readonly ConcurrentDictionary<CacheKey, FieldInfo[]> _typeFieldInfos;
	private static readonly ConcurrentDictionary<CacheKey, Dictionary<string, PropertyInfo>> _typeJsonPropertyNameDictionaries;
	private static readonly ConcurrentDictionary<CacheKey, MethodInfo> _typeMethodInfos;
	private static readonly ConcurrentDictionary<CacheKey, MethodInfo[]> _typeMethodsInfos;
	private static readonly ConcurrentDictionary<CacheKey, Dictionary<string, PropertyInfo>> _typePropertyInfoDictionaries;
	private static readonly ConcurrentDictionary<CacheKey, PropertyInfo[]> _typePropertyInfos;
	private static readonly ConcurrentDictionary<CacheKey, PropertyInfo[]> _typeVirtualPropertyInfos;

	#endregion

	#region Constructors

	static ReflectionExtensions()
	{
		_genericMethod = new ConcurrentDictionary<CacheKey, MethodInfo>();
		_makeGenericMethods = new ConcurrentDictionary<CacheKey, MethodInfo>();
		_makeGenericTypes = new ConcurrentDictionary<CacheKey, Type>();
		_methodParameters = new ConcurrentDictionary<CacheKey, ParameterInfo[]>();
		_methodsGenericArgumentInfos = new ConcurrentDictionary<CacheKey, Type[]>();
		_typeCustomAttribute = new ConcurrentDictionary<CacheKey, Attribute>();
		_typeConstructors = new ConcurrentDictionary<CacheKey, ConstructorInfo>();
		_typeConstructorsAll = new ConcurrentDictionary<CacheKey, ConstructorInfo[]>();
		_typeFieldInfos = new ConcurrentDictionary<CacheKey, FieldInfo[]>();
		_typeJsonPropertyNameDictionaries = new ConcurrentDictionary<CacheKey, Dictionary<string, PropertyInfo>>();
		_typeMethodInfos = new ConcurrentDictionary<CacheKey, MethodInfo>();
		_typeMethodsInfos = new ConcurrentDictionary<CacheKey, MethodInfo[]>();
		_typePropertyInfoDictionaries = new ConcurrentDictionary<CacheKey, Dictionary<string, PropertyInfo>>();
		_typePropertyInfos = new ConcurrentDictionary<CacheKey, PropertyInfo[]>();
		_typeVirtualPropertyInfos = new ConcurrentDictionary<CacheKey, PropertyInfo[]>();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Get the constructor of provided types.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the constructor for. </param>
	/// <param name="parameterTypes">
	/// An array of type objects representing the number, order, and type of the parameters for the constructor to get.-or-
	/// An empty array of type objects (as provided by the EmptyTypes field) to get a constructor that takes no parameters.
	/// </param>
	/// <param name="flags"> The flags used to query with. </param>
	/// <returns> The constructor info. </returns>
	public static ConstructorInfo GetCachedConstructor(this Type type, Type[] parameterTypes, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var label = GetLabel(type, parameterTypes);
		var key = new CacheKey(type, typeFlags, label);
		return _typeConstructors.GetOrAdd(key, _ => type.GetConstructor(parameterTypes));
	}

	/// <summary>
	/// Get the constructors for a types.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the generic arguments for. </param>
	/// <param name="flags"> The flags used to query with. </param>
	/// <returns> The method information with generics. </returns>
	public static ConstructorInfo[] GetCachedConstructors(this Type type, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var key = new CacheKey(type, typeFlags, type.FullName);
		return _typeConstructorsAll.GetOrAdd(key, _ => type.GetConstructors(typeFlags));
	}

	/// <summary>
	/// Gets a custom attribute for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <typeparam name="T"> The type of the attribute. </typeparam>
	/// <param name="type"> The type to get the properties for. </param>
	/// <returns> The list of properties for the type. </returns>
	public static T GetCachedCustomAttribute<T>(this Type type) where T : Attribute
	{
		var key = new CacheKey(type ?? throw new InvalidOperationException(), BindingFlags.Public, typeof(T).FullName);
		return (T) _typeCustomAttribute.GetOrAdd(key, _ => type.GetCustomAttribute<T>(false));
	}

	/// <summary>
	/// Gets a field by name for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="item"> The item to get the field for. </param>
	/// <param name="name"> The type field name to locate. </param>
	/// <param name="flags"> The flags used to query with. Defaults to <see cref="DefaultPrivateFlags" /> if not provided. </param>
	/// <returns> The field information for the type. </returns>
	public static FieldInfo GetCachedField(this object item, string name, BindingFlags? flags = null)
	{
		return GetCachedField(item.GetType(), name, flags);
	}

	/// <summary>
	/// Gets a field by name for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the fields for. </param>
	/// <param name="name"> The type field name to locate. </param>
	/// <param name="flags"> The flags used to query with. Defaults to <see cref="DefaultPrivateFlags" /> if not provided. </param>
	/// <returns> The field information for the type. </returns>
	public static FieldInfo GetCachedField(this Type type, string name, BindingFlags? flags = null)
	{
		return type.GetCachedFields(flags).FirstOrDefault(x => x.Name == name);
	}

	/// <summary>
	/// Gets a list of fields for the provided item. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="item"> The item to get the fields for. </param>
	/// <param name="flags"> The flags used to query with. Defaults to <see cref="DefaultPrivateFlags" /> if not provided. </param>
	/// <returns> The list of field infos for the item. </returns>
	public static IList<FieldInfo> GetCachedFields(this object item, BindingFlags? flags = null)
	{
		return item.GetType().GetCachedFields(flags);
	}

	/// <summary>
	/// Gets a list of fields for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the fields for. </param>
	/// <param name="flags"> The flags used to query with. Defaults to <see cref="DefaultPrivateFlags" /> if not provided. </param>
	/// <returns> The list of field infos for the type. </returns>
	public static IList<FieldInfo> GetCachedFields(this Type type, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPrivateFlags;
		var key = new CacheKey(type ?? throw new InvalidOperationException(), typeFlags, null);
		return _typeFieldInfos.GetOrAdd(key, _ => type.GetFields(typeFlags));
	}

	/// <summary>
	/// Gets a list of generic arguments for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the generic arguments for. </param>
	/// <returns> The list of generic arguments for the type of the value. </returns>
	public static IList<Type> GetCachedGenericArguments(this Type type)
	{
		var key = new CacheKey(type ?? throw new InvalidOperationException(), BindingFlags.Public, null);
		return _methodsGenericArgumentInfos.GetOrAdd(key,
			_ => type.GetGenericArguments());
	}

	/// <summary>
	/// Gets a list of generic arguments for the provided method information.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="info"> The method information to get the generic arguments for. </param>
	/// <param name="flags"> The flags used to query with. </param>
	/// <returns> The list of generic arguments for the method information of the value. </returns>
	public static IList<Type> GetCachedGenericArguments(this MethodInfo info, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var name = info.IsGenericMethod ? info.GetCodeTypeName() : info.Name;
		var key = new CacheKey(info.ReflectedType ?? throw new InvalidOperationException(), typeFlags, name);
		return _methodsGenericArgumentInfos.GetOrAdd(key, _ => info.GetGenericArguments());
	}

	/// <summary>
	/// Get a method info from a generic type with the provided method types.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the generic arguments for. </param>
	/// <param name="methodName"> The name of the method. </param>
	/// <param name="methodGenericTypes"> An array of types to create the generic method with. </param>
	/// <param name="parameterTypes"> An array of type objects representing the number, order, and type of the parameters for the method to get.-or- An empty array of type objects (as provided by the EmptyTypes field) to get a method that takes no parameters. </param>
	/// <param name="flags"> The flags used to query with. </param>
	/// <returns> The method information with generics. </returns>
	public static MethodInfo GetCachedGenericMethod(this Type type, string methodName, Type[] methodGenericTypes, Type[] parameterTypes, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var label = GetLabel(type, methodName, methodGenericTypes, parameterTypes);
		var key = new CacheKey(type, typeFlags, label);
		return _genericMethod.GetOrAdd(key, _ =>
		{
			var methodInfos = type.GetCachedMethods(flags);
			var methods = methodInfos.Where(m => m.Name == methodName).ToList();

			foreach (var method in methods)
			{
				var gCount = method.GetCachedGenericArguments(flags).Count;
				var pCount = method.GetCachedParameters(flags).Count;

				if ((methodGenericTypes.Length != gCount) || (parameterTypes.Length != pCount))
				{
					continue;
				}

				var m = method.GetCachedMakeGenericMethod(methodGenericTypes);
				return m;
			}

			return null;
		});
	}

	/// <summary>
	/// Gets a list of property information for the provided type based;
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the properties for. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static IDictionary<string, PropertyInfo> GetCachedJsonPropertyNameDictionary(this Type type, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var key = new CacheKey(type ?? throw new InvalidOperationException(), typeFlags, null);
		return _typeJsonPropertyNameDictionaries
			.GetOrAdd(key, _ =>
			{
				var properties = type.GetProperties(typeFlags);
				var attributes = properties
					.ToDictionary(x => x, x => x.GetCustomAttribute<JsonPropertyNameAttribute>())
					.Where(x => x.Value != null);

				var response = attributes
					.ToDictionary(p => p.Value.Name, p => p.Key, StringComparer.InvariantCultureIgnoreCase);

				return response;
			});
	}

	/// <summary>
	/// Substitutes the elements of an array of types for the type parameters of the current generic method definition, and returns a
	/// MethodInfo object representing the resulting constructed method. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="info"> The property information to get the generic arguments for. </param>
	/// <param name="arguments"> An array of types to be substituted for the type parameters of the current generic method definition. </param>
	/// <returns> The method information with generics. </returns>
	public static MethodInfo GetCachedMakeGenericMethod(this MethodInfo info, params Type[] arguments)
	{
		var name = info.IsGenericMethod ? info.GetType().GetCodeTypeName() : info.Name;
		var label = name + string.Join(",", arguments.Select(x => x.FullName));
		var key = new CacheKey(info.ReflectedType ?? throw new InvalidOperationException(), BindingFlags.Public, label);
		return _makeGenericMethods.GetOrAdd(key, _ => info.MakeGenericMethod(arguments));
	}

	/// <summary>
	/// Get the type of generic with the provided types. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the generic arguments for. </param>
	/// <param name="genericTypes"> An array of types to create the generic type with. </param>
	/// <returns> The method information with generics. </returns>
	public static Type GetCachedMakeGenericType(this Type type, params Type[] genericTypes)
	{
		if (!type.IsGenericType || !type.IsGenericTypeDefinition)
		{
			throw new ArgumentException("The type provided is not a generic type or is a generic type definition.");
		}

		var key = new CacheKey(type, BindingFlags.Public, string.Join(", ", genericTypes.Select(x => x.FullName)));
		return _makeGenericTypes.GetOrAdd(key, _ => type.MakeGenericType(genericTypes));
	}

	/// <summary>
	/// Searches for the specified public method whose parameters match the specified argument types.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="value"> The value to get the methods for. </param>
	/// <param name="name"> The string containing the name of the public method to get. </param>
	/// <param name="parameterTypes"> An array of type objects representing the number, order, and type of the parameters for the method to get.-or- An empty array of type objects (as provided by the EmptyTypes field) to get a method that takes no parameters. </param>
	/// <returns> An object representing the public method whose parameters match the specified argument types, if found; otherwise null. </returns>
	public static MethodInfo GetCachedMethod(this object value, string name, params Type[] parameterTypes)
	{
		return GetCachedMethod(value?.GetType(), name, parameterTypes);
	}

	/// <summary>
	/// Searches for the specified public method whose parameters match the specified argument types.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the method for. </param>
	/// <param name="name"> The string containing the name of the public method to get. </param>
	/// <param name="parameterTypes"> An array of type objects representing the number, order, and type of the parameters for the method to get.-or- An empty array of type objects (as provided by the EmptyTypes field) to get a method that takes no parameters. </param>
	/// <returns> An object representing the public method whose parameters match the specified argument types, if found; otherwise null. </returns>
	public static MethodInfo GetCachedMethod(this Type type, string name, params Type[] parameterTypes)
	{
		return GetCachedMethod(type, name, DefaultPublicFlags, parameterTypes);
	}

	/// <summary>
	/// Searches for the specified public method whose parameters match the specified argument types.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="value"> The value to get the methods for. </param>
	/// <param name="name"> The string containing the name of the public method to get. </param>
	/// <param name="flags"> The flags used to query with. </param>
	/// <param name="parameterTypes"> An array of type objects representing the number, order, and type of the parameters for the method to get.-or- An empty array of type objects (as provided by the EmptyTypes field) to get a method that takes no parameters. </param>
	/// <returns> An object representing the public method whose parameters match the specified argument types, if found; otherwise null. </returns>
	public static MethodInfo GetCachedMethod(this object value, string name, BindingFlags flags, params Type[] parameterTypes)
	{
		return GetCachedMethod(value?.GetType(), name, flags, parameterTypes);
	}

	/// <summary>
	/// Searches for the specified public method whose parameters match the specified argument types.
	/// The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the method for. </param>
	/// <param name="name"> The string containing the name of the public method to get. </param>
	/// <param name="flags"> The flags used to query with. </param>
	/// <param name="parameterTypes"> An array of type objects representing the number, order, and type of the parameters for the method to get.-or- An empty array of type objects (as provided by the EmptyTypes field) to get a method that takes no parameters. </param>
	/// <returns> An object representing the public method whose parameters match the specified argument types, if found; otherwise null. </returns>
	public static MethodInfo GetCachedMethod(this Type type, string name, BindingFlags flags, params Type[] parameterTypes)
	{
		var key = new CacheKey(type, flags, name);
		return _typeMethodInfos.GetOrAdd(key,
			_ => parameterTypes.Any()
				? type.GetMethod(name, parameterTypes)
				: type.GetMethod(name)
		);
	}

	/// <summary>
	/// Gets a list of methods for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the methods for. </param>
	/// <param name="flags"> The flags used to query with. </param>
	/// <returns> The list of method infos for the type. </returns>
	public static IList<MethodInfo> GetCachedMethods(this Type type, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var key = new CacheKey(type ?? throw new InvalidOperationException(), typeFlags, null);
		return _typeMethodsInfos.GetOrAdd(key, type.GetMethods(typeFlags));
	}

	/// <summary>
	/// Gets a list of methods for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="value"> The value to get the methods for. </param>
	/// <param name="flags"> The flags used to query with. </param>
	/// <returns> The list of method infos for the type. </returns>
	public static IList<MethodInfo> GetCachedMethods(this object value, BindingFlags? flags = null)
	{
		return GetCachedMethods(value?.GetType(), flags);
	}

	/// <summary>
	/// Gets a list of parameter infos for the provided method info. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="info"> The method info to get the parameters for. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of parameter infos for the type. </returns>
	public static IList<ParameterInfo> GetCachedParameters(this MethodInfo info, BindingFlags? flags = null)
	{
		if (info == null)
		{
			throw new InvalidOperationException();
		}

		var typeFlags = flags ?? DefaultPublicFlags;
		var name = info.IsGenericMethod ? info.GetType().GetCodeTypeName() : info.Name;
		var key = new CacheKey(info.ReflectedType ?? throw new InvalidOperationException(), typeFlags, name);
		return _methodParameters.GetOrAdd(key, _ => info.GetParameters());
	}

	/// <summary>
	/// Gets a list of property information for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="value"> The value to get the properties for. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static IList<PropertyInfo> GetCachedProperties(this object value, BindingFlags? flags = null)
	{
		return value?.GetType().GetCachedProperties(flags);
	}

	/// <summary>
	/// Gets a list of property information for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the properties for. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static IList<PropertyInfo> GetCachedProperties(this Type type, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var key = new CacheKey(type ?? throw new InvalidOperationException(), typeFlags, null);
		return _typePropertyInfos.GetOrAdd(key, _ =>
		{
			var response = type
				.GetProperties(typeFlags)
				.GroupBy(x => x.Name)
				.Select(x => x.FirstOrDefault())
				.Where(x => x != null)
				.OrderBy(x => x.Name)
				.ToArray();
			return response;
		});
	}

	/// <summary>
	/// Gets the information for the provided type and property name. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="value"> The value to get the property for. </param>
	/// <param name="lookup"> The name lookup of the property to be queried. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static PropertyInfo GetCachedProperty<T>(this T value, Expression<Func<T, object>> lookup, BindingFlags? flags = null)
	{
		if (!lookup.TryGetPropertyName(out var name))
		{
			return null;
		}

		return GetCachedProperty(value.GetType(), name, flags);
	}

	/// <summary>
	/// Gets the information for the provided type and property name. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="value"> The value to get the property for. </param>
	/// <param name="name"> The name of the property to be queried. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static PropertyInfo GetCachedProperty<T>(this T value, string name, BindingFlags? flags = null)
	{
		return GetCachedProperty(value.GetType(), name, flags);
	}

	/// <summary>
	/// Gets the information for the provided type and property name. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the property for. </param>
	/// <param name="name"> The name of the property to be queried. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static PropertyInfo GetCachedProperty(this Type type, string name, BindingFlags? flags = null)
	{
		return GetCachedProperties(type, flags).FirstOrDefault(x => x.Name == name);
	}

	/// <summary>
	/// Gets a list of property information for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="value"> The value to get the properties for. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static IDictionary<string, PropertyInfo> GetCachedPropertyDictionary<T>(this T value, BindingFlags? flags = null)
	{
		return GetCachedPropertyDictionary(value.GetType(), flags);
	}

	/// <summary>
	/// Gets a list of property information for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the properties for. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static IDictionary<string, PropertyInfo> GetCachedPropertyDictionary(this Type type, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var key = new CacheKey(type ?? throw new InvalidOperationException(), typeFlags, null);
		return _typePropertyInfoDictionaries.GetOrAdd(key, _ =>
		{
			var properties = type.GetProperties(typeFlags).GroupBy(x => x.Name);
			return properties.ToDictionary(p => p.Key, p => p.First(), StringComparer.InvariantCultureIgnoreCase);
		});
	}

	/// <summary>
	/// Gets a list of virtual property types for the provided type. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The type to get the properties for. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of properties for the type. </returns>
	public static IList<PropertyInfo> GetCachedVirtualProperties(this Type type, BindingFlags? flags = null)
	{
		var typeFlags = flags ?? DefaultPublicFlags;
		var key = new CacheKey(type ?? throw new InvalidOperationException(), typeFlags, null);

		return _typeVirtualPropertyInfos.GetOrAdd(key, _ =>
		{
			return type
				.GetCachedProperties(typeFlags)
				.Where(p => p.IsVirtual())
				.OrderBy(p => p.Name)
				.ToArray();
		});
	}

	/// <summary>
	/// Get the name of the expression.
	/// </summary>
	/// <param name="expression"> The expression to process. </param>
	/// <returns> The name of the expression. </returns>
	public static string GetExpressionName(this LambdaExpression expression)
	{
		if (expression.Body is UnaryExpression unaryExpression)
		{
			return ((dynamic) unaryExpression.Operand).Member?.Name;
		}

		return ((dynamic) expression).Body.Member.Name;
	}

	/// <summary>
	/// Gets the public or private member using reflection.
	/// </summary>
	/// <param name="value"> The value that contains the member. </param>
	/// <param name="memberName"> The name of the field or property to get the value of. </param>
	/// <returns> The value of member. </returns>
	public static object GetMemberValue(this object value, string memberName)
	{
		var memberInfo = GetCachedMember(value, memberName);

		return memberInfo switch
		{
			PropertyInfo propertyInfo => propertyInfo.GetValue(value, null),
			FieldInfo fieldInfo => fieldInfo.GetValue(value),
			_ => throw new Exception("The member was not found.")
		};
	}

	/// <summary>
	/// Gets the member value of an object using the provider info.
	/// </summary>
	/// <param name="memberInfo"> The info for the member. </param>
	/// <param name="value"> </param>
	/// <returns> The value of the value member. </returns>
	/// <exception cref="NotImplementedException"> The member info is not a field or property. </exception>
	public static object GetMemberValue(this MemberInfo memberInfo, object value)
	{
		return memberInfo.MemberType switch
		{
			MemberTypes.Field => ((FieldInfo) memberInfo).GetValue(value),
			MemberTypes.Property => ((PropertyInfo) memberInfo).GetValue(value),
			_ => throw new NotImplementedException()
		};
	}

	/// <summary>
	/// Gets the real type of the entity. For use with proxy entities.
	/// </summary>
	/// <param name="item"> The object to process. </param>
	/// <returns> The real base type for the proxy or just the initial type if it is not a proxy. </returns>
	public static Type GetRealTypeUsingReflection(this object item)
	{
		var type = item.GetType();
		return GetRealTypeUsingReflection(type);
	}

	/// <summary>
	/// Gets the real type of the entity. For use with proxy entities.
	/// </summary>
	/// <param name="type"> The type to process. </param>
	/// <returns> The real base type for the proxy or just the initial type if it is not a proxy. </returns>
	public static Type GetRealTypeUsingReflection(this Type type)
	{
		var isProxy = (type.FullName?.Contains("System.Data.Entity.DynamicProxies") == true)
			|| (type.FullName?.Contains("Castle.Proxies") == true);

		return isProxy ? type.BaseType : type;
	}

	/// <summary>
	/// Gets a list of virtual property names. The results are cached so the next query is much faster.
	/// </summary>
	/// <param name="type"> The value to get the property names for. </param>
	/// <param name="flags"> The flags to find properties by. Defaults to Public, Instance, Flatten Hierarchy </param>
	/// <returns> The list of virtual property names for the type. </returns>
	public static IEnumerable<string> GetVirtualPropertyNames(this Type type, BindingFlags? flags = null)
	{
		return GetCachedVirtualProperties(type, flags)
			.Select(x => x.Name)
			.ToArray();
	}

	/// <summary>
	/// Determine if the property is an abstract property.
	/// </summary>
	/// <param name="info"> The info to process. </param>
	/// <returns> True if the accessor is abstract. </returns>
	public static bool IsAbstract(this PropertyInfo info)
	{
		return (info.CanRead
				&& (info.GetMethod != null)
				&& info.GetMethod.IsAbstract)
			|| (info.CanWrite
				&& (info.SetMethod != null)
				&& info.SetMethod.IsAbstract);
	}

	public static bool IsDelegate(this Type type)
	{
		return (type == typeof(MulticastDelegate))
			|| (type.BaseType == typeof(MulticastDelegate));
	}

	public static bool IsIndexer(this PropertyInfo type)
	{
		return type.GetIndexParameters().Length > 0;
	}

	/// <summary>
	/// Determine if the property is a virtual property.
	/// </summary>
	/// <param name="info"> The info to process. </param>
	/// <returns> True if the accessor is virtual. </returns>
	public static bool IsVirtual(this PropertyInfo info)
	{
		return (info.CanRead
				&& (info.GetMethod != null)
				&& info.GetMethod.IsVirtual
				&& !info.GetMethod.IsFinal
				&& info.GetMethod.Attributes.HasFlag(MethodAttributes.VtableLayoutMask))
			|| (info.CanWrite
				&& (info.SetMethod != null)
				&& info.SetMethod.IsVirtual
				&& !info.SetMethod.IsFinal
				&& info.SetMethod.Attributes.HasFlag(MethodAttributes.VtableLayoutMask));
	}

	/// <summary>
	/// Gets the public or private member using reflection.
	/// </summary>
	/// <param name="obj"> The target object. </param>
	/// <param name="lookup"> The name lookup of the property to be updated. </param>
	/// <param name="newValue"> The new value to be set. </param>
	/// <returns> Old Value </returns>
	public static T SetMemberValue<T>(this T obj, Expression<Func<T, object>> lookup, object newValue)
	{
		if (!lookup.TryGetPropertyName(out var name))
		{
			return obj;
		}

		SetMemberValue(obj, name, newValue);
		return obj;
	}

	/// <summary>
	/// Gets the public or private member using reflection.
	/// </summary>
	/// <param name="obj"> The target object. </param>
	/// <param name="memberName"> Name of the field or property. </param>
	/// <param name="newValue"> The new value to be set. </param>
	/// <returns> Old Value </returns>
	public static object SetMemberValue(this object obj, string memberName, object newValue)
	{
		var memberInfo = GetCachedMember(obj, memberName);

		if (memberInfo == null)
		{
			throw new Exception("memberName");
		}

		switch (memberInfo)
		{
			case PropertyInfo propertyInfo:
			{
				var oldValue = propertyInfo.CanRead
					? obj.GetMemberValue(memberName)
					: null;

				if (propertyInfo.CanWrite)
				{
					propertyInfo.SetValue(obj, newValue, null);
				}

				return oldValue;
			}
			case FieldInfo fieldInfo:
			{
				var oldValue = obj.GetMemberValue(memberName);
				fieldInfo.SetValue(obj, newValue);
				return oldValue;
			}
			default:
			{
				throw new Exception();
			}
		}
	}

	/// <summary>
	/// Gets the public or private member using reflection.
	/// </summary>
	/// <param name="value"> The value that contains the member. </param>
	/// <param name="memberName"> The name of the field or property to get the value of. </param>
	/// <param name="memberValue"> The value of the member. </param>
	/// <returns> True if the member value was read otherwise false. </returns>
	public static bool TryGetMemberValue(this object value, string memberName, out object memberValue)
	{
		var memberInfo = GetCachedMember(value, memberName);

		switch (memberInfo)
		{
			case PropertyInfo propertyInfo:
			{
				memberValue = propertyInfo.GetValue(value, null);
				return true;
			}
			case FieldInfo fieldInfo:
			{
				memberValue = fieldInfo.GetValue(value);
				return true;
			}
			default:
			{
				memberValue = null;
				return false;
			}
		}
	}

	/// <summary>
	/// Tries to set the property on an object. Fails if the property is not found or is readonly.
	/// </summary>
	/// <param name="obj"> The target value. </param>
	/// <param name="expression"> Name of the property. </param>
	/// <param name="value"> The new value to be set. </param>
	/// <returns> True if the property was set otherwise false. </returns>
	public static bool TrySetProperty<T, T2>(this T obj, Expression<Func<T, T2>> expression, T2 value)
	{
		try
		{
			var type = obj.GetType();
			var name = expression.GetExpressionName();
			var propertyDetails = type.GetCachedProperty(name);
			if ((propertyDetails == null) || !propertyDetails.CanWrite)
			{
				return false;
			}
			propertyDetails.SetValue(obj, value);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static MemberInfo FindField(Type type, string memberName, BindingFlags flags)
	{
		var field = type.GetCachedField(memberName, flags);
		if (field != null)
		{
			return field;
		}

		if (type.BaseType == typeof(object))
		{
			return null;
		}

		return FindField(type.BaseType, memberName, flags);
	}

	private static MemberInfo GetCachedMember(object obj, string memberName)
	{
		var type = obj?.GetType() ?? throw new ArgumentNullException(nameof(obj));
		var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
		var info = (MemberInfo) type.GetCachedProperty(memberName, flags);

		if (info != null)
		{
			return info;
		}

		info = FindField(obj.GetType(), memberName, flags);
		return info;
	}

	private static string GetLabel(Type type, Type[] parameterTypes)
	{
		var label = $"{type.FullName}.{string.Join(",", parameterTypes.Select(x => x.FullName))}";
		return label;
	}

	private static string GetLabel(Type type, Type[] genericTypes, Type[] parameterTypes)
	{
		var label = $"{type.FullName}<{string.Join(",", genericTypes.Select(x => x.FullName))}>.{string.Join(",", parameterTypes.Select(x => x.FullName))}";
		return label;
	}

	private static string GetLabel(Type type, string methodName, Type[] genericTypes, Type[] parameterTypes)
	{
		var label = $"{type.FullName}.{methodName}<{string.Join(",", genericTypes.Select(x => x.FullName))}>.{string.Join(",", parameterTypes.Select(x => x.FullName))}";
		return label;
	}

	#endregion

	#region Structures

	public struct CacheKey : IEquatable<CacheKey>, IEqualityComparer<CacheKey>
	{
		#region Constructors

		public CacheKey(Type type, BindingFlags flags, string label)
		{
			Type = type;
			Flags = flags;
			Label = label;
		}

		#endregion

		#region Properties

		public BindingFlags Flags { get; }

		public string Label { get; }

		public Type Type { get; }

		#endregion

		#region Methods

		/// <inheritdoc />
		public bool Equals(CacheKey other)
		{
			return (Type.FullName == other.Type.FullName)
				&& (Flags == other.Flags)
				&& Equals(Label, other.Label);
		}

		/// <inheritdoc />
		public bool Equals(CacheKey x, CacheKey y)
		{
			return x.Equals(y);
		}

		public static CacheKey From(MethodInfo info)
		{
			return new CacheKey();
		}

		/// <inheritdoc />
		public int GetHashCode(CacheKey obj)
		{
			return HashCodeCalculator.Combine(Type, Flags, Label);
		}

		#endregion
	}

	#endregion
}