#region References

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Reflection;

public static class SourceAttributeExtensions
{
	#region Methods

	public static T GetAttributeConstructorArgument<T>(this IReadOnlyList<SourceAttributeInfo> attributes, string attributeFullName, int index)
	{
		var attribute = attributes.FirstOrDefault(x => x.FullyQualifiedName == attributeFullName);
		return attribute?.ConstructorArguments.Length >= (index + 1) ? attribute.ConstructorArguments[index] is T tValue ? tValue : default : default;
	}

	public static string GetAttributeNamedArgument(this IReadOnlyList<SourceAttributeInfo> attributes, string attributeFullName, string name)
	{
		var attribute = attributes.FirstOrDefault(x => x.FullyQualifiedName == attributeFullName);
		return attribute?.NamedArguments.TryGetValue(name, out var value) == true ? value?.ToString() : string.Empty;
	}

	public static T GetAttributeNamedArgument<T>(this IReadOnlyList<SourceAttributeInfo> attributes, string attributeFullName, string name)
	{
		var attribute = attributes.FirstOrDefault(x => x.FullyQualifiedName == attributeFullName);
		return attribute?.NamedArguments.TryGetValue(name, out var value) == true ? value is T tValue ? tValue : default : default;
	}

	#endregion
}