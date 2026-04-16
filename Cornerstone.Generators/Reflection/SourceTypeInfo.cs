#region References

using System;
using System.ComponentModel;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public partial class SourceTypeInfo
{
	#region Fields

	private SourceConstructorInfo[] _declaredConstructors;
	private SourceFieldInfo[] _declaredFields;
	private SourceInterfaceInfo[] _declaredInterfaces;
	private SourceMethodInfo[] _declaredMethods;
	private SourcePropertyInfo[] _declaredProperties;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; set; }

	public SourceAttributeInfo[] Attributes { get; set; }

	public Type BaseType { get; set; }

	public SourceConstructorInfo[] DeclaredConstructors => _declaredConstructors ??= DeclaredConstructorsInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourceConstructorInfo[]> DeclaredConstructorsInitializer { get; set; }

	public SourceFieldInfo[] DeclaredFields => _declaredFields ??= DeclaredFieldsInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourceFieldInfo[]> DeclaredFieldsInitializer { get; set; }

	public SourceInterfaceInfo[] DeclaredInterfaces => _declaredInterfaces ??= DeclaredInterfacesInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourceInterfaceInfo[]> DeclaredInterfacesInitializer { get; set; }

	public SourceMethodInfo[] DeclaredMethods => _declaredMethods ??= DeclaredMethodsInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourceMethodInfo[]> DeclaredMethodsInitializer { get; set; }

	public SourcePropertyInfo[] DeclaredProperties => _declaredProperties ??= DeclaredPropertiesInitializer?.Invoke();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Func<SourcePropertyInfo[]> DeclaredPropertiesInitializer { get; set; }

	public bool IsAbstract { get; set; }

	public bool IsClass { get; set; }

	public bool IsEnum { get; set; }

	public bool IsGenericType { get; set; }

	public bool IsGenericTypeDefinition { get; set; }

	public bool IsPartial { get; set; }

	public bool IsReadOnly { get; set; }

	public bool IsReflected { get; set; }

	public bool IsStatic { get; set; }

	public bool IsStruct { get; set; }

	public string Name { get; set; }

	public Type Type { get; set; }

	#endregion
}