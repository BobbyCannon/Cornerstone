#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Sample.ViewModels;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabThemes : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Themes";

	#endregion

	#region Constructors

	public TabThemes() : this(ViewDependencyProvider.Get<ApplicationSettings>(), null)
	{
	}

	[DependencyInjectionConstructor]
	public TabThemes(ApplicationSettings applicationSettings, IDispatcher dispatcher) : base(dispatcher)
	{
		ApplicationSettings = applicationSettings;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ApplicationSettings ApplicationSettings { get; }

	#endregion

	#region Methods

	private void ThemeOnClick(object sender, RoutedEventArgs e)
	{
		if (sender is RadioButton { DataContext: ThemeColor themeColor })
		{
			ApplicationSettings.ThemeColor = themeColor;
		}
	}

	#endregion
}