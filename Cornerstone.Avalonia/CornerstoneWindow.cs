#region References

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Dispatcher = Avalonia.Threading.Dispatcher;

#endregion

namespace Cornerstone.Avalonia;

public partial class CornerstoneWindow<T> : CornerstoneWindow
{
	#region Constructors

	public CornerstoneWindow() : this(default)
	{
	}

	public CornerstoneWindow(T viewModel)
	{
		ViewModel = viewModel;
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial T ViewModel { get; set; }

	#endregion

	#region Methods

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ViewModelProperty)
		{
			DataContext = ViewModel;
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}

public partial class CornerstoneWindow : Window
{
	#region Fields

	public new static readonly StyledProperty<Bitmap> IconProperty;

	private Typeface? _cachedTypeface;
	private Button _closeButton;
	private Button _maximizeButton;
	private Path _maximizeIcon;
	private Button _minimizeButton;
	private PropertyChangedEventHandler _propertyChangedHandler;
	private Grid _windowGrid;
	private Image _windowIcon;

	#endregion

	#region Constructors

	public CornerstoneWindow()
	{
		MainMenu = new SpeedyList<MenuItemView>();
	}

	static CornerstoneWindow()
	{
		IconProperty = AvaloniaProperty.Register<CornerstoneWindow, Bitmap>(nameof(Icon));
	}

	#endregion

	#region Properties

	public ICommand ExitApplicationCommand { get; private set; }

	public new Bitmap Icon
	{
		get => GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	[StyledProperty]
	public partial object InnerRightContent { get; set; }

	[StyledProperty]
	public partial SpeedyList<MenuItemView> MainMenu { get; set; }

	[StyledProperty]
	public partial Profiler Profiler { get; set; }

	public Typeface Typeface => _cachedTypeface ??= CornerstoneExtensions.CreateTypeface(this);

	protected override Type StyleKeyOverride => typeof(CornerstoneWindow);

	#endregion

	#region Methods

	public static T GetInstance<T>()
	{
		return CornerstoneApplication.DependencyProvider.GetInstance<T>();
	}

	public static object GetInstance(Type type)
	{
		return CornerstoneApplication.DependencyProvider.GetInstance(type);
	}

	public bool IsOnScreen()
	{
		var windowRect = new PixelRect(Position.X, Position.Y, (int) Width, (int) Height);
		return Screens.All.Any(s => s.WorkingArea.Intersects(windowRect));
	}

	public void RestoreWindowLocation(WindowLocation location)
	{
		if (location == null)
		{
			return;
		}

		if (location.Height > int.MinValue)
		{
			Height = location.Height;
		}

		if (location.Width > int.MinValue)
		{
			Width = location.Width;
		}

		if ((location.Top == -1) && (location.Left == -1))
		{
			CenterOnScreen();
		}
		else
		{
			Position = new PixelPoint(location.Left, location.Top);
		}

		if (!IsOnScreen())
		{
			CenterOnScreen();
		}

		if (location.Maximized)
		{
			Dispatcher.UIThread.Post(() => WindowState = WindowState.Maximized);
		}
	}

	protected virtual void BindCommands()
	{
		ExitApplicationCommand = new RelayCommand(ExitApplicationOnExecute, ExitApplicationCanExecute);
	}

	protected virtual void BuildMenu()
	{
	}

	protected void CenterOnScreen()
	{
		var screen = Screens.Primary;
		if (screen == null)
		{
			return;
		}

		var left = (int) (((double) screen.WorkingArea.Width / 2) - (Math.Max(Bounds.Width, Width) / 2));
		var top = (int) (((double) screen.WorkingArea.Height / 2) - (Math.Max(Bounds.Height, Height) / 2));
		Position = new PixelPoint(left, top);
	}

	protected virtual bool ExitApplicationCanExecute(object args)
	{
		return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;
	}

	protected virtual void ExitApplicationOnExecute(object e)
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
		{
			lifetime.Shutdown();
		}
	}

	protected WindowLocation GetWindowLocation()
	{
		var maximized = WindowState == WindowState.Maximized;
		var currentLocation = new WindowLocation
		{
			Top = Position.Y,
			Left = Position.X,
			Height = (int) Height,
			Width = (int) Width,
			Maximized = maximized
		};

		return currentLocation;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_minimizeButton = e.NameScope.Find<Button>("MinimizeButton")!;
		_maximizeButton = e.NameScope.Find<Button>("MaximizeButton")!;
		_maximizeIcon = e.NameScope.Find<Path>("MaximizeIcon")!;
		_closeButton = e.NameScope.Find<Button>("CloseButton")!;
		_windowIcon = e.NameScope.Find<Image>("WindowIcon")!;
		_windowGrid = e.NameScope.Find<Grid>("WindowGrid")!;

		_minimizeButton.Click += MinimizeWindow;
		_maximizeButton.Click += MaximizeWindow;
		_closeButton.Click += CloseWindow;
		_windowIcon.DoubleTapped += CloseWindow;

		SubscribeToWindowState();
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		PositionChanged -= OnPositionChanged;
		base.OnClosing(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		PositionChanged += OnPositionChanged;

		BindCommands();
		BuildMenu();
		base.OnLoaded(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == FontFamilyProperty)
			|| (change.Property == FontSizeProperty)
			|| (change.Property == FontStretchProperty)
			|| (change.Property == ForegroundProperty))
		{
			_cachedTypeface = null;
			InvalidateVisual();
		}
	}

	protected virtual void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void CloseWindow(object sender, RoutedEventArgs e)
	{
		var hostWindow = (Window) VisualRoot;
		if (hostWindow != null)
		{
			hostWindow.Close();
		}
	}

	private void MaximizeWindow(object sender, RoutedEventArgs e)
	{
		var hostWindow = (Window) VisualRoot;

		if (hostWindow != null)
		{
			hostWindow.WindowState = hostWindow.WindowState == WindowState.Normal
				? WindowState.Maximized
				: WindowState.Normal;
		}
	}

	private void MinimizeWindow(object sender, RoutedEventArgs e)
	{
		var hostWindow = (Window) VisualRoot;
		if (hostWindow != null)
		{
			hostWindow.WindowState = WindowState.Minimized;
		}
	}

	private void OnPositionChanged(object sender, PixelPointEventArgs e)
	{
		OnPropertyChanged(nameof(Position));
	}

	private async void SubscribeToWindowState()
	{
		var hostWindow = (Window) VisualRoot;

		while (hostWindow == null)
		{
			hostWindow = (Window) VisualRoot;
			await Task.Delay(50);
		}

		hostWindow
			.GetObservable(WindowStateProperty)
			.Subscribe(s =>
			{
				if (s != WindowState.Maximized)
				{
					_maximizeIcon.Data = Geometry.Parse("M2048 2048v-2048h-2048v2048h2048zM1843 1843h-1638v-1638h1638v1638z");
					_windowGrid.Margin = new Thickness(0, 0, 0, 0);
				}
				if (s == WindowState.Maximized)
				{
					_maximizeIcon.Data = Geometry.Parse("M2048 1638h-410v410h-1638v-1638h410v-410h1638v1638zm-614-1024h-1229v1229h1229v-1229zm409-409h-1229v205h1024v1024h205v-1229z");
					// todo: remove this for Avalonia v12
					_windowGrid.Margin = new Thickness(8, 8, 8, 0);
				}
			});
	}

	#endregion
}