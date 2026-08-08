#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents an sync object.
/// </summary>
public class SyncObject
{
	#region Properties

	/// <summary>
	/// The serialized data of the object being synced.
	/// </summary>
	public byte[] Data { get; set; }

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

	#endregion

	#region Methods

	public static string GetTypeName(ISyncEntity syncEntity)
	{
		return syncEntity.GetRealType().ToAssemblyName();
	}

	/// <summary>
	/// Converts the sync object back into it's proper type.
	/// </summary>
	/// <returns> The deserialized sync object. </returns>
	public SyncModel ToSyncModel()
	{
		// bug: convert to try and test type for sync model?
		var type = Type.GetType(TypeName);
		var response = (SyncModel) Activator.CreateInstance(type);
		response?.FromSpeedyPacket(SpeedyPacket.Unpack(Data));
		return response;
	}

	/// <summary>
	/// Converts the sync entity into a sync object.
	/// </summary>
	/// <returns> The sync entity to convert into a sync object. </returns>
	public static SyncObject ToSyncObject<T>(T model) where T : SyncModel
	{
		return new SyncObject
		{
			Data = model.ToSpeedyPacket().ToByteArray().ToArray(),
			ModifiedOn = model.ModifiedOn,
			Status = model.IsDeleted
				? SyncObjectStatus.Deleted
				: model.CreatedOn == model.ModifiedOn
					? SyncObjectStatus.Added
					: SyncObjectStatus.Updated,
			SyncId = model.SyncId,
			TypeName = GetTypeName(model)
		};
	}

	#endregion
}