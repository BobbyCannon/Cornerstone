#region References

using Cornerstone.Presentation.Threading;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public static class DispatcherTimerUtils
{
	#region Methods

	public static void ForceFire(this DispatcherTimer timer)
	{
		timer.Promote();
		timer.Dispatcher.RemoveTimer(timer);
		Dispatcher.UIThread.RunJobs();
	}

	#endregion
}