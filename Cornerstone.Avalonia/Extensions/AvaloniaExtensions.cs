#region References

using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.VisualTree;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Extensions;

public static class AvaloniaExtensions
{
	#region Methods

	public static KeyBinding CreateKeyBinding(this ICommand command, KeyModifiers modifiers, Key key)
	{
		return command.CreateKeyBinding(new KeyGesture(key, modifiers));
	}

	public static KeyBinding CreateKeyBinding(this ICommand command, KeyGesture gesture)
	{
		return new KeyBinding { Command = command, Gesture = gesture };
	}

	public static T FindParent<T>(this StyledElement element, bool includeSelf = false)
		where T : class
	{
		if (element is null)
		{
			return null;
		}

		var parent = includeSelf ? element : element.Parent;

		while (parent != null)
		{
			if (parent is T result)
			{
				return result;
			}

			parent = parent.Parent;
		}

		return null;
	}

	public static TopLevel GetTopLevel(this Application app)
	{
		switch (app?.ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				return desktop.MainWindow;
			}
			case ISingleViewApplicationLifetime viewApp:
			{
				var visualRoot = viewApp.MainView?.GetVisualRoot();
				return visualRoot as TopLevel;
			}
			default:
			{
				return null;
			}
		}
	}

	internal static PropertyChangedEventHandler GetPropertyChangedHandler(this AvaloniaObject value)
	{
		var t = typeof(AvaloniaObject).GetCachedField("_inpcChanged");
		var v = t.GetValue(value);
		var h = v as PropertyChangedEventHandler;
		return h;
	}

	#endregion
}