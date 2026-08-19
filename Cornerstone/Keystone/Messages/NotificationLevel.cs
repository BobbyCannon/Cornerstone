namespace Cornerstone.Keystone.Messages;

/// <summary>
/// UI-free notification severity for bus messages.
/// Values match Avalonia NotificationType for host mapping.
/// </summary>
public enum NotificationLevel
{
	Information = 0,
	Success = 1,
	Warning = 2,
	Error = 3
}