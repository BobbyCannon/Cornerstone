#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Storage;

#endregion

namespace Sample.Shared.Storage.Server;

public class GroupEntity : Entity<int>, IModifiableEntity
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public GroupEntity()
	{
		Members = new List<GroupMemberEntity>();
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DateTime CreatedOn { get; set; }

	public string Description { get; set; }

	public override int Id { get; set; }

	public virtual ICollection<GroupMemberEntity> Members { get; set; }

	/// <inheritdoc />
	public DateTime ModifiedOn { get; set; }

	public string Name { get; set; }

	#endregion
}