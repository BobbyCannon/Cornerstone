namespace Cornerstone.Reflection;

public partial class SourceFieldInfo
{
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

	#endregion
}