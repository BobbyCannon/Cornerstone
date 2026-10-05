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

#endregion

namespace Cornerstone.Sample.Tabs.Inputs;

[SourceReflection]
public partial class TabInputsGamepad : SampleUserControl
{
	#region Constants

	public const string HeaderName = "Gamepad";

	#endregion

	#region Constructors

	public TabInputsGamepad()
		: this(AppBootstrap.GetInstance<GamepadsManager>(), AppBootstrap.GetInstance<IDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabInputsGamepad(GamepadsManager gamepadsManager, IDispatcher dispatcher)
	{
		GamepadsManager = gamepadsManager;
		GamepadHistory = new PresentationList<GamepadState>(dispatcher, [new OrderBy<GamepadState>(x => x.DateTime, true)])
		{
			Limit = 100
		};
		ClearHistoryCommand = new RelayCommand(_ => GamepadHistory.Clear());
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ICommand ClearHistoryCommand { get; }

	public PresentationList<GamepadState> GamepadHistory { get; }

	public GamepadsManager GamepadsManager { get; }

	[Notify]
	public partial bool MonitorGamepads { get; set; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			foreach (var gamepad in GamepadsManager.Gamepads)
			{
				gamepad.Changed += GamepadOnChanged;
			}

			MonitorGamepads = true;
		}
		else
		{
			for (var i = 0; i < GamepadsManager.Gamepads.Length; i++)
			{
				GamepadsManager.Gamepads[i].State.Index = i;
				GamepadsManager.Gamepads[i].State.IsConnected = i == 0;
			}
		}

		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			foreach (var gamepad in GamepadsManager.Gamepads)
			{
				gamepad.Changed -= GamepadOnChanged;
			}

			MonitorGamepads = false;
			GamepadsManager.StopWorking();
		}

		base.OnDetachedFromVisualTree(e);
	}

	public override void OnPropertyChanged(string propertyName)
	{
		if (propertyName == nameof(MonitorGamepads))
		{
			if (MonitorGamepads && !GamepadsManager.IsWorking)
			{
				GamepadsManager.StartWorking();
			}
			if (!MonitorGamepads && GamepadsManager.IsWorking)
			{
				GamepadsManager.StopWorking();
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	private void GamepadOnChanged(object sender, GamepadState e)
	{
		this.Dispatch(() => GamepadHistory.Add(e));
	}

	#endregion
}