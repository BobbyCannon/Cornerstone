#region References

using System;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

internal partial class DockingWindow : CornerstoneWindow
{
	#region Constructors

	public DockingWindow()
	{
		InitializeComponent();
	}

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