﻿namespace Cornerstone.Logging;

/// <summary>
/// Interface for storing tracker path data.
/// </summary>
public interface ITrackerRepository
{
	#region Methods

	/// <summary>
	/// Writes a collection of tracker paths.
	/// </summary>
	/// <param name="paths"> The tracker paths to write. </param>
	void Write(params TrackerPath[] paths);

	#endregion
}