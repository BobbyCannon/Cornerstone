#region References

using Avalonia.Input;

#endregion

namespace Avalonia.Diagnostics;

public class HotKeyConfiguration
{
	#region Constructors

	public HotKeyConfiguration()
	{
		InspectHoveredControl = new(Key.None, KeyModifiers.Shift | KeyModifiers.Control);
		ScreenshotSelectedControl = new(Key.F8);
		TogglePopupFreeze = new(Key.F, KeyModifiers.Alt | KeyModifiers.Control);
		ValueFramesFreeze = new(Key.S, KeyModifiers.Alt);
		ValueFramesUnfreeze = new(Key.D, KeyModifiers.Alt);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Inspects the hovered Control in the Logical or Visual Tree Page
	/// </summary>
	public KeyGesture InspectHoveredControl { get; init; }

	/// <summary>
	/// Saves a Screenshot of the Selected Control in the Logical or Visual Tree Page
	/// </summary>
	public KeyGesture ScreenshotSelectedControl { get; init; }

	/// <summary>
	/// Toggles the freezing of Popups which prevents visible Popups from closing so they can be inspected
	/// </summary>
	public KeyGesture TogglePopupFreeze { get; init; }

	/// <summary>
	/// Freezes refreshing the Value Frames inspector for the selected Control
	/// </summary>
	public KeyGesture ValueFramesFreeze { get; init; }

	/// <summary>
	/// Resumes refreshing the Value Frames inspector for the selected Control
	/// </summary>
	public KeyGesture ValueFramesUnfreeze { get; init; }

	#endregion
}