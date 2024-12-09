#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Methods

	public string CheckForWarning(PropertyData propertyData, InvokerTypes invokerType)
	{
		var propertyDefinition = propertyData.PropertyDefinition;
		var setMethod = propertyDefinition.SetMethod;
		if ((setMethod.Name == "set_Item") && (setMethod.Parameters.Count == 2) && (setMethod.Parameters[1].Name == "value"))
		{
			return "Property is an indexer.";
		}
		if (setMethod.IsAbstract)
		{
			return "Property is abstract.";
		}
		if ((propertyData.BackingFieldReference == null) && (propertyDefinition.GetMethod == null))
		{
			return "Property has no field set logic or it contains multiple sets and the names cannot be mapped to a property.";
		}
		if ((invokerType == InvokerTypes.Before) && (propertyDefinition.GetMethod == null))
		{
			return "When using a before/after invoker the property have a 'get'.";
		}
		return null;
	}

	public void CheckForWarnings()
	{
		CheckForWarnings(NotifyNodes);
	}

	private void CheckForWarnings(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes)
		{
			foreach (var propertyData in node.PropertyData.ToList())
			{
				var warning = CheckForWarning(propertyData, node.EventInvoker.InvokerType);
				if (warning != null)
				{
					ModuleWeaver.WriteInfo($"\t{propertyData.PropertyDefinition.GetName()} {warning} Property will be ignored.");
					node.PropertyData.Remove(propertyData);
				}
			}
			CheckForWarnings(node.Nodes);
		}
	}

	#endregion
}