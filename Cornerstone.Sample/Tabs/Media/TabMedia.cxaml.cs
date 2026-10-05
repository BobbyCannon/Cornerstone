#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs.Media;

[SourceReflection]
public partial class TabMedia : UserControl
{
	#region Constants

	public const string HeaderName = "Media";

	#endregion

	#region Constructors

	public TabMedia() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabMedia(IRuntimeInformation runtimeInformation)
	{
		IsCaptureAvailable = runtimeInformation.DevicePlatform is DevicePlatform.Windows
			or DevicePlatform.Android
			or DevicePlatform.IOS;
		RuntimeInformation = runtimeInformation;
		VlcDescription = IsVlcAvailable
			? "Native LibVLC VideoView island per platform."
			: "Native LibVLC is not available in the browser. Run Desktop, Android, or iOS.";
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool IsCaptureAvailable { get; }

	public bool IsVlcAvailable => RuntimeInformation.DevicePlatform != DevicePlatform.Browser;

	public IRuntimeInformation RuntimeInformation { get; }

	public string VlcDescription { get; }

	#endregion

	#region Methods

	private static Control CreateVlcUnavailablePage()
	{
		return new TextBlock
		{
			Margin = new Thickness(16),
			TextWrapping = TextWrapping.Wrap,
			Text = "LibVLC VideoView is a native island (Windows, Android, iOS). It cannot run in net10-browser. Run Sample.Desktop, Sample.Android, or Sample.iOS."
		};
	}

	private void Open(string title, Control page)
	{
		PageNavigator.GetPageNavigator(this).Navigate(title, page);
	}

	private void OpenCamera(object sender, RoutedEventArgs e)
	{
		Open(TabCamera.HeaderName, new TabCamera());
	}

	private void OpenPlayer(object sender, RoutedEventArgs e)
	{
		Open(TabMediaPlayer.HeaderName, new TabMediaPlayer());
	}

	private void OpenVlc(object sender, RoutedEventArgs e)
	{
		if (!IsVlcAvailable)
		{
			Open(TabMediaVlc.HeaderName, CreateVlcUnavailablePage());
			return;
		}

		try
		{
			Open(TabMediaVlc.HeaderName, new TabMediaVlc());
		}
		catch (Exception exception)
		{
			var message = exception.GetBaseException().Message;
			Open(TabMediaVlc.HeaderName, new TextBlock
			{
				Margin = new Thickness(16),
				TextWrapping = TextWrapping.Wrap,
				Text = message
			});
		}
	}

	private void OpenWeb(object sender, RoutedEventArgs e)
	{
		Open(TabWebView.HeaderName, new TabWebView());
	}

	#endregion
}
