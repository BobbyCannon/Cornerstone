#region References

using Avalonia.Controls.Notifications;
using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Keystone.Channels;

[SourceReflection]
[DependencyInjected]
public partial class NotificationChannel : KeystoneChannel
{
	#region Records

	[ChannelMessage<NotificationChannel>("ShowMessage")]
	public record struct NotificationMessage(string Title, string Message, NotificationType Type) : IChannelMessage;

	#endregion
}
