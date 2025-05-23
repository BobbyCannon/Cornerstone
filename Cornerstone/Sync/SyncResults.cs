﻿#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The results of the sync.
/// </summary>
public class SyncResults : Bindable
{
	#region Constructors

	/// <summary>
	/// Initiates an instances of the sync results.
	/// </summary>
	public SyncResults()
	{
		Elapsed = TimeSpan.Zero;
		Settings = new SyncSettings();
		SyncIssues = new List<SyncIssue>();
		SyncStatus = default;
		SyncType = null;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The sync client for the client.
	/// </summary>
	public SyncClient Client { get; set; }

	/// <summary>
	/// The elapsed time for the sync.
	/// </summary>
	public TimeSpan Elapsed { get; set; }

	/// <summary>
	/// The sync options.
	/// </summary>
	public SyncSettings Settings { get; set; }

	/// <summary>
	/// The sync client for the server.
	/// </summary>
	public SyncClient Server { get; set; }

	/// <summary>
	/// Gets the ID of the sync session.
	/// </summary>
	public Guid SessionId { get; set; }

	/// <summary>
	/// Gets a value indicating if the last sync was started.
	/// </summary>
	public bool SyncCancelled
	{
		get => SyncStatus.HasFlag(SyncResultStatus.Cancelled);
		set =>
			SyncStatus = value
				? SyncStatus.SetFlag(SyncResultStatus.Cancelled)
				: SyncStatus.ClearFlag(SyncResultStatus.Cancelled);
	}

	/// <summary>
	/// The sync ran to completion.
	/// </summary>
	public bool SyncCompleted
	{
		get => SyncStatus.HasFlag(SyncResultStatus.Completed);
		set =>
			SyncStatus = value
				? SyncStatus = SyncStatus.SetFlag(SyncResultStatus.Completed)
				: SyncStatus = SyncStatus.ClearFlag(SyncResultStatus.Completed);
	}

	/// <summary>
	/// Gets the list of issues that occurred during the last sync.
	/// </summary>
	public IList<SyncIssue> SyncIssues { get; }

	/// <summary>
	/// Gets a value indicating if the last sync was started.
	/// </summary>
	public bool SyncStarted
	{
		get => SyncStatus.HasFlag(SyncResultStatus.Started);
		set =>
			SyncStatus = value
				? SyncStatus.SetFlag(SyncResultStatus.Started)
				: SyncStatus.ClearFlag(SyncResultStatus.Started);
	}

	/// <summary>
	/// The sync result status
	/// </summary>
	public SyncResultStatus SyncStatus { get; set; }

	/// <summary>
	/// Gets a value indicating if the last sync was successful.
	/// </summary>
	public bool SyncSuccessful
	{
		get => SyncStatus.HasFlag(SyncResultStatus.Successful);
		set =>
			SyncStatus = value
				? SyncStatus.SetFlag(SyncResultStatus.Successful)
				: SyncStatus.ClearFlag(SyncResultStatus.Successful);
	}

	/// <summary>
	/// The Type for the sync.
	/// </summary>
	public string SyncType { get; set; }

	#endregion
}