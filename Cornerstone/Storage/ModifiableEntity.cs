#region References

using System;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a modifiable entity.
/// </summary>
/// <typeparam name="T"> The type of the entity key. </typeparam>
public abstract class ModifiableEntity<T> : CreatedEntity<T>, IModifiableEntity
{
	#region Properties

	public DateTime ModifiedOn { get; set; }

	#endregion
}

/// <summary>
/// Represents a Cornerstone entity that track the date and time it was last modified.
/// </summary>
public interface IModifiableEntity : ICreatedEntity
{
	#region Properties

	/// <summary>
	/// Gets or sets the date and time the entity was modified.
	/// </summary>
	public DateTime ModifiedOn { get; set; }

	#endregion
}