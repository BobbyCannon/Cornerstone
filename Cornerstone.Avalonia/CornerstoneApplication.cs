#region References

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.Serialization;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Dispatcher = Avalonia.Threading.Dispatcher;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.Avalonia;

public abstract class CornerstoneApplication<T> : CornerstoneApplication
	where T : ILifecycle
{
	#region Fields

	private System.Threading.Timer _processLifecycleTimer;

	#endregion

	#region Properties

	public T Keystone { get; protected set; }

	#endregion

	#region Methods

	public override void Initialize()
	{
		// Serializer + infrastructure (base)
		base.Initialize();

		// Keystone after infrastructure (Init/Load timed by LifecycleTracker when StartupProfiler is set)
		using (AppBootstrap.StartupProfiler.Start("Keystone.Resolve"))
		{
			Keystone = AppBootstrap.GetInstance<T>();
		}

		Keystone.InitializeLifecycle();
		Keystone.LoadLifecycle();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.ShutdownRequested += OnShutdownRequested;
		}

		// Works on any platform that implements IControlledApplicationLifetime
		// Note: pretty sure this is desktops only (Windows, Linux, MacOS, etc.)
		//	meaning not mobile, browser, single view, etc
		if (ApplicationLifetime is IControlledApplicationLifetime controlled)
		{
			controlled.Exit += OnExit;
		}

		// Dispatcher hook + Avalonia base + StartOwnedLifecycles (Keystone then infrastructure)
		base.OnFrameworkInitializationCompleted();
	}

	protected override void OnShutdown()
	{
		_processLifecycleTimer?.Dispose();
		_processLifecycleTimer = null;

		if (Keystone is not null)
		{
			AppBootstrap.TeardownLifecycle(Keystone);
			Keystone = default;
		}

		base.OnShutdown();
	}

	/// <inheritdoc />
	protected override void StartOwnedLifecycles()
	{
		Keystone.StartLifecycle();
		_processLifecycleTimer = new System.Threading.Timer(_ =>
		{
			try
			{
				Keystone?.ProcessLifecycle();
			}
			catch (Exception ex)
			{
				AppBootstrap.LogException(ex);
			}
		}, null, 50, 50);
		base.StartOwnedLifecycles();
	}

	protected virtual void OnShutdownRequested(object sender, ShutdownRequestedEventArgs e)
	{
		// This is just the request, we will do the real process in OnExit.
		//OnShutdown();
	}

	private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
	{
		// Best-effort on platforms that support controlled exit
		OnShutdown();
	}

	#endregion
}

public abstract class CornerstoneApplication : Application, IDispatchable
{
	#region Fields

	private static readonly Version _avaloniaRuntimeVersion;
	private static CornerstoneDispatcher _dispatcher;
	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Constructors

	protected CornerstoneApplication()
	{
		DataTemplates.Add(new ViewLocator());
		AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
		TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
	}

	static CornerstoneApplication()
	{
		// Avalonia version is known here; applied once AppBootstrap is ready.
		_avaloniaRuntimeVersion = typeof(AppBuilder).Assembly.GetName().Version;
	}

	#endregion

	#region Properties

	public static CornerstoneDispatcher CornerstoneDispatcher => _dispatcher ??= new CornerstoneDispatcher();

	#endregion

	#region Methods

	public IDispatcher GetDispatcher()
	{
		return CornerstoneDispatcher;
	}

	public static TopLevel GetTopLevel()
	{
		var response = Current.GetTopLevel();
		return response;
	}

	public override void Initialize()
	{
		using (AppBootstrap.StartupProfiler.Start("App.Initialize"))
		{
			CornerstoneAvaloniaSerializerConfigurator.Configure();
			base.Initialize();

			EnsureAppBootstrapForAvalonia();
			ApplyAvaloniaRuntimeVersionOverride();
			AppBootstrap.InitializeInfrastructure();
		}
	}

	public override void OnFrameworkInitializationCompleted()
	{
		// Subscribe to dispatcher unhandled exceptions
		Dispatcher.UIThread.UnhandledException += OnDispatcherOnUnhandledException;
		base.OnFrameworkInitializationCompleted();
		StartOwnedLifecycles();
		CompleteStartupProfiling();
	}

	public override void RegisterServices()
	{
		using (AppBootstrap.StartupProfiler.Start("App.RegisterServices"))
		{
			EnsureAppBootstrapForAvalonia();
			ApplyAvaloniaRuntimeVersionOverride();

			// UI dispatcher may not have existed at host Main; replace null/placeholder registration.
			AppBootstrap.DependencyProvider.SetSingleton<IDispatcher>(CornerstoneDispatcher);
			AppBootstrap.DependencyProvider.AddSingleton(CornerstoneDispatcher);
			AppBootstrap.DependencyProvider.AddSingleton<ClipboardService>();

			base.RegisterServices();
		}
	}

	/// <summary>
	/// Start app-owned lifecycles after the framework is ready.
	/// Keystone apps start Keystone first, then infrastructure.
	/// </summary>
	protected virtual void StartOwnedLifecycles()
	{
		AppBootstrap.StartInfrastructure();
	}

	/// <summary>
	/// Freeze <see cref="AppBootstrap.StartupProfiler" /> after owned lifecycles have started.
	/// </summary>
	protected virtual void CompleteStartupProfiling()
	{
		AppBootstrap.StartupProfiler?.Complete();
	}

	/// <summary>
	/// Stop Keystone (subclass) then infrastructure.
	/// </summary>
	protected virtual void OnShutdown()
	{
		AppBootstrap.ShutdownInfrastructure();
	}

	public static async Task<string> TryOpenFileAsync(
		string startingDirectory = null,
		params FilePickerFileType[] pickerTypes)
	{
		var topLevel = GetTopLevel();
		if (topLevel == null)
		{
			return null;
		}

		if (string.IsNullOrWhiteSpace(startingDirectory))
		{
			startingDirectory = AppBootstrap.RuntimeInformation.ApplicationDataLocation;
		}

		var defaultDirectory = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startingDirectory);
		var options = new FilePickerOpenOptions
		{
			AllowMultiple = false,
			SuggestedStartLocation = defaultDirectory,
			FileTypeFilter = pickerTypes
		};

		var selected = await topLevel.StorageProvider.OpenFilePickerAsync(options);
		return selected.Count == 1 ? selected[0].Path.LocalPath : null;
	}

	public static async Task<string> TrySelectFileForSave(
		string startingDirectory = null,
		string defaultExtension = null,
		params FilePickerFileType[] fileTypeChoices)
	{
		var topLevel = GetTopLevel();
		if (topLevel == null)
		{
			return null;
		}

		startingDirectory ??= AppBootstrap.RuntimeInformation.ApplicationDataLocation;

		var defaultDirectory = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startingDirectory);
		var options = new FilePickerSaveOptions
		{
			SuggestedStartLocation = defaultDirectory,
			SuggestedFileType = defaultExtension == null ? null : fileTypeChoices.FirstOrDefault(x => x.Patterns.Any(p => p.EndsWith($".{defaultExtension}"))),
			FileTypeChoices = fileTypeChoices,
			DefaultExtension = defaultExtension
		};

		var selected = await topLevel.StorageProvider.SaveFilePickerAsync(options);
		return selected?.TryGetLocalPath();
	}

	public static async Task<string> TrySelectFolderAsync(string startingDirectory = null)
	{
		var topLevel = GetTopLevel();
		if (topLevel == null)
		{
			return null;
		}

		var options = new FolderPickerOpenOptions { AllowMultiple = false, Title = "Select Folder" };
		var selected = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
		var path = selected!.FirstOrDefault();
		var response = path?.TryGetLocalPath() ?? path?.Path.ToString();
		return response;
	}

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Design-time / tests may construct Avalonia without host Main.
	/// </summary>
	private static void EnsureAppBootstrapForAvalonia()
	{
		AppBootstrap.EnsureInitialized(
			applicationName: "Cornerstone",
			applicationAssembly: typeof(CornerstoneApplication).Assembly,
			dispatcher: CornerstoneDispatcher
		);
	}

	private static void ApplyAvaloniaRuntimeVersionOverride()
	{
		if (!AppBootstrap.IsInitialized || _avaloniaRuntimeVersion == null)
		{
			return;
		}

		AppBootstrap.RuntimeInformation.SetPlatformOverride(
			nameof(IRuntimeInformation.AvaloniaRuntimeVersion),
			_avaloniaRuntimeVersion
		);
	}

	private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		AppBootstrap.LogException(e.ExceptionObject as Exception);
	}

	private void OnDispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		AppBootstrap.LogException(e.Exception);
	}

	private void TaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
	{
		if (e.Exception.InnerException is { Message: "Looping animations must not use the Run method." })
		{
			// Ignore this but would be nice to fix it.
			return;
		}

		AppBootstrap.LogException(e.Exception);
	}

	#endregion
}