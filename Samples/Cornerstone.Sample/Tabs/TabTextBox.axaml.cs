#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Sample.ViewModels;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabTextBox : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "TextBox";

	#endregion

	#region Constructors

	public TabTextBox() : this(ViewDependencyProvider.Get<ApplicationSettings>(), null)
	{
	}

	[DependencyInjectionConstructor]
	public TabTextBox(ApplicationSettings applicationSettings, IDispatcher dispatcher) : base(dispatcher)
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