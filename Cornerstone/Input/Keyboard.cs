#region References

using System;
using System.Linq;
using System.Text;
using System.Threading;
using Cornerstone.Presentation.Managers;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the keyboard and allows for simulated input.
/// </summary>
public abstract class Keyboard : Manager
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="Keyboard" />.
	/// </summary>
	protected Keyboard()
	{
		State = new KeyboardState();
		DefaultInputDelay = TimeSpan.Zero;
		TextInputAsKeyPresses = true;
	}

	~Keyboard()
	{
		StopMonitoring();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Default input delay if the SendInput delay is TimeSpan.Zero.
	/// If this default input delay is also TimeSpan.Zero then no delay will occur.
	/// </summary>
	public TimeSpan DefaultInputDelay { get; set; }

	/// <summary>
	/// True if the keyboard is being monitored.
	/// </summary>
	public abstract bool IsMonitoring { get; }

	/// <summary>
	/// The last state of the keyboard when monitoring.
	/// </summary>
	public KeyboardState State { get; }

	/// <summary>
	/// Defaults the keyboard SendInput to send text as key presses if true.
	/// Otherwise, the send input will send the text as string.
	/// </summary>
	public bool TextInputAsKeyPresses { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Returns a string with all printable characters.
	/// </summary>
	/// <returns> All printable characters in order by KeyboardKey. </returns>
	public static string GetAllPrintableCharacters()
	{
		var printableCharacters = new StringBuilder();

		foreach (var key in Enum.GetValues(typeof(KeyboardKey)).Cast<KeyboardKey>())
		{
			var state = new KeyboardState { Key = key };
			var value = state.ToString();
			if (value != null)
			{
				printableCharacters.Append(value);
			}

			state.Character = null;
			state.IsLeftShiftPressed = true;
			value = state.ToString();

			if (value != null)
			{
				printableCharacters.Append(value);
			}
		}

		return printableCharacters.ToString();
	}

	/// <summary>
	/// Determines if the <see cref="KeyboardKey" /> is an ExtendedKey
	/// </summary>
	/// <param name="keyCode"> The key code. </param>
	/// <returns> true if the key code is an extended key otherwise false. </returns>
	/// <remarks>
	/// The extended keys consist of the ALT and CTRL keys on the right-hand side of the keyboard;
	/// the INS, DEL, HOME, END, PAGE UP, PAGE DOWN, and arrow keys in the clusters to the left of the numeric keypad;
	/// the NUM LOCK key; the BREAK (CTRL+PAUSE) key; the PRINT SCRN key; and the divide (/) and ENTER keys in the numeric keypad.
	/// See http://msdn.microsoft.com/en-us/library/ms646267(v=vs.85).aspx Section "Extended-Key Flag"
	/// </remarks>
	public static bool IsExtendedKey(KeyboardKey keyCode)
	{
		return (keyCode == KeyboardKey.Alt) ||
			(keyCode == KeyboardKey.LeftAlt) ||
			(keyCode == KeyboardKey.RightAlt) ||
			(keyCode == KeyboardKey.Control) ||
			(keyCode == KeyboardKey.RightControl) ||
			(keyCode == KeyboardKey.Insert) ||
			(keyCode == KeyboardKey.Delete) ||
			(keyCode == KeyboardKey.Home) ||
			(keyCode == KeyboardKey.End) ||
			(keyCode == KeyboardKey.PageUp) ||
			(keyCode == KeyboardKey.PageDown) ||
			(keyCode == KeyboardKey.RightArrow) ||
			(keyCode == KeyboardKey.UpArrow) ||
			(keyCode == KeyboardKey.LeftArrow) ||
			(keyCode == KeyboardKey.DownArrow) ||
			(keyCode == KeyboardKey.NumLock) ||
			(keyCode == KeyboardKey.ControlBreak) ||
			(keyCode == KeyboardKey.Snapshot) ||
			(keyCode == KeyboardKey.Divide);
	}

	/// <summary>
	/// Determines whether the specified key is down.
	/// </summary>
	/// <param name="keys"> The <see cref="KeyboardKey" /> for the key(s). </param>
	/// <returns>
	/// True if the key is down otherwise false.
	/// </returns>
	public abstract bool IsKeyDown(params KeyboardKey[] keys);

	/// <summary>
	/// Determines whether the specified key is up.
	/// </summary>
	/// <param name="keys"> The <see cref="KeyboardKey" /> for the key(s). </param>
	/// <returns>
	/// True if the key is up otherwise false.
	/// </returns>
	public abstract bool IsKeyUp(params KeyboardKey[] keys);

	/// <summary>
	/// Determines whether the toggling key is toggled on (in-effect) or not by calling the GetKeyState function.
	/// </summary>
	/// <param name="key"> The <see cref="KeyboardKey" /> for the key. </param>
	/// <returns>
	/// True if the toggling key is toggled on (in-effect) otherwise false.
	/// </returns>
	public abstract bool IsTogglingKeyInEffect(KeyboardKey key);

	/// <summary>
	/// Calls the SendInput method to simulate key down.
	/// </summary>
	/// <param name="keys"> The key(s) to press down. </param>
	public Keyboard KeyDown(params KeyboardKey[] keys)
	{
		if (keys.Length <= 0)
		{
			return this;
		}

		SendInput(GetInputBuilder().AddKeyDown(keys));

		return this;
	}

	/// <summary>
	/// Simulates a key press for each of the specified key codes in the order they are specified.
	/// </summary>
	/// <param name="keys"> The keys to press. </param>
	public Keyboard KeyPress(params KeyboardKey[] keys)
	{
		if (keys.Length <= 0)
		{
			return this;
		}

		SendInput(GetInputBuilder(keys));

		return this;
	}

	/// <summary>
	/// Simulates a modified keystroke where there is one modifier and multiple keys like CTRL-K-C where CTRL is the
	/// modifierKey and K and C are the keys.
	/// The flow is Modifier KeyDown, Keys Press in order, Modifier KeyUp.
	/// </summary>
	/// <param name="modifier"> The modifier key. </param>
	/// <param name="keys"> The list of keys to press. </param>
	public Keyboard KeyPress(KeyboardModifier modifier, params KeyboardKey[] keys)
	{
		if (keys.Length <= 0)
		{
			return this;
		}

		SendInput(GetInputBuilder(modifier, keys));

		return this;
	}

	/// <summary>
	/// Calls the SendInput method to simulate key up.
	/// </summary>
	/// <param name="keys"> The key(s) to release. </param>
	public Keyboard KeyUp(params KeyboardKey[] keys)
	{
		if (keys.Length <= 0)
		{
			return this;
		}

		SendInput(GetInputBuilder().AddKeyUp(keys));

		return this;
	}

	/// <summary>
	/// Sends provided text and an optional set of keys as input.
	/// </summary>
	/// <param name="text"> The text to be sent. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public Keyboard SendInput(string text)
	{
		SendInput(GetInputBuilder(text, TextInputAsKeyPresses), TimeSpan.Zero);
		return this;
	}

	/// <summary>
	/// Sends provided text and an optional set of keys as input.
	/// </summary>
	/// <param name="text"> The text to be sent. </param>
	/// <param name="delay"> An optional delay after sending input. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public Keyboard SendInput(string text, TimeSpan delay)
	{
		SendInput(GetInputBuilder(text, TextInputAsKeyPresses), delay);
		return this;
	}

	public abstract InputBuilder SendInput(InputBuilder builder, TimeSpan delay);

	/// <summary>
	/// Sends provided text and an optional set of keys as input.
	/// </summary>
	/// <param name="text"> The text to be sent. </param>
	/// <param name="keys"> The set of keys to be sent. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public Keyboard SendInput(string text, params KeyboardKey[] keys)
	{
		return SendInput(text, TimeSpan.Zero, keys);
	}

	/// <summary>
	/// Sends provided text as input. Can delay with before sending an optional set of keys.
	/// </summary>
	/// <param name="text"> The text to be sent. </param>
	/// <param name="delay"> An optional delay to wait before sending the provided keys. </param>
	/// <param name="keys"> An optional set of keys to be sent. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	/// <exception cref="ArgumentException"> The text parameter is too long. </exception>
	public Keyboard SendInput(string text, TimeSpan delay, params KeyboardKey[] keys)
	{
		if (text.Length > (uint.MaxValue / 2))
		{
			throw new ArgumentException($"The text parameter is too long. It must be less than {uint.MaxValue / 2} characters.", nameof(text));
		}

		var inputList = SendInput(GetInputBuilder(text, TextInputAsKeyPresses), delay);

		if ((keys != null) && (keys.Length > 0))
		{
			SendInput(inputList.Clear().AddKeyPress(keys));
		}

		return this;
	}

	/// <summary>
	/// Sends provided text and optional set of keystrokes as input.
	/// </summary>
	/// <param name="text"> The text to be sent. </param>
	/// <param name="keyStrokes"> An optional set of keystrokes to be sent. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	/// <exception cref="ArgumentException"> The text parameter is too long. </exception>
	public Keyboard SendInput(string text, params KeyStroke[] keyStrokes)
	{
		return SendInput(text, TimeSpan.Zero, keyStrokes);
	}

	/// <summary>
	/// Sends provided text as input. Can delay with before sending an optional set of keystrokes.
	/// </summary>
	/// <param name="text"> The text to be sent. </param>
	/// <param name="delay"> An optional delay to wait before sending the provided keys. </param>
	/// <param name="keyStrokes"> An optional set of keystrokes to be sent. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	/// <exception cref="ArgumentException"> The text parameter is too long. </exception>
	public Keyboard SendInput(string text, TimeSpan delay, params KeyStroke[] keyStrokes)
	{
		if (text.Length > (uint.MaxValue / 2))
		{
			throw new ArgumentException($"The text parameter is too long. It must be less than {uint.MaxValue / 2} characters.", nameof(text));
		}

		var inputList = SendInput(GetInputBuilder(text, TextInputAsKeyPresses), delay);

		if (keyStrokes.Length > 0)
		{
			SendInput(inputList.Clear().Add(keyStrokes));
		}

		return this;
	}

	/// <summary>
	/// Sends provided set of key as input.
	/// </summary>
	/// <param name="keys"> The set of keys to be sent. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public Keyboard SendInput(params KeyboardKey[] keys)
	{
		if (keys.Length <= 0)
		{
			return this;
		}

		SendInput(GetInputBuilder(keys));

		return this;
	}

	/// <summary>
	/// Sends provided set of keys as input with a modifier (ctrl, shift, etc).
	/// </summary>
	/// <param name="modifiers"> The modifier key(s). </param>
	/// <param name="keys"> The set of keys to be sent. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public Keyboard SendInput(KeyboardModifier modifiers, params KeyboardKey[] keys)
	{
		if (keys.Length <= 0)
		{
			return this;
		}

		SendInput(GetInputBuilder(modifiers, keys));

		return this;
	}

	/// <summary>
	/// Sends provided keystrokes as input.
	/// </summary>
	/// <param name="keyStrokes"> The set of keystrokes to be sent. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	/// <exception cref="ArgumentException"> The text parameter is too long. </exception>
	public Keyboard SendInput(params KeyStroke[] keyStrokes)
	{
		if (keyStrokes.Length > 0)
		{
			SendInput(GetInputBuilder(keyStrokes));
		}

		return this;
	}

	/// <summary>
	/// Sleeps the executing thread to create a pause between simulated inputs.
	/// </summary>
	/// <param name="timeoutInMilliseconds"> The number of milliseconds to wait. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public Keyboard Sleep(int timeoutInMilliseconds)
	{
		Thread.Sleep(timeoutInMilliseconds);
		return this;
	}

	/// <summary>
	/// Sleeps the executing thread to create a pause between simulated inputs.
	/// </summary>
	/// <param name="timeout"> The time to wait. </param>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public Keyboard Sleep(TimeSpan timeout)
	{
		Thread.Sleep(timeout);
		return this;
	}

	/// <summary>
	/// Start monitoring the keyboard input.
	/// </summary>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public abstract Keyboard StartMonitoring();

	/// <summary>
	/// Stop monitoring the keyboard input.
	/// </summary>
	/// <returns> This <see cref="Keyboard" /> instance. </returns>
	public abstract Keyboard StopMonitoring();

	/// <summary>
	/// Convert a keyboard key to a character
	/// </summary>
	/// <param name="key"> The keyboard key </param>
	/// <param name="state"> </param>
	/// <returns> </returns>
	public static char? ToCharacter(KeyboardKey key, KeyboardState state)
	{
		var altKey = state.IsCapsLockOn || state.IsShiftPressed;

		if ((key >= KeyboardKey.A) && (key <= KeyboardKey.Z))
		{
			return altKey ? (char) key : ((char) key).ToString().ToLower()[0];
		}

		if ((key >= KeyboardKey.Number0) && (key <= KeyboardKey.Number9))
		{
			if (state.IsShiftPressed)
			{
				return key switch
				{
					KeyboardKey.Number1 => '!',
					KeyboardKey.Number2 => '@',
					KeyboardKey.Number3 => '#',
					KeyboardKey.Number4 => '$',
					KeyboardKey.Number5 => '%',
					KeyboardKey.Number6 => '^',
					KeyboardKey.Number7 => '&',
					KeyboardKey.Number8 => '*',
					KeyboardKey.Number9 => '(',
					KeyboardKey.Number0 => ')',
					_ => (char) key
				};
			}

			return (char) key;
		}

		if ((key >= KeyboardKey.Numpad0) && (key <= KeyboardKey.Numpad9))
		{
			return (char) (key - 0x30);
		}

		return key switch
		{
			KeyboardKey.Add => '+',
			KeyboardKey.Decimal => '.',
			KeyboardKey.Divide => '/',
			KeyboardKey.Subtract => '-',
			KeyboardKey.Space => ' ',
			KeyboardKey.Tab => '\t',
			KeyboardKey.Oem1 => state.IsShiftPressed ? ':' : ';',
			KeyboardKey.Oem2 => state.IsShiftPressed ? '?' : '/',
			KeyboardKey.Oem3 => state.IsShiftPressed ? '~' : '`',
			KeyboardKey.Oem4 => state.IsShiftPressed ? '{' : '[',
			KeyboardKey.Oem5 => state.IsShiftPressed ? '|' : '\\',
			KeyboardKey.Oem6 => state.IsShiftPressed ? '}' : ']',
			KeyboardKey.Oem7 => state.IsShiftPressed ? '"' : '\'',
			KeyboardKey.OemComma => state.IsShiftPressed ? '<' : ',',
			KeyboardKey.OemMinus => state.IsShiftPressed ? '_' : '-',
			KeyboardKey.OemPeriod => state.IsShiftPressed ? '>' : '.',
			KeyboardKey.OemPlus => state.IsShiftPressed ? '+' : '=',
			_ => null
		};
	}

	protected abstract InputBuilder GetInputBuilder();

	protected abstract InputBuilder GetInputBuilder(KeyStroke[] keyStrokes);

	protected abstract InputBuilder GetInputBuilder(KeyboardModifier modifier, KeyboardKey[] keys);

	protected abstract InputBuilder GetInputBuilder(string text, bool textInputAsKeyPresses);

	protected abstract InputBuilder GetInputBuilder(KeyboardKey[] keys);

	protected virtual void OnKeyPressed(KeyboardStateArg e)
	{
		KeyChanged?.Invoke(this, e);
	}

	protected abstract InputBuilder SendInput(InputBuilder builder);

	#endregion

	#region Events

	/// <summary>
	/// Called when monitoring keyboard and a key is pressed.
	/// </summary>
	public event EventHandler<KeyboardStateArg> KeyChanged;

	#endregion
}