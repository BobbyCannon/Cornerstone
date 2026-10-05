#region References

using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;

#endregion

namespace Cornerstone.Presentation.Diagnostics;

/// <summary>
/// Describes options used to customize DiagnosticTools.
/// </summary>
public class DiagnosticToolsOptions
{
	public DiagnosticToolsOptions()
	{
		Gesture = new(Key.F12);
		HotKeys = new();
		ShowAsChildWindow = true;
		ShowImplementedInterfaces = true;
		Size = new(1280, 720);
	}

	#region Properties

	/// <summary>
	/// Get or set Focus Highlighter <see cref="Brush" />
	/// </summary>
	public IBrush FocusHighlighterBrush { get; set; }

	/// <summary>
	/// Gets or sets the key gesture used to open DiagnosticTools.
	/// </summary>
	public KeyGesture Gesture { get; set; }

	/// <summary>
	/// Gets or inits the <see cref="HotKeyConfiguration" /> used to activate DiagnosticTools features
	/// </summary>
	public HotKeyConfiguration HotKeys { get; init; }

	/// <summary>
	/// Set the <see cref="DiagnosticToolsViewKind"> kind </see> of diagnostic view that show at launch of DiagnosticTools
	/// </summary>
	public DiagnosticToolsViewKind LaunchView { get; init; }

	/// <summary>
	/// Gets or sets a value indicating whether DiagnosticTools should be displayed as a child window
	/// of the window being inspected. The default value is true.
	/// </summary>
	/// <remarks> This setting is ignored if DiagnosticTools is attached to <see cref="Application" /> </remarks>
	public bool ShowAsChildWindow { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether DiagnosticTools should be displayed implemented interfaces on Control details. The default value is true.
	/// </summary>
	public bool ShowImplementedInterfaces { get; set; }

	/// <summary>
	/// Gets or sets the initial size of the DiagnosticTools window. The default value is 1280x720.
	/// </summary>
	public Size Size { get; set; }

	/// <summary>
	/// Get or set the startup screen index where the DiagnosticTools window will be displayed.
	/// </summary>
	public int? StartupScreenIndex { get; set; }

	/// <summary>
	/// Gets or sets whether DiagnosticTools theme.
	/// </summary>
	public ThemeVariant ThemeVariant { get; set; }

	#endregion
}