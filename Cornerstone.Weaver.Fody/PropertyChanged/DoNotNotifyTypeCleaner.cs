#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	public void CleanDoNotNotifyTypes()
	{
		Process(NotifyNodes);
	}

	private static void Process(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes.ToList())
		{
			var containsDoNotNotifyAttribute = node
				.TypeDefinition
				.CustomAttributes
				.ContainsAttribute(Constants.DoNotNotifyAttribute);

			if (containsDoNotNotifyAttribute)
			{
				notifyNodes.Remove(node);
				continue;
			}
			Process(node.Nodes);
		}
	}

	#endregion
}