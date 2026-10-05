#region References

using System;
using System.Windows.Input;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Notifications;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabControlsButtons : UserControl
{
	#region Constants

	public const string HeaderName = "Buttons";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private WindowNotificationManager _notificationManager;

	#endregion

	#region Constructors

	public TabControlsButtons() : this(AppBootstrap.GetInstance<IDateTimeProvider>())
	{
	}

	public TabControlsButtons(IDateTimeProvider dateTimeProvider)
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
