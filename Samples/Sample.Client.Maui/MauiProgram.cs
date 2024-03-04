#region References

using CommunityToolkit.Maui;
using Cornerstone.Maui;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Sample.Client.Maui.Pages;
using Sample.Shared;
using Dispatcher = Microsoft.Maui.Dispatching.Dispatcher;

#endregion

namespace Sample.Client.Maui;

public static class MauiProgram
{
	#region Properties

	public static MauiViewManager MauiViewManager { get; set; }

	#endregion

	#region Methods

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("DejaVuSansMono.ttf", "FontMono");
				fonts.AddFont("fa-solid-900.ttf", "FontAwesome");
				fonts.AddFont("OpenSans-Light.ttf", "OpenSansLight");
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.ConfigureEssentials(essentials => { essentials.UseVersionTracking(); });

		var dispatcher = new MauiDispatcher(Dispatcher.GetForCurrentThread());
		var runtimeInformation = new MauiRuntimeInformation(dispatcher);
		runtimeInformation.Initialize();

		var secureVault = new MauiSecureVault(runtimeInformation, dispatcher);
		var locationProvider = new MauiLocationProvider(dispatcher);

		MauiViewManager = new MauiViewManager(runtimeInformation, locationProvider, dispatcher);

		builder.Services.AddSingleton<IRuntimeInformation>(_ => runtimeInformation);
		builder.Services.AddSingleton<RuntimeInformation>(_ => runtimeInformation);
		builder.Services.AddSingleton<SecureVault>(_ => secureVault);
		builder.Services.AddSingleton<IDispatcher>(_ => dispatcher);
		builder.Services.AddSingleton<SharedViewModel>(_ => MauiViewManager);
		builder.Services.AddSingleton(_ => MauiViewManager);

		builder.Services.AddScoped<LogInPage>();
		builder.Services.AddScoped<SettingsPage>();
		builder.Services.AddScoped<SpeedyListPage>();
		builder.Services.AddScoped<WebClientPage>();

		return builder.Build();
	}

	#endregion
}