#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Storage;

#endregion

namespace Sample.Shared.Storage.Server;

public class PetTypeEntity : ModifiableEntity<string>
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public PetTypeEntity()
	{
		Types = new List<PetEntity>();
		ResetHasChanges();
	}

	#endregion

	#region Properties

	public override string Id { get; set; }

	public string Type { get; set; }

	public virtual ICollection<PetEntity> Types { get; set; }

	#endregion
}