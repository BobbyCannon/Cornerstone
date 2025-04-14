#region References

using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabMediaPlayer : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Media Player";

	#endregion

	#region Fields

	private readonly IRuntimeInformation _runtimeInformation;

	#endregion

	#region Constructors

	public TabMediaPlayer()
		: this(GetInstance<IRuntimeInformation>(),
			GetInstance<IDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabMediaPlayer(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: base(dispatcher)
	{
		_runtimeInformation = runtimeInformation;

		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Methods

	private void PauseOnClick(object sender, RoutedEventArgs e)
	{
		MediaPlayer.PauseVideo();
	}

	private void PlayOnClick(object sender, RoutedEventArgs e)
	{
		//MediaPlayer.MediaUrl = "avares://Cornerstone.Sample/Assets/SampleVideo.mp4";
		//MediaPlayer.PlayVideo("C:\\Workspaces\\BobsToolbox\\Cornerstone\\Samples\\Cornerstone.Sample\\Assets\\SampleVideo.mp4");
		//var path = System.IO.Path.Join(_runtimeInformation.ApplicationLocation, "SampleData", "SampleVideo.mp4");
		MediaPlayer.PlayVideo("https://sample-videos.com/video321/mp4/720/big_buck_bunny_720p_1mb.mp4");
	}

	private void StopOnClick(object sender, RoutedEventArgs e)
	{
		MediaPlayer.StopVideo();
	}

	#endregion
}