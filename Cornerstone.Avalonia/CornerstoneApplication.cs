#region References

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Exceptions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.Avalonia;

public abstract class CornerstoneApplication : Application, IDispatchable
{
	#region Fields

	private static CornerstoneDispatcher _dispatcher;

	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Constructors

	protected CornerstoneApplication()
	{
		Dependencies = new DependencyInjector();
		ApplicationArguments = new ApplicationArguments();
	}

	static CornerstoneApplication()
	{
		PlatformDependencies = new DependencyInjector();
	}

	#endregion

	#region Properties

	public ApplicationArguments ApplicationArguments { get; }

	public DependencyInjector Dependencies { get; }

	public static CornerstoneDispatcher Dispatcher => _dispatcher ??= new CornerstoneDispatcher();

	public static DependencyInjector PlatformDependencies { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public IDispatcher GetDispatcher()
	{
		return Dispatcher;
	}

	public static T GetService<T>()
	{
		var app = (CornerstoneApplication) Current;
		if (app != null)
		{
			return app.Dependencies.GetInstance<T>();
		}

		throw new CornerstoneException("Application is not available.");
	}

	/// <inheritdoc />
	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			ApplicationArguments.Parse(desktop.Args);
		}
		base.OnFrameworkInitializationCompleted();
	}

	public void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <inheritdoc />
	public override void RegisterServices()
	{
		PlatformDependencies.Lock();
		Dependencies.Import(PlatformDependencies);
		Dependencies.AddSingleton<IClipboardService, ClipboardService>();
		Dependencies.AddSingleton<IDispatcher>(Dispatcher);
		Dependencies.AddSingleton<IRuntimeInformation>(CornerstoneRuntimeInformation.Instance);
		Dependencies.Lock();
		base.RegisterServices();
	}

	public static bool TryGetService<T>(out T service)
	{
		var app = (CornerstoneApplication) Current;
		if (app != null)
		{
			service = app.Dependencies.GetInstance<T>();
			return true;
		}

		service = default;
		return false;
	}

	#endregion
}