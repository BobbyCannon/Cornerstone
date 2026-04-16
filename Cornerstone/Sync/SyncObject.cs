#region References

using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Sync;

public class SyncObject
{
	#region Properties

	/// <summary>
	/// The serialized data of the object being synced.
	/// </summary>
	public byte[] Data { get; set; }

	/// <summary>
	/// Gets or sets the status of this sync object.
	/// </summary>
	public SyncObjectStatus Status { get; set; }

	/// <summary>
	/// The name of the type.
	/// </summary>
	public string TypeName { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Converts the sync object back into the sync model it represents.
	/// </summary>
	/// <returns> The instance of the sync model. </returns>
	public SyncModel ToSyncModel()
	{
		var typeInfo = SourceReflector.GetSourceType(TypeName);
		var response = (SyncModel) SourceReflector.CreateInstance(typeInfo);
		response?.FromSpeedyPacket(SpeedyPacket.Unpack(Data));
		return response;
	}

	/// <summary>
	/// Converts the sync entity into a sync object.
	/// </summary>
	/// <returns> The sync entity to convert into a sync object. </returns>
	public static SyncObject ToSyncObject(SyncModel model)
	{
		return new SyncObject
		{
			Data = model.ToSpeedyPacket().ToByteArray().ToArray(),
			Status = model.IsDeleted
				? SyncObjectStatus.Deleted
				: model.CreatedOn == model.ModifiedOn
					? SyncObjectStatus.Added
					: SyncObjectStatus.Updated,
			TypeName = TypeExtensions.ToAssemblyName(model.GetType())
		};
	}

	#endregion
}