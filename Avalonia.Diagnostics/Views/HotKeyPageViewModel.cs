#region References

using System.Collections.ObjectModel;
using Avalonia.Input;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.Views;

public record HotKeyDescription(string Gesture, string BriefDescription, string DetailedDescription = null);

public class HotKeyPageViewModel : ViewModel
{
	#region Fields

	private ObservableCollection<HotKeyDescription> _hotKeyDescriptions;

	#endregion

	#region Properties

	public ObservableCollection<HotKeyDescription> HotKeyDescriptions
	{
		get => _hotKeyDescriptions;
		private set => SetProperty(ref _hotKeyDescriptions, value);
	}

	#endregion

	#region Methods

	public void SetOptions(DevToolsOptions options)
	{
		var hotKeys = options.HotKeys;

		HotKeyDescriptions =
		[
			new(CreateDescription(options.Gesture), "Launch DevTools", "Launches DevTools to inspect the TopLevel that received the hotkey input"),
			new(CreateDescription(hotKeys.ValueFramesFreeze), "Freeze Value Frames", "Pauses refreshing the Value Frames inspector for the selected Control"),
			new(CreateDescription(hotKeys.ValueFramesUnfreeze), "Unfreeze Value Frames", "Resumes refreshing the Value Frames inspector for the selected Control"),
			new(CreateDescription(hotKeys.InspectHoveredControl), "Inspect Control Under Pointer", "Inspects the hovered Control in the Logical or Visual Tree Page"),
			new(CreateDescription(hotKeys.TogglePopupFreeze), "Toggle Popup Freeze", "Prevents visible Popups from closing so they can be inspected"),
			new(CreateDescription(hotKeys.ScreenshotSelectedControl), "Screenshot Selected Control", "Saves a Screenshot of the Selected Control in the Logical or Visual Tree Page")
		];
	}

	private string CreateDescription(KeyGesture gesture)
	{
		if (gesture.Key == Key.None && gesture.KeyModifiers != KeyModifiers.None)
		{
			return gesture.ToString().Replace("+None", "");
		}
		return gesture.ToString();
	}

	#endregion
}