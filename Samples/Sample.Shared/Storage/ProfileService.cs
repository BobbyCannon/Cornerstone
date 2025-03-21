#region References

using Cornerstone.Logging;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Storage;

public class ProfileService : Tracker
{
	#region Constructors

	public ProfileService(
		IDateTimeProvider dateTimeProvider,
		ITrackerRepository repository,
		IDispatcher dispatcher)
		: base(repository, dateTimeProvider, dispatcher)
	{
		RuntimeTimer = new Timer(dateTimeProvider, dispatcher);
	}

	#endregion

	#region Properties

	public SyncTimer AverageSyncTimeForAccounts { get; set; }

	public SyncTimer AverageSyncTimeForAddress { get; set; }

	public SyncTimer AverageSyncTimeForAddresses { get; set; }

	public SyncTimer AverageSyncTimeForAll { get; set; }

	public SyncTimer AverageSyncTimeForLogEvents { get; set; }

	public Timer RuntimeTimer { get; }

	#endregion

	#region Methods

	public void Reset()
	{
		AverageSyncTimeForAccounts?.Reset();
		AverageSyncTimeForAddress?.Reset();
		AverageSyncTimeForAddresses?.Reset();
		AverageSyncTimeForAll?.Reset();
		AverageSyncTimeForLogEvents?.Reset();
		RuntimeTimer.Reset();
	}

	#endregion
}

public class ProfilerRepository : ITrackerRepository
{
	#region Methods

	public void Write(params TrackerPath[] paths)
	{
	}

	#endregion
}