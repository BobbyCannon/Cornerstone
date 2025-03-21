#region References

using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Cornerstone.Avalonia.DockingManager;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;
using Dispatcher = Avalonia.Threading.Dispatcher;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneWindow<T> : CornerstoneWindow
{
	#region Fields

	public static readonly StyledProperty<T> ViewModelProperty = AvaloniaProperty.Register<CornerstoneWindow, T>(nameof(ViewModel));

	#endregion

	#region Constructors

	public CornerstoneWindow() : this(default, null)
	{
	}

	public CornerstoneWindow(T viewModel, IDispatcher dispatcher) : base(dispatcher)
	{
		ViewModel = viewModel;
	}

	#endregion

	#region Properties

	public T ViewModel
	{
		get => (T) DataContext;
		set => DataContext = value;
	}

	#endregion
}

public class CornerstoneWindow : Window, IDispatchable
{
	#region Fields

	private readonly IDispatcher _dispatcher;
	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Constructors

	protected CornerstoneWindow(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}

	#endregion

	#region Properties

	public ICommand ExitApplicationCommand { get; private set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public static T GetInstance<T>()
	{
		return CornerstoneApplication.GetInstance<T>();
	}

	public bool IsOnScreen()
	{
		var windowRect = new PixelRect(Position.X, Position.Y, (int) Width, (int) Height);
		return Screens.All.Any(s => s.WorkingArea.Intersects(windowRect));
	}

	public void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void RestoreWindowLocation(WindowLocation location)
	{
		if (location == null)
		{
			return;
		}

		if ((location.Top == -1) && (location.Left == -1))
		{
			CenterOnScreen();
		}
		else
		{
			Position = new PixelPoint(location.Left, location.Top);
		}

		if (location.Height > int.MinValue)
		{
			Height = location.Height;
		}

		if (location.Width > int.MinValue)
		{
			Width = location.Width;
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

	protected void CenterOnScreen()
	{
		var screen = Screens.Primary;
		if (screen == null)
		{
			return;
		}

		var left = (int) (((double) screen.WorkingArea.Width / 2) - (Width / 2));
		var top = (int) (((double) screen.WorkingArea.Height / 2) - (Height / 2));
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

	/// <inheritdoc />
	protected override void OnClosing(WindowClosingEventArgs e)
	{
		PositionChanged -= OnPositionChanged;
		base.OnClosing(e);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		PositionChanged += OnPositionChanged;

		BindCommands();
		base.OnLoaded(e);
	}

	private void OnPositionChanged(object sender, PixelPointEventArgs e)
	{
		OnPropertyChanged(nameof(Position));
	}

	#endregion
}