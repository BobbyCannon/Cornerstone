#region References

using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabCamera : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Camera";

	#endregion

	#region Fields

	private readonly IRuntimeInformation _runtimeInformation;

	#endregion

	#region Constructors

	public TabCamera()
		: this(GetInstance<IRuntimeInformation>(),
			GetInstance<IDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabCamera(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: base(dispatcher)
	{
		_runtimeInformation = runtimeInformation;

		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Methods

	private void TogglePreviewingOnClick(object sender, RoutedEventArgs e)
	{
		if (Camera.IsPreviewing)
		{
			Camera.StopAsync();
			return;
		}

		Camera.StartAsync();
	}

	#endregion
}