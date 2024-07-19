#region References

using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabNotificationCard : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "NotificationCard";

	#endregion

	#region Fields

	private WindowNotificationManager _notificationManagerBottomCenter;
	private WindowNotificationManager _notificationManagerBottomLeft;
	private WindowNotificationManager _notificationManagerBottomRight;
	private WindowNotificationManager _notificationManagerTopCenter;
	private WindowNotificationManager _notificationManagerTopLeft;
	private WindowNotificationManager _notificationManagerTopRight;

	#endregion

	#region Constructors

	public TabNotificationCard()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		var topLevel = TopLevel.GetTopLevel(this);

		_notificationManagerTopCenter = new WindowNotificationManager(topLevel) { Position = NotificationPosition.TopCenter };
		_notificationManagerTopRight = new WindowNotificationManager(topLevel) { Position = NotificationPosition.TopRight };
		_notificationManagerTopLeft = new WindowNotificationManager(topLevel) { Position = NotificationPosition.TopLeft };
		_notificationManagerBottomRight = new WindowNotificationManager(topLevel) { Position = NotificationPosition.BottomRight };
		_notificationManagerBottomLeft = new WindowNotificationManager(topLevel) { Position = NotificationPosition.BottomLeft };
		_notificationManagerBottomCenter = new WindowNotificationManager(topLevel) { Position = NotificationPosition.BottomCenter };
	}

	private void ShowBottomCenterNotification_OnClick(object sender, RoutedEventArgs e)
	{
		_notificationManagerBottomCenter?.Show(new Notification("Notification", "This is a bottom center Error notification.", NotificationType.Error),
			NotificationType.Error, null, null, null, ["bottom"]
		);
	}

	private void ShowBottomLeftNotification_OnClick(object sender, RoutedEventArgs e)
	{
		_notificationManagerBottomLeft?.Show(
			new Notification("Notification", "This is a bottom left Warning notification.", NotificationType.Warning),
			NotificationType.Warning, null, null, null, ["left"]
		);
	}

	private void ShowBottomRightNotification_OnClick(object sender, RoutedEventArgs e)
	{
		_notificationManagerBottomRight?.Show(new Notification("Notification", "This is a bottom right Error notification.", NotificationType.Error),
			NotificationType.Error, null, null, null, ["right"]
		);
	}

	private void ShowTopCenterNotification_OnClick(object sender, RoutedEventArgs e)
	{
		_notificationManagerTopCenter?.Show(
			new Notification("Notification", "This is a top center notification."),
			NotificationType.Information, null, null, null, ["top"]
		);
	}

	private void ShowTopLeftNotification_OnClick(object sender, RoutedEventArgs e)
	{
		_notificationManagerTopLeft?.Show(new Notification("Notification", "This is a top left Success notification.", NotificationType.Success),
			NotificationType.Success, null, null, null, ["left"]
		);
	}

	private void ShowTopRightNotification_OnClick(object sender, RoutedEventArgs e)
	{
		_notificationManagerTopRight?.Show(new Notification("Notification", "This is a top right Information notification."),
			NotificationType.Information, null, null, null, ["right"]
		);
	}

	#endregion
}