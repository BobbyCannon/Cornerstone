#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	public void CleanCodeGenedTypes()
	{
		ProcessNotifyNodes(NotifyNodes);
	}

	private static void ProcessNotifyNodes(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes.ToList())
		{
			var customAttributes = node.TypeDefinition.CustomAttributes;
			if (customAttributes.ContainsAttribute("System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
			{
				notifyNodes.Remove(node);
				continue;
			}
			ProcessNotifyNodes(node.Nodes);
		}
	}

	#endregion
}