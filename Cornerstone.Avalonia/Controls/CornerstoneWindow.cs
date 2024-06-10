#region References

using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.Input;
using Cornerstone.Windows;
using PropertyChanged;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class CornerstoneWindow : Window
{
	#region Fields

	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Constructors

	public CornerstoneWindow()
	{
		CommandHandler = new RoutedCommandHandler();
	}

	static CornerstoneWindow()
	{
		ExitApplication = new RoutedCommand("Exit Application");
	}

	#endregion

	#region Properties

	public RoutedCommandHandler CommandHandler { get; }

	public static RoutedCommand ExitApplication { get; }

	#endregion

	#region Methods

	public virtual void BindCommands()
	{
		CommandHandler.AddBinding(ExitApplication, KeyModifiers.Alt, Key.Y, OnExitApplication, CanExitApplication);
		CommandHandler.Attach(this);
	}

	public void CenterOnScreen()
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

	public static T GetService<T>()
	{
		return CornerstoneApplication.GetService<T>();
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

	protected virtual void CanExitApplication(object sender, CanExecuteRoutedEventArgs args)
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
		{
			args.CanExecute = true;
			args.Handled = true;
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

	protected virtual void OnExitApplication(object sender, ExecutedRoutedEventArgs e)
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
		{
			lifetime.Shutdown();
		}
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		PositionChanged += OnPositionChanged;

		BindCommands();
		base.OnLoaded(e);
	}

	[SuppressPropertyChangedWarnings]
	private void OnPositionChanged(object sender, PixelPointEventArgs e)
	{
		OnPropertyChanged(nameof(Position));
	}

	#endregion
}