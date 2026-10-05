#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Chrome;

#endregion

namespace Cornerstone.Presentation.Controls.DockingManager;

internal class DockingWindow : Window
{
	#region Constructors

	public DockingWindow(DockingManager dockingManager)
	{
		DockingManager = dockingManager;
		Content = dockingManager;
	}

	#endregion

	#region Properties

	public DockingManager DockingManager { get; }

	#endregion

	#region Methods

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		DockingManager.TabDropped -= DockingManagerOnTabDropped;
		DockingManager.TabModelRemoved -= DockingManagerOnTabModelRemoved;
		DockingManager.Close();
		base.OnClosing(e);
	}

	protected override void OnOpened(EventArgs e)
	{
		DockingManager.TabDropped += DockingManagerOnTabDropped;
		DockingManager.TabModelRemoved += DockingManagerOnTabModelRemoved;
		base.OnOpened(e);
	}

	private void DockingManagerOnTabDropped(object sender, DockableTabView e)
	{
		MaybeClose();
	}

	private void DockingManagerOnTabModelRemoved(object sender, DockableTabModel e)
	{
		MaybeClose();
	}

	private void MaybeClose()
	{
		if (DockingManager.Children is [DockingTabControl { Items.Count: 0 }])
		{
			Close();
		}
	}

	#endregion
}