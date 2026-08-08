#region References

using System;
using System.Diagnostics;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Reflection;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents an object converter.
/// </summary>
/// <typeparam name="TSyncClient"> The sync client type. </typeparam>
/// <typeparam name="TSyncModel"> The sync entity type to convert from. </typeparam>
/// <typeparam name="TSyncEntity"> The sync entity type to convert to. </typeparam>
public class SyncObjectConverter<TSyncClient, TSyncModel, TSyncEntity> : SyncObjectConverter
	where TSyncClient : SyncClient
	where TSyncModel : SyncModel, new()
	where TSyncEntity : class, ISyncEntity, new()
{
	#region Fields

	private readonly Action<TSyncClient, TSyncModel, TSyncEntity> _fromSyncModel;
	private readonly Func<TSyncClient, SyncObject, TSyncModel> _fromSyncObject;
	private readonly Action<TSyncClient, TSyncEntity, TSyncModel> _toSyncModel;
	private readonly Func<TSyncClient, TSyncModel, SyncObject> _toSyncObject;
	private readonly Func<TSyncClient, TSyncEntity, TSyncEntity, Action, SyncObjectStatus, bool> _update;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of a converter.
	/// </summary>
	public SyncObjectConverter(
		Func<TSyncClient, SyncObject, TSyncModel> fromSyncObject = null,
		Action<TSyncClient, TSyncModel, TSyncEntity> fromSyncModel = null,
		Action<TSyncClient, TSyncEntity, TSyncModel> toSyncModel = null,
		Func<TSyncClient, TSyncModel, SyncObject> toSyncObject = null,
		Func<TSyncClient, TSyncEntity, TSyncEntity, Action, SyncObjectStatus, bool> update = null)
		: base(
			typeof(TSyncModel).GetRealType().ToAssemblyName(),
			typeof(TSyncEntity).GetRealType().ToAssemblyName()
		)
	{
		// Incoming
		_fromSyncObject = fromSyncObject;
		_fromSyncModel = fromSyncModel;

		// Outgoing
		_toSyncModel = toSyncModel;
		_toSyncObject = toSyncObject;
		_update = update;
	}

	#endregion

	#region Methods

	public override bool CanUpdate(ISyncEntity syncEntity)
	{
		return syncEntity is TSyncEntity;
	}

	public override TSyncEntity ConvertForIncoming(SyncClient client, SyncObject syncObject)
	{
		return IncomingConvert((TSyncClient) client, syncObject, _fromSyncObject, _fromSyncModel, UpdateableAction.SyncOutgoing);
	}

	public override SyncObject ConvertForOutgoing(SyncClient client, ISyncEntity syncEntity)
	{
		return OutgoingConvert((TSyncClient) client, (TSyncEntity) syncEntity, _toSyncModel, _toSyncObject, UpdateableAction.SyncOutgoing);
	}

	public override bool Update(SyncClient client, ISyncEntity source, ISyncEntity destination, SyncObjectStatus status)
	{
		return Update((TSyncClient) client, (TSyncEntity) source, (TSyncEntity) destination, _update, status);
	}

	#endregion
}

/// <summary>
/// Represents an object converter.
/// </summary>
public abstract class SyncObjectConverter
{
	#region Constructors

	/// <summary>
	/// Instantiate an object converter.
	/// </summary>
	protected SyncObjectConverter(string syncModel, string syncEntity)
	{
		SyncModel = syncModel;
		SyncEntity = syncEntity;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The sync entity type name.
	/// </summary>
	protected string SyncEntity { get; }

	/// <summary>
	/// The sync model type name.
	/// </summary>
	protected string SyncModel { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Test a sync object to see if this converter can convert this object.
	/// </summary>
	/// <param name="syncObjectTypeName"> The sync object to test. </param>
	/// <returns> True if the sync object can be converted or false if otherwise. </returns>
	public bool CanConvertIncoming(string syncObjectTypeName)
	{
		return syncObjectTypeName == SyncModel;
	}

	/// <summary>
	/// Test a sync entity to see if this converter can convert this object.
	/// </summary>
	/// <param name="syncEntityTypeName"> The sync entity to test. </param>
	/// <returns> True if the sync object can be converted or false if otherwise. </returns>
	public bool CanConvertOutgoing(string syncEntityTypeName)
	{
		return syncEntityTypeName == SyncEntity;
	}

	/// <summary>
	/// Test a sync entity to see if this converter can update this object.
	/// </summary>
	/// <param name="syncEntity"> The sync entity to test. </param>
	/// <returns> True if the sync entity can be updated or false if otherwise. </returns>
	public abstract bool CanUpdate(ISyncEntity syncEntity);

	/// <summary>
	/// Convert this sync object to a different sync object
	/// </summary>
	/// <param name="client"> The sync client. </param>
	/// <param name="syncObject"> The sync object to process. </param>
	/// <returns> The converted sync object into a sync entity format. </returns>
	public abstract ISyncEntity ConvertForIncoming(SyncClient client, SyncObject syncObject);

	/// <summary>
	/// Convert this sync object to a different sync object
	/// </summary>
	/// <param name="client"> The sync client. </param>
	/// <param name="syncEntity"> The sync entity to process. </param>
	/// <returns> The converted sync entity into a sync object format. </returns>
	public abstract SyncObject ConvertForOutgoing(SyncClient client, ISyncEntity syncEntity);

	/// <summary>
	/// Updates this sync object with another object.
	/// </summary>
	/// <param name="client"> The sync client. </param>
	/// <param name="source"> The entity with the updates. </param>
	/// <param name="destination"> The destination sync entity to be updated. </param>
	/// <param name="status"> The status of the update. </param>
	/// <returns> Return true if the entity was updated and should be saved. </returns>
	public abstract bool Update(SyncClient client, ISyncEntity source, ISyncEntity destination, SyncObjectStatus status);

	/// <summary>
	/// Convert this sync object to a different sync object
	/// </summary>
	/// <typeparam name="TSyncModel"> The sync model type to convert from. </typeparam>
	/// <typeparam name="TSyncEntity"> The sync entity type to convert to. </typeparam>
	/// <typeparam name="TSyncClient"> </typeparam>
	/// <param name="syncClient"> The sync client. </param>
	/// <param name="syncObject"> The sync object to be converted. </param>
	/// <param name="toSyncModel"> </param>
	/// <param name="toSyncEntity"> An optional convert method to do some additional conversion. </param>
	/// <param name="action"> The type of the action this convert is for. </param>
	/// <returns> The converted sync entity in a sync object format. </returns>
	protected static TSyncEntity IncomingConvert<TSyncClient, TSyncModel, TSyncEntity>(
		TSyncClient syncClient, SyncObject syncObject,
		Func<TSyncClient, SyncObject, TSyncModel> toSyncModel,
		Action<TSyncClient, TSyncModel, TSyncEntity> toSyncEntity,
		UpdateableAction action)
		where TSyncClient : SyncClient
		where TSyncModel : SyncModel, new()
		where TSyncEntity : class, ISyncEntity
	{
		var source = (TSyncModel) (toSyncModel?.Invoke(syncClient, syncObject) ?? syncObject.ToSyncModel());
		var destination = SourceReflector.CreateInstance<TSyncEntity>();

		// Handle all one to one properties (same name & type) and all sync entity base properties.
		destination.UpdateWith(source, action);

		// Update will not set the sync ID
		destination.SyncId = source.SyncId;

		// Optional convert to do additional conversions
		toSyncEntity?.Invoke(syncClient, source, destination);
		return destination;
	}

	/// <summary>
	/// Convert this sync object to a different sync object
	/// </summary>
	/// <typeparam name="TSyncClient"> </typeparam>
	/// <typeparam name="TSyncEntity"> The sync type to convert from. </typeparam>
	/// <typeparam name="TSyncModel"> The sync type to convert to. </typeparam>
	/// <param name="syncClient"> The sync client. </param>
	/// <param name="syncEntity"> The sync object to be converted. </param>
	/// <param name="toSyncModel"> An optional convert method to do some additional conversion. </param>
	/// <param name="toSyncObject"> </param>
	/// <param name="action"> The type of the action this convert is for. </param>
	/// <returns> The converted sync entity in a sync object format. </returns>
	protected static SyncObject OutgoingConvert<TSyncClient, TSyncEntity, TSyncModel>(
		TSyncClient syncClient, TSyncEntity syncEntity,
		Action<TSyncClient, TSyncEntity, TSyncModel> toSyncModel,
		Func<TSyncClient, TSyncModel, SyncObject> toSyncObject,
		UpdateableAction action)
		where TSyncClient : SyncClient
		where TSyncEntity : class, ISyncEntity, new()
		where TSyncModel : SyncModel
	{
		var destination = SourceReflector.CreateInstance<TSyncModel>();

		// Handle all one to one properties (same name & type) and all sync entity base properties.
		destination.UpdateWith(syncEntity, action);

		// Update will not set the sync ID
		destination.SyncId = syncEntity.SyncId;

		// Optional convert to do additional conversions
		toSyncModel?.Invoke(syncClient, syncEntity, destination);

		#if DEBUG
		if ((destination is ICreatedEntity c && (c.CreatedOn == DateTime.MinValue))
			|| (destination is IModifiableEntity m && (m.ModifiedOn == DateTime.MinValue))
			|| (destination is ISyncEntity s && (s.SyncId == Guid.Empty)))
		{
			Debugger.Break();
		}
		#endif

		// Convert this sync model to a sync object
		return toSyncObject?.Invoke(syncClient, destination)
			?? SyncObject.ToSyncObject(destination);
	}

	/// <summary>
	/// Updates this sync object with another object.
	/// </summary>
	/// <typeparam name="TSyncClient"> The sync client being processed. </typeparam>
	/// <typeparam name="T1"> The sync entity type to process. </typeparam>
	/// <param name="client"> The sync client. </param>
	/// <param name="source"> The entity with the updates. </param>
	/// <param name="destination"> The destination sync entity to be updated. </param>
	/// <param name="update"> The function to do the updating. </param>
	/// <param name="status"> The status of the update. </param>
	/// <returns> Return true if the entity was updated and should be saved. </returns>
	protected static bool Update<TSyncClient, T1>(TSyncClient client, T1 source, T1 destination, Func<TSyncClient, T1, T1, Action, SyncObjectStatus, bool> update, SyncObjectStatus status)
		where TSyncClient : SyncClient
		where T1 : ISyncEntity
	{
		destination ??= SourceReflector.CreateInstance<T1>();

		// todo: move this to after all updates?
		if (!Cache.ShouldProcessProperty(destination.GetRealType(), UpdateableAction.SyncIncomingUpdate, nameof(ISyncEntity.SyncId))
			&& (destination.SyncId != source.SyncId))
		{
			// Update will not set the sync ID and they are different so set it
			destination.SyncId = source.SyncId;
		}

		// Handle all one to one properties (same name & type) and all sync entity base properties
		Action convert = () => destination.UpdateWith(source, status == SyncObjectStatus.Added ? UpdateableAction.SyncIncomingAdd : UpdateableAction.SyncIncomingUpdate);

		// See if we have custom conversion
		if (update != null)
		{
			// Update the destination with the source using provided action
			return update.Invoke(client, source, destination, convert, status);
		}

		convert();
		return true;
	}

	#endregion
}