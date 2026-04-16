#region References

using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabButton : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Button";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private WindowNotificationManager _notificationManager;

	#endregion

	#region Constructors

	public TabButton() : this(GetInstance<IDateTimeProvider>())
	{
	}

	public TabButton(IDateTimeProvider dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;

		ButtonCommand = new RelayCommand(ShowNotification);

		InitializeComponent();
	}

	#endregion

	#region Properties

	public ICommand ButtonCommand { get; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		var topLevel = TopLevel.GetTopLevel(this);

		_notificationManager = new WindowNotificationManager(topLevel) { Position = NotificationPosition.TopRight };
	}

	private void ShowNotification(object message)
	{
		_notificationManager?.Show(
			new Notification($"{message?.ToString() ?? "Unknown"} Button", _dateTimeProvider.Now.ToString("G")),
			NotificationType.Information, TimeSpan.FromSeconds(3), null, null, ["right"]
		);
	}

	#endregion
}