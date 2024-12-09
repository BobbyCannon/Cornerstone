#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Fody;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	public void WalkPropertyData()
	{
		WalkPropertyData(NotifyNodes);
	}

	private static void ComputeDependenciesRecursively(PropertyDefinition propertyDefinition, HashSet<PropertyDefinition> fullDependencies, TypeNode node)
	{
		// TODO: An optimization could be done to avoid the multiple computation of one property for each property of the type
		// By keeping the in memory the full dependencies of each property of the type

		foreach (var dependentProperty in node.PropertyDependencies.Where(_ => _.WhenPropertyIsSet == propertyDefinition).Select(_ => _.ShouldAlsoNotifyFor))
		{
			if (!fullDependencies.Add(dependentProperty))
			{
				continue;
			}

			ComputeDependenciesRecursively(dependentProperty, fullDependencies, node);
		}
	}

	private static List<PropertyDefinition> GetFullDependencies(PropertyDefinition propertyDefinition, IEnumerable<PropertyDefinition> dependenciesForProperty, TypeNode node)
	{
		// Create an HashSet to contain all dependent properties (direct or transitive)
		// Add the initial Property to stop the recursion if this property is a dependency of another property
		var fullDependencies = new HashSet<PropertyDefinition> { propertyDefinition };

		foreach (var dependentProperty in dependenciesForProperty)
		{
			// Check if the property is already present in the HashSet before starting the recursion
			if (fullDependencies.Add(dependentProperty))
			{
				ComputeDependenciesRecursively(dependentProperty, fullDependencies, node);
			}
		}

		// Remove the initial Property of the HashSet.
		fullDependencies.Remove(propertyDefinition);

		return fullDependencies.ToList();
	}

	private void GetPropertyData(PropertyDefinition propertyDefinition, TypeNode node)
	{
		var notifyPropertyData = ReadAlsoNotifyForData(propertyDefinition, node.AllProperties);
		var dependenciesForProperty = node.PropertyDependencies
			.Where(_ => _.WhenPropertyIsSet == propertyDefinition)
			.Select(_ => _.ShouldAlsoNotifyFor);

		var backingFieldReference = node.Mappings.First(_ => _.PropertyDefinition == propertyDefinition).FieldDefinition;

		if (backingFieldReference?.CustomAttributes.ContainsAttribute(Constants.DoNotNotifyAttribute) == true)
		{
			return;
		}

		if (notifyPropertyData == null)
		{
			if (node.EventInvoker == null)
			{
				return;
			}

			node.PropertyData.Add(
				new()
				{
					ParentType = node,
					BackingFieldReference = backingFieldReference,
					PropertyDefinition = propertyDefinition,
					// Compute full dependencies for the current property
					AlsoNotifyFor = GetFullDependencies(propertyDefinition, dependenciesForProperty, node),
					AlreadyNotifies = GetAlreadyNotifies(propertyDefinition).ToList()
				});
			return;
		}

		if (node.EventInvoker == null)
		{
			throw new WeavingException(
				$"""
				Could not find field for PropertyChanged event on type '{node.TypeDefinition.FullName}'.
				Looked for 'PropertyChanged', 'propertyChanged', '_PropertyChanged' and '_propertyChanged'.
				The most likely cause is that you have implemented a custom event accessor for the PropertyChanged event and have called the PropertyChangedEventHandler something stupid.
				""");
		}

		node.PropertyData.Add(
			new()
			{
				ParentType = node,
				BackingFieldReference = backingFieldReference,
				PropertyDefinition = propertyDefinition,
				// Compute full dependencies for the current property
				AlsoNotifyFor = GetFullDependencies(propertyDefinition, notifyPropertyData.AlsoNotifyFor.Union(dependenciesForProperty), node),
				AlreadyNotifies = GetAlreadyNotifies(propertyDefinition).ToList()
			});
	}

	private void WalkPropertyData(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes)
		{
			foreach (var property in node.TypeDefinition.Properties)
			{
				if (property.CustomAttributes.ContainsAttribute(Constants.DoNotNotifyAttribute))
				{
					continue;
				}

				if (property.SetMethod == null)
				{
					continue;
				}

				if (property.SetMethod.IsStatic)
				{
					continue;
				}

				GetPropertyData(property, node);
			}

			WalkPropertyData(node.Nodes);
		}
	}

	#endregion
}