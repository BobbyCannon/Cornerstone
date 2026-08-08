#region References

using System.Linq;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync object converter for the sync client.
/// </summary>
public class SyncClientConverter
{
	#region Fields

	private readonly SyncObjectConverter[] _converters;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a sync converter to be used during syncing.
	/// </summary>
	/// <param name="converters"> The converters to process during conversion. </param>
	public SyncClientConverter(params SyncObjectConverter[] converters)
	{
		_converters = converters.ToArray();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Test a sync object to see if this converter can convert this object.
	/// </summary>
	/// <param name="syncObjectTypeName"> The sync object name to test. </param>
	/// <returns> True if the sync object can be converted or false if otherwise. </returns>
	public bool CanConvertOutgoing(string syncObjectTypeName)
	{
		// Cycle through each converter to process each object.
		foreach (var converter in _converters)
		{
			// Ensure this converter can process the object.
			if (converter.CanConvertOutgoing(syncObjectTypeName))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Process the provided sync object through the converters.
	/// </summary>
	/// <param name="client"> </param>
	/// <param name="value"> The sync object to process. </param>
	/// <returns> The process sync object. </returns>
	public ISyncEntity ConvertIncoming(SyncClient client, SyncObject value)
	{
		// Cycle through each converter to process each object.
		foreach (var converter in _converters)
		{
			// Ensure this converter can process the object.
			if (!converter.CanConvertIncoming(value.TypeName))
			{
				continue;
			}

			// Convert the object.
			return converter.ConvertForIncoming(client, value);
		}

		return null;
	}

	/// <summary>
	/// Process the provided sync object through the converters.
	/// </summary>
	/// <param name="client"> </param>
	/// <param name="value"> The sync object to process. </param>
	/// <returns> The process sync object. </returns>
	public SyncObject ConvertOutgoing(SyncClient client, ISyncEntity value)
	{
		// Cycle through each converter to process each object.
		foreach (var converter in _converters)
		{
			// Ensure this converter can process the object.
			if (!converter.CanConvertOutgoing(SyncObject.GetTypeName(value)))
			{
				continue;
			}

			// Convert the object.
			return converter.ConvertForOutgoing(client, value);
		}

		return null;
	}

	/// <summary>
	/// Updates this sync object with another object.
	/// </summary>
	/// <param name="client"> The sync client doing the update. </param>
	/// <param name="source"> The entity with the updates. </param>
	/// <param name="destination"> The destination sync entity to be updated. </param>
	/// <param name="status"> The status of the update. </param>
	/// <returns> Return true if the entity was updated and should be saved. </returns>
	public bool Update(SyncClient client, ISyncEntity source, ISyncEntity destination, SyncObjectStatus status)
	{
		// Cycle through each converter to process each object.
		foreach (var converter in _converters)
		{
			// Ensure this converter can process the object.
			if (!converter.CanUpdate(source))
			{
				continue;
			}

			// Convert the object.
			return converter.Update(client, source, destination, status);
		}

		return false;
	}

	#endregion
}