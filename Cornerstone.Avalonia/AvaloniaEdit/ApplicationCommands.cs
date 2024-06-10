#region References

using System.Runtime.InteropServices;
using Avalonia.Input;
using Cornerstone.Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit;

public static class ApplicationCommands
{
	#region Fields

	private static readonly KeyModifiers _platformCommandKey;

	#endregion

	#region Constructors

	static ApplicationCommands()
	{
		_platformCommandKey = GetPlatformCommandKey();

		Copy = new(nameof(Copy), new KeyGesture(Key.C, _platformCommandKey));
		Cut = new(nameof(Cut), new KeyGesture(Key.X, _platformCommandKey));
		Delete = new(nameof(Delete), new KeyGesture(Key.Delete));
		Find = new(nameof(Find), new KeyGesture(Key.F, _platformCommandKey));
		Paste = new(nameof(Paste), new KeyGesture(Key.V, _platformCommandKey));
		Redo = new(nameof(Redo), new KeyGesture(Key.Y, _platformCommandKey));
		Replace = new(nameof(Replace), GetReplaceKeyGesture());
		SelectAll = new(nameof(SelectAll), new KeyGesture(Key.A, _platformCommandKey));
		Undo = new(nameof(Undo), new KeyGesture(Key.Z, _platformCommandKey));
	}

	#endregion

	#region Properties

	public static RoutedCommand Copy { get; }

	public static RoutedCommand Cut { get; }

	public static RoutedCommand Delete { get; }

	public static RoutedCommand Find { get; }

	public static RoutedCommand Paste { get; }

	public static RoutedCommand Redo { get; }

	public static RoutedCommand Replace { get; }

	public static RoutedCommand SelectAll { get; }

	public static RoutedCommand Undo { get; }

	#endregion

	#region Methods

	private static KeyModifiers GetPlatformCommandKey()
	{
		return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
			? KeyModifiers.Meta
			: KeyModifiers.Control;
	}

	private static KeyGesture GetReplaceKeyGesture()
	{
		return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
			? new KeyGesture(Key.F, KeyModifiers.Meta | KeyModifiers.Alt)
			: new KeyGesture(Key.H, _platformCommandKey);
	}

	#endregion
}