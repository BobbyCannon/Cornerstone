#region References

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace Cornerstone.Automation.Platforms.Windows;

internal static partial class NativeProcessInfoSafe
{
	#region Constants

	private const int _processQueryInformation = 0x0400;
	private const int _processQueryLimitedInformation = 0x1000;
	private const int _processVmRead = 0x0010;

	#endregion

	#region Methods

	public static string TryGetCommandLine(Process process)
	{
		if (process.HasExited)
		{
			return null;
		}

		var pid = (uint) process.Id;
		var hProcess = OpenProcess(
			_processQueryLimitedInformation | _processQueryInformation | _processVmRead,
			false,
			pid
		);

		if ((hProcess == 0) || (hProcess == new nint(-1)))
		{
			return null;
		}

		try
		{
			if (NtQueryInformationProcess(
					hProcess,
					Processinfoclass.ProcessBasicInformation,
					out var pbi,
					(uint) Marshal.SizeOf<ProcessBasicInformation>(),
					out _)
				!= 0)
			{
				return null;
			}

			var pebAddr = pbi.PebBaseAddress;
			if (pebAddr == 0)
			{
				return null;
			}

			// Offset to ProcessParameters pointer (RTL_USER_PROCESS_PARAMETERS*)
			// x64 → 0x20, x86 → 0x10   (most Windows 10/11 versions)
			nint paramsOffset = Environment.Is64BitProcess ? 0x20 : 0x10;
			var buffer = new byte[nint.Size];

			if (!ReadMemory(hProcess, pebAddr + paramsOffset, buffer))
			{
				return null;
			}

			var processParamsAddr = BinaryPrimitives.ReadInt64LittleEndian(buffer);

			if (processParamsAddr == 0)
			{
				return null;
			}

			// UNICODE_STRING CommandLine is at offset:
			// x64 → 0x70, x86 → 0x40 in RTL_USER_PROCESS_PARAMETERS
			nint cmdLineOffset = nint.Size == 8 ? 0x70 : 0x40;

			var usBuffer = new byte[Marshal.SizeOf<UnicodeString>()];
			if (!ReadMemory(hProcess, (nint) (processParamsAddr + cmdLineOffset), usBuffer))
			{
				return null;
			}

			var cmdLine = MemoryMarshal.Read<UnicodeString>(usBuffer);

			if ((cmdLine.Length == 0) || (cmdLine.Buffer == 0))
			{
				return null;
			}

			// in bytes (UTF-16)
			int byteLength = cmdLine.Length;
			if (byteLength is <= 0 or > 8192)
			{
				return null;
			}

			var cmdBytes = new byte[byteLength];
			return ReadMemory(hProcess, cmdLine.Buffer, cmdBytes)
				? Encoding.Unicode.GetString(cmdBytes)
				: null;
		}
		catch
		{
			return null;
		}
		finally
		{
			_ = CloseHandle(hProcess);
		}
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool CloseHandle(nint hObject);

	[LibraryImport("ntdll.dll")]
	private static partial int NtQueryInformationProcess(
		nint processHandle,
		Processinfoclass processInformationClass,
		out ProcessBasicInformation processInformation,
		uint processInformationLength,
		out uint returnLength);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	private static partial nint OpenProcess(
		int dwDesiredAccess,
		[MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
		uint dwProcessId);

	private static bool ReadMemory(nint hProcess, nint address, byte[] buffer)
	{
		var success = ReadProcessMemory(
			hProcess,
			address,
			buffer,
			(nuint) buffer.Length,
			out var read);

		return success && (read == (nuint) buffer.Length);
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool ReadProcessMemory(
		nint hProcess,
		nint lpBaseAddress,
		[Out] byte[] lpBuffer,
		nuint nSize,
		out nuint lpNumberOfBytesRead);

	#endregion

	#region Structures

	[StructLayout(LayoutKind.Sequential)]
	private struct ProcessBasicInformation
	{
		public nint ExitStatus;
		public nint PebBaseAddress;
		public nuint AffinityMask;
		public int BasePriority;
		public nuint Pid;
		public nuint ParentPid;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct UnicodeString
	{
		public ushort Length;
		public ushort MaximumLength;
		public nint Buffer; // PWSTR in native
	}

	#endregion

	#region Enumerations

	private enum Processinfoclass
	{
		ProcessBasicInformation = 0
	}

	#endregion
}