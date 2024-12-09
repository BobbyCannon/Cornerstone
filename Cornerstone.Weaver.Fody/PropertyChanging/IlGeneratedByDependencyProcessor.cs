#region References

using System.Collections.Generic;
using Cornerstone.Weaver.Fody.PropertyShared;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Methods

	public void DetectIlGeneratedByDependency()
	{
		DetectIlGeneratedByDependency(NotifyNodes);
	}

	private static void DetectIlGeneratedByDependency(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes)
		{
			var ilGeneratedByDependencyReader = new IlGeneratedByDependencyReader(node);
			ilGeneratedByDependencyReader.Process();
			DetectIlGeneratedByDependency(node.Nodes);
		}
	}

	#endregion
}