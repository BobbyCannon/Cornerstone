#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents as issue that occurred during sync.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class SyncIssue : CornerstoneObject<SyncIssue>
{
	#region Properties

	/// <summary>
	/// The ID of the sync item.
	/// </summary>
	public partial Guid Id { get; set; }

	/// <summary>
	/// The type of issue. Example like CannotDelete due to relationship dependencies.
	/// </summary>
	public partial SyncIssueType IssueType { get; set; }

	/// <summary>
	/// Get the description of the issue.
	/// </summary>
	public partial string Message { get; set; }

	/// <summary>
	/// Gets or sets the type name of the object.
	/// </summary>
	public partial string TypeName { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Convert this sync object to a different sync object
	/// </summary>
	/// <returns> The converted sync entity in a sync object format. </returns>
	public SyncIssue Convert(string newTypeName)
	{
		var destination = new SyncIssue();

		// Handle all one to one properties (same name & type) and all sync entity base properties.
		// This will override any exclusions. Meaning this entity will copy all possible properties.
		destination.UpdateWith(this);
		destination.TypeName = newTypeName;

		return destination;
	}

	public override string ToString()
	{
		return $"{IssueType} : {TypeName} - {Message}";
	}

	#endregion
}