#region References

using System;
using System.Drawing;
using System.Runtime.InteropServices;

#endregion

namespace Cornerstone.Platforms.Windows.Native;

/// <summary>
/// References all the Native Windows API methods for the WindowsInput functionality.
/// </summary>
internal static class NativeGeneral
{
	#region Constants

	public const int Th32CsSnapProcess = 0x00000002;

	#endregion

	#region Fields

	private static readonly nint _notTopMost = new(-2);
	private static readonly nint _topMost = new(-1);

	#endregion

	#region Methods

	public static void BringToTop(nint handle)
	{
		SetWindowPos(handle, _notTopMost, 0, 0, 0, 0, SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize);
		SetWindowPos(handle, _topMost, 0, 0, 0, 0, SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize);
		SetWindowPos(handle, _notTopMost, 0, 0, 0, 0, SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize);
	}

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool BringWindowToTop(nint hWnd);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CloseHandle(nint hObject);

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern nint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessId);

	[DllImport("user32.dll")]
	public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, nint lParam);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint GetForegroundWindow();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint GetParent(nint hWnd);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool GetTokenInformation(nint tokenHandle, TokenInformationClass tokenInformationClass, nint tokenInformation, uint tokenInformationLength, out uint returnLength);

	public static WindowPlacement GetWindowPlacement(nint handle)
	{
		var placement = new WindowPlacement();
		placement.length = Marshal.SizeOf(placement);
		GetWindowPlacement(handle, ref placement);
		return placement;
	}

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetWindowPlacement(nint hWnd, ref WindowPlacement lpwndpl);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetWindowRect(nint hWnd, out Rect lpRect);

	public static bool IsElevated(nint handle)
	{
		const uint tokenQuery = 0x0008;

		if (!OpenProcessToken(handle, tokenQuery, out var hToken))
		{
			var error = Marshal.GetLastWin32Error();
			throw new Exception($"{error}: Failed to access the process token.");
		}

		var pElevationType = Marshal.AllocHGlobal(sizeof(TokenElevationType));
		GetTokenInformation(hToken, TokenInformationClass.TokenElevationType, pElevationType, sizeof(TokenElevationType), out var dwSize);
		var elevationType = (TokenElevationType) Marshal.ReadInt32(pElevationType);
		Marshal.FreeHGlobal(pElevationType);

		switch (elevationType)
		{
			case TokenElevationType.TokenElevationTypeDefault:
				//Console.WriteLine("\nTokenElevationTypeDefault - User is not using a split token.\n");
				return false;

			case TokenElevationType.TokenElevationTypeFull:
				//Console.WriteLine("\nTokenElevationTypeFull - User has a split token, and the process is running elevated.\n");
				return true;

			case TokenElevationType.TokenElevationTypeLimited:
				//Console.WriteLine("\nTokenElevationTypeLimited - User has a split token, but the process is not running elevated.\n");
				return false;

			default:
				return false;
		}
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool IsWow64Process([In] nint hProcess, [Out] out bool isX86);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool MoveWindow(nint hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool Process32First(nint hSnapshot, ref ProcessEntry32 lppe);

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool Process32Next(nint hSnapshot, ref ProcessEntry32 lppe);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SetFocus(nint hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);

	#endregion

	#region Structures

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	internal struct ProcessEntry32
	{
		public uint dwSize;
		public uint cntUsage;
		public uint th32ProcessID;
		public IntPtr th32DefaultHeapID;
		public uint th32ModuleID;
		public uint cntThreads;
		public uint th32ParentProcessID;
		public int pcPriClassBase;
		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szExeFile;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Point
	{
		#region Fields

		public int X;
		public int Y;

		#endregion
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Rect
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public Rect(int left, int top, int right, int bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WindowPlacement
	{
		public int length;
		public int flags;
		public int ShowState;
		public Point ptMinPosition;
		public Point ptMaxPosition;
		public Rectangle rcNormalPosition;
	}

	#endregion

	#region Enumerations

	[Flags]
	public enum SetWindowPosFlags : uint
	{
		/// <summary>
		/// Retains the current position (ignores X and Y parameters).
		/// </summary>
		NoMove = 0x0002,

		/// <summary>
		/// Retains the current size (ignores the cx and cy parameters).
		/// </summary>
		NoSize = 0x0001
	}

	internal enum TokenInformationClass
	{
		TokenUser = 1,
		TokenGroups,
		TokenPrivileges,
		TokenOwner,
		TokenPrimaryGroup,
		TokenDefaultDacl,
		TokenSource,
		TokenType,
		TokenImpersonationLevel,
		TokenStatistics,
		TokenRestrictedSids,
		TokenSessionId,
		TokenGroupsAndPrivileges,
		TokenSessionReference,
		TokenSandBoxInert,
		TokenAuditPolicy,
		TokenOrigin,
		TokenElevationType,
		TokenLinkedToken,
		TokenElevation,
		TokenHasRestrictions,
		TokenAccessInformation,
		TokenVirtualizationAllowed,
		TokenVirtualizationEnabled,
		TokenIntegrityLevel,
		TokenUIAccess,
		TokenMandatoryPolicy,
		TokenLogonSid,
		MaxTokenInfoClass
	}

	// Define other methods and classes here
	private enum TokenElevationType
	{
		TokenElevationTypeDefault = 1,
		TokenElevationTypeFull,
		TokenElevationTypeLimited
	}

	#endregion

	#region Delegates

	public delegate bool EnumThreadDelegate(nint hWnd, nint lParam);

	#endregion
}