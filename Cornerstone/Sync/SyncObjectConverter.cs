#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents an object converter.
/// </summary>
/// <typeparam name="TSyncModel"> The sync entity type to convert from. </typeparam>
/// <typeparam name="TSyncEntity"> The sync entity type to convert to. </typeparam>
public class SyncObjectIncomingConverter<TSyncModel, TSyncEntity> : SyncObjectIncomingConverter
	where TSyncModel : class, ISyncEntity
	where TSyncEntity : class, ISyncEntity
{
	#region Fields

	private readonly Action<TSyncModel, TSyncEntity> _convert;
	private readonly Func<TSyncEntity, TSyncEntity, Action, SyncObjectStatus, bool> _update;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of a converter.
	/// </summary>
	/// <param name="convert"> An optional convert method to do some additional conversion. </param>
	/// <param name="update"> An optional update method to do some additional updating. </param>
	public SyncObjectIncomingConverter(Action<TSyncModel, TSyncEntity> convert = null, Func<TSyncEntity, TSyncEntity, Action, SyncObjectStatus, bool> update = null)
		: base(
			typeof(TSyncModel).GetRealTypeUsingReflection().ToAssemblyName(),
			typeof(TSyncEntity).GetRealTypeUsingReflection().ToAssemblyName()
		)
	{
		_convert = convert;
		_update = update;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool CanUpdate(ISyncEntity syncEntity)
	{
		return syncEntity is TSyncEntity;
	}

	/// <inheritdoc />
	public override SyncObject Convert(SyncObject syncObject)
	{
		return IncomingConvert(syncObject, _convert, UpdateableAction.SyncOutgoing);
	}

	/// <inheritdoc />
	public override bool Update(ISyncEntity source, ISyncEntity destination, SyncObjectStatus status)
	{
		return Update((TSyncEntity) source, (TSyncEntity) destination, _update,
			status == SyncObjectStatus.Added
				? UpdateableAction.SyncIncomingAdd
				: UpdateableAction.SyncIncomingModified,
			status
		);
	}

	#endregion
}

/// <summary>
/// Represents an outgoing object converter.
/// </summary>
/// <typeparam name="TSyncEntity"> The sync entity type to convert from. </typeparam>
/// <typeparam name="TSyncModel"> The sync model type to convert to. </typeparam>
public class SyncObjectOutgoingConverter<TSyncEntity, TSyncModel> : SyncObjectOutgoingConverter
	where TSyncEntity : class, ISyncEntity
	where TSyncModel : class, ISyncEntity
{
	#region Fields

	private readonly Action<TSyncEntity, TSyncModel> _convert;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of a converter.
	/// </summary>
	/// <param name="convert"> An optional convert method to do some additional conversion. </param>
	public SyncObjectOutgoingConverter(Action<TSyncEntity, TSyncModel> convert = null)
		: base(
			typeof(TSyncEntity).GetRealTypeUsingReflection().ToAssemblyName(),
			typeof(TSyncModel).GetRealTypeUsingReflection().ToAssemblyName()
		)
	{
		_convert = convert;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool CanUpdate(ISyncEntity syncEntity)
	{
		return false;
	}

	/// <inheritdoc />
	public override SyncObject Convert(SyncObject syncObject)
	{
		return OutgoingConvert<TSyncEntity, TSyncModel>(syncObject, _convert, UpdateableAction.SyncOutgoing);
	}

	/// <inheritdoc />
	public override bool Update(ISyncEntity source, ISyncEntity destination, SyncObjectStatus status)
	{
		return false;
	}

	#endregion
}

/// <summary>
/// Represents an incoming object converter.
/// </summary>
public abstract class SyncObjectIncomingConverter : SyncObjectConverter
{
	#region Constructors

	/// <summary>
	/// Instantiate an incoming object converter.
	/// </summary>
	protected SyncObjectIncomingConverter(string sourceName, string destinationName) : base(sourceName, destinationName)
	{
	}

	#endregion
}

/// <summary>
/// Represents an outgoing object converter.
/// </summary>
public abstract class SyncObjectOutgoingConverter : SyncObjectConverter
{
	#region Constructors

	/// <summary>
	/// Instantiate an outgoing object converter.
	/// </summary>
	protected SyncObjectOutgoingConverter(string sourceName, string destinationName) : base(sourceName, destinationName)
	{
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
	protected SyncObjectConverter(string sourceName, string destinationName)
	{
		SourceName = sourceName;
		DestinationName = destinationName;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The destination type name.
	/// </summary>
	protected string DestinationName { get; }

	/// <summary>
	/// The source type name.
	/// </summary>
	protected string SourceName { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Test a sync object name to see if this converter can convert this object.
	/// </summary>
	/// <param name="name"> The sync object name to test. </param>
	/// <returns> True if the sync object can be converted or false if otherwise. </returns>
	public bool CanConvert(string name)
	{
		return SourceName == name;
	}

	/// <summary>
	/// Test a sync object to see if this converter can convert this object.
	/// </summary>
	/// <param name="syncObject"> The sync object to test. </param>
	/// <returns> True if the sync object can be converted or false if otherwise. </returns>
	public bool CanConvert(SyncObject syncObject)
	{
		return CanConvert(syncObject.TypeName);
	}

	/// <summary>
	/// Test a sync issue to see if this converter can convert this object.
	/// </summary>
	/// <param name="syncIssue"> The sync issue to test. </param>
	/// <returns> True if the sync issue can be converted or false if otherwise. </returns>
	public bool CanConvert(SyncIssue syncIssue)
	{
		return CanConvert(syncIssue.TypeName);
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
	/// <param name="syncObject"> The sync object to process. </param>
	/// <returns> The converted sync entity in a sync object format. </returns>
	public abstract SyncObject Convert(SyncObject syncObject);

	/// <summary>
	/// Convert this sync issue to a different sync object
	/// </summary>
	/// <param name="syncIssue"> The sync issue to process. </param>
	/// <returns> The converted sync issue in a sync issue format. </returns>
	public SyncIssue Convert(SyncIssue syncIssue)
	{
		return syncIssue.Convert(DestinationName);
	}

	/// <summary>
	/// Updates this sync object with another object.
	/// </summary>
	/// <param name="source"> The entity with the updates. </param>
	/// <param name="destination"> The destination sync entity to be updated. </param>
	/// <param name="status"> The status of the update. </param>
	/// <returns> Return true if the entity was updated and should be saved. </returns>
	public abstract bool Update(ISyncEntity source, ISyncEntity destination, SyncObjectStatus status);

	/// <summary>
	/// Convert this sync object to a different sync object
	/// </summary>
	/// <typeparam name="TSyncModel"> The sync model type to convert from. </typeparam>
	/// <typeparam name="TSyncEntity"> The sync entity type to convert to. </typeparam>
	/// <param name="syncObject"> The sync object to be converted. </param>
	/// <param name="convert"> An optional convert method to do some additional conversion. </param>
	/// <param name="action"> The type of the action this convert is for. </param>
	/// <returns> The converted sync entity in a sync object format. </returns>
	protected static SyncObject IncomingConvert<TSyncModel, TSyncEntity>(SyncObject syncObject, Action<TSyncModel, TSyncEntity> convert, UpdateableAction action)
		where TSyncModel : class, ISyncEntity
		where TSyncEntity : class, ISyncEntity
	{
		var source = syncObject.ToSyncEntity<TSyncModel>();
		var destination = System.Activator.CreateInstance<TSyncEntity>();

		// Handle all one to one properties (same name & type) and all sync entity base properties.
		destination.UpdateWith(source, action);

		// Update will not set the sync ID
		destination.SyncId = source.SyncId;

		// Optional convert to do additional conversions
		convert?.Invoke(source, destination);

		// Keep status because it should be the same, ex Deleted
		var response = destination.ToSyncObject();
		response.Status = syncObject.Status;
		return response;
	}

	/// <summary>
	/// Convert this sync object to a different sync object
	/// </summary>
	/// <typeparam name="TSyncEntity"> The sync type to convert from. </typeparam>
	/// <typeparam name="TSyncModel"> The sync type to convert to. </typeparam>
	/// <param name="syncObject"> The sync object to be converted. </param>
	/// <param name="convert"> An optional convert method to do some additional conversion. </param>
	/// <param name="action"> The type of the action this convert is for. </param>
	/// <returns> The converted sync entity in a sync object format. </returns>
	protected static SyncObject OutgoingConvert<TSyncEntity, TSyncModel>(SyncObject syncObject, Action<TSyncEntity, TSyncModel> convert, UpdateableAction action)
		where TSyncEntity : class, ISyncEntity
		where TSyncModel : class, ISyncEntity
	{
		var source = syncObject.ToSyncEntity<TSyncEntity>();
		var destination = System.Activator.CreateInstance<TSyncModel>();

		// Handle all one to one properties (same name & type) and all sync entity base properties.
		destination.UpdateWith(source, action);

		// Update will not set the sync ID
		destination.SyncId = source.SyncId;

		// Optional convert to do additional conversions
		convert?.Invoke(source, destination);

		// Keep status because it should be the same, ex Deleted
		var response = destination.ToSyncObject();
		response.Status = syncObject.Status;
		return response;
	}

	/// <summary>
	/// Updates this sync object with another object.
	/// </summary>
	/// <typeparam name="T1"> The sync entity type to process. </typeparam>
	/// <param name="source"> The entity with the updates. </param>
	/// <param name="destination"> The destination sync entity to be updated. </param>
	/// <param name="update"> The function to do the updating. </param>
	/// <param name="action"> The type of the action this update is for. </param>
	/// <param name="status"> The status of the update. </param>
	/// <returns> Return true if the entity was updated and should be saved. </returns>
	protected static bool Update<T1>(T1 source, T1 destination, Func<T1, T1, Action, SyncObjectStatus, bool> update, UpdateableAction action, SyncObjectStatus status)
		where T1 : ISyncEntity
	{
		destination ??= System.Activator.CreateInstance<T1>();

		// todo: move this to after all updates?
		if (!Cache.ShouldProcessProperty(destination.GetRealType(), UpdateableAction.SyncIncomingModified, nameof(ISyncEntity.SyncId))
			&& (destination.SyncId != source.SyncId))
		{
			// Update will not set the sync ID and they are different so set it
			destination.SyncId = source.SyncId;
		}

		// Handle all one to one properties (same name & type) and all sync entity base properties
		Action convert = () => destination.UpdateWith(source, action);

		// See if we have custom conversion
		if (update != null)
		{
			// Update the destination with the source using provided action
			return update.Invoke(source, destination, convert, status);
		}

		convert();
		return true;
	}

	#endregion
}