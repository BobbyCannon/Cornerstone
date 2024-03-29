﻿#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Location;

/// <summary>
/// The state comparer for the <see cref="ILocationInformation" /> type.
/// </summary>
public class LocationInformationComparer<T> : Bindable
	where T : ILocationInformation, IUpdateable
{
	#region Constructors

	/// <summary>
	/// Instantiate a state comparer.
	/// </summary>
	public LocationInformationComparer() : this(null)
	{
	}

	/// <summary>
	/// Instantiate a state comparer.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public LocationInformationComparer(IDispatcher dispatcher)
	{
		AlwaysTrustSameSource = true;
		SourceTimeout = TimeSpan.FromSeconds(10);
	}

	#endregion

	#region Properties

	/// <summary>
	/// If true all updates from the existing source will always be accepted regardless of
	/// the quality of the data.
	/// </summary>
	public bool AlwaysTrustSameSource { get; set; }

	/// <summary>
	/// The timeout before the current data will expire and allow data that is lower quality.
	/// </summary>
	public TimeSpan SourceTimeout { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// 
	/// </summary>
	/// <param name="current"></param>
	/// <param name="update"></param>
	/// <returns></returns>
	public bool ShouldUpdate(T current, T update)
	{
		if (update.StatusTime < current.StatusTime)
		{
			// This is an old update so reject it
			return false;
		}

		if (update.HasAccuracy
			&& update.HasValue
			&& current.HasAccuracy
			&& current.HasValue
			&& (update.Accuracy <= current.Accuracy))
		{
			// Both have altitude and accuracy and the update is better
			return true;
		}

		// todo: should we have an accuracy limit? or does "better" accurate update handle

		if (update.HasAccuracy && !current.HasAccuracy)
		{
			// The update has accuracy but the current state does not, so take the update
			return true;
		}

		if (update.HasValue && !current.HasValue)
		{
			// The update has value but the current state does not, so take the update
			return true;
		}

		// You may have an update from the same source, but it's not as accurate
		if (AlwaysTrustSameSource && (current.SourceName == update.SourceName))
		{
			return true;
		}

		// Has the current state expired?
		var elapsed = update.StatusTime - current.StatusTime;
		if (elapsed >= SourceTimeout)
		{
			return true;
		}

		return false;
	}

	#endregion
}