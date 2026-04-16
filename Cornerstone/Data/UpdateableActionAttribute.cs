#region References

using System;

#endregion

namespace Cornerstone.Data;

[AttributeUsage(AttributeTargets.Property)]
public class UpdateableActionAttribute : CornerstoneAttribute
{
	#region Constructors

	public UpdateableActionAttribute() : this(UpdateableAction.None)
	{
	}

	public UpdateableActionAttribute(UpdateableAction actions, int order = int.MaxValue)
	{
		Actions = actions;
		Order = order;
	}

	#endregion

	#region Properties

	public UpdateableAction Actions { get; set; }

	public int Order { get; set; }

	#endregion
}