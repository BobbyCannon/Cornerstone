#region References

using Cornerstone.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class ApplicationViewModelTrackTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AttachJoinsApplyLoopWithoutLifecycle()
	{
		var app = new ApplicationViewModel(this, Dispatcher);
		var child = new HostViewModel();
		child.Attach(app);

		var list = new System.Collections.Generic.List<DispatchableViewModel>();
		app.CopyTrackedDispatchables(list);
		AreEqual(1, list.Count);
		AreEqual(child, list[0]);

		IsFalse(child.IsLifecycleInitialized());

		child.Detach(app);
		app.CopyTrackedDispatchables(list);
		AreEqual(0, list.Count);
	}

	[TestMethod]
	public void TrackInConstructorCascadesWithParent()
	{
		var app = new ApplicationViewModel(this, Dispatcher);
		var child = new HostViewModel();
		app.Track(child);
		app.InitializeLifecycle();
		app.LoadLifecycle();

		IsTrue(child.IsLifecycleInitialized());
		IsTrue(child.IsLifecycleLoaded());
		IsFalse(child.IsLifecycleStarted());

		app.StartLifecycle();
		IsTrue(child.IsLifecycleStarted());

		app.StopLifecycle();
		app.UnloadLifecycle();
		app.UninitializeLifecycle();
	}

	#endregion

	#region Classes

	private sealed class HostViewModel : DispatchableViewModel
	{
	}

	#endregion
}
