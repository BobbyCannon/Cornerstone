#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Linux;

/// <summary>
/// Reads Linux gamepads through evdev. Devices must report BTN_GAMEPAD (the kernel gamepad specification).
/// Stick Y is inverted because the kernel reports positive Y as down.
/// Each poll snapshots keys and axes with ioctls. Draining the event queue on the UI thread stalls while a stick is moving.
/// A readable hardware gamepad is used by itself. When that node is hidden, a virtual pad other than Steam's
/// Xbox 360 device is used. Steam's virtual 360 pad is only the fallback.
/// </summary>
[SourceReflection]
public class LinuxGamepad : Gamepad
{
	#region Fields

	private static readonly object _pathGate;
	private static string _loggedPaths;
	private static string[] _paths;
	private static long _pathsTicks;
	private readonly NativeLinuxEvdev.Axis[] _axes;
	private readonly byte[] _keys;
	private readonly IDateTimeProvider _dateTimeProvider;
	private int _boundIndex;
	private int _fd;
	private bool _hasReading;
	private StandardReading _lastReading;
	private bool _readFailed;

	#endregion

	#region Constructors

	static LinuxGamepad()
	{
		_pathGate = new object();
		_paths = Array.Empty<string>();
	}

	[DependencyInjectionConstructor]
	public LinuxGamepad(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;
		_axes = new NativeLinuxEvdev.Axis[NativeLinuxEvdev.AbsCount];
		_keys = new byte[NativeLinuxEvdev.KeyBitmapLength];
		_boundIndex = -1;
		_fd = -1;
	}

	#endregion

	#region Methods

	public override void Update()
	{
		try
		{
			if (!EnsureDevice())
			{
				Disconnect();
				return;
			}

			if (!NativeLinuxEvdev.TryReadKeys(_fd, _keys) || !NativeLinuxEvdev.TryRefreshValues(_fd, _axes))
			{
				InvalidatePaths();
				Disconnect();
				return;
			}

			var reading = ReadState();
			if (_hasReading && _lastReading.Equals(reading))
			{
				return;
			}

			_hasReading = true;
			_lastReading = reading;
			Apply(reading);
		}
		catch (Exception ex)
		{
			if (!_readFailed)
			{
				_readFailed = true;
				Console.WriteLine(ex);
			}

			InvalidatePaths();
			Disconnect();
		}
	}

	private void Apply(StandardReading reading)
	{
		var lastButtons = State.Buttons;
		State.IsConnected = true;
		State.Buttons = reading.Buttons;
		State.LeftTriggerValue = reading.LeftTrigger;
		State.RightTriggerValue = reading.RightTrigger;
		State.UpdateLeftThumb(reading.LeftX, reading.LeftY);
		State.UpdateRightThumb(reading.RightX, reading.RightY);
		State.UpdateTriggers();

		if (State.HasChanges())
		{
			State.DateTime = _dateTimeProvider.UtcNow;
			if (lastButtons != State.Buttons)
			{
				TriggerButtonEvents(lastButtons, State.Buttons);
			}

			OnChanged(State.ShallowClone());
			State.ResetHasChanges();
		}
	}

	private static int CompareEventPaths(string left, string right)
	{
		return EventNumber(left).CompareTo(EventNumber(right));
	}

	private void Disconnect()
	{
		CloseDevice();
		_hasReading = false;
		if (!State.IsConnected)
		{
			return;
		}

		State.Reset(_dateTimeProvider.UtcNow);
		OnChanged(State.ShallowClone());
	}

	private bool EnsureDevice()
	{
		if ((_fd >= 0) && (_boundIndex == State.Index))
		{
			return true;
		}

		CloseDevice();
		if (State.Index < 0)
		{
			return false;
		}

		var paths = GetPaths(_dateTimeProvider.UtcNow.Ticks);
		if (State.Index >= paths.Length)
		{
			return false;
		}

		var fd = NativeLinuxEvdev.Open(paths[State.Index], out var error);
		if (fd < 0)
		{
			NativeLinuxEvdev.LogAccessDenied(paths[State.Index], error);
			InvalidatePaths();
			return false;
		}

		if (!NativeLinuxEvdev.IsGamepad(fd) || !NativeLinuxEvdev.TryReadState(fd, _keys, _axes))
		{
			NativeLinuxEvdev.Close(fd);
			InvalidatePaths();
			return false;
		}

		_fd = fd;
		_boundIndex = State.Index;
		return true;
	}

	private static int EventNumber(string path)
	{
		var name = Path.GetFileName(path);
		if ((name.Length > 5)
			&& name.StartsWith("event", StringComparison.Ordinal)
			&& int.TryParse(name.AsSpan(5), out var number))
		{
			return number;
		}

		return int.MaxValue;
	}

	private static string[] GetPaths(long ticks)
	{
		lock (_pathGate)
		{
			if ((_pathsTicks != 0) && (ticks - _pathsTicks < TimeSpan.TicksPerSecond))
			{
				return _paths;
			}

			_paths = Scan();
			_pathsTicks = ticks;
			return _paths;
		}
	}

	private static void InvalidatePaths()
	{
		lock (_pathGate)
		{
			_pathsTicks = 0;
		}
	}

	private byte ReadTrigger(int primary, int legacy, int digitalButton)
	{
		var axis = _axes[primary];
		if (!axis.Present)
		{
			axis = _axes[legacy];
		}

		var value = axis.Present
			? GamepadValues.ToTriggerFromRange(axis.Value, axis.Minimum, axis.Maximum)
			: (byte) 0;
		if ((value == 0) && NativeLinuxEvdev.HasBit(_keys, digitalButton))
		{
			return 255;
		}

		return value;
	}

	private StandardReading ReadState()
	{
		var buttons = GamepadButton.None;
		SetButton(ref buttons, GamepadButton.A, NativeLinuxEvdev.BtnSouth);
		SetButton(ref buttons, GamepadButton.B, NativeLinuxEvdev.BtnEast);
		SetButton(ref buttons, GamepadButton.X, NativeLinuxEvdev.BtnWest);
		SetButton(ref buttons, GamepadButton.Y, NativeLinuxEvdev.BtnNorth);
		SetButton(ref buttons, GamepadButton.LeftBumper, NativeLinuxEvdev.BtnTl);
		SetButton(ref buttons, GamepadButton.RightBumper, NativeLinuxEvdev.BtnTr);
		SetButton(ref buttons, GamepadButton.Back, NativeLinuxEvdev.BtnSelect);
		SetButton(ref buttons, GamepadButton.Start, NativeLinuxEvdev.BtnStart);
		SetButton(ref buttons, GamepadButton.LeftStickThumb, NativeLinuxEvdev.BtnThumbl);
		SetButton(ref buttons, GamepadButton.RightStickThumb, NativeLinuxEvdev.BtnThumbr);
		SetButton(ref buttons, GamepadButton.DpadUp, NativeLinuxEvdev.BtnDpadUp);
		SetButton(ref buttons, GamepadButton.DpadDown, NativeLinuxEvdev.BtnDpadDown);
		SetButton(ref buttons, GamepadButton.DpadLeft, NativeLinuxEvdev.BtnDpadLeft);
		SetButton(ref buttons, GamepadButton.DpadRight, NativeLinuxEvdev.BtnDpadRight);
		SetAnalogBumper(ref buttons, GamepadButton.LeftBumper, NativeLinuxEvdev.AbsHat1Y);
		SetAnalogBumper(ref buttons, GamepadButton.RightBumper, NativeLinuxEvdev.AbsHat1X);

		var hatX = HatDirection(NativeLinuxEvdev.AbsHat0X);
		var hatY = HatDirection(NativeLinuxEvdev.AbsHat0Y);
		if (hatX < 0)
		{
			buttons |= GamepadButton.DpadLeft;
		}
		else if (hatX > 0)
		{
			buttons |= GamepadButton.DpadRight;
		}

		if (hatY < 0)
		{
			buttons |= GamepadButton.DpadUp;
		}
		else if (hatY > 0)
		{
			buttons |= GamepadButton.DpadDown;
		}

		return new StandardReading(
			buttons,
			ReadThumb(NativeLinuxEvdev.AbsX, false),
			ReadThumb(NativeLinuxEvdev.AbsY, true),
			ReadThumb(NativeLinuxEvdev.AbsRx, false),
			ReadThumb(NativeLinuxEvdev.AbsRy, true),
			ReadTrigger(NativeLinuxEvdev.AbsHat2Y, NativeLinuxEvdev.AbsZ, NativeLinuxEvdev.BtnTl2),
			ReadTrigger(NativeLinuxEvdev.AbsHat2X, NativeLinuxEvdev.AbsRz, NativeLinuxEvdev.BtnTr2));
	}

	private int HatDirection(int code)
	{
		var axis = _axes[code];
		if (!axis.Present)
		{
			return 0;
		}

		return GamepadValues.HatDirection(axis.Value, axis.Minimum, axis.Maximum, axis.Flat);
	}

	private short ReadThumb(int code, bool invert)
	{
		var axis = _axes[code];
		if (!axis.Present)
		{
			return 0;
		}

		return GamepadValues.ToThumbFromRange(axis.Value, axis.Minimum, axis.Maximum, invert);
	}

	private static string[] Scan()
	{
		string[] entries;
		try
		{
			entries = Directory.GetFiles("/dev/input", "event*");
		}
		catch (DirectoryNotFoundException)
		{
			return Array.Empty<string>();
		}
		catch (UnauthorizedAccessException)
		{
			return Array.Empty<string>();
		}
		catch (IOException)
		{
			return Array.Empty<string>();
		}

		Array.Sort(entries, CompareEventPaths);
		var found = new List<ScannedPad>();
		foreach (var path in entries)
		{
			var fd = NativeLinuxEvdev.Open(path, out var error);
			if (fd < 0)
			{
				NativeLinuxEvdev.LogAccessDenied(path, error);
				continue;
			}

			try
			{
				if (NativeLinuxEvdev.IsGamepad(fd))
				{
					found.Add(ReadPad(path));
				}
			}
			finally
			{
				NativeLinuxEvdev.Close(fd);
			}
		}

		var chosen = ChoosePads(found);
		LogChoice(chosen);
		return chosen;
	}

	// Hardware wins. Otherwise keep a direct virtual target and drop Steam's Xbox 360 pad,
	// which is a second node for the same controller and adds a processing hop.
	// Steam's pad remains when it is the only readable gamepad.
	private static string[] ChoosePads(List<ScannedPad> found)
	{
		var hardware = new List<string>();
		var direct = new List<string>();
		var steam = new List<string>();
		foreach (var pad in found)
		{
			if (!pad.VirtualDevice)
			{
				hardware.Add(pad.Path);
				continue;
			}

			if (pad.IsSteamVirtual)
			{
				steam.Add(pad.Path);
				continue;
			}

			direct.Add(pad.Path);
		}

		if (hardware.Count > 0)
		{
			return hardware.ToArray();
		}

		if (direct.Count > 0)
		{
			return direct.ToArray();
		}

		return steam.ToArray();
	}

	private static void LogChoice(string[] chosen)
	{
		var text = chosen.Length == 0 ? "(none)" : string.Join(", ", chosen);
		if (text == _loggedPaths)
		{
			return;
		}

		_loggedPaths = text;
		Console.WriteLine("Linux gamepads: " + text);
	}

	private static ScannedPad ReadPad(string path)
	{
		var name = Path.GetFileName(path);
		var sysfs = "/sys/class/input/" + name + "/device";
		return new ScannedPad(
			path,
			ReadHex(sysfs + "/id/vendor"),
			ReadHex(sysfs + "/id/product"),
			ReadSysfsText(sysfs + "/name"),
			NativeLinuxEvdev.IsVirtualNode("/sys/class/input/" + name));
	}

	private static int ReadHex(string path)
	{
		var text = ReadSysfsText(path);
		if (text.Length == 0)
		{
			return 0;
		}

		try
		{
			return System.Convert.ToInt32(text, 16);
		}
		catch (FormatException)
		{
			return 0;
		}
		catch (OverflowException)
		{
			return 0;
		}
	}

	private static string ReadSysfsText(string path)
	{
		try
		{
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				var buffer = new byte[256];
				var count = stream.Read(buffer, 0, buffer.Length);
				if (count <= 0)
				{
					return string.Empty;
				}

				return Encoding.UTF8.GetString(buffer, 0, count).Trim();
			}
		}
		catch (IOException)
		{
			return string.Empty;
		}
		catch (UnauthorizedAccessException)
		{
			return string.Empty;
		}
	}

	private void SetAnalogBumper(ref GamepadButton buttons, GamepadButton flag, int code)
	{
		var axis = _axes[code];
		if (!axis.Present)
		{
			return;
		}

		if (GamepadValues.AxisPastHalf(axis.Value, axis.Minimum, axis.Maximum))
		{
			buttons |= flag;
		}
	}

	private void SetButton(ref GamepadButton buttons, GamepadButton flag, int code)
	{
		if (NativeLinuxEvdev.HasBit(_keys, code))
		{
			buttons |= flag;
		}
	}

	private void CloseDevice()
	{
		var fd = _fd;
		_fd = -1;
		_boundIndex = -1;
		if (fd >= 0)
		{
			NativeLinuxEvdev.Close(fd);
		}
	}

	private void TriggerButtonEvents(GamepadButton lastButtons, GamepadButton stateButtons)
	{
		var oldButtons = lastButtons.GetFlagValues();
		var newButtons = stateButtons.GetFlagValues();

		foreach (var released in oldButtons.Except(newButtons))
		{
			OnButtonChanged(released, false);
		}

		foreach (var pressed in newButtons.Except(oldButtons))
		{
			OnButtonChanged(pressed, true);
		}
	}

	#endregion

	#region Structures

	internal readonly struct ScannedPad
	{
		public ScannedPad(string path, int vendor, int product, string deviceName, bool virtualDevice)
		{
			Path = path;
			VirtualDevice = virtualDevice;
			IsSteamVirtual = ((vendor == 0x28de) && (product == 0x11ff))
				|| ((deviceName != null) && deviceName.StartsWith("Microsoft X-Box 360 pad", StringComparison.Ordinal));
		}

		public string Path { get; }

		public bool VirtualDevice { get; }

		public bool IsSteamVirtual { get; }
	}

	private readonly struct StandardReading : IEquatable<StandardReading>
	{
		public StandardReading(
			GamepadButton buttons,
			short leftX,
			short leftY,
			short rightX,
			short rightY,
			byte leftTrigger,
			byte rightTrigger)
		{
			Buttons = buttons;
			LeftX = leftX;
			LeftY = leftY;
			RightX = rightX;
			RightY = rightY;
			LeftTrigger = leftTrigger;
			RightTrigger = rightTrigger;
		}

		public GamepadButton Buttons { get; }

		public short LeftX { get; }

		public short LeftY { get; }

		public short RightX { get; }

		public short RightY { get; }

		public byte LeftTrigger { get; }

		public byte RightTrigger { get; }

		public bool Equals(StandardReading other)
		{
			return (Buttons == other.Buttons)
				&& (LeftX == other.LeftX)
				&& (LeftY == other.LeftY)
				&& (RightX == other.RightX)
				&& (RightY == other.RightY)
				&& (LeftTrigger == other.LeftTrigger)
				&& (RightTrigger == other.RightTrigger);
		}
	}

	#endregion
}
