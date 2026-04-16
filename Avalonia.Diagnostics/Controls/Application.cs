#region References

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Rendering;
using Avalonia.Styling;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;

#endregion

namespace Avalonia.Diagnostics.Controls;

public class Application : TopLevelGroup
	, ICloseable, IDisposable

{
	#region Fields

	public static readonly StyledProperty<ThemeVariant> RequestedThemeVariantProperty =
		ThemeVariantScope.RequestedThemeVariantProperty.AddOwner<Application>();

	#endregion

	#region Constructors

	public Application(ClassicDesktopStyleApplicationLifetimeTopLevelGroup group, Avalonia.Application application)
		: base(group)
	{
		Instance = application;

		if (Instance.ApplicationLifetime is Lifetimes.IControlledApplicationLifetime controller)
		{
			EventHandler<Lifetimes.ControlledApplicationLifetimeExitEventArgs> eh = null!;
			eh = (s, e) =>
			{
				controller.Exit -= eh;
				Closed?.Invoke(s, e);
			};
			controller.Exit += eh;
		}
		RendererRoot = application.ApplicationLifetime switch
		{
			Lifetimes.IClassicDesktopStyleApplicationLifetime classic => classic.MainWindow?.Renderer,
			//Lifetimes.ISingleViewApplicationLifetime single => single.MainView?.VisualRoot?.Renderer,
			_ => null
		};

		SetCurrentValue(RequestedThemeVariantProperty, application.RequestedThemeVariant);
		Instance.PropertyChanged += ApplicationOnPropertyChanged;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Application lifetime, use it for things like setting the main window and exiting the app from code
	/// Currently supported lifetimes are:
	/// - <see cref="Lifetimes.IClassicDesktopStyleApplicationLifetime" />
	/// - <see cref="Lifetimes.ISingleViewApplicationLifetime" />
	/// - <see cref="Lifetimes.IControlledApplicationLifetime" />
	/// </summary>
	public Lifetimes.IApplicationLifetime ApplicationLifetime => Instance.ApplicationLifetime;

	/// <summary>
	/// Defines the <see cref="DataContext" /> property.
	/// </summary>
	public object DataContext => Instance.DataContext;

	/// <summary>
	/// Gets or sets the application's global data templates.
	/// </summary>
	/// <value>
	/// The application's global data templates.
	/// </value>
	public DataTemplates DataTemplates => Instance.DataTemplates;

	public Avalonia.Application Instance { get; }

	/// <summary>
	/// Application name to be used for various platform-specific purposes
	/// </summary>
	public string Name => Instance.Name;

	/// <summary>
	/// Gets the root of the visual tree, if the control is attached to a visual tree.
	/// </summary>
	internal IRenderer RendererRoot { get; }

	/// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariant" />
	public ThemeVariant RequestedThemeVariant
	{
		get => GetValue(RequestedThemeVariantProperty);
		set => SetValue(RequestedThemeVariantProperty, value);
	}

	/// <summary>
	/// Gets the application's global resource dictionary.
	/// </summary>
	public IResourceDictionary Resources => Instance.Resources;

	/// <summary>
	/// Gets the application's global styles.
	/// </summary>
	/// <value>
	/// The application's global styles.
	/// </value>
	/// <remarks>
	/// Global styles apply to all windows in the application.
	/// </remarks>
	public Styles Styles => Instance.Styles;

	/// <summary>
	/// Gets the application's input manager.
	/// </summary>
	/// <value>
	/// The application's input manager.
	/// </value>
	internal InputManager InputManager => Instance.InputManager;

	#endregion

	#region Methods

	public void Dispose()
	{
		Instance.PropertyChanged -= ApplicationOnPropertyChanged;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == RequestedThemeVariantProperty)
		{
			Instance.RequestedThemeVariant = change.GetNewValue<ThemeVariant>();
		}
	}

	private void ApplicationOnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == Avalonia.Application.RequestedThemeVariantProperty)
		{
			SetCurrentValue(RequestedThemeVariantProperty, e.GetNewValue<ThemeVariant>());
		}
	}

	#endregion

	#region Events

	public event EventHandler Closed;

	#endregion
}