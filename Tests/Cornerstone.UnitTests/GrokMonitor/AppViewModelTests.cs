#region References

using System;
using Cornerstone.GrokMonitor;
using Cornerstone.GrokMonitor.GrokUsage.Channels;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.State;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor;

[TestClass]
public class AppViewModelTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void HomeTabProjectionAddsAndRemovesTabsFromHomes()
	{
		var bus = new AppBus(new GrokUsageChannel());
		var state = new AppState(new AppSettings(), new GrokUsageState());
		var runtime = new RuntimeInformation();
		var host = new AppViewModel(bus, state, this, Dispatcher, this, runtime);
		host.InitializeLifecycle();

		try
		{
			var personal = new GrokHomeUsageState(Guid.Parse("11111111-2222-3333-4444-555555555555"))
			{
				DisplayName = "Personal",
				Path = @"C:\Users\Ada\.grok"
			};
			var work = new GrokHomeUsageState(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
			{
				DisplayName = "Work",
				Path = @"C:\Users\Ada\.grok-work"
			};
			state.GrokUsage.Homes.Add(personal);
			state.GrokUsage.Homes.Add(work);
			host.ApplyHomeTabProjection();

			AreEqual(2, host.HomeTabs.Count);
			AreEqual(personal.Id, host.HomeTabs[0].HomeId);
			AreEqual(work.Id, host.HomeTabs[1].HomeId);
			AreEqual(3, host.ShellTabs.Count);
			AreEqual(host.SettingsTab, host.ShellTabs[2]);
			IsTrue(host.HasHomeTabs);
			IsTrue(host.ShowShellTabHeaders);
			AreEqual(personal.Id, host.SelectedHomeTab.HomeId);

			state.GrokUsage.Homes.RemoveAt(0);
			host.ApplyHomeTabProjection();

			AreEqual(1, host.HomeTabs.Count);
			AreEqual(work.Id, host.HomeTabs[0].HomeId);
			AreEqual(2, host.ShellTabs.Count);
			AreEqual(work.Id, host.SelectedHomeTab.HomeId);

			state.GrokUsage.Homes.Clear();
			host.ApplyHomeTabProjection();
			AreEqual(0, host.HomeTabs.Count);
			AreEqual(1, host.ShellTabs.Count);
			IsFalse(host.HasHomeTabs);
			IsTrue(ReferenceEquals(host.SettingsTab, host.SelectedShellTab));
		}
		finally
		{
			host.UninitializeLifecycle();
		}
	}

	#endregion
}
