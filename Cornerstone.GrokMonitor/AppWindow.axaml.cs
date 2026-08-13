#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Data.Times;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.GrokMonitor;

public partial class AppWindow : CornerstoneWindow<AppViewModel>
{
	#region Constructors

	public AppWindow() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	public AppWindow(AppViewModel viewModel) : base(viewModel)
	{
		if (!Design.IsDesignMode)
		{
			RestoreWindowLocation(ViewModel.State.Settings.WindowLocation);
		}

		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void BuildMenu()
	{
		var file = MainMenu.Add(new MenuItemView { Name = "_File", IsParent = true });
		file.Children.Add(new MenuItemView
		{
			Name = "E_xit",
			Command = ExitApplicationCommand,
			InputGesture = "ALT+F4"
		});

		base.BuildMenu();
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		ViewModel.State.Settings.WindowLocation ??= new WindowLocation();
		ViewModel.State.Settings.WindowLocation.UpdateWith(GetWindowLocation());
		base.OnClosing(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		Title += $" ({(ViewModel.RuntimeInformation.ApplicationIsElevated ? "administrator, " : "")}";
		Title += $"{ViewModel.RuntimeInformation.ApplicationStartup.Humanize(new HumanizeSettings { MaxUnit = TimeUnit.Millisecond, WordFormat = WordFormat.Abbreviation })})";
		base.OnLoaded(e);
	}

	#endregion
}