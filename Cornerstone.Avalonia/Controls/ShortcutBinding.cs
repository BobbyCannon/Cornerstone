#region References

using System;
using System.Text;
using Avalonia.Input;
using Cornerstone.Avalonia.Input;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Controls;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class ShortcutBinding : Notifiable<ShortcutBinding>
{
	#region Constructors

	public ShortcutBinding()
	{
		Reset();
	}

	#endregion

	#region Properties

	[AlsoNotify(nameof(HasBinding), nameof(HasGamepadBinding))]
	public partial GamepadButton GamepadButton { get; set; }

	[AlsoNotify(nameof(HasBinding), nameof(HasGamepadBinding))]
	public partial byte GamepadIndex { get; set; }

	public bool HasBinding => HasGamepadBinding || HasKeyBinding || HasKeyChord;

	public bool HasGamepadBinding => GamepadButton != GamepadButton.None;

	public bool HasKeyBinding => (Key != Key.None) || (KeyModifiers != KeyModifiers.None);

	public bool HasKeyChord => (SecondKey != Key.None) || (SecondKeyModifiers != KeyModifiers.None);

	[AlsoNotify(nameof(HasBinding), nameof(HasKeyBinding))]
	public partial Key Key { get; set; }

	[AlsoNotify(nameof(HasBinding), nameof(HasKeyBinding))]
	public partial KeyModifiers KeyModifiers { get; set; }

	public partial string Name { get; set; }

	[AlsoNotify(nameof(HasBinding), nameof(HasKeyChord))]
	public partial Key SecondKey { get; set; }

	[AlsoNotify(nameof(HasBinding), nameof(HasKeyChord))]
	public partial KeyModifiers SecondKeyModifiers { get; set; }

	#endregion

	#region Methods

	public bool Equals(ShortcutBinding other)
	{
		if (other == null)
		{
			return false;
		}

		return (GamepadButton == other.GamepadButton) &&
			(Key == other.Key) &&
			(KeyModifiers == other.KeyModifiers) &&
			(Name == other.Name) &&
			(SecondKey == other.SecondKey) &&
			(SecondKeyModifiers == other.SecondKeyModifiers);
	}

	public override bool Equals(object otherObject)
	{
		return Equals(otherObject as ShortcutBinding);
	}

	/// <summary>
	/// Returns the full display string, including chords (e.g. "Ctrl+K, W")
	/// </summary>
	public string GetDisplayString()
	{
		if (!HasKeyBinding && !HasKeyChord)
		{
			return string.Empty;
		}

		var firstPart = HasKeyBinding ? GetKeyString() : string.Empty;

		if (!HasKeyChord)
		{
			return firstPart;
		}

		var secondPart = GetSecondKeyString();

		return string.IsNullOrEmpty(firstPart)
			? secondPart
			: $"{firstPart}, {secondPart}";
	}

	public override int GetHashCode()
	{
		HashCode hash = new();
		hash.Add(GamepadButton);
		hash.Add(Key);
		hash.Add(KeyModifiers);
		hash.Add(Name);
		hash.Add(SecondKey);
		hash.Add(SecondKeyModifiers);
		return hash.ToHashCode();
	}

	public KeyGesture GetKeyGesture()
	{
		return new KeyGesture(Key, KeyModifiers);
	}

	public void Reset()
	{
		GamepadIndex = 0;
		GamepadButton = GamepadButton.None;
		Key = Key.None;
		KeyModifiers = KeyModifiers.None;
		SecondKey = Key.None;
		SecondKeyModifiers = KeyModifiers.None;
	}

	public override string ToString()
	{
		var keyboard = HasKeyBinding || HasKeyChord ? GetDisplayString() : null;
		var gamepad = HasGamepadBinding ? GetButtonString() : null;

		return (keyboard, gamepad) switch
		{
			(null, null) => "",
			({ } k, null) => k,
			(null, { } g) => g,
			({ } k, { } g) => $"{k} or {g}"
		};
	}

	/// <summary>
	/// Updates the primary binding (first key)
	/// </summary>
	public void UpdateBinding(Key key, KeyModifiers modifier)
	{
		switch (key)
		{
			case Key.LeftCtrl or Key.RightCtrl:
				Key = Key.None;
				KeyModifiers = modifier.SetFlag(KeyModifiers.Control);
				break;
			case Key.LeftShift or Key.RightShift:
				Key = Key.None;
				KeyModifiers = modifier.SetFlag(KeyModifiers.Shift);
				break;
			case Key.LeftAlt or Key.RightAlt:
				Key = Key.None;
				KeyModifiers = modifier.SetFlag(KeyModifiers.Alt);
				break;
			case Key.LWin or Key.RWin:
				Key = Key.None;
				KeyModifiers = modifier.SetFlag(KeyModifiers.Meta);
				break;
			default:
				Key = key;
				KeyModifiers = modifier;
				break;
		}
	}

	/// <summary>
	/// Updates the second key of a chord
	/// </summary>
	public void UpdateChordBinding(Key secondKey, KeyModifiers secondModifiers)
	{
		SecondKey = secondKey;
		SecondKeyModifiers = secondModifiers;
	}

	private string GetButtonString()
	{
		return GamepadButton
			is GamepadButton.A
			or GamepadButton.B
			or GamepadButton.X
			or GamepadButton.Y
			or GamepadButton.Start
			or GamepadButton.Back
			? $"({GamepadIndex}:{GamepadButton})"
			: $"{GamepadIndex}:{GamepadButton}";
	}

	private string GetKeyString()
	{
		return Key switch
		{
			Key.LeftAlt or Key.RightAlt when KeyModifiers == KeyModifiers.Alt => Key.ToString(),
			Key.LeftCtrl or Key.RightCtrl when KeyModifiers == KeyModifiers.Control => Key.ToString(),
			Key.LeftShift or Key.RightShift when KeyModifiers == KeyModifiers.Shift => Key.ToString(),
			Key.LWin or Key.RWin when KeyModifiers == KeyModifiers.Meta => Key.ToString(),
			Key.None when KeyModifiers == KeyModifiers.None => string.Empty,
			_ => GetKeyStringDetails()
		};
	}

	private string GetKeyStringDetails()
	{
		using var sd = StringBuilderPool.Rent();
		var s = sd.Value;
		var formatInfo = KeyGestureFormatter.Instance;

		if (KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			s.Append(formatInfo.Ctrl);
		}

		if (KeyModifiers.HasFlag(KeyModifiers.Alt))
		{
			Plus(s);
			s.Append(formatInfo.Alt);
		}

		if (KeyModifiers.HasFlag(KeyModifiers.Shift))
		{
			Plus(s);
			s.Append(formatInfo.Shift);
		}

		if (KeyModifiers.HasFlag(KeyModifiers.Meta))
		{
			Plus(s);
			s.Append(formatInfo.Meta);
		}

		if ((Key != Key.None) || (KeyModifiers == KeyModifiers.None))
		{
			Plus(s);
			s.Append(formatInfo.FormatKey(Key));
		}

		return s.ToString();

		static void Plus(StringBuilder s)
		{
			if (s.Length > 0)
			{
				s.Append('+');
			}
		}
	}

	private string GetSecondKeyString()
	{
		if ((SecondKey == Key.None) && (SecondKeyModifiers == KeyModifiers.None))
		{
			return string.Empty;
		}

		using var sd = StringBuilderPool.Rent();
		var s = sd.Value;
		var formatInfo = KeyGestureFormatter.Instance;

		if (SecondKeyModifiers.HasFlag(KeyModifiers.Control))
		{
			s.Append(formatInfo.Ctrl);
		}
		if (SecondKeyModifiers.HasFlag(KeyModifiers.Alt))
		{
			Plus(s);
			s.Append(formatInfo.Alt);
		}
		if (SecondKeyModifiers.HasFlag(KeyModifiers.Shift))
		{
			Plus(s);
			s.Append(formatInfo.Shift);
		}
		if (SecondKeyModifiers.HasFlag(KeyModifiers.Meta))
		{
			Plus(s);
			s.Append(formatInfo.Meta);
		}

		if ((SecondKey != Key.None) || (SecondKeyModifiers == KeyModifiers.None))
		{
			Plus(s);
			s.Append(formatInfo.FormatKey(SecondKey));
		}

		return s.ToString();

		static void Plus(StringBuilder s)
		{
			if (s.Length > 0)
			{
				s.Append('+');
			}
		}
	}

	#endregion
}