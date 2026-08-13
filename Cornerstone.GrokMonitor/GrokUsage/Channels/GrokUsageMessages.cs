#region References

using System;
using Cornerstone.Keystone.Messages;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Channels;

public record struct GrokUsageMessageForEnsureHomes : IChannelMessage;

public record struct GrokUsageMessageForRefreshHome(Guid HomeId) : IChannelMessage;

public record struct GrokUsageMessageForRefreshAll : IChannelMessage;

public record struct GrokUsageMessageForSelectHome(Guid HomeId) : IChannelMessage;

public record struct GrokUsageMessageForSetSince(DateTimeOffset SinceUtc) : IChannelMessage;

public record struct GrokUsageMessageForSelectPeriod(
	Guid HomeId,
	DateTimeOffset PeriodStart,
	DateTimeOffset PeriodEnd) : IChannelMessage;

public record struct GrokUsageMessageForSetViewAsOf(DateTimeOffset ViewAsOf) : IChannelMessage;

public record struct GrokUsageMessageForSetViewLive : IChannelMessage;