#region References

using System;
using Cornerstone.Storage;

#endregion

namespace Sample.Shared.Storage.Server;

public class FoodRelationshipEntity : Entity<int>, IModifiableEntity
{
	#region Constructors

	public FoodRelationshipEntity()
	{
		ResetHasChanges();
	}

	#endregion

	#region Properties

	public virtual FoodEntity Child { get; set; }

	public int ChildId { get; set; }

	/// <inheritdoc />
	public DateTime CreatedOn { get; set; }

	public override int Id { get; set; }

	/// <inheritdoc />
	public DateTime ModifiedOn { get; set; }

	public virtual FoodEntity Parent { get; set; }

	public int ParentId { get; set; }

	public double Quantity { get; set; }

	#endregion
}