#region References

using Cornerstone.Data;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a timer for tracking a sync session.
/// </summary>
public partial class SyncTimer : AverageTimer, IUpdateable<SyncTimer>
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the class.
	/// </summary>
	public SyncTimer() : base(10, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the class.
	/// </summary>
	/// <param name="limit"> Optional limit of syncs to average. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	public SyncTimer(int limit, IDateTimeProvider timeProvider) : base(limit, timeProvider)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Tracks the number of cancelled syncs.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int CancelledSyncs { get; set; }

	/// <summary>
	/// Tracks the number of failed syncs.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int FailedSyncs { get; set; }

	/// <summary>
	/// Tracks the number of successful syncs.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int SuccessfulSyncs { get; set; }

	#endregion

	#region Methods

	public override string ToString()
	{
		return $"S{SuccessfulSyncs},C{CancelledSyncs},F{FailedSyncs}";
	}

	#endregion
}