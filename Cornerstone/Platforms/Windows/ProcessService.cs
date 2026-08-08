#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// Represents a service that handling processes.
/// </summary>
public static class ProcessService
{
	#region Constants

	private const uint ProcessQueryInformation = 0x0400;
	private const uint ProcessVmRead = 0x0010;

	#endregion

	#region Fields

	private static readonly string[] _extensions;

	#endregion

	#region Constructors

	static ProcessService()
	{
		_extensions = [".exe", ".com"];
	}

	#endregion

	#region Methods

	/// <summary>
	/// Formats the string to be able to include inside inner string.
	/// </summary>
	/// <param name="source"> The source string value. </param>
	/// <returns> The string formatted to be place inside inner string. </returns>
	public static string FormatForInnerString(this string source)
	{
		return source.Replace("\\", "\\\\");
	}

	/// <summary>
	/// Gets a list of all running processes
	/// </summary>
	/// <returns> The current process list. </returns>
	public static IEnumerable<SafeProcess> GetAllProcesses()
	{
		return Process.GetProcesses()
			.Select(p => new SafeProcess(p))
			.Where(TryPopulate);
	}

	/// <summary>
	/// Get the safe process for the current process.
	/// </summary>
	/// <returns> The safe process for the current process or null if an issue occurs. </returns>
	public static SafeProcess GetCurrentProcess()
	{
		var process = new SafeProcess(Process.GetCurrentProcess());
		return TryPopulate(process) ? process : null;
	}

	/// <summary>
	/// Get the safe process for the process id.
	/// </summary>
	/// <returns> The safe process for the provided process id or null if an issue occurs. </returns>
	public static SafeProcess GetProcessById(int id)
	{
		var process = new SafeProcess(Process.GetProcessById(id));
		return TryPopulate(process) ? process : null;
	}

	public static string GetSafeFilePath(Process process)
	{
		if ((process == null) || process.HasExited)
		{
			return null;
		}

		var handle = IntPtr.Zero;
		try
		{
			handle = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, process.Id);
			if (handle == IntPtr.Zero)
			{
				return null;
			}

			var sb = new StringBuilder(260);
			if (GetModuleFileNameEx(handle, IntPtr.Zero, sb, (uint) sb.Capacity) > 0)
			{
				return sb.ToString();
			}

			return null;
		}
		finally
		{
			if (handle != IntPtr.Zero)
			{
				CloseHandle(handle);
			}
		}
	}

	/// <summary>
	/// Start an application and return the safe process representing it.
	/// </summary>
	/// <param name="filePath"> The file path for the application to start. </param>
	/// <param name="arguments"> The optional arguments for the application. </param>
	/// <returns> The safe process for the current process or null if an issue occurs. </returns>
	public static SafeProcess Start(string filePath, string arguments = null)
	{
		var info = new ProcessStartInfo { FileName = filePath, Arguments = arguments ?? string.Empty, UseShellExecute = true };
		var process = Process.Start(info);
		var response = new SafeProcess(process);

		if (TryPopulate(response))
		{
			return response;
		}

		return Wait(filePath, arguments);
	}

	/// <summary>
	/// Creates a new instance of the universal application.
	/// </summary>
	/// <param name="executablePathOrName"> The executable file path or name of the process to load. </param>
	/// <param name="packageFamilyName"> The application package family name. </param>
	/// <returns> The instance that represents the application. </returns>
	public static SafeProcess StartUniversal(string executablePathOrName, string packageFamilyName)
	{
		var shellPath = $@"shell:appsFolder\{packageFamilyName}!App";
		var info = new ProcessStartInfo { FileName = shellPath, Arguments = string.Empty, UseShellExecute = true };
		Process.Start(info);
		var watch = Stopwatch.StartNew();

		while (watch.Elapsed.TotalMilliseconds <= 10000)
		{
			var process = WhereUniversal(executablePathOrName).FirstOrDefault();
			if (process != null)
			{
				return process;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets a list of safe processes by executable path.
	/// </summary>
	/// <param name="executablePathOrName"> The executable file path or name of the processes to load. </param>
	/// <param name="arguments"> The optional arguments the process was started with. </param>
	/// <returns> The processes for the executable path. </returns>
	public static IEnumerable<SafeProcess> Where(string executablePathOrName, string arguments = null)
	{
		var hasExtension = _extensions.Any(x => executablePathOrName.EndsWith(x, StringComparison.OrdinalIgnoreCase));

		return Process.GetProcesses()
			.Select(p => new SafeProcess(p))
			.Where(TryPopulate)
			.Where(p =>
			{
				if (hasExtension && (p.FilePath != null))
				{
					return p.FilePath.Contains(executablePathOrName);
				}
				if (!hasExtension && (p.Name != null))
				{
					return p.Name.StartsWith(executablePathOrName, StringComparison.OrdinalIgnoreCase);
				}
				return false;
			});

		// Note: Filtering by arguments is not supported via System.Diagnostics.Process without WMI or P/Invoke.
	}

	/// <summary>
	/// Gets a list of safe processes filtered by provided filter.
	/// </summary>
	/// <param name="filter"> The filter to reduce collection. </param>
	/// <returns> The processes that match the filter. </returns>
	public static IEnumerable<SafeProcess> Where(Func<SafeProcess, bool> filter)
	{
		return Process.GetProcesses()
			.Select(p => new SafeProcess(p))
			.Where(TryPopulate)
			.Where(filter);
	}

	/// <summary>
	/// Gets a list of safe processes filtered by provided filter.
	/// </summary>
	/// <param name="name"> The name of the process. </param>
	/// <param name="filter"> The filter to reduce collection. </param>
	/// <returns> The processes that match the filter. </returns>
	public static IEnumerable<SafeProcess> WhereByName(string name, Func<SafeProcess, bool> filter = null)
	{
		return Process.GetProcesses()
			.Select(p => new SafeProcess(p))
			.Where(TryPopulate)
			.Where(p => (p.Name != null) && p.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
			.Where(filter ?? (_ => true));
	}

	/// <summary>
	/// Gets a list of safe processes by executable path.
	/// </summary>
	/// <param name="executablePathOrName"> The executable file path or name of the processes to load. </param>
	/// <returns> The processes for the executable path. </returns>
	public static IEnumerable<SafeProcess> WhereUniversal(string executablePathOrName)
	{
		return Process.GetProcesses()
			.Select(p => new SafeProcess(p))
			.Where(TryPopulate)
			.Where(p => (p.FilePath != null) && p.FilePath.StartsWith(executablePathOrName, StringComparison.OrdinalIgnoreCase));
	}

	internal static SafeProcess Wait(string name, Func<SafeProcess, bool> func, int timeoutInMilliseconds = 2000, int waitDelay = 10)
	{
		SafeProcess response = null;

		var result = Utility.WaitUntil(() => (response = Where(name).FirstOrDefault(func)) != null, timeoutInMilliseconds, waitDelay);
		if (!result || (response == null))
		{
			throw new Exception("Failed to find the process...");
		}

		return response;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CloseHandle(IntPtr hObject);

	[DllImport("psapi.dll", SetLastError = true)]
	private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseFileName, uint nSize);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	private static bool TryPopulate(SafeProcess safeProcess)
	{
		try
		{
			var process = safeProcess.Process;
			safeProcess.Name = process?.ProcessName;
			safeProcess.FilePath = GetSafeFilePath(process);
			safeProcess.FileName = Path.GetFileName(safeProcess.FilePath);
			safeProcess.Arguments = null;
			return !string.IsNullOrEmpty(safeProcess.FilePath);
		}
		catch
		{
			return false;
		}
	}

	private static SafeProcess Wait(string name, string arguments, int timeoutInMilliseconds = 2000, int waitDelay = 10)
	{
		SafeProcess response = null;

		var result = Utility.WaitUntil(() => (response = Where(name, arguments).FirstOrDefault()) != null, timeoutInMilliseconds, waitDelay);
		if (!result || (response == null))
		{
			throw new Exception("Failed to find the process...");
		}

		return response;
	}

	#endregion
}