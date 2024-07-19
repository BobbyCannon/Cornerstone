#region References

using Cornerstone;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security.Vault;

#endregion

namespace Avalonia.Sample.ViewModels;

internal static class ViewModelProviderForDesignMode
{
	#region Constructors

	static ViewModelProviderForDesignMode()
	{
		Dependencies = new DependencyInjector();
		RuntimeInformation = new RuntimeInformation();

		Dependencies.AddSingleton<IDispatcher>(() => null);
		Dependencies.AddSingleton<IRuntimeInformation, RuntimeInformation>(() => RuntimeInformation);
		Dependencies.AddSingleton<IClipboardService, ClipboardService>();
		Dependencies.AddSingleton<IWindowsHelloService, WindowsHelloServiceDummy>();

		Dependencies.AddSingleton<ApplicationSettings>();
		Dependencies.AddSingleton<MainViewModel>();
	}

	#endregion

	#region Properties

	public static DependencyInjector Dependencies { get; }

	public static RuntimeInformation RuntimeInformation { get; set; }

	#endregion

	#region Methods

	public static T Get<T>()
	{
		return Dependencies.GetInstance<T>();
	}

	#endregion
}