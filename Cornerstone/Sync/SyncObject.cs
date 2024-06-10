#region References

using System;
using System.IO;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Serialization;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents an sync object.
/// </summary>
public struct SyncObject : IComparable<SyncObject>, IEquatable<SyncObject>
{
	#region Constructors

	static SyncObject()
	{
		OutgoingSerializationOptions = new SerializationOptions
		{
			IgnoreDefaultValues = true,
			IgnoreNullValues = true,
			IgnoreReadOnly = true,
			MaxDepth = 1,
			NamingConvention = NamingConvention.PascalCase,
			TextFormat = TextFormat.None,
			UpdateableAction = UpdateableAction.SyncOutgoing
		};
	}

	#endregion

	#region Properties

	/// <summary>
	/// The serialized data of the object being synced.
	/// </summary>
	public string Data { get; set; }

	/// <summary>
	/// The date and time of the synced object.
	/// </summary>
	public DateTime ModifiedOn { get; set; }

	/// <summary>
	/// Gets or sets the status of this sync object.
	/// </summary>
	public SyncObjectStatus Status { get; set; }

	/// <summary>
	/// Gets or sets the ID of the sync object.
	/// </summary>
	public Guid SyncId { get; set; }

	/// <summary>
	/// Gets or sets the type name of the object. The data contains the serialized data.
	/// </summary>
	public string TypeName { get; set; }

	/// <summary>
	/// Cached serializer settings.
	/// </summary>
	internal static SerializationOptions OutgoingSerializationOptions { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public int CompareTo(SyncObject other)
	{
		var dataComparison = string.Compare(Data, other.Data, StringComparison.Ordinal);
		if (dataComparison != 0)
		{
			return dataComparison;
		}
		var modifiedOnComparison = ModifiedOn.CompareTo(other.ModifiedOn);
		if (modifiedOnComparison != 0)
		{
			return modifiedOnComparison;
		}
		var statusComparison = Status.CompareTo(other.Status);
		if (statusComparison != 0)
		{
			return statusComparison;
		}
		var syncIdComparison = SyncId.CompareTo(other.SyncId);
		if (syncIdComparison != 0)
		{
			return syncIdComparison;
		}
		return string.Compare(TypeName, other.TypeName, StringComparison.Ordinal);
	}

	/// <inheritdoc />
	public bool Equals(SyncObject other)
	{
		return (Data == other.Data)
			&& ModifiedOn.Equals(other.ModifiedOn)
			&& (Status == other.Status)
			&& SyncId.Equals(other.SyncId)
			&& (TypeName == other.TypeName);
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		return obj is SyncObject other && Equals(other);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return HashCodeCalculator.Combine(Data, ModifiedOn, Status, SyncId, TypeName);
	}

	/// <summary>
	/// Converts the sync object back into it's proper type.
	/// </summary>
	/// <returns> The deserialized sync object. </returns>
	public T ToSyncEntity<T>() where T : class, ISyncEntity
	{
		return (T) ToSyncEntity(typeof(T));
	}

	/// <summary>
	/// Converts the sync object back into it's proper type.
	/// </summary>
	/// <returns> The deserialized sync object. </returns>
	public ISyncEntity ToSyncEntity()
	{
		var type = Type.GetType(TypeName);
		return ToSyncEntity(type);
	}

	/// <summary>
	/// Converts the sync object back into it's proper type.
	/// </summary>
	/// <returns> The deserialized sync object. </returns>
	public ISyncEntity ToSyncEntity(Type type)
	{
		if (type == null)
		{
			throw new InvalidDataException("The sync object has an invalid type name.");
		}

		return Data.FromJson(type, OutgoingSerializationOptions) as ISyncEntity;
	}

	/// <summary>
	/// Converts the sync entity into a sync object.
	/// </summary>
	/// <returns> The sync entity to convert into a sync object. </returns>
	public static SyncObject ToSyncObject(ISyncEntity syncEntity)
	{
		var json = syncEntity.ToJson(OutgoingSerializationOptions);

		return new SyncObject
		{
			Data = json,
			ModifiedOn = syncEntity.ModifiedOn,
			SyncId = syncEntity.GetEntitySyncId(),
			TypeName = syncEntity.GetRealType().ToAssemblyName(),
			Status = syncEntity.IsDeleted
				? SyncObjectStatus.Deleted
				: syncEntity.CreatedOn == syncEntity.ModifiedOn
					? SyncObjectStatus.Added
					: SyncObjectStatus.Modified
		};
	}

	#endregion
}