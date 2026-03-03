#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public class SourceTypeInfo : SourceMemberInfo
{
	#region Fields

	private SourceTypeInfo[] _baseTypes;
	private SourceConstructorInfo[] _constructors;
	private SourceConstructorInfo[] _declaredConstructors;
	private SourceFieldInfo[] _declaredFields;
	private SourceInterfaceInfo[] _declaredInterfaces;
	private SourceMethodInfo[] _declaredMethods;
	private SourcePropertyInfo[] _declaredProperties;
	private SourceFieldInfo[] _fields;
	private SourceMethodInfo[] _methods;
	private SourcePropertyInfo[] _properties;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; init; }

	public Type BaseType { get; init; }

	public SourceConstructorInfo[] DeclaredConstructors => _declaredConstructors ??= DeclaredConstructorsInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourceConstructorInfo[]> DeclaredConstructorsInitializer { get; init; }

	public SourceFieldInfo[] DeclaredFields => _declaredFields ??= DeclaredFieldsInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourceFieldInfo[]> DeclaredFieldsInitializer { get; init; }

	public SourceInterfaceInfo[] DeclaredInterfaces => _declaredInterfaces ??= DeclaredInterfacesInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourceInterfaceInfo[]> DeclaredInterfacesInitializer { get; init; }

	public SourceMethodInfo[] DeclaredMethods => _declaredMethods ??= DeclaredMethodsInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourceMethodInfo[]> DeclaredMethodsInitializer { get; init; } = null!;

	public SourcePropertyInfo[] DeclaredProperties => _declaredProperties ??= DeclaredPropertiesInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourcePropertyInfo[]> DeclaredPropertiesInitializer { get; init; }

	public bool IsAbstract { get; init; }

	public bool IsClass { get; init; }

	public bool IsEnum { get; init; }

	public bool IsGenericType { get; init; }

	public bool IsGenericTypeDefinition { get; init; }

	public bool IsPartial { get; init; }

	public bool IsReadOnly { get; init; }

	public bool IsReflected { get; set; }

	public bool IsStatic { get; init; }

	public bool IsStruct { get; init; }

	public Type Type { get; init; }

	#endregion

	#region Methods

	public SourceConstructorInfo[] GetConstructors()
	{
		return _constructors ??= GetThisAndAncestors()
			.SelectMany(x => x.DeclaredConstructors)
			.GroupBy(x => x.Name)
			.OrderBy(x => x.Key)
			.Select(x => x.First())
			.ToArray();
	}

	public SourceFieldInfo[] GetFields()
	{
		return _fields ??= GetThisAndAncestors()
			.SelectMany(x => x.DeclaredFields)
			.GroupBy(x => x.Name)
			.OrderBy(x => x.Key)
			.Select(x => x.First())
			.ToArray();
	}

	public SourceMemberInfo GetMethod(string name)
	{
		return GetMethods().FirstOrDefault(x => x.Name == name);
	}

	public SourceMemberInfo[] GetMethods()
	{
		return _methods ??= GetThisAndAncestors()
			.SelectMany(x => x.DeclaredMethods)
			.GroupBy(x => x.Name)
			.OrderBy(x => x.Key)
			.Select(x => x.First())
			.ToArray();
	}

	public SourcePropertyInfo[] GetProperties()
	{
		return _properties ??= GetThisAndAncestors()
			.SelectMany(x => x.DeclaredProperties)
			.GroupBy(x => x.Name)
			.OrderBy(x => x.Key)
			.Select(x => x.First())
			.ToArray();
	}

	public SourcePropertyInfo GetProperty(string name)
	{
		return GetProperties().FirstOrDefault(x => x.Name == name);
	}

	public bool ImplementsType(Type type)
	{
		return DeclaredInterfaces.Any(x => x.FullyQualifiedName == type.FullName);
	}

	private static IEnumerable<SourceTypeInfo> EnumerableInherit(SourceTypeInfo type)
	{
		yield return type;
		while (type.BaseType != null)
		{
			type = SourceReflector.GetSourceType(type.BaseType);
			if (type == null)
			{
				break;
			}
			yield return type;
		}
	}

	private SourceTypeInfo[] GetThisAndAncestors()
	{
		return _baseTypes ??= EnumerableInherit(this).ToArray();
	}

	#endregion
}