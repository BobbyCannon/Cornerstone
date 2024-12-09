#region References

using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Input;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabInputs : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Inputs";

	#endregion

	#region Fields

	private readonly Keyboard _keyboard;
	private readonly WeakEventManager _manager;
	private readonly Mouse _mouse;

	#endregion

	#region Constructors

	public TabInputs() : this(GetService<WeakEventManager>(), GetService<IDispatcher>())
	{
	}

	public TabInputs(WeakEventManager manager, IDispatcher dispatcher) : base(dispatcher)
	{
		_keyboard = GetService<Keyboard>();
		_mouse = GetService<Mouse>();
		_manager = manager;

		KeyboardHistory = new SpeedyList<KeyboardState>(dispatcher, new OrderBy<KeyboardState>(x => x.DateTime, true))
		{
			Limit = 100
		};

		MouseHistory = new SpeedyList<MouseState>(dispatcher, new OrderBy<MouseState>(x => x.DateTime, true))
		{
			Limit = 100
		};

		ClearHistoryCommand = new RelayCommand(ClearHistory);
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ICommand ClearHistoryCommand { get; }

	public ISpeedyList<KeyboardState> KeyboardHistory { get; set; }

	public bool MonitorKeyboard { get; set; }

	public bool MonitorMouse { get; set; }

	public bool MonitorMouseMove { get; set; }

	public ISpeedyList<MouseState> MouseHistory { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(MonitorKeyboard):
			{
				if (!_keyboard.IsMonitoring && MonitorKeyboard)
				{
					_keyboard.StartMonitoring();
				}
				if (_keyboard.IsMonitoring && !MonitorKeyboard)
				{
					_keyboard.StopMonitoring();
				}
				break;
			}
			case nameof(MonitorMouse):
			{
				if (!_mouse.IsMonitoring && MonitorMouse)
				{
					_mouse.StartMonitoring();
				}
				if (_mouse.IsMonitoring && !MonitorMouse)
				{
					_mouse.StopMonitoring();
				}
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (!_keyboard.IsMonitoring && MonitorKeyboard)
		{
			_keyboard.StartMonitoring();
		}

		if (!_mouse.IsMonitoring && MonitorMouse)
		{
			_mouse.StartMonitoring();
		}

		_manager.Add<Keyboard, KeyboardState>(_keyboard, nameof(Keyboard.KeyChanged), KeyboardOnKeyChanged);
		_manager.Add<Mouse, MouseState>(_mouse, nameof(Mouse.MouseChanged), MouseOnMouseChanged);

		if (Design.IsDesignMode && (KeyboardHistory.Count <= 0))
		{
			KeyboardHistory.Add(new KeyboardState
			{
				DateTime = DateTime.Now,
				Event = KeyboardEvent.KeyUp,
				Key = KeyboardKey.A,
				Character = 'a',
				IsAltPressed = true,
				IsLeftAltPressed = true
			});

			MouseHistory.Add(new MouseState
			{
				DateTime = DateTime.Now,
				Event = MouseEvent.MouseMove,
				LeftButton = true,
				WheelVerticalDelta = 130
			});
		}

		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		_keyboard.StopMonitoring();
		base.OnUnloaded(e);
	}

	private void ClearHistory(object obj)
	{
		KeyboardHistory.Clear();
		MouseHistory.Clear();
	}

	private void KeyboardOnKeyChanged(object sender, KeyboardState e)
	{
		KeyboardHistory.Add(e);
	}

	private void MouseOnMouseChanged(object sender, MouseState e)
	{
		if ((e.Event == MouseEvent.MouseMove)
			&& !MonitorMouseMove)
		{
			return;
		}

		MouseHistory.Add(e);
	}

	#endregion
}