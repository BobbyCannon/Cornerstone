#region References

using System;
using System.Collections.Generic;
using Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.Input;

/// <summary>
/// Provides platform specific formatting information for the KeyGesture class
/// </summary>
public sealed class KeyGestureFormatter : IFormatProvider
{
	#region Fields

	/// <summary>
	/// A dictionary of the common platform Key overrides. These are used as a fallback
	/// if platformKeyOverrides doesn't contain the Key in question.
	/// </summary>
	private static readonly Dictionary<Key, string> _keyOverrides = new()
	{
		{ Key.Add, "+" },
		{ Key.D0, "0" },
		{ Key.D1, "1" },
		{ Key.D2, "2" },
		{ Key.D3, "3" },
		{ Key.D4, "4" },
		{ Key.D5, "5" },
		{ Key.D6, "6" },
		{ Key.D7, "7" },
		{ Key.D8, "8" },
		{ Key.D9, "9" },
		{ Key.Decimal, "." },
		{ Key.Divide, "/" },
		{ Key.Multiply, "*" },
		{ Key.OemBackslash, "\\" },
		{ Key.OemCloseBrackets, "]" },
		{ Key.OemComma, "," },
		{ Key.OemMinus, "-" },
		{ Key.OemOpenBrackets, "[" },
		{ Key.OemPeriod, "." },
		{ Key.OemPipe, "|" },
		{ Key.OemPlus, "+" },
		{ Key.OemQuestion, "/" },
		{ Key.OemQuotes, "\"" },
		{ Key.OemSemicolon, ";" },
		{ Key.OemTilde, "`" },
		{ Key.Separator, "/" },
		{ Key.Subtract, "-" },
		{ Key.Back, "Backspace" },
		{ Key.Down, "Down Arrow" },
		{ Key.Left, "Left Arrow" },
		{ Key.Right, "Right Arrow" },
		{ Key.Up, "Up Arrow" },
		{ Key.Return, "Enter" }
	};

	#endregion

	#region Constructors

	/// <summary>
	/// Provides platform specific formatting information for the KeyGesture class
	/// </summary>
	/// <param name="meta"> The string to use for the Meta modifier, defaults to "Cmd" </param>
	/// <param name="ctrl"> The string to use for the Ctrl modifier, defaults to "Ctrl" </param>
	/// <param name="alt"> The string to use for the Alt modifier, defaults to "Alt" </param>
	/// <param name="shift"> The string to use for the Shift modifier, defaults to "Shift" </param>
	public KeyGestureFormatter(
		string meta = "Cmd",
		string ctrl = "Ctrl",
		string alt = "Alt",
		string shift = "Shift")
	{
		Meta = meta;
		Ctrl = ctrl;
		Alt = alt;
		Shift = shift;
	}

	static KeyGestureFormatter()
	{
		Instance = new();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The string used to represent Alt on the appropriate platform.  Defaults to "Alt".
	/// </summary>
	public string Alt { get; }

	/// <summary>
	/// The string used to represent Ctrl on the appropriate platform.  Defaults to "Ctrl".
	/// </summary>
	public string Ctrl { get; }

	/// <summary>
	/// The global instance. Only uses strings straight from the appropriate Enums.
	/// </summary>
	public static KeyGestureFormatter Instance { get; }

	/// <summary>
	/// The string used to represent Meta on the appropriate platform.  Defaults to "Cmd".
	/// </summary>
	public string Meta { get; }

	/// <summary>
	/// The string used to represent Shift on the appropriate platform.  Defaults to "Shift".
	/// </summary>
	public string Shift { get; }

	#endregion

	#region Methods

	public string FormatKey(Key key)
	{
		return _keyOverrides.TryGetValue(key, out var result2) ? result2 : key.ToString();
	}

	public object GetFormat(Type formatType)
	{
		return formatType == typeof(KeyGestureFormatter) ? this : null;
	}

	#endregion
}