#region References

using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Methods

	public void CleanAttributes()
	{
		foreach (var type in ModuleDefinition.GetTypes())
		{
			ProcessType(type);
		}
	}

	private void ProcessType(TypeDefinition type)
	{
		RemoveAttributes(type.CustomAttributes);
		foreach (var property in type.Properties)
		{
			RemoveAttributes(property.CustomAttributes);
		}
		foreach (var field in type.Fields)
		{
			RemoveAttributes(field.CustomAttributes);
		}
	}

	private void RemoveAttributes(Collection<CustomAttribute> customAttributes)
	{
		var attributes = customAttributes
			.Where(attribute => Constants.AttributeNames.Contains(attribute.Constructor.DeclaringType.FullName));

		foreach (var customAttribute in attributes.ToList())
		{
			customAttributes.Remove(customAttribute);
		}
	}

	#endregion
}