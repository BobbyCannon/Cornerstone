#region References

using System.Windows.Input;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Input;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Keyboard = Cornerstone.Input.Keyboard;
using Mouse = Cornerstone.Input.Mouse;

#endregion

namespace Cornerstone.Sample.Tabs.Inputs;

[SourceReflection]
public partial class TabInputsKeyboardMouse : SampleUserControl
{
	#region Constants

	public const string HeaderName = "Keyboard and Mouse";

	#endregion

	#region Fields

	private readonly Keyboard _keyboard;
	private readonly Mouse _mouse;

	#endregion

	#region Constructors

	public TabInputsKeyboardMouse()
		: this(
			AppBootstrap.GetInstance<Keyboard>(),
			AppBootstrap.GetInstance<Mouse>(),
			AppBootstrap.GetInstance<IDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabInputsKeyboardMouse(Keyboard keyboard, Mouse mouse, IDispatcher dispatcher)
	{
		_keyboard = keyboard;
		_mouse = mouse;
		KeyboardHistory = new PresentationList<KeyboardStateArg>(dispatcher, [new OrderBy<KeyboardStateArg>(x => x.DateTime, true)])
		{
			Limit = 100
		};
		MouseHistory = new PresentationList<MouseState>(dispatcher, [new OrderBy<MouseState>(x => x.DateTime, true)])
		{
			Limit = 100
		};
		ClearHistoryCommand = new RelayCommand(_ =>
		{
			KeyboardHistory.Clear();
			MouseHistory.Clear();
		});
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ICommand ClearHistoryCommand { get; }

	public PresentationList<KeyboardStateArg> KeyboardHistory { get; }

	[Notify]
	public partial bool MonitorKeyboard { get; set; }

	[Notify]
	public partial bool MonitorMouse { get; set; }

	[Notify]
	public partial bool MonitorMouseMove { get; set; }

	public PresentationList<MouseState> MouseHistory { get; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			WeakEventManager.Add<Keyboard, TabInputsKeyboardMouse, KeyboardStateArg>(
				_keyboard, nameof(Keyboard.KeyChanged), this, KeyboardOnKeyChanged);
			WeakEventManager.Add<Mouse, TabInputsKeyboardMouse, MouseState>(
				_mouse, nameof(Mouse.MouseChanged), this, MouseOnMouseChanged);
		}

		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			MonitorKeyboard = false;
			MonitorMouse = false;
			_keyboard.StopMonitoring();
			_mouse.StopMonitoring();
		}

		base.OnDetachedFromVisualTree(e);
	}

	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(MonitorKeyboard):
			{
				if (MonitorKeyboard && !_keyboard.IsMonitoring)
				{
					_keyboard.StartMonitoring();
				}
				if (!MonitorKeyboard && _keyboard.IsMonitoring)
				{
					_keyboard.StopMonitoring();
				}
				break;
			}
			case nameof(MonitorMouse):
			{
				if (MonitorMouse && !_mouse.IsMonitoring)
				{
					_mouse.StartMonitoring();
				}
				if (!MonitorMouse && _mouse.IsMonitoring)
				{
					_mouse.StopMonitoring();
				}
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	private void KeyboardOnKeyChanged(object sender, KeyboardStateArg e)
	{
		this.Dispatch(() => KeyboardHistory.Add(e));
	}

	private void MouseOnMouseChanged(object sender, MouseState e)
	{
		if ((e.Event == MouseEvent.MouseMove) && !MonitorMouseMove)
		{
			return;
		}

		this.Dispatch(() => MouseHistory.Add(e));
	}

	#endregion
}
