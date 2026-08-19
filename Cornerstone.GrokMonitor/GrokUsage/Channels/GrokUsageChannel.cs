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
public partial class GrokUsageChannel : KeystoneChannel
{
	#region Records

	[ChannelMessage<GrokUsageChannel>]
	public record struct EnsureHomesMessage : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct RefreshHomeMessage(Guid HomeId) : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct RefreshAllMessage : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct SelectHomeMessage(Guid HomeId) : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct SetSinceMessage(DateTimeOffset SinceUtc) : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct SelectPeriodMessage(Guid HomeId, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd) : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct SetViewAsOfMessage(Guid HomeId, DateTimeOffset ViewAsOf) : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct SetViewLiveMessage(Guid HomeId) : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct StartReplayMessage(Guid HomeId) : IChannelMessage;

	[ChannelMessage<GrokUsageChannel>]
	public record struct StopReplayMessage(Guid HomeId) : IChannelMessage;

	#endregion
}