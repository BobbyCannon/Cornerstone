#region References

using System;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.DockingManager;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabDockingManager : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Docking / Layout";

	#endregion

	#region Fields

	private WindowNotificationManager _notificationManagerTopRight;

	#endregion

	#region Constructors

	public TabDockingManager()
	{
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		_notificationManagerTopRight = new WindowNotificationManager(topLevel) { Position = NotificationPosition.TopRight };
		base.OnAttachedToVisualTree(e);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		LoadSample1OnClick(this, e);
		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		ClearOnClick(this, e);
		base.OnUnloaded(e);
	}

	private void ClearOnClick(object sender, RoutedEventArgs e)
	{
		DockingManager.Close();
	}

	private void LoadLayoutOnClick(object sender, RoutedEventArgs e)
	{
		try
		{
			DockingManager.FromDockLayoutJson(LayoutJson.Text);
		}
		catch (Exception ex)
		{
			_notificationManagerTopRight?.Show(
				new Notification("Error Restoring Layout", ex.Message),
				NotificationType.Error, null, null, null, ["right"]
			);
		}
	}

	private void LoadSample1OnClick(object sender, RoutedEventArgs e)
	{
		DockingManager.Close();

		DockingManager.AddTab(new TextTabModel { Header = "Tab 1", Text = "Sample Tab 1" });
		DockingManager.AddTab(new TextTabModel { Header = "Tab 2", Text = "Sample Tab 2" });
		DockingManager.AddTab(new TextTabModel { Header = "Tab 3", Text = "Sample Tab 3" });
		DockingManager.AddTab(new TextTabModel { Header = "Tab 4", Text = "Sample Tab 4" });
		DockingManager.AddTab(new TextBoxTabModel { Header = "Fox", Text = "The quick brown fox jumped" });
	}

	private void LoadSample2OnClick(object sender, RoutedEventArgs e)
	{
		DockingManager.Close();
	}

	private void SaveLayoutOnClick(object sender, RoutedEventArgs e)
	{
		var json = DockingManager.ToDockLayoutJson();
		LayoutJson.LoadData(json, Encoding.UTF8, ".json");
	}

	#endregion
}

public class TextBoxTabModel : TextTabModel
{
	#region Constructors

	public TextBoxTabModel()
		: base(Guid.NewGuid(), "TextBox", "FontAwesome.File.Regular", null)
	{
		PopupCommand = new RelayCommand(ShowSamplePopup);
	}

	#endregion

	#region Properties

	public ICommand PopupCommand { get; }

	#endregion

	#region Methods

	private void ShowSamplePopup(object obj)
	{
		Popup = new TextBoxSamplePopup();
	}

	#endregion
}

public class TextBoxSamplePopup : TabPopup
{
	#region Constructors

	public TextBoxSamplePopup()
	{
		ProgressDescription = "aoeu aoeu aoeu...";
	}

	#endregion
}

public class TextTabModel : DockableTabModel
{
	#region Constructors

	public TextTabModel()
		: base(Guid.NewGuid(), "Text", "FontAwesome.File.Solid", null)
	{
	}

	protected TextTabModel(Guid id, string header, string iconName, IDispatcher dispatcher)
		: base(id, header, iconName, dispatcher)
	{
	}

	#endregion

	#region Properties

	public string Text { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void GetLayoutData(PartialUpdate update)
	{
		update.AddOrUpdate(nameof(Header), Header);
		update.AddOrUpdate(nameof(Text), Text);
		base.GetLayoutData(update);
	}

	/// <inheritdoc />
	protected override void RestoreLayoutData(PartialUpdate update)
	{
		update.TryGet<string>(nameof(Header), x => Header = x);
		update.TryGet<string>(nameof(Text), x => Text = x);
		base.RestoreLayoutData(update);
	}

	#endregion
}