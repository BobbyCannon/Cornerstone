#region References

using System;
using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Channels;

[SourceReflection]
[DependencyInjected]
public partial class GrokUsageChannel : KeystoneChannel<GrokUsageMessageType>
{
	#region Methods

	[ChannelSubscription<GrokUsageMessageType, GrokUsageMessageForEnsureHomes>(GrokUsageMessageType.EnsureHomes)]
	public void EnsureHomes()
	{
		Publish(GrokUsageMessageType.EnsureHomes, new GrokUsageMessageForEnsureHomes());
	}

	[ChannelSubscription<GrokUsageMessageType, GrokUsageMessageForRefreshAll>(GrokUsageMessageType.RefreshAll)]
	public void RefreshAll()
	{
		Publish(GrokUsageMessageType.RefreshAll, new GrokUsageMessageForRefreshAll());
	}

	[ChannelSubscription<GrokUsageMessageType, GrokUsageMessageForRefreshHome>(GrokUsageMessageType.RefreshHome)]
	public void RefreshHome(Guid homeId)
	{
		Publish(GrokUsageMessageType.RefreshHome, new GrokUsageMessageForRefreshHome(homeId));
	}

	[ChannelSubscription<GrokUsageMessageType, GrokUsageMessageForSelectHome>(GrokUsageMessageType.SelectHome)]
	public void SelectHome(Guid homeId)
	{
		Publish(GrokUsageMessageType.SelectHome, new GrokUsageMessageForSelectHome(homeId));
	}

	[ChannelSubscription<GrokUsageMessageType, GrokUsageMessageForSelectPeriod>(GrokUsageMessageType.SelectPeriod)]
	public void SelectPeriod(Guid homeId, DateTimeOffset periodStart, DateTimeOffset periodEnd)
	{
		Publish(GrokUsageMessageType.SelectPeriod, new GrokUsageMessageForSelectPeriod(homeId, periodStart, periodEnd));
	}

	[ChannelSubscription<GrokUsageMessageType, GrokUsageMessageForSetSince>(GrokUsageMessageType.SetSince)]
	public void SetSince(DateTimeOffset sinceUtc)
	{
		Publish(GrokUsageMessageType.SetSince, new GrokUsageMessageForSetSince(sinceUtc));
	}

	[ChannelSubscription<GrokUsageMessageType, GrokUsageMessageForSetViewAsOf>(GrokUsageMessageType.SetViewAsOf)]
	public void SetViewAsOf(DateTimeOffset viewAsOf)
	{
		Publish(GrokUsageMessageType.SetViewAsOf, new GrokUsageMessageForSetViewAsOf(viewAsOf));
	}

	[ChannelSubscription<GrokUsageMessageType, GrokUsageMessageForSetViewLive>(GrokUsageMessageType.SetViewLive)]
	public void SetViewLive()
	{
		Publish(GrokUsageMessageType.SetViewLive, new GrokUsageMessageForSetViewLive());
	}

	#endregion
}