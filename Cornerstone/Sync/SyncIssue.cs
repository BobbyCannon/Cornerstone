#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents as issue that occurred during sync.
/// </summary>
public class SyncIssue : Bindable<SyncIssue>
{
	#region Properties

	/// <summary>
	/// The ID of the sync item.
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// The type of issue. Example like CannotDelete due to relationship dependencies.
	/// </summary>
	public SyncIssueType IssueType { get; set; }

	/// <summary>
	/// Get the description of the issue.
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// Gets or sets the type name of the object.
	/// </summary>
	public string TypeName { get; set; }

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

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{IssueType} : {TypeName} - {Message}";
	}

	/// <summary>
	/// Update the SyncIssue with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(SyncIssue update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(Id, update.Id, settings.ShouldProcessProperty(nameof(Id)), x => Id = x);
		UpdateProperty(IssueType, update.IssueType, settings.ShouldProcessProperty(nameof(IssueType)), x => IssueType = x);
		UpdateProperty(Message, update.Message, settings.ShouldProcessProperty(nameof(Message)), x => Message = x);
		UpdateProperty(TypeName, update.TypeName, settings.ShouldProcessProperty(nameof(TypeName)), x => TypeName = x);

		// Code Generated - /UpdateWith

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			SyncIssue value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}