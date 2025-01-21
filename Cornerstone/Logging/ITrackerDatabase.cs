#region References

using Cornerstone.Sync;

#endregion

namespace Cornerstone.Logging;

public interface ITrackerDatabase
{
	#region Properties

	ISyncableRepository<TrackerPathConfigurationEntity, int> TrackerPathConfigurations { get; }
	ISyncableRepository<TrackerPathEntity, long> TrackerPaths { get; }

	#endregion
}