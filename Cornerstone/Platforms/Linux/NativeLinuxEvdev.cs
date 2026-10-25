#region References

using System;
using System.Runtime.InteropServices;

#endregion

namespace Cornerstone.Platforms.Linux;

/// <summary>
/// Linux evdev (linux/input.h) for gamepads that follow the kernel gamepad specification.
/// </summary>
internal static class NativeLinuxEvdev
{
	#region Constants

	internal const int AbsCount = 0x40;
	internal const int AbsHat0X = 0x10;
	internal const int AbsHat0Y = 0x11;
	internal const int AbsHat1X = 0x12;
	internal const int AbsHat1Y = 0x13;
	internal const int AbsHat2X = 0x14;
	internal const int AbsHat2Y = 0x15;
	internal const int AbsRx = 0x03;
	internal const int AbsRy = 0x04;
	internal const int AbsRz = 0x05;
	internal const int AbsX = 0x00;
	internal const int AbsY = 0x01;
	internal const int AbsZ = 0x02;
	internal const int BtnDpadDown = 0x221;
	internal const int BtnDpadLeft = 0x222;
	internal const int BtnDpadRight = 0x223;
	internal const int BtnDpadUp = 0x220;
	internal const int BtnEast = 0x131;
	internal const int BtnNorth = 0x133;
	internal const int BtnSelect = 0x13a;
	internal const int BtnSouth = 0x130;
	internal const int BtnStart = 0x13b;
	internal const int BtnThumbl = 0x13d;
	internal const int BtnThumbr = 0x13e;
	internal const int BtnTl = 0x136;
	internal const int BtnTl2 = 0x138;
	internal const int BtnTr = 0x137;
	internal const int BtnTr2 = 0x139;
	internal const int BtnWest = 0x134;
	internal const ushort EvAbs = 0x03;
	internal const ushort EvKey = 0x01;
	internal const int KeyBitmapLength = 96;

	private const int Eacces = 13;
	private const int O_CLOEXEC = 0x80000;
	private const int O_NONBLOCK = 0x800;
	private const int O_RDONLY = 0;
	private const uint IocRead = 2;

	#endregion

	#region Methods

	internal static void Close(int fd)
	{
		if (fd >= 0)
		{
			close(fd);
		}
	}

	internal static bool HasBit(byte[] bits, int code)
	{
		var index = code >> 3;
		if ((index < 0) || (index >= bits.Length))
		{
			return false;
		}

		return (bits[index] & (1 << (code & 7))) != 0;
	}

	internal static bool IsGamepad(int fd)
	{
		var bits = new byte[KeyBitmapLength];
		if (!IoctlOut(fd, Ioc(IocRead, (uint) 'E', 0x20 + EvKey, KeyBitmapLength), bits))
		{
			return false;
		}

		// BTN_GAMEPAD is an alias of BTN_SOUTH. Legacy joysticks that do not report it are skipped.
		return HasBit(bits, BtnSouth);
	}

	internal static bool IsVirtualNode(string path)
	{
		// realpath of the event node. The device symlink's own target is only "../../inputN",
		// which still lives under /sys/class/input and does not say virtual versus hardware.
		var resolved = CanonicalPath(path);
		return (resolved != null) && resolved.Contains("/devices/virtual/", StringComparison.Ordinal);
	}

	internal static void LogAccessDenied(string path, int error)
	{
		if ((error != Eacces) || _loggedAccessDenied)
		{
			return;
		}

		_loggedAccessDenied = true;
		Console.WriteLine("Linux gamepad cannot open " + path + ". The process needs read access to /dev/input/event devices.");
	}

	internal static int Open(string path, out int error)
	{
		error = 0;
		var fd = open(path, O_RDONLY | O_NONBLOCK | O_CLOEXEC, 0);
		if (fd < 0)
		{
			error = Marshal.GetLastPInvokeError();
		}

		return fd;
	}

	internal static bool TryReadKeys(int fd, byte[] keys)
	{
		return IoctlOut(fd, Ioc(IocRead, (uint) 'E', 0x18, (uint) keys.Length), keys);
	}

	internal static bool TryReadState(int fd, byte[] keys, Axis[] axes)
	{
		Array.Clear(keys);
		for (var i = 0; i < axes.Length; i++)
		{
			axes[i].Present = false;
			axes[i].Minimum = 0;
			axes[i].Maximum = 0;
			axes[i].Flat = 0;
			axes[i].Value = 0;
		}

		var absoluteBits = new byte[16];
		if (!IoctlOut(fd, Ioc(IocRead, (uint) 'E', 0x20 + EvAbs, (uint) absoluteBits.Length), absoluteBits))
		{
			return false;
		}

		if (!TryReadKeys(fd, keys))
		{
			return false;
		}

		var limit = axes.Length;
		if (limit > absoluteBits.Length * 8)
		{
			limit = absoluteBits.Length * 8;
		}

		for (var code = 0; code < limit; code++)
		{
			if (!HasBit(absoluteBits, code))
			{
				continue;
			}

			if (!TryGetAbsolute(fd, code, out var info))
			{
				continue;
			}

			axes[code].Present = true;
			axes[code].Minimum = info.Minimum;
			axes[code].Maximum = info.Maximum;
			axes[code].Flat = info.Flat;
			axes[code].Value = info.Value;
		}

		return true;
	}

	internal static bool TryRefreshValues(int fd, Axis[] axes)
	{
		for (var code = 0; code < axes.Length; code++)
		{
			if (!axes[code].Present)
			{
				continue;
			}

			if (!TryGetAbsolute(fd, code, out var info))
			{
				return false;
			}

			axes[code].Value = info.Value;
		}

		return true;
	}

	private static string CanonicalPath(string path)
	{
		var result = realpath(path, IntPtr.Zero);
		if (result == IntPtr.Zero)
		{
			return null;
		}

		try
		{
			return Marshal.PtrToStringUTF8(result);
		}
		finally
		{
			free(result);
		}
	}

	private static uint Ioc(uint direction, uint type, uint number, uint size)
	{
		// Matches asm-generic/ioctl.h. EVIOCGNAME(256) is 0x81004506.
		return (direction << 30) | (size << 16) | (type << 8) | number;
	}

	private static bool IoctlOut(int fd, uint request, byte[] data)
	{
		var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
		try
		{
			return ioctl(fd, request, handle.AddrOfPinnedObject()) >= 0;
		}
		finally
		{
			handle.Free();
		}
	}

	private static bool TryGetAbsolute(int fd, int code, out InputAbsInfo info)
	{
		info = default;
		var request = Ioc(IocRead, (uint) 'E', (uint) (0x40 + code), 24);
		return ioctlAbs(fd, request, ref info) >= 0;
	}

	[DllImport("libc", EntryPoint = "close", SetLastError = true)]
	private static extern int close(int fd);

	[DllImport("libc", EntryPoint = "free")]
	private static extern void free(IntPtr pointer);

	[DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
	private static extern int ioctl(int fd, nuint request, IntPtr argument);

	[DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
	private static extern int ioctlAbs(int fd, nuint request, ref InputAbsInfo argument);

	[DllImport("libc", EntryPoint = "open", SetLastError = true)]
	private static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string pathname, int flags, int mode);

	[DllImport("libc", EntryPoint = "realpath", SetLastError = true)]
	private static extern IntPtr realpath([MarshalAs(UnmanagedType.LPUTF8Str)] string path, IntPtr resolved);

	#endregion

	#region Structures

	internal struct Axis
	{
		public int Flat;
		public int Maximum;
		public int Minimum;
		public bool Present;
		public int Value;
	}

	#endregion

	#region Nested Types

	/// <summary>
	/// linux/input.h input_absinfo. Field order is the kernel ABI.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct InputAbsInfo
	{
		public int Value;
		public int Minimum;
		public int Maximum;
		public int Fuzz;
		public int Flat;
		public int Resolution;
	}

	#endregion

	#region Fields

	private static bool _loggedAccessDenied;

	#endregion
}
