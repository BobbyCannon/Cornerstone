#region References

using System.Linq;
using Cornerstone.Input;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Inputs;

[SourceReflection]
public partial class TabInputsShortcut : UserControl
{
	#region Constants

	public const string HeaderName = "Shortcut";

	#endregion

	#region Constructors

	public TabInputsShortcut()
		: this(AppBootstrap.GetInstance<GamepadsManager>())
	{
	}

	[DependencyInjectionConstructor]
	public TabInputsShortcut(GamepadsManager gamepadsManager)
	{
		Gamepad = gamepadsManager.Gamepad1;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public Gamepad Gamepad { get; }

	#endregion

	#region Methods

	private void ResetOnClick(object sender, RoutedEventArgs e)
	{
		foreach (var input in this.GetVisualDescendants().OfType<ShortcutBox>())
		{
			input.Reset();
		}
	}

	#endregion
}
