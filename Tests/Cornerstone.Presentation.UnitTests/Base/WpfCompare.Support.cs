#region References

using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

// Cornerstone backend for the shared DispatcherExecutionContextTests. The WPF project
// (Cornerstone.Presentation.UnitTests.WpfCompare) provides its own version of these two types in its own namespace.

public sealed class CrossTestMethodAttribute : PresentationTestMethodAttribute
{
	#region Constructors

	public CrossTestMethodAttribute(
		[CallerFilePath] string sourceFilePath = null,
		[CallerLineNumber] int sourceLineNumber = -1)
	{
	}

	#endregion
}

internal static class DispatcherTestServices
{
	#region Methods

	// Drains every dispatcher operation queued at or above <paramref name="priority"/> by pumping a frame
	// until a sentinel posted at that priority runs. Replaces the Cornerstone-only impl.ExecuteSignal() so the
	// same body works against WPF too.
	public static void DrainQueue(Dispatcher dispatcher, DispatcherPriority priority)
	{
		var frame = new DispatcherFrame();
		dispatcher.InvokeAsync(() => frame.Continue = false, priority);
		PushFrame(dispatcher, frame);
	}

	// Pumps the dispatcher until the frame is stopped. Cornerstone's PushFrame is an instance method
	// (WPF's is static) - that is the only difference from the WPF backend.
	public static void PushFrame(Dispatcher dispatcher, DispatcherFrame frame)
	{
		dispatcher.PushFrame(frame);
	}

	#endregion
}