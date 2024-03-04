#region References

using System.Threading.Tasks;
using System.Windows.Input;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Microsoft.Maui.Controls;

#endregion

namespace Sample.Client.Maui.Pages;

public class PageSwitcher : Bindable
{
	#region Constants

	public const string SettingsPageName = "Settings";
	public const string SettingsPageRoute = "Settings";
	public const string SpeedyListPageName = "Speedy List";
	public const string SpeedyListPageRoute = "SpeedyList";
	public const string WebClientPageName = "Web Client";
	public const string WebClientPageRoute = "WebClient";

	#endregion

	#region Fields

	private readonly MauiViewManager _mauiViewManager;

	#endregion

	#region Constructors

	public PageSwitcher(MauiViewManager mauiViewManager, IDispatcher dispatcher) : base(dispatcher)
	{
		_mauiViewManager = mauiViewManager;

		SwitchToSpeedyListCommand = new RelayCommand(_ => SwitchToSpeedyListPage());
		SwitchToWebClientCommand = new RelayCommand(_ => SwitchToWebClientPage());
	}

	#endregion

	#region Properties

	public ICommand SwitchToSpeedyListCommand { get; }

	public ICommand SwitchToWebClientCommand { get; }

	#endregion

	#region Methods

	public async void SwitchToLoginPage()
	{
		await SwitchToRouteAsync("Login");
	}

	public async void SwitchToSpeedyListPage()
	{
		await SwitchToRouteAsync(SpeedyListPageRoute);
	}

	public async void SwitchToWebClientPage()
	{
		await SwitchToRouteAsync(WebClientPageRoute);
	}

	private async Task SwitchToRouteAsync(string route)
	{
		await this.DispatchAsync(() => Shell.Current.GoToAsync($"//{route}"));
	}

	#endregion
}