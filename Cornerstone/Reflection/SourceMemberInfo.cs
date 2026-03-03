#region References

using System.Linq;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public abstract class SourceMemberInfo
{
	#region Properties

	public SourceAttributeInfo[] Attributes { get; init; }

	public string Name { get; init; }
	
	#endregion

	#region Methods

	public string GetAttributeValue(string attributeFullName, string name)
	{
		var attribute = Attributes.FirstOrDefault(x => x.FullyQualifiedName == attributeFullName);
		return attribute?.NamedArguments.TryGetValue(name, out var value) == true ? value?.ToString() : string.Empty;
	}

	public T GetAttributeValue<T>(string attributeFullName, string name)
	{
		var attribute = Attributes.FirstOrDefault(x => x.FullyQualifiedName == attributeFullName);
		return attribute?.NamedArguments.TryGetValue(name, out var value) == true ? value is T tValue ? tValue : default : default;
	}

	#endregion
}