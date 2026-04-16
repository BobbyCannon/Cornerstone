#region References

using System.ComponentModel;
using Cornerstone.Data;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a Cornerstone entity.
/// </summary>
/// <typeparam name="T"> The type of the entity primary ID. </typeparam>
public abstract class Entity<T> : Entity
{
	#region Properties

	/// <summary>
	/// Gets or sets the ID of the entity.
	/// </summary>
	public abstract T Id { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Allows the entity to calculate the next key.
	/// </summary>
	/// <param name="currentKey"> The current version of the key. </param>
	/// <returns> The new key to be used in. </returns>
	public virtual T NewId(ref T currentKey)
	{
		currentKey = currentKey switch
		{
			sbyte sbKey => (T) (object) (sbKey + 1),
			byte bKey => (T) (object) (bKey + 1),
			short sKey => (T) (object) (sKey + 1),
			ushort usKey => (T) (object) (usKey + 1),
			int iKey => (T) (object) (iKey + 1),
			uint uiKey => (T) (object) (uiKey + 1),
			long lKey => (T) (object) (lKey + 1),
			ulong ulKey => (T) (object) (ulKey + 1),
			_ => currentKey
		};

		return currentKey;
	}

	#endregion
}

/// <summary>
/// Represents a Cornerstone entity.
/// </summary>
public abstract class Entity : Notifiable, IEntity
{
	#region Constructors

	/// <summary>
	/// Initializes an entity
	/// </summary>
	protected Entity()
	{
		Cache.Initialize(this);
	}

	#endregion
}

/// <summary>
/// Represents a Cornerstone entity.
/// </summary>
public interface IEntity : INotifyPropertyChanged, IUpdateable, ITrackPropertyChanges
{
}