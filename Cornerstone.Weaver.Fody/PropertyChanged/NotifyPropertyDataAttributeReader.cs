#region References

using System.Collections.Generic;
using System.Linq;
using Fody;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	public NotifyPropertyData ReadAlsoNotifyForData(PropertyDefinition property, List<PropertyDefinition> allProperties)
	{
		var notifyAttribute = property.CustomAttributes.GetAttribute(Constants.AlsoNotifyForAttribute);
		if (notifyAttribute == null)
		{
			return null;
		}

		var propertyNamesToNotify = GetPropertyNamesToNotify(notifyAttribute, property, allProperties);

		return new()
		{
			AlsoNotifyFor = propertyNamesToNotify.ToList()
		};
	}

	private static PropertyDefinition GetPropertyDefinition(PropertyDefinition property, List<PropertyDefinition> allProperties, string argument)
	{
		var propertyDefinition = allProperties.FirstOrDefault(_ => _.Name == argument);
		if (propertyDefinition != null)
		{
			return propertyDefinition;
		}
		throw new WeavingException($"Could not find property '{argument}' for AlsoNotifyFor attribute assigned to '{property.Name}'.");
	}

	private static IEnumerable<PropertyDefinition> GetPropertyNamesToNotify(CustomAttribute notifyAttribute, PropertyDefinition property, List<PropertyDefinition> allProperties)
	{
		var customAttributeArguments = notifyAttribute.ConstructorArguments.ToList();
		var value = (string) customAttributeArguments[0].Value;
		yield return GetPropertyDefinition(property, allProperties, value);
		if (customAttributeArguments.Count <= 1)
		{
			yield break;
		}
		var paramsArguments = (CustomAttributeArgument[]) customAttributeArguments[1].Value;
		foreach (string argument in paramsArguments.Select(x => x.Value))
		{
			yield return GetPropertyDefinition(property, allProperties, argument);
		}
	}

	#endregion
}