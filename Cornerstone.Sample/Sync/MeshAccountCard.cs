#region References

using System;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// Display name and modified time for the one account on a mesh node.
/// </summary>
public readonly struct MeshAccountCard
{
	#region Constructors

	public MeshAccountCard(string name, DateTime modifiedOn)
	{
		Name = name;
		ModifiedOn = modifiedOn;
	}

	#endregion

	#region Properties

	public DateTime ModifiedOn { get; }

	public string Name { get; }

	#endregion
}
