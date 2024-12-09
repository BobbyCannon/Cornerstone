#region References

using System.Collections.Generic;
using Cornerstone.Weaver.Fody.PropertyShared;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	public void DetectIlGeneratedByDependency()
	{
		DetectIlGeneratedByDependency(NotifyNodes);
	}

	private void DetectIlGeneratedByDependency(List<TypeNode> notifyNodes)
	{
		if (!TriggerDependentProperties)
		{
			return;
		}

		foreach (var node in notifyNodes)
		{
			var ilGeneratedByDependencyReader = new IlGeneratedByDependencyReader(node);
			ilGeneratedByDependencyReader.Process();
			DetectIlGeneratedByDependency(node.Nodes);
		}
	}

	#endregion
}