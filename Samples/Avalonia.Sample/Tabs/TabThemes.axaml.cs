#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Sample.ViewModels;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabThemes : CornerstoneUserControl<MainViewModel>
{
	#region Constants

	public const string HeaderName = "Themes";

	#endregion

	#region Constructors

	public TabThemes() : this(GetService<MainViewModel>())
	{
	}

	public TabThemes(MainViewModel mainViewModel) : base(mainViewModel)
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	private void ThemeOnClick(object sender, RoutedEventArgs e)
	{
		if (sender is RadioButton { DataContext: ThemeColor themeColor })
		{
			ViewModel.ApplicationSettings.ThemeColor = themeColor;
		}
	}

	#endregion
}