#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Serialization;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Timer = System.Threading.Timer;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Theming;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Chrome;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Cornerstone application that owns a Keystone (or other) lifecycle.
/// </summary>
public abstract class Application<T> : Application
	where T : ILifecycle
{
	#region Fields

	private Timer _processLifecycleTimer;

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
		using (AppBootstrap.StartupProfiler.Start("Application.Keystone.Resolve"))
		{
			Keystone = AppBootstrap.GetInstance<T>();
		}

		Keystone.InitializeLifecycle();
		Keystone.LoadLifecycle();
		MarkAfterInitialize();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		RecordAfterInitializeGap();

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

		// Dispatcher hook + Cornerstone base + StartOwnedLifecycles (Keystone then infrastructure)
		base.OnFrameworkInitializationCompleted();
	}

	protected override void OnShutdown()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.ShutdownRequested -= OnShutdownRequested;
		}

		if (ApplicationLifetime is IControlledApplicationLifetime controlled)
		{
			controlled.Exit -= OnExit;
		}

		_processLifecycleTimer?.Dispose();
		_processLifecycleTimer = null;

		if (Keystone is not null)
		{
			AppBootstrap.TeardownLifecycle(Keystone);
			Keystone = default;
		}

		base.OnShutdown();
	}

	protected virtual void OnShutdownRequested(object sender, ShutdownRequestedEventArgs e)
	{
		// This is just the request, we will do the real process in OnExit.
		//OnShutdown();
	}

	/// <inheritdoc />
	protected override void StartOwnedLifecycles()
	{
		Keystone.StartLifecycle();
		_processLifecycleTimer = new Timer(_ =>
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

	private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
	{
		// Best-effort on platforms that support controlled exit
		OnShutdown();
	}

	#endregion
}

/// <summary>
/// Encapsulates a Cornerstone application.
/// </summary>
/// <remarks>
/// The <see cref="Application" /> class encapsulates Cornerstone application-specific
/// functionality, including:
/// - A global set of <see cref="DataTemplates" />.
/// - A global set of <see cref="Styles" />.
/// - A <see cref="FocusManager" />.
/// - An <see cref="InputManager" />.
/// - Registers services needed by the rest of Cornerstone in the <see cref="RegisterServices" />
/// method.
/// - Tracks the lifetime of the application.
/// - AppBootstrap, UI dispatcher, infrastructure lifecycle, and crash logging.
/// </remarks>
public class Application : PresentationObject, IDataContextProvider, IGlobalDataTemplates, IGlobalStyles, IThemeVariantHost, IThemeVariantRoot, IOptionalFeatureProvider, IDispatchable
{
	#region Fields

	/// <inheritdoc cref="ThemeVariantScope.ActualThemeVariantProperty" />
	public static readonly StyledProperty<ThemeVariant> ActualThemeVariantProperty;

	/// <summary>
	/// Defines the <see cref="DataContext" /> property.
	/// </summary>
	public static readonly StyledProperty<object?> DataContextProperty;

	/// <summary>
	/// Defines Name property
	/// </summary>
	public static readonly DirectProperty<Application, string?> NameProperty;

	/// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariantProperty" />
	public static readonly StyledProperty<ThemeVariant?> RequestedThemeVariantProperty;

	private long _afterInitializeTicks;
	private IApplicationLifetime? _applicationLifetime;
	private static readonly Version _cornerstoneRuntimeVersion;

	/// <summary>
	/// The application-global data templates.
	/// </summary>
	private DataTemplates? _dataTemplates;

	private string? _name;
	private PropertyChangedEventHandler _propertyChangedHandler;
	private IResourceDictionary? _resources;
	private bool _setupCompleted;

	private Styles? _styles;
	private Action<IReadOnlyList<IStyle>>? _stylesAdded;
	private Action<IReadOnlyList<IStyle>>? _stylesRemoved;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an instance of the <see cref="Application" /> class.
	/// </summary>
	public Application()
	{
		Name = "Cornerstone Application";
		TryAddThemeViewLocator();
		AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
		TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
	}

	static Application()
	{
		// Cornerstone version is known here; applied once AppBootstrap is ready.
		ActualThemeVariantProperty = ThemeVariantScope.ActualThemeVariantProperty.AddOwner<Application>();
		DataContextProperty = StyledElement.DataContextProperty.AddOwner<Application>();
		NameProperty = PresentationProperty.RegisterDirect<Application, string>("Name", o => o.Name, (o, v) => o.Name = v);
		RequestedThemeVariantProperty = ThemeVariantScope.RequestedThemeVariantProperty.AddOwner<Application>();
		_cornerstoneRuntimeVersion = typeof(AppBuilder).Assembly.GetName().Version;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	[SuppressMessage("PresentationProperty", "AVP1031", Justification = "This property is supposed to be a styled readonly property.")]
	[SuppressMessage("PresentationProperty", "AVP1030", Justification = "False positive.")]
	public ThemeVariant ActualThemeVariant => GetValue(ActualThemeVariantProperty);

	/// <summary>
	/// Application lifetime, use it for things like setting the main window and exiting the app from code
	/// Currently supported lifetimes are:
	/// - <see cref="IClassicDesktopStyleApplicationLifetime" />
	/// - <see cref="ISingleViewApplicationLifetime" />
	/// - <see cref="ISingleTopLevelApplicationLifetime" />
	/// - <see cref="IControlledApplicationLifetime" />
	/// </summary>
	public IApplicationLifetime? ApplicationLifetime
	{
		get => _applicationLifetime;
		set
		{
			if (_setupCompleted)
			{
				throw new InvalidOperationException($"It's not possible to change {nameof(ApplicationLifetime)} after Application was initialized.");
			}

			_applicationLifetime = value;
		}
	}

	/// <summary>
	/// Gets the current instance of the <see cref="Application" /> class.
	/// </summary>
	/// <value>
	/// The current instance of the <see cref="Application" /> class.
	/// </value>
	public static Application? Current => PresentationLocator.Current.GetService<Application>();

	/// <summary>
	/// Gets or sets the Applications's data context.
	/// </summary>
	/// <remarks>
	/// The data context property specifies the default object that will
	/// be used for data binding.
	/// </remarks>
	public object? DataContext
	{
		get => GetValue(DataContextProperty);
		set => SetValue(DataContextProperty, value);
	}

	/// <summary>
	/// Gets or sets the application's global data templates.
	/// </summary>
	/// <value>
	/// The application's global data templates.
	/// </value>
	public DataTemplates DataTemplates => _dataTemplates ??= [];

	/// <summary>
	/// Application name to be used for various platform-specific purposes
	/// </summary>
	public string? Name
	{
		get => _name;
		set => SetAndRaise(NameProperty, ref _name, value);
	}

	/// <summary>
	/// Represents a contract for accessing global platform-specific settings.
	/// </summary>
	/// <remarks>
	/// PlatformSettings can be null only if application wasn't initialized yet.
	/// <see cref="TopLevel" />'s <see cref="TopLevel.PlatformSettings" /> is an equivalent API
	/// which should always be preferred over a global one,
	/// as specific top levels might have different settings set-up.
	/// </remarks>
	public IPlatformSettings? PlatformSettings => this.TryGetFeature<IPlatformSettings>();

	/// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariant" />
	public ThemeVariant? RequestedThemeVariant
	{
		get => GetValue(RequestedThemeVariantProperty);
		set => SetValue(RequestedThemeVariantProperty, value);
	}

	/// <summary>
	/// Gets the application's global resource dictionary.
	/// </summary>
	public IResourceDictionary Resources
	{
		get => _resources ??= new ResourceDictionary(this);
		set
		{
			value = value ?? throw new ArgumentNullException(nameof(value));
			_resources?.RemoveOwner(this);
			_resources = value;
			_resources.AddOwner(this);
		}
	}

	/// <summary>
	/// Gets the application's global styles.
	/// </summary>
	/// <value>
	/// The application's global styles.
	/// </value>
	/// <remarks>
	/// Global styles apply to all windows in the application.
	/// </remarks>
	public Styles Styles => _styles ??= new Styles(this);

	/// <summary>
	/// Gets the application's input manager.
	/// </summary>
	/// <value>
	/// The application's input manager.
	/// </value>
	internal InputManager? InputManager { get; private set; }

	/// <inheritdoc />
	bool IResourceNode.HasResources =>
		(_resources?.HasResources ?? false) ||
		(((IResourceNode?) _styles)?.HasResources ?? false);

	/// <inheritdoc />
	bool IDataTemplateHost.IsDataTemplatesInitialized => _dataTemplates != null;

	/// <inheritdoc />
	bool IStyleHost.IsStylesInitialized => _styles != null;

	bool IThemeVariantRoot.IsThemeVariantRoot => true;

	/// <summary>
	/// Gets the styling parent of the application, which is null.
	/// </summary>
	IStyleHost? IStyleHost.StylingParent => null;

	#endregion

	#region Methods

	public IDispatcher GetDispatcher()
	{
		return Dispatcher.CurrentDispatcher;
	}

	/// <summary>
	/// Gets the current application's top-level visual.
	/// </summary>
	public static TopLevel GetTopLevel()
	{
		var current = Current;
		if (current == null)
		{
			return null;
		}

		return current.ApplicationLifetime switch
		{
			IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
			ISingleViewApplicationLifetime viewApp => TopLevel.GetTopLevel(viewApp.MainView),
			_ => null
		};
	}

	/// <summary>
	/// Initializes the application by loading XAML etc.
	/// </summary>
	public virtual void Initialize()
	{
		using (AppBootstrap.StartupProfiler.Start("App.Initialize"))
		{
			TryConfigureSerializer();
			EnsureAppBootstrapForCornerstone();
			ApplyCornerstoneRuntimeVersionOverride();
			AppBootstrap.InitializeInfrastructure();
		}
	}

	public virtual void OnFrameworkInitializationCompleted()
	{
		using var scope = AppBootstrap.StartupProfiler.Start("Application.OnFrameworkInitializationCompleted");
		RecordAfterInitializeGap();

		// Subscribe to dispatcher unhandled exceptions
		Dispatcher.UIThread.UnhandledException += OnDispatcherOnUnhandledException;

		StartOwnedLifecycles();
		ArmFirstWindowProfiling();
	}

	/// <summary>
	/// Register's the services needed by Cornerstone.Presentation.
	/// </summary>
	public virtual void RegisterServices()
	{
		AppBootstrap.StartupProfiler?.RecordMark();
		using (AppBootstrap.StartupProfiler.Start("App.RegisterServices"))
		{
			EnsureAppBootstrapForCornerstone();
			ApplyCornerstoneRuntimeVersionOverride();

			// UI dispatcher may not have existed at host Main; replace null/placeholder registration.
			// SetSingleton (not AddSingleton) so per-test isolation can drop the previous dispatcher.
			AppBootstrap.DependencyProvider.SetSingleton<IDispatcher>(Dispatcher);
			AppBootstrap.DependencyProvider.SetSingleton(Dispatcher);

			PresentationSynchronizationContext.InstallIfNeeded();
			InputManager = new InputManager();

			InitializeThemeVariant();

			PresentationLocator.CurrentMutable
				.Bind<IAccessKeyHandler>().ToTransient<AccessKeyHandler>()
				.Bind<IGlobalDataTemplates>().ToConstant(this)
				.Bind<IGlobalStyles>().ToConstant(this)
				.Bind<IThemeVariantHost>().ToConstant(this)
				.Bind<IInputManager>().ToConstant(InputManager)
				.Bind<IToolTipService>().ToConstant(new ToolTipService(InputManager))
				.Bind<IKeyboardNavigationHandler>().ToTransient<KeyboardNavigationHandler>()
				.Bind<IDragDropDevice>().ToConstant(DragDropDevice.Instance);

			// TODO: Fix this, for now we keep this behavior since someone might be relying on it in 0.9.x
			if (PresentationLocator.Current.GetService<IPlatformDragSource>() == null)
			{
				PresentationLocator.CurrentMutable
					.Bind<IPlatformDragSource>()
					.ToTransient<InProcessDragSource>();
			}

			PresentationLocator
				.CurrentMutable
				.Bind<IGlobalClock>()
				.ToConstant(MediaContext.Instance.Clock);

			_setupCompleted = true;
		}
	}

	/// <summary>
	/// Queries for an optional feature.
	/// </summary>
	/// <param name="featureType"> Feature type. </param>
	/// <remarks>
	/// Features currently supported by <see cref="Application.TryGetFeature" />:
	/// <list type="bullet">
	/// <item> IPlatformSettings </item>
	/// <item> IActivatableApplicationLifetime </item>
	/// </list>
	/// </remarks>
	public object? TryGetFeature(Type featureType)
	{
		if (featureType == typeof(IPlatformSettings))
		{
			return PresentationLocator.Current.GetService<IPlatformSettings>();
		}

		if (featureType == typeof(IActivatableLifetime))
		{
			return PresentationLocator.Current.GetService<IActivatableLifetime>();
		}

		// Do not return just any service from PresentationLocator.
		return null;
	}

	/// <inheritdoc />
	public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
	{
		value = null;
		return (_resources?.TryGetResource(key, theme, out value) ?? false)
			|| Styles.TryGetResource(key, theme, out value);
	}

	/// <summary>
	/// Freeze <see cref="AppBootstrap.StartupProfiler" /> after the first window has loaded.
	/// </summary>
	protected virtual void CompleteStartupProfiling()
	{
		AppBootstrap.StartupProfiler?.Complete();
	}

	/// <summary>
	/// Stamp the clock when Cornerstone Initialize() returns (after Keystone Load).
	/// </summary>
	protected void MarkAfterInitialize()
	{
		_afterInitializeTicks = AppBootstrap.StartupProfiler?.GetTicks() ?? 0;
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == ActualThemeVariantProperty)
		{
			ActualThemeVariantChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		_propertyChangedHandler ??= ApplicationPropertyChangedAccessors.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Stop Keystone (subclass) then infrastructure.
	/// </summary>
	protected virtual void OnShutdown()
	{
		DetachLifetimeHandlers();
		AppBootstrap.ShutdownInfrastructure();
	}

	/// <summary>
	/// Drops static event subscriptions that would otherwise keep this instance alive
	/// after a locator scope is disposed (per-test headless isolation).
	/// </summary>
	internal void DetachLifetimeHandlers()
	{
		AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
		TaskScheduler.UnobservedTaskException -= TaskSchedulerUnobservedTaskException;
		Dispatcher.UIThread.UnhandledException -= OnDispatcherOnUnhandledException;

		var platformSettings = PlatformSettings;
		if (platformSettings is not null)
		{
			platformSettings.ColorValuesChanged -= OnColorValuesChanged;
		}
	}

	/// <summary>
	/// Name the gap between Initialize() returning and OnFrameworkInitializationCompleted.
	/// Call at the start of every OnFrameworkInitializationCompleted override.
	/// </summary>
	protected void RecordAfterInitializeGap()
	{
		if (_afterInitializeTicks <= 0)
		{
			return;
		}

		AppBootstrap.StartupProfiler?.Record("Cornerstone.Presentation.AfterInitialize", _afterInitializeTicks);
		_afterInitializeTicks = 0;
	}

	/// <summary>
	/// Start app-owned lifecycles after the framework is ready.
	/// Keystone apps start Keystone first, then infrastructure.
	/// </summary>
	protected virtual void StartOwnedLifecycles()
	{
		AppBootstrap.StartInfrastructure();
	}

	protected virtual void TryConfigureSerializer()
	{
		CornerstonePresentationSerializerConfigurator.Configure();
	}

	internal void InitializeThemeVariant()
	{
		PlatformSettings?.ColorValuesChanged += OnColorValuesChanged;
		ThemeVariant.UpdateActualThemeVariant(this);
	}

	private static void ApplyCornerstoneRuntimeVersionOverride()
	{
		if (!AppBootstrap.IsInitialized || (_cornerstoneRuntimeVersion == null))
		{
			return;
		}

		AppBootstrap.RuntimeInformation.SetPlatformOverride(
			nameof(IRuntimeInformation.CornerstoneRuntimeVersion),
			_cornerstoneRuntimeVersion
		);
	}

	/// <summary>
	/// Complete profiling after the main window Loaded (dock restore, etc.).
	/// Completes immediately when there is no desktop main window.
	/// </summary>
	private void ArmFirstWindowProfiling()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
		{
			void OnOpened(object sender, EventArgs e)
			{
				window?.Opened -= OnOpened;
				AppBootstrap.StartupProfiler?.Mark("FirstFrame");
				Dispatcher.UIThread.Post(CompleteStartupProfiling, DispatcherPriority.Loaded);
			}

			window.Opened += OnOpened;
			return;
		}

		CompleteStartupProfiling();
	}

	private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		AppBootstrap.LogException(e.ExceptionObject as Exception);
	}

	/// <summary>
	/// Design-time / tests may construct Cornerstone without host Main.
	/// </summary>
	private static void EnsureAppBootstrapForCornerstone()
	{
		AppBootstrap.EnsureInitialized(
			"Cornerstone",
			typeof(Application).Assembly,
			dispatcher: Dispatcher.CurrentDispatcher
		);
	}

	void IResourceHost.NotifyHostedResourcesChanged(ResourcesChangedEventArgs e)
	{
		ResourcesChanged?.Invoke(this, e);
	}

	private void OnColorValuesChanged(object? sender, PlatformColorValues e)
	{
		ThemeVariant.UpdateActualThemeVariant(this);
	}

	private void OnDispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		AppBootstrap.LogException(e.Exception);
	}

	void IStyleHost.StylesAdded(IReadOnlyList<IStyle> styles)
	{
		_stylesAdded?.Invoke(styles);
	}

	void IStyleHost.StylesRemoved(IReadOnlyList<IStyle> styles)
	{
		_stylesRemoved?.Invoke(styles);
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

	private void TryAddThemeViewLocator()
	{
		DataTemplates.Add(new ViewLocator());
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler? ActualThemeVariantChanged;

	/// <inheritdoc />
	public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

	event Action<IReadOnlyList<IStyle>>? IGlobalStyles.GlobalStylesAdded
	{
		add => _stylesAdded += value;
		remove => _stylesAdded -= value;
	}

	event Action<IReadOnlyList<IStyle>>? IGlobalStyles.GlobalStylesRemoved
	{
		add => _stylesRemoved += value;
		remove => _stylesRemoved -= value;
	}

	#endregion
}

internal static class ApplicationPropertyChangedAccessors
{
	#region Methods

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_inpcChanged")]
	public static extern ref PropertyChangedEventHandler GetPropertyChangedHandler(PresentationObject instance);

	#endregion
}