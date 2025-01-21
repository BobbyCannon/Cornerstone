#region References

using Cornerstone.Data;

#endregion

namespace Cornerstone.Attributes;

/// <summary>
/// Attribute for properties when UpdateWith is called.
/// </summary>
public class SyncPropertyAttribute : CornerstoneAttribute
{
	#region Constructors

	public SyncPropertyAttribute(UpdateableAction action)
	{
		Action = action;
	}

	#endregion

	#region Properties

	public UpdateableAction Action { get; }

	#endregion

	#region Methods

	public bool Supports(UpdateableAction action)
	{
		return Action.HasFlag(action);
	}

	#endregion
}