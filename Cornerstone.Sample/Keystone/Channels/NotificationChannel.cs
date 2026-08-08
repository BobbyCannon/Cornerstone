#region References

using Avalonia.Controls.Notifications;
using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Keystone.Channels;

[SourceReflection]
[DependencyInjected]
public partial class NotificationChannel : KeystoneChannel<NotificationChannel.NotificationMessageType>
{
	#region Methods

	[ChannelSubscription<NotificationMessageType, NotificationMessage>(NotificationMessageType.ShowMessage)]
	public void ShowMessage(string title, string message, NotificationType type)
	{
		Publish(NotificationMessageType.ShowMessage, new NotificationMessage(title, message, type));
	}

	#endregion

	#region Records

	public record struct NotificationMessage(string Title, string Message, NotificationType Type) : IChannelMessage;

	#endregion

	#region Enumerations

	public enum NotificationMessageType
	{
		Unknown,
		ShowMessage
	}

	#endregion
}