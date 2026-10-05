#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Controls.DockingManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cornerstone.Presentation.Controls;
using Cornerstone.Sample.Tabs.Controls;

#endregion

namespace Cornerstone.UnitTests.Presentation.Docking;

[TestClass]
public class DockingManagerRestoreActivateTests : CornerstoneCornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddActivatesNewTab()
	{
		RunOnUi(() =>
		{
			var manager = CreateStartedManager();
			try
			{
				var tab = new TextTabViewModel { Header = "Added" };
				IsFalse(tab.IsLifecycleStarted());
				manager.Add(tab);
				IsTrue(tab.IsLifecycleStarted());
			}
			finally
			{
				manager.UninitializeLifecycle();
			}
		});
	}

	[TestMethod]
	public void CloseNeverActivatedRestoredTabDoesNotThrow()
	{
		RunOnUi(() =>
		{
			var layout = CaptureTwoTabLayout(out _, out var backgroundId);
			var manager = CreateStartedManager();
			try
			{
				manager.RestoreDockLayout(layout);
				var background = GetTabs(manager).Single(x => x.TabModel.Id == backgroundId);
				manager.CloseTab(background.TabModel);
				AreEqual(1, GetTabs(manager).Length);
			}
			finally
			{
				manager.UninitializeLifecycle();
			}
		});
	}

	[TestMethod]
	public void RestoreDockLayoutActivatesOnlySelectedTab()
	{
		RunOnUi(() =>
		{
			var layout = CaptureTwoTabLayout(out var selectedId, out var backgroundId);
			var manager = CreateStartedManager();
			try
			{
				manager.RestoreDockLayout(layout);
				var tabs = GetTabs(manager);
				AreEqual(2, tabs.Length);
				var selected = tabs.Single(x => x.TabModel.Id == selectedId);
				var background = tabs.Single(x => x.TabModel.Id == backgroundId);
				IsTrue(selected.TabModel.IsLifecycleStarted());
				IsFalse(background.TabModel.IsLifecycleStarted());
			}
			finally
			{
				manager.UninitializeLifecycle();
			}
		});
	}

	[TestMethod]
	public void SelectingRestoredTabActivatesItWithoutStoppingPrevious()
	{
		RunOnUi(() =>
		{
			var layout = CaptureTwoTabLayout(out var selectedId, out var backgroundId);
			var manager = CreateStartedManager();
			try
			{
				manager.RestoreDockLayout(layout);
				var tabs = GetTabs(manager);
				var selected = tabs.Single(x => x.TabModel.Id == selectedId);
				var background = tabs.Single(x => x.TabModel.Id == backgroundId);
				manager.RootTabControl.SelectedItem = background;
				IsTrue(background.TabModel.IsLifecycleStarted());
				IsTrue(selected.TabModel.IsLifecycleStarted());
			}
			finally
			{
				manager.UninitializeLifecycle();
			}
		});
	}

	private DockLayoutItem CaptureTwoTabLayout(out Guid selectedId, out Guid backgroundId)
	{
		var manager = CreateStartedManager();
		try
		{
			var first = new TextTabViewModel { Header = "First" };
			var second = new TextTabViewModel { Header = "Second" };
			manager.Add(first);
			manager.Add(second);
			manager.RootTabControl.SelectedItem = GetTabs(manager).Single(x => x.TabModel.Id == first.Id);
			selectedId = first.Id;
			backgroundId = second.Id;
			return DockLayoutItem.From(manager);
		}
		finally
		{
			manager.UninitializeLifecycle();
		}
	}

	private DockingManager CreateStartedManager()
	{
		var manager = new DockingManager(this, RuntimeInformation);
		manager.Initialize([typeof(DocumentTabModel)]);
		manager.InitializeLifecycle();
		manager.LoadLifecycle();
		manager.StartLifecycle();
		return manager;
	}

	private static DockableTabView[] GetTabs(DockingManager manager)
	{
		return manager.RootTabControl.Items.OfType<DockableTabView>().ToArray();
	}

	#endregion
}