#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Agent.Views;
using Cornerstone.Presentation.Theme;
using Cornerstone.Data.Times;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Text;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.DesignTime;

#endregion

namespace Cornerstone.Agent;

public partial class AppWindow : Window<AppViewModel>
{
	#region Constructors

	public AppWindow() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	public AppWindow(AppViewModel viewModel) : base(viewModel)
	{
		if (!Design.IsDesignMode)
		{
			this.RestoreWindowLocation(ViewModel.State.Settings.WindowLocation);
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

		var help = MainMenu.Add(new MenuItemView { Name = "_Help" });
		help.Children.Add(new MenuItemView { Name = "_About", Command = ViewModel.SelectViewCommand, CommandParameter = AboutViewModel.AssemblyName });

		base.BuildMenu();
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		ViewModel.State.Settings.WindowLocation.UpdateWith(this.GetWindowLocation());
		base.OnClosing(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		Title += $" ({(ViewModel.State.RuntimeInformation.ApplicationIsElevated ? "administrator, " : "")}";
		Title += $"{ViewModel.State.RuntimeInformation.ApplicationStartup.Humanize(new HumanizeSettings { MaxUnit = TimeUnit.Millisecond, WordFormat = WordFormat.Abbreviation })})";
		base.OnLoaded(e);
	}

	#endregion
}