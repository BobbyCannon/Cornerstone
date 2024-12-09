#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
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
			if (customAttributes.ContainsAttribute(Constants.CompilerGeneratedAttribute))
			{
				notifyNodes.Remove(node);
				continue;
			}
			ProcessNotifyNodes(node.Nodes);
		}
	}

	#endregion
}