#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Reflection;

public partial class SourceTypeInfo
{
	#region Fields

	private SourceTypeInfo[] _baseTypes;
	private SourceConstructorInfo[] _constructors;
	private SourceFieldInfo[] _fields;
	private SourceMethodInfo[] _methods;
	private SourcePropertyInfo[] _properties;
	private string[] _propertyBitMap;

	#endregion

	#region Methods

	public T GetAttributeConstructorArgument<T>(string attributeFullName, int index)
	{
		return Attributes.GetAttributeConstructorArgument<T>(attributeFullName, index);
	}

	public string GetAttributeNamedArgument(string attributeFullName, string name)
	{
		return Attributes.GetAttributeNamedArgument(attributeFullName, name);
	}

	public T GetAttributeNamedArgument<T>(string attributeFullName, string name)
	{
		return Attributes.GetAttributeNamedArgument<T>(attributeFullName, name);
	}

	public SourceConstructorInfo[] GetConstructors()
	{
		return _constructors ??= GetThisAndAncestors()
			.SelectMany(x => x.DeclaredConstructors)
			.GroupBy(x => x.Name)
			.OrderBy(x => x.Key)
			.Select(x => x.First())
			.ToArray();
	}

	public SourceFieldInfo GetField(string name)
	{
		return GetFields().FirstOrDefault(x => x.Name == name);
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

	public SourceMethodInfo GetMethod(string name)
	{
		return GetMethods().FirstOrDefault(x => x.Name == name);
	}

	public SourceMethodInfo[] GetMethods()
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

	public int GetPropertyBit(string propertyName)
	{
		_propertyBitMap ??= GetProperties().Select(x => x.Name).OrderBy(x => x).ToArray();
		var index = Array.BinarySearch(_propertyBitMap, 0, _propertyBitMap.Length, propertyName);
		return index >= 0 ? index : -1;
	}

	public string GetPropertyNameByBit(int bit)
	{
		_propertyBitMap ??= GetProperties().Select(x => x.Name).OrderBy(x => x).ToArray();
		return (bit >= 0) && (bit < _propertyBitMap.Length) ? _propertyBitMap[bit] : null;
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