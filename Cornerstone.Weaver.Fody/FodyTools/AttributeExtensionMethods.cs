#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

internal static class AttributeExtensionMethods
{
	#region Methods

	public static CustomAttribute GetAttribute(this ICustomAttributeProvider attributeProvider, string attributeName)
	{
		return attributeProvider?.CustomAttributes.GetAttribute(attributeName);
	}

	public static CustomAttribute GetAttribute(this IEnumerable<CustomAttribute> attributes, string attributeName)
	{
		return attributes?.FirstOrDefault(attribute => attribute.Constructor.DeclaringType.FullName == attributeName);
	}

	public static T GetPropertyValue<T>(this CustomAttribute attribute, string propertyName, T defaultValue)
	{
		return attribute.Properties.Where(p => p.Name == propertyName)
			.Select(p => (T) p.Argument.Value)
			.DefaultIfEmpty(defaultValue)
			.Single();
	}

	public static T GetReferenceTypeConstructorArgument<T>(this CustomAttribute attribute)
		where T : class
	{
		return attribute?.ConstructorArguments?
			.Select(arg => arg.Value as T)
			.FirstOrDefault(value => value != null);
	}

	public static T? GetValueTypeConstructorArgument<T>(this CustomAttribute attribute)
		where T : struct
	{
		return attribute?.ConstructorArguments?
			.Select(arg => arg.Value as T?)
			.FirstOrDefault(value => value != null);
	}

	#endregion
}