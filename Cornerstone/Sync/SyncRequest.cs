#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The details to ask a sync client for changes.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class SyncRequest : ServiceRequest<SyncObject>
{
	#region Constructors

	/// <summary>
	/// Instantiates a sync request.
	/// </summary>
	public SyncRequest() : this([])
	{
	}

	/// <summary>
	/// Instantiates a sync request.
	/// </summary>
	public SyncRequest(params SyncObject[] collection) : this(collection.ToList())
	{
	}

	/// <summary>
	/// Instantiates a sync request.
	/// </summary>
	public SyncRequest(IEnumerable<SyncObject> collection) : base(collection)
	{
		Reset();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The start date and time to get changes for.
	/// </summary>
	public partial DateTime Since { get; set; }

	/// <summary>
	/// The end date and time to get changes for.
	/// </summary>
	public partial DateTime Until { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Resets the filter back to defaults.
	/// </summary>
	public void Reset()
	{
		Since = DateTime.MinValue;
		Until = DateTimeProvider.RealTime.UtcNow;
		Skip = 0;
		Take = 1000;
	}

	#endregion
}