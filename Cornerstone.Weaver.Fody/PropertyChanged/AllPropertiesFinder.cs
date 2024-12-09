#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	public void FindAllProperties()
	{
		FindAllProperties(NotifyNodes, new());
	}

	private static void FindAllProperties(List<TypeNode> notifyNodes, List<PropertyDefinition> list)
	{
		foreach (var node in notifyNodes)
		{
			var properties = node.TypeDefinition.Properties.ToList();
			properties.AddRange(list);
			node.AllProperties = properties;
			FindAllProperties(node.Nodes, properties);
		}
	}

	#endregion
}