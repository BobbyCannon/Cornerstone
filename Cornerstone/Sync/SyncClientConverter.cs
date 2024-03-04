#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync object input converter for the sync client.
/// </summary>
public class SyncClientIncomingConverter : SyncClientConverter
{
	#region Constructors

	/// <summary>
	/// Initializes a sync input converter to be used during syncing.
	/// </summary>
	/// <param name="converters"> The converters to process during conversion. </param>
	public SyncClientIncomingConverter(params SyncObjectIncomingConverter[] converters) : base(converters)
	{
	}

	#endregion
}

/// <summary>
/// Represents a sync object output converter for the sync client.
/// </summary>
public class SyncClientOutgoingConverter : SyncClientConverter
{
	#region Constructors

	/// <summary>
	/// Initializes a sync output converter to be used during syncing.
	/// </summary>
	/// <param name="converters"> The converters to process during conversion. </param>
	public SyncClientOutgoingConverter(params SyncObjectOutgoingConverter[] converters) : base(converters)
	{
	}

	#endregion
}

/// <summary>
/// Represents a sync object converter for the sync client.
/// </summary>
public abstract class SyncClientConverter
{
	#region Fields

	private readonly SyncObjectConverter[] _converters;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a sync converter to be used during syncing.
	/// </summary>
	/// <param name="converters"> The converters to process during conversion. </param>
	protected SyncClientConverter(IEnumerable<SyncObjectConverter> converters)
	{
		_converters = converters.ToArray();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Test a sync object name to see if this converter can convert this object.
	/// </summary>
	/// <param name="name"> The sync object name to test. </param>
	/// <returns> True if the sync object can be converted or false if otherwise. </returns>
	public bool CanConvert(string name)
	{
		return _converters.Any(x => x.CanConvert(name));
	}

	/// <summary>
	/// Process the provided request through the converters.
	/// </summary>
	/// <param name="collection"> The collection to process. </param>
	/// <returns> The request with an updated collection. </returns>
	public IEnumerable<SyncObject> Convert(IEnumerable<SyncObject> collection)
	{
		return collection.Select(Convert);
	}

	/// <summary>
	/// Process the provided sync object through the converters.
	/// </summary>
	/// <param name="value"> The sync object to process. </param>
	/// <returns> The process sync object. </returns>
	public SyncObject Convert(SyncObject value)
	{
		// Cycle through each converter to process each object.
		foreach (var converter in _converters)
		{
			// Ensure this converter can process the object.
			if (!converter.CanConvert(value))
			{
				continue;
			}

			// Convert the object.
			return converter.Convert(value);
		}

		return SyncObjectExtensions.Empty;
	}

	/// <summary>
	/// Process the provided sync issue through the converters.
	/// </summary>
	/// <param name="issue"> The sync issue to process. </param>
	/// <returns> The process sync issue otherwise null if could not be converted. </returns>
	public SyncIssue Convert(SyncIssue issue)
	{
		// Cycle through each converter to process each object.
		foreach (var converter in _converters)
		{
			// Ensure this converter can process the object.
			if (!converter.CanConvert(issue))
			{
				continue;
			}

			// Convert the object.
			return converter.Convert(issue);
		}

		return null;
	}

	/// <summary>
	/// Updates this sync object with another object.
	/// </summary>
	/// <param name="source"> The entity with the updates. </param>
	/// <param name="destination"> The destination sync entity to be updated. </param>
	/// <param name="status"> The status of the update. </param>
	/// <returns> Return true if the entity was updated and should be saved. </returns>
	public bool Update(ISyncEntity source, ISyncEntity destination, SyncObjectStatus status)
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
			return converter.Update(source, destination, status);
		}

		return false;
	}

	#endregion
}