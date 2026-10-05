#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.DockingManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Docking;

[TestClass]
public class DockingManagerDragTests : CornerstoneCornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void OverlayWindowDoesNotHitTest()
	{
		RunOnUi(() =>
		{
			var manager = new DockingManager(this, RuntimeInformation);
			try
			{
				manager.InitializeLifecycle();
				var overlay = new DockingOverlayWindow(manager);
				IsFalse(overlay.IsHitTestVisible);
			}
			finally
			{
				manager.UninitializeLifecycle();
			}
		});
	}

	[TestMethod]
	public void TabHeaderIsTabDragSource()
	{
		IsTrue(DockingTabControl.IsTabHeaderDragSource(new DockableTabView()));
	}

	[TestMethod]
	public void TabStripScrollArrowDoesNotStartTabDrag()
	{
		IsFalse(DockingTabControl.IsTabHeaderDragSource(new RepeatButton()));
		IsFalse(DockingTabControl.IsTabHeaderDragSource(new Button()));
	}

	#endregion
}