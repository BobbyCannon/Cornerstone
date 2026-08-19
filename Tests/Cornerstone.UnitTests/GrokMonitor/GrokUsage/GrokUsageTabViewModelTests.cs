#region References

using System;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.GrokMonitor.GrokUsage.Channels;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor.GrokUsage;

[TestClass]
public class GrokUsageTabViewModelTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void ApplyModelChangesProjectsSessionsWithoutRereadingHostAfterWire()
	{
		var bus = new AppBus(new GrokUsageChannel());
		var state = new AppState(new AppSettings(), new GrokUsageState());
		var homeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
		var home = new GrokHomeUsageState(homeId)
		{
			DisplayName = "Personal",
			Path = @"C:\Users\Ada\.grok",
			HomeExists = true,
			HasBilling = true,
			UsagePercent = 10,
			PeriodStart = DateTimeOffset.Parse("2026-08-10T00:00:00Z"),
			PeriodEnd = DateTimeOffset.Parse("2026-08-17T00:00:00Z")
		};
		home.Sessions.Add(new GrokSessionUsageState
		{
			SessionId = "sess-1",
			Title = "First",
			TotalTokens = 100
		});
		state.GrokUsage.Homes.Add(home);

		var tab = new GrokUsageTabViewModel(bus, home, state.GrokUsage, state.Settings, Dispatcher, this);
		tab.InitializeLifecycle();
		tab.Attach(this);
		tab.ApplyModelChanges();

		AreEqual(1, tab.Sessions.Count);
		AreEqual("First", tab.Sessions[0].Title);
		AreEqual(100L, tab.Sessions[0].TotalTokens);
		AreEqual("Personal", tab.DisplayName);

		home.Sessions[0].Title = "Updated";
		home.Sessions[0].TotalTokens = 250;
		home.Sessions.Add(new GrokSessionUsageState
		{
			SessionId = "sess-2",
			Title = "Second",
			TotalTokens = 50
		});
		tab.ApplyModelChanges();

		AreEqual(2, tab.Sessions.Count);
		AreEqual("Updated", tab.Sessions[0].Title);
		AreEqual(250L, tab.Sessions[0].TotalTokens);
		AreEqual("Second", tab.Sessions[1].Title);
	}

	[TestMethod]
	public void SelectPeriodPublishesWhenProjectedSelectionDiffers()
	{
		var bus = new AppBus(new GrokUsageChannel());
		var state = new AppState(new AppSettings(), new GrokUsageState());
		var homeId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
		var currentStart = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
		var currentEnd = DateTimeOffset.Parse("2026-08-17T00:00:00Z");
		var prevStart = DateTimeOffset.Parse("2026-08-03T00:00:00Z");
		var prevEnd = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
		var home = new GrokHomeUsageState(homeId)
		{
			DisplayName = "Personal",
			Path = @"C:\Users\Ada\.grok",
			HomeExists = true,
			SelectedPeriodStart = currentStart,
			SelectedPeriodEnd = currentEnd,
			PeriodStart = currentStart,
			PeriodEnd = currentEnd
		};
		home.AvailablePeriods.Add(new GrokUsagePeriodState
		{
			PeriodStart = currentStart,
			PeriodEnd = currentEnd,
			IsCurrent = true,
			DisplayName = "current"
		});
		home.AvailablePeriods.Add(new GrokUsagePeriodState
		{
			PeriodStart = prevStart,
			PeriodEnd = prevEnd,
			DisplayName = "previous"
		});
		state.GrokUsage.Homes.Add(home);

		var published = 0;
		DateTimeOffset publishedStart = default;
		DateTimeOffset publishedEnd = default;
		bus.GrokUsage.SubscribeToSelectPeriod(message =>
		{
			published++;
			publishedStart = message.PeriodStart;
			publishedEnd = message.PeriodEnd;
		});

		var tab = new GrokUsageTabViewModel(bus, home, state.GrokUsage, state.Settings, Dispatcher, this);
		tab.InitializeLifecycle();
		tab.Attach(this);
		tab.ApplyModelChanges();

		AreEqual(2, tab.AvailablePeriods.Count);
		IsNotNull(tab.SelectedPeriod);
		AreEqual(currentStart, tab.SelectedPeriodStart);

		tab.SelectPeriod(tab.SelectedPeriod);
		AreEqual(0, published);

		tab.SelectPeriod(tab.AvailablePeriods[1]);
		AreEqual(1, published);
		AreEqual(prevStart, publishedStart);
		AreEqual(prevEnd, publishedEnd);
	}

	[TestMethod]
	public void SelectedPeriodAssignmentPublishesWhenUserChangesCombo()
	{
		var bus = new AppBus(new GrokUsageChannel());
		var state = new AppState(new AppSettings(), new GrokUsageState());
		var homeId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
		var currentStart = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
		var currentEnd = DateTimeOffset.Parse("2026-08-17T00:00:00Z");
		var prevStart = DateTimeOffset.Parse("2026-08-03T00:00:00Z");
		var prevEnd = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
		var home = new GrokHomeUsageState(homeId)
		{
			DisplayName = "Personal",
			Path = @"C:\Users\Ada\.grok",
			HomeExists = true,
			SelectedPeriodStart = currentStart,
			SelectedPeriodEnd = currentEnd,
			PeriodStart = currentStart,
			PeriodEnd = currentEnd
		};
		home.AvailablePeriods.Add(new GrokUsagePeriodState
		{
			PeriodStart = currentStart,
			PeriodEnd = currentEnd,
			IsCurrent = true
		});
		home.AvailablePeriods.Add(new GrokUsagePeriodState
		{
			PeriodStart = prevStart,
			PeriodEnd = prevEnd
		});
		state.GrokUsage.Homes.Add(home);

		var published = 0;
		bus.GrokUsage.SubscribeToSelectPeriod(_ => published++);

		var tab = new GrokUsageTabViewModel(bus, home, state.GrokUsage, state.Settings, Dispatcher, this);
		tab.InitializeLifecycle();
		tab.Attach(this);
		tab.ApplyModelChanges();
		AreEqual(0, published);

		tab.SelectedPeriod = tab.AvailablePeriods[1];
		AreEqual(1, published);
	}

	#endregion
}
