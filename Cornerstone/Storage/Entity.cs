#region References

using System;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Sync;
using PropertyChanged;
using ICloneable = Cornerstone.Data.ICloneable;

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
	[SuppressPropertyChangedWarnings]
	public abstract T Id { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object GetEntityId()
	{
		return Id;
	}

	/// <inheritdoc />
	public override bool IdIsSet()
	{
		return !Equals(Id, default(T));
	}

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

	/// <inheritdoc />
	public override bool TrySetId(string id)
	{
		try
		{
			Id = id.FromJson<T>();
			return true;
		}
		catch
		{
			return false;
		}
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

	#region Methods

	/// <inheritdoc />
	public virtual bool CanBeModified()
	{
		return true;
	}

	/// <inheritdoc />
	public virtual void EntityAdded(DateTime utcNow)
	{
	}

	/// <inheritdoc />
	public virtual void EntityAddedDeletedOrModified(DateTime utcNow)
	{
		if (this is IClientEntity clientEntity)
		{
			clientEntity.LastClientUpdate = utcNow;
		}
	}

	/// <inheritdoc />
	public virtual void EntityDeleted(DateTime utcNow)
	{
	}

	/// <inheritdoc />
	public virtual void EntityModified(DateTime utcNow)
	{
	}

	/// <inheritdoc />
	public abstract object GetEntityId();

	/// <inheritdoc />
	public abstract bool IdIsSet();

	/// <inheritdoc />
	public bool ShouldProcessProperty(UpdateableAction action, string propertyName)
	{
		return Cache.ShouldProcessProperty(GetRealType(), action, propertyName);
	}

	/// <inheritdoc />
	public abstract bool TrySetId(string id);

	/// <summary>
	/// Update all local sync IDs.
	/// </summary>
	public void UpdateLocalSyncIds()
	{
		var syncEntityInterface = typeof(ISyncEntity);
		var properties = GetRealType().GetCachedProperties().ToList();
		var entityRelationships = properties
			.Where(x => x.IsVirtual())
			.Where(x => syncEntityInterface.IsAssignableFrom(x.PropertyType))
			.ToList();

		foreach (var entityRelationship in entityRelationships)
		{
			var entityRelationshipSyncIdProperty = properties.FirstOrDefault(x => x.Name == $"{entityRelationship.Name}SyncId");

			if (entityRelationship.GetValue(this, null) is ISyncEntity syncEntity && (entityRelationshipSyncIdProperty != null))
			{
				var otherEntitySyncId = (Guid?) entityRelationshipSyncIdProperty.GetValue(this, null);
				var syncEntitySyncId = syncEntity.GetEntitySyncId();
				if (otherEntitySyncId != syncEntitySyncId)
				{
					// resets entitySyncId to entity.SyncId if it does not match
					entityRelationshipSyncIdProperty.SetValue(this, syncEntitySyncId, null);
				}
			}

			// todo: maybe?, support the setting Entity.Id would then query the entity sync id and set it?
		}
	}

	#endregion
}

/// <summary>
/// Represents a Cornerstone entity.
/// </summary>
public interface IEntity : INotifyPropertyChanged, IUpdateable, IUpdateableOptionsProvider, ITrackPropertyChanges, ICloneable
{
	#region Methods

	/// <summary>
	/// Checks to see if an entity can be modified.
	/// </summary>
	bool CanBeModified();

	/// <summary>
	/// Update an entity that has been added.
	/// </summary>
	void EntityAdded(DateTime utcNow);

	/// <summary>
	/// Update an entity that has been added, deleted, or modified.
	/// </summary>
	void EntityAddedDeletedOrModified(DateTime utcNow);

	/// <summary>
	/// Update an entity that has been deleted.
	/// </summary>
	void EntityDeleted(DateTime utcNow);

	/// <summary>
	/// Update an entity that has been modified.
	/// </summary>
	void EntityModified(DateTime utcNow);

	/// <summary>
	/// Gets the primary key (ID) of the entity.
	/// </summary>
	/// <returns> The primary key value for the entity. </returns>
	object GetEntityId();

	/// <summary>
	/// Cached version of the "real" type, meaning not EF proxy but rather root type
	/// </summary>
	Type GetRealType();

	/// <summary>
	/// Determine if the ID is set on the entity.
	/// </summary>
	/// <returns> True if the ID is set or false if otherwise. </returns>
	bool IdIsSet();

	/// <summary>
	/// Checks a property to see if it should be processed for the provided action.
	/// </summary>
	/// <param name="action"> The action to validate. </param>
	/// <param name="propertyName"> The name of the property to validate. </param>
	/// <returns> True if the property should be processed otherwise false. </returns>
	bool ShouldProcessProperty(UpdateableAction action, string propertyName);

	/// <summary>
	/// Try to set the ID from a serialized version.
	/// </summary>
	/// <returns> True if the ID is successfully set or false if otherwise. </returns>
	bool TrySetId(string id);

	#endregion
}