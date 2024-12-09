#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Methods

	public void ProcessDependsOnAttributes(TypeNode node)
	{
		foreach (var propertyDefinition in node.TypeDefinition.Properties)
		{
			ReadDependsOnData(propertyDefinition, node);
		}
	}

	public void ProcessDependsOnAttributes()
	{
		ProcessDependsOnAttributes(NotifyNodes);
	}

	public void ReadDependsOnData(PropertyDefinition property, TypeNode node)
	{
		var dependsOnAttribute = property
			.CustomAttributes
			.GetAttribute(Constants.DependsOnAttribute);
		if (dependsOnAttribute == null)
		{
			return;
		}

		var customAttributeArguments = dependsOnAttribute.ConstructorArguments.ToList();
		var value = (string) customAttributeArguments[0].Value;
		AddIfPropertyExists(property, value, node);
		if (customAttributeArguments.Count > 1)
		{
			var otherValue = (CustomAttributeArgument[]) customAttributeArguments[1].Value;
			foreach (string other in otherValue.Select(_ => _.Value))
			{
				AddIfPropertyExists(property, other, node);
			}
		}
	}

	private void AddIfPropertyExists(PropertyDefinition targetProperty, string isGeneratedUsingPropertyName, TypeNode node)
	{
		//TODO: all properties
		var propertyDefinition = targetProperty.DeclaringType.Properties.FirstOrDefault(_ => _.Name == isGeneratedUsingPropertyName);
		if (propertyDefinition == null)
		{
			ModuleWeaver.WriteInfo($"Could not find property '{isGeneratedUsingPropertyName}' for DependsOnAttribute assigned to '{targetProperty.Name}'.");
			return;
		}

		node.PropertyDependencies.Add(new()
		{
			WhenPropertyIsSet = propertyDefinition,
			ShouldAlsoNotifyFor = targetProperty
		});
	}

	private void ProcessDependsOnAttributes(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes)
		{
			ProcessDependsOnAttributes(node);
			ProcessDependsOnAttributes(node.Nodes);
		}
	}

	#endregion
}