#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Text;

#endregion

#pragma warning disable IL2067
#pragma warning disable IL3050

namespace Cornerstone.Reflection;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
public static class SourceReflector
{
	#region Constants

	public const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

	public const DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes =
		System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors
		| System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors
		| System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

	#endregion

	#region Constructors

	static SourceReflector()
	{
		Lookup = [];
		Types = [];
	}

	#endregion

	#region Properties

	public static Dictionary<string, Type> Lookup { get; }

	public static Dictionary<Type, SourceTypeInfo> Types { get; }

	#endregion

	#region Methods

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void Add(SourceTypeInfo typeInfo)
	{
		if (typeInfo.Type == null)
		{
			return;
		}

		if (Types.TryAdd(typeInfo.Type, typeInfo))
		{
			Lookup.Add(typeInfo.Type.ToAssemblyName(), typeInfo.Type);
		}
	}

	public static object CreateCollectionInstance(Type collectionType, Type elementType, int elementsCount)
	{
		// Validate elementsCount
		if (elementsCount < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(elementsCount), "Elements count cannot be negative.");
		}

		// Handle arrays (T[] or Array)
		if (collectionType.IsArray)
		{
			return Array.CreateInstance(elementType, elementsCount);
		}

		// Handle IList<T> or other generic collections
		if (collectionType.IsGenericType)
		{
			var genericTypeDefinition = collectionType.GetGenericTypeDefinition();
			var genericElementType = collectionType.GetGenericArguments()[0];

			// Ensure elementType matches the generic type argument
			if (elementType != genericElementType)
			{
				throw new ArgumentException($"Provided elementType {elementType.Name} does not match collection's generic type {genericElementType.Name}.");
			}

			//if ((genericTypeDefinition == typeof(ISpeedyList<>)) ||
			//	collectionType.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ISpeedyList<>))))
			//{
			//	var listType = typeof(SpeedyList<>).MakeGenericType(elementType);
			//	return Activator.CreateInstance(listType);
			//}

			if ((genericTypeDefinition == typeof(IList<>)) ||
				collectionType.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IList<>))))
			{
				var listType = typeof(List<>).MakeGenericType(elementType);
				return Activator.CreateInstance(listType, elementsCount);
			}

			// If the type itself is a concrete generic type (e.g., List<T>)
			if (!collectionType.IsAbstract && !collectionType.IsInterface)
			{
				return Activator.CreateInstance(collectionType, elementsCount);
			}
		}

		var response = Activator.CreateInstance(collectionType);
		if (response != null)
		{
			return response;
		}

		// Handle non-generic IEnumerable (e.g., ArrayList)
		if (typeof(IEnumerable).IsAssignableFrom(collectionType) && !collectionType.IsGenericType)
		{
			return new ArrayList(elementsCount);
		}

		throw new ArgumentException($"Cannot create instance of type {collectionType.Name}.");
	}

	public static object CreateInstance(Type type, params object[] args)
	{
		var info = GetSourceType(type);
		return info == null ? null : CreateInstance(info, args);
	}

	public static object CreateInstance(string typeName, params object[] args)
	{
		var info = GetSourceType(typeName);
		return info == null ? null : CreateInstance(info, args);
	}

	public static object CreateInstance(SourceTypeInfo typeInfo, params object[] args)
	{
		args ??= [];

		// Fast path: parameterless public ctor when no args provided
		if (args.Length == 0)
		{
			var parameterless = typeInfo
				.DeclaredConstructors
				.FirstOrDefault(x =>
					x.Accessibility
						is SourceAccessibility.Public
						or SourceAccessibility.Internal
					&& (x.Parameters.Length == 0)
				);

			if (parameterless != null)
			{
				return parameterless.Invoke([])
					?? throw new InvalidOperationException("Parameterless ctor returned null");
			}

			// If we have a static factory-like pattern you support, handle here
		}

		// General case: find best matching constructor
		var candidates = typeInfo.DeclaredConstructors
			.Where(c =>
				c.Accessibility
					is SourceAccessibility.Public
					or SourceAccessibility.Internal
				&& (c.Parameters.Length >= args.Length)
				&& (c.Parameters.Count(p => !p.HasDefaultValue) <= args.Length)
			)
			.ToList();

		if (candidates.Count == 0)
		{
			throw new MissingMethodException($"No suitable constructor found on {typeInfo.Type.Name} for {args.Length} argument{(args.Length == 1 ? "" : "s")}.");
		}

		// Sort by "best match" — most specific first (more parameters usually = better match)
		candidates.Sort((a, b) => b.Parameters.Length.CompareTo(a.Parameters.Length));

		foreach (var ctor in candidates)
		{
			if (TryMatchAndConvert(ctor.Parameters, args, out var preparedArgs))
			{
				return ctor.Invoke(preparedArgs)
					?? throw new InvalidOperationException($"Constructor on {typeInfo.Type.Name} returned null");
			}
		}

		throw new MissingMethodException($"No constructor on {typeInfo.Type.Name} matches the supplied argument types.");
	}

	public static SourceTypeInfo CreateSourceTypeInfoUsingReflection(Type type)
	{
		var isEnum = type.IsEnum;
		var isStruct = type.IsValueType && !type.IsPrimitive && !isEnum;
		var response = new SourceTypeInfo
		{
			Type = type,
			Accessibility = type.IsPublic ? SourceAccessibility.Public : SourceAccessibility.Internal,
			BaseType = type.BaseType,
			Name = type.Name,
			IsAbstract = type.IsAbstract,
			IsClass = type.IsClass,
			IsEnum = isEnum,
			IsGenericType = type.IsGenericType,
			IsGenericTypeDefinition = type.IsGenericTypeDefinition,
			IsPartial = false,
			IsReadOnly = isStruct && (type.GetCustomAttribute<IsReadOnlyAttribute>() != null),
			IsReflected = true,
			IsStatic = type.IsAbstract && type.IsSealed,
			IsStruct = isStruct,
			Attributes = GetAttributes(type),
			DeclaredConstructorsInitializer = () => GetConstructors(type),
			DeclaredFieldsInitializer = () => GetFields(type),
			DeclaredInterfacesInitializer = () => GetInterfaces(type),
			DeclaredMethodsInitializer = () => GetMethods(type),
			DeclaredPropertiesInitializer = () => GetProperties(type)
		};

		return response;
	}

	public static EnumDetails[] GetEnumDetails<T>() where T : Enum
	{
		return GetEnumDetails(typeof(T));
	}

	public static EnumDetails[] GetEnumDetails(Type type)
	{
		return Cache.EnumDetails.GetOrAdd(type,
			add => GetRequiredSourceType(add)
				.DeclaredFields
				.OrderBy(x => x.Name)
				.Where(x => x.IsStatic)
				.Select(x => new EnumDetails
				{
					Name = x.Name,
					Value = x.GetValue(null),
					DisplayName = x.GetAttributeValue(StringFormatter.DisplayAttributeTypeFullName, nameof(DisplayAttribute.Name)),
					DisplayShortName = x.GetAttributeValue(StringFormatter.DisplayAttributeTypeFullName, nameof(DisplayAttribute.ShortName)),
					DisplayOrder = x.GetAttributeValue<int>(StringFormatter.DisplayAttributeTypeFullName, nameof(DisplayAttribute.Order))
				})
				.ToArray()
		);
	}

	public static ImmutableDictionary<T, EnumDetails> GetEnumDetailsDictionary<T>() where T : Enum
	{
		return GetEnumDetails(typeof(T))
			.ToImmutableDictionary(x => (T) x.Value, x => x);
	}

	public static T[] GetEnumValues<T>(params T[] except) where T : Enum
	{
		return GetRequiredSourceType(typeof(T))
			.DeclaredFields
			.Where(x => x.IsStatic)
			.Select(x => (T) x.GetValue(null)!)
			.Where(x => !except.Contains(x))
			.OrderBy(x => x)
			.ToArray();
	}

	public static object[] GetEnumValues(Type type)
	{
		return GetRequiredSourceType(type)
			.DeclaredFields
			.Where(x => x.IsStatic)
			.Select(x => x.GetValue(null)!)
			.OrderBy(x => x)
			.ToArray();
	}

	public static object GetMemberValue<T>(this T value, string name)
	{
		return value.TryGetMemberValue(name, out var m) ? m : null;
	}

	[return: NotNull]
	public static SourceTypeInfo GetRequiredSourceType<T>()
	{
		return GetRequiredSourceType(typeof(T));
	}

	[return: NotNull]
	public static SourceTypeInfo GetRequiredSourceType(Type type)
	{
		return GetSourceType(type)
			?? throw new KeyNotFoundException($"No type info found for type '{type}', please ensure that the type '{type}' has the [SourceReflectionAttribute]");
	}

	public static SourceTypeInfo GetSourceType<T>()
	{
		return GetSourceType(typeof(T));
	}

	public static SourceTypeInfo GetSourceType(object value)
	{
		var type = value?.GetType();
		return Types.GetValueOrDefault(type)
			?? CreateSourceTypeInfoUsingReflection(type);
	}

	public static SourceTypeInfo GetSourceType(Type type)
	{
		return Types.GetValueOrDefault(type)
			?? CreateSourceTypeInfoUsingReflection(type);
	}

	public static SourceTypeInfo GetSourceType(string name)
	{
		return Lookup.TryGetValue(name, out var found)
			? Types.GetValueOrDefault(found)
			: null;
	}

	public static bool TryGetMemberValue<T>(this T value, string name, out object memberValue)
	{
		var type = GetSourceType(value?.GetType() ?? typeof(T));
		var properties = type.GetProperties();
		var property = properties.FirstOrDefault(x => x.Name == name);
		if (property is { CanRead: true })
		{
			memberValue = property.PropertyInfo.GetValue(value);
			return true;
		}

		var backingFieldName = property != null ? $"<{property.Name}>k__BackingField" : null;
		var fields = type.GetFields();
		var field = fields.FirstOrDefault(x => x.Name == backingFieldName)
			?? type.GetFields().FirstOrDefault(x => x.Name == name);

		if (field == null)
		{
			memberValue = null;
			return false;
		}

		memberValue = field.GetValue(value);
		return true;
	}

	public static bool TrySetMemberValue<T>(this T value, string name, object memberValue)
	{
		var type = GetSourceType(value?.GetType() ?? typeof(T));
		var property = type.GetProperties().FirstOrDefault(x => x.Name == name);
		if (property is { CanWrite: true })
		{
			property.PropertyInfo.SetValue(value, memberValue);
			return true;
		}

		var backingFieldName = property != null ? $"<{property.Name}>k__BackingField" : null;
		var fields = type.GetFields();
		var field = fields.FirstOrDefault(x => x.Name == backingFieldName)
			?? type.GetFields().FirstOrDefault(x => x.Name == name);

		if (field == null)
		{
			return false;
		}

		field.SetValue(value, memberValue);
		return true;
	}

	internal static SourceAccessibility GetAccessibility(this FieldInfo field)
	{
		if (field.IsPublic)
		{
			return SourceAccessibility.Public;
		}
		if (field.IsFamilyAndAssembly)
		{
			return SourceAccessibility.ProtectedOrInternal;
		}
		if (field.IsFamily)
		{
			return SourceAccessibility.Protected;
		}
		if (field.IsAssembly)
		{
			return SourceAccessibility.Internal;
		}
		return SourceAccessibility.Private;
	}

	internal static SourceAccessibility GetAccessibility(this PropertyInfo property)
	{
		return property.CanRead ? property.GetMethod!.GetAccessibility() : property.SetMethod!.GetAccessibility();
	}

	internal static SourceAccessibility GetAccessibility(this MethodBase method)
	{
		if (method.IsPublic)
		{
			return SourceAccessibility.Public;
		}
		if (method.IsFamilyAndAssembly)
		{
			return SourceAccessibility.ProtectedOrInternal;
		}
		if (method.IsFamily)
		{
			return SourceAccessibility.Protected;
		}
		if (method.IsAssembly)
		{
			return SourceAccessibility.Internal;
		}
		return SourceAccessibility.Private;
	}

	private static SourceParameterInfo[] CreateParameterInfos(ParameterInfo[] parameterInfos)
	{
		var parameters = new SourceParameterInfo[parameterInfos.Length];
		for (var i = 0; i < parameterInfos.Length; i++)
		{
			var p = parameterInfos[i];
			parameters[i] = new SourceParameterInfo
			{
				Name = p.Name!,
				DefaultValue = p.DefaultValue,
				HasDefaultValue = p.HasDefaultValue,
				NullableAnnotation = SourceNullableAnnotation.None,
				ParameterType = p.ParameterType
			};
		}
		return parameters;
	}

	private static SourceAttributeInfo[] GetAttributes(Type type)
	{
		var attributes = type.GetCustomAttributes(false).OfType<Attribute>();
		return GetAttributes(attributes);
	}

	private static SourceAttributeInfo[] GetAttributes(IEnumerable<Attribute> attributes)
	{
		var response = attributes
			.Select(x =>
			{
				var at = x.GetType();
				var info = at.GetProperties();
				var response = new SourceAttributeInfo
				{
					FullyQualifiedName = at.FullName,
					FullyGlobalQualifiedName = $"global::{at.FullName}",
					Name = at.Name,
					NamedArguments = new Dictionary<string, object>(),
					Type = at
				};

				foreach (var p in info)
				{
					try
					{
						response.NamedArguments.Add(p.Name, p.GetValue(x));
					}
					catch
					{
						// ignore
					}
				}

				return response;
			});

		return response.ToArray();
	}

	private static SourceConstructorInfo[] GetConstructors(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException(nameof(type));
		}

		var constructors = type.GetConstructors(DeclaredOnlyLookup);
		var response = constructors
			.Select(x =>
				new SourceConstructorInfo
				{
					Accessibility = type.IsPublic
						? SourceAccessibility.Public
						: SourceAccessibility.Internal,
					Attributes = GetAttributes(x.GetCustomAttributes()),
					ConstructorInfo = x,
					Invoke = x.Invoke,
					IsStatic = x.IsStatic,
					Name = x.Name,
					Parameters = GetParameters(x.GetParameters())
				}
			)
			.ToArray();

		return response;
	}

	private static SourceFieldInfo[] GetFields(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException(nameof(type));
		}

		var methods = type.GetFields(DeclaredOnlyLookup);
		var response = methods
			.Select(x =>
				new SourceFieldInfo
				{
					Name = x.Name,
					FieldInfo = x,
					GetValue = x.GetValue
				}
			)
			.ToArray();

		return response;
	}

	private static SourceInterfaceInfo[] GetInterfaces(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException(nameof(type));
		}

		var methods = type.GetInterfaces();
		var response = methods
			.Select(x =>
				new SourceInterfaceInfo
				{
					Name = x.Name,
					FullyGlobalQualifiedName = $"global::{x.FullName}",
					FullyQualifiedName = x.FullName
				}
			)
			.ToArray();

		return response;
	}

	private static SourceMethodInfo[] GetMethods(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException(nameof(type));
		}

		var methods = type.GetMethods(DeclaredOnlyLookup);
		var response = methods
			.Select(x =>
				new SourceMethodInfo
				{
					Name = x.Name
				}
			)
			.ToArray();

		return response;
	}

	private static SourceParameterInfo[] GetParameters(ParameterInfo[] parameters)
	{
		return parameters
			.Select(p => new SourceParameterInfo
			{
				Name = p.Name,
				DefaultValue = p.DefaultValue,
				HasDefaultValue = p.HasDefaultValue,
				IsOut = p.IsOut,
				IsRef = p.ParameterType.IsByRef && !p.IsOut,
				NullableAnnotation = SourceNullableAnnotation.None,
				ParameterType = p.ParameterType
			})
			.ToArray();
	}

	private static SourcePropertyInfo[] GetProperties(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException(nameof(type));
		}

		var properties = type.GetProperties(DeclaredOnlyLookup)
			.Where(x => x.GetIndexParameters().Length == 0)
			.ToArray();

		var response = properties
			.Select(x =>
			{
				var canRead = x.CanRead;
				var canWrite = x.CanWrite;
				var getMethod = x.GetGetMethod(true);
				var setMethod = x.GetSetMethod(true);

				var info = new SourcePropertyInfo
				{
					Attributes = GetAttributes(x.GetCustomAttributes()),
					Name = x.Name,
					PropertyInfo = x,

					IsStatic = (x.GetMethod?.IsStatic == true) || (x.SetMethod?.IsStatic == true),
					IsVirtual = (x.GetMethod?.IsVirtual == true) || (x.SetMethod?.IsVirtual == true),
					IsAbstract = (x.GetMethod?.IsAbstract == true) || (x.SetMethod?.IsAbstract == true),

					CanRead = canRead,
					CanWrite = canWrite,
					IsReadOnly = canRead && !canWrite,
					IsInitOnly = IsInitOnlyProperty(x),

					Accessibility = x.GetAccessibility(),
					AccessibilityForGet = getMethod != null ? getMethod.GetAccessibility() : SourceAccessibility.None,
					AccessibilityForSet = setMethod != null ? setMethod.GetAccessibility() : SourceAccessibility.None,

					IsIndexer = false,
					IndexerParameters = [],
					IsRequired = x.GetCustomAttribute<RequiredMemberAttribute>() != null,
					IsDependencyInjected = false,

					GetValue = v => getMethod.Invoke(v, null),
					SetValue = (o, v) => setMethod.Invoke(o, [v])
				};

				return info;
			});

		return response.ToArray();
	}

	private static bool IsInitOnlyProperty(PropertyInfo prop)
	{
		if (!prop.CanWrite)
		{
			return false;
		}

		var setter = prop.SetMethod;
		if (setter == null)
		{
			return false;
		}

		var requiredMods = setter.ReturnParameter?.GetRequiredCustomModifiers();
		return requiredMods.Contains(typeof(IsExternalInit));
	}

	private static bool TryMatchAndConvert(
		SourceParameterInfo[] parameters,
		object[] suppliedArgs,
		out object[] prepared)
	{
		prepared = null;

		if (parameters.Length < suppliedArgs.Length)
		{
			return false;
		}

		var result = new object[parameters.Length];

		for (var i = 0; i < parameters.Length; i++)
		{
			var param = parameters[i];

			if (i < suppliedArgs.Length)
			{
				var arg = suppliedArgs[i];
				if (arg == null)
				{
					if (param.ParameterType.IsValueType && (Nullable.GetUnderlyingType(param.ParameterType) == null))
					{
						// can't pass null to non-nullable struct
						return false;
					}
					result[i] = null;
				}
				else if (param.ParameterType.IsAssignableFrom(arg.GetType()))
				{
					result[i] = arg;
				}

				// todo: use converter once migrated
				//else if (TryConvert(arg, param.ParameterType, out var converted))
				//{
				//	result[i] = converted;
				//}
				else
				{
					return false;
				}
			}
			else // optional parameter
			{
				if (!param.HasDefaultValue)
				{
					// shouldn't happen due to earlier filter
					return false;
				}

				// assuming you store boxed default value
				result[i] = param.DefaultValue;
			}
		}

		prepared = result;
		return true;
	}

	#endregion
}