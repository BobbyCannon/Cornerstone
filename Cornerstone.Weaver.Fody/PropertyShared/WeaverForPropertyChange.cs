#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public abstract partial class WeaverForPropertyChange : CornerstoneWeaver
{
	#region Fields

	public readonly List<TypeNode> Nodes;
	public readonly List<TypeNode> NotifyNodes;

	#endregion

	#region Constructors

	protected WeaverForPropertyChange(ModuleWeaver moduleWeaver) : base(moduleWeaver)
	{
		Nodes = [];
		NotifyNodes = [];
	}

	#endregion
}