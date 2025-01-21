#region References

using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Input;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sample.ViewModels;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabInputs : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Inputs";

	#endregion

	#region Fields

	private readonly Keyboard _keyboard;
	private readonly WeakEventManager _manager;
	private readonly Mouse _mouse;
	private readonly ApplicationSettings _settings;

	#endregion

	#region Constructors

	public TabInputs() : this(GetInstance<WeakEventManager>(), GetInstance<IDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabInputs(WeakEventManager manager, IDispatcher dispatcher) : base(dispatcher)
	{
		_manager = manager;

		_keyboard = GetInstance<Keyboard>();
		_mouse = GetInstance<Mouse>();
		_settings = GetInstance<ApplicationSettings>();

		Gamepad = GetInstance<Gamepad>();
		MonitorGamepads = _settings.UseGamepadForInput;

		GamepadHistory = new SpeedyList<GamepadState>(dispatcher, new OrderBy<GamepadState>(x => x.DateTime, true)) { Limit = 100 };
		KeyboardHistory = new SpeedyList<KeyboardState>(dispatcher, new OrderBy<KeyboardState>(x => x.DateTime, true)) { Limit = 100 };
		MouseHistory = new SpeedyList<MouseState>(dispatcher, new OrderBy<MouseState>(x => x.DateTime, true)) { Limit = 100 };

		ClearHistoryCommand = new RelayCommand(ClearHistory);
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public ICommand ClearHistoryCommand { get; }

	public Gamepad Gamepad { get; }

	public ISpeedyList<GamepadState> GamepadHistory { get; set; }

	public ISpeedyList<KeyboardState> KeyboardHistory { get; set; }

	public bool MonitorGamepads
	{
		get => _settings.UseGamepadForInput;
		set => _settings.UseGamepadForInput = value;
	}

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
			case nameof(MonitorGamepads):
			{
				// This is handled in App.axaml.cs
				_settings.UseGamepadForInput = MonitorGamepads;
				break;
			}
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
		if (Design.IsDesignMode)
		{
			// Ensure this is disabled in design mode.
			_settings.UseGamepadForInput = false;
		}
		else
		{
			if (!_keyboard.IsMonitoring && MonitorKeyboard)
			{
				_keyboard.StartMonitoring();
			}

			if (!_mouse.IsMonitoring && MonitorMouse)
			{
				_mouse.StartMonitoring();
			}
		}

		_manager.AddPropertyChanged(_settings, SettingsOnPropertyChanged);
		_manager.Add<Gamepad, GamepadState>(Gamepad, nameof(Input.Gamepad.Changed), GamepadOnChanged);
		_manager.Add<Keyboard, KeyboardStateArg>(_keyboard, nameof(Keyboard.KeyChanged), KeyboardOnKeyChanged);
		_manager.Add<Mouse, MouseState>(_mouse, nameof(Mouse.MouseChanged), MouseOnMouseChanged);

		if (Design.IsDesignMode && (KeyboardHistory.Count <= 0))
		{
			Gamepad.State.UpdateWith(new GamepadState
			{
				Buttons = GamepadButton.A,
				DateTime = DateTimeProvider.RealTime.UtcNow,
				LeftTriggerValue = 13,
				IsConnected = true,
				RightTriggerValue = 123,
				LeftThumbX = 10000,
				LeftThumbY = 8000,
				RightThumbX = -8000,
				RightThumbY = -8000
			});
			GamepadHistory.Add(Gamepad.State);

			KeyboardHistory.Add(new KeyboardState
			{
				DateTime = DateTimeProvider.RealTime.UtcNow,
				Event = KeyboardEvent.KeyUp,
				Key = KeyboardKey.A,
				Character = 'a',
				IsAltPressed = true,
				IsLeftAltPressed = true
			});

			MouseHistory.Add(new MouseState
			{
				DateTime = DateTimeProvider.RealTime.UtcNow,
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
		// Do NOT change monitoring, this is used globally
		//Gamepad.StopWorking();
		_keyboard.StopMonitoring();
		_mouse.StopMonitoring();
		base.OnUnloaded(e);
	}

	private void ClearHistory(object obj)
	{
		GamepadHistory.Clear();
		KeyboardHistory.Clear();
		MouseHistory.Clear();
	}

	private void GamepadOnChanged(object sender, GamepadState e)
	{
		GamepadHistory.Add(e);
	}

	private void KeyboardOnKeyChanged(object sender, KeyboardStateArg e)
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

	private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ApplicationSettings.UseGamepadForInput):
			{
				OnPropertyChanged(nameof(MonitorGamepads));
				break;
			}
		}
	}

	#endregion
}