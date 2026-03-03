#region References

using Cornerstone.Platforms.Windows.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;


#endregion

namespace Cornerstone.Automation.Platforms.Windows;

public class WindowsApplication : Application
{
	#region Fields

	private readonly Process _process;

	#endregion

	#region Constructors

	private WindowsApplication(Process process)
	{
		_process = process;
	}

	#endregion

	#region Properties

	public override Point Location => GetWindowLocation();

	public override Size Size => GetWindowSize();

	#endregion

	#region Methods

	public new static WindowsApplication Attach(string executablePath, string arguments = null)
	{
		var process = Where(executablePath, arguments).FirstOrDefault();
		return process == null ? null : new WindowsApplication(process);
	}

	public new static WindowsApplication AttachOrCreate(string executablePath, string arguments = null)
	{
		return Attach(executablePath, arguments)
			?? Create(executablePath, arguments);
	}

	public override Application BringToFront()
	{
		NativeGeneral.BringWindowToTop(_process.MainWindowHandle);
		return this;
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	/// <param name="timeout"> The optional timeout in milliseconds. If not provided the Timeout value will be used. </param>
	public Application Close(int? timeout = null)
	{
		try
		{
			var process = _process;

			// See if the process has exited
			if ((process == null) || process.HasExited)
			{
				return this;
			}

			// Ask the process to close gracefully and give it a chance to close.
			process.Refresh();
			process.CloseMainWindow();
			process.WaitForExit(timeout ?? (int) Timeout.TotalMilliseconds);

			if (!process.HasExited)
			{
				process.Kill();
			}
		}
		catch
		{
			// Ignore any errors
		}

		return this;
	}

	public new static WindowsApplication Create(string executablePath, string arguments = null)
	{
		// Need to start the process because it was not found
		var process = Start(executablePath, arguments);
		return process == null
			? throw new InvalidOperationException("Failed to start the application.")
			: new WindowsApplication(process);
	}

	public static (uint Pid, string Name)[] GetProcesses(string nameFilter = null)
	{
		var results = new List<(uint, string)>();
		var snapshot = NativeGeneral.CreateToolhelp32Snapshot(NativeGeneral.Th32CsSnapProcess, 0);
		if (snapshot == new nint(-1))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateToolhelp32Snapshot failed");
		}

		try
		{
			var processEntry = new NativeGeneral.ProcessEntry32
			{
				dwSize = (uint) Marshal.SizeOf<NativeGeneral.ProcessEntry32>()
			};

			if (NativeGeneral.Process32First(snapshot, ref processEntry))
			{
				do
				{
					var procName = processEntry.szExeFile;
					if ((nameFilter == null)
						|| procName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
					{
						results.Add((processEntry.th32ProcessID, procName.ToLower()));
					}
				} while (NativeGeneral.Process32Next(snapshot, ref processEntry));
			}
		}
		finally
		{
			NativeGeneral.CloseHandle(snapshot);
		}

		return results.ToArray();
	}

	/// <summary>
	/// First the main window location for the process.
	/// </summary>
	/// <returns> The location of the window. </returns>
	public Point GetWindowLocation()
	{
		var p = NativeGeneral.GetWindowPlacement(_process.MainWindowHandle);
		var location = p.rcNormalPosition.Location;

		if (p.ShowState is not (2 or 3))
		{
			return location;
		}

		NativeGeneral.GetWindowRect(_process.MainWindowHandle, out var windowsRect);
		location = new Point(windowsRect.Left + 8, windowsRect.Top + 8);

		return location;
	}

	/// <summary>
	/// Gets the size of the main window for the process.
	/// </summary>
	/// <returns> The size of the main window. </returns>
	public Size GetWindowSize()
	{
		NativeGeneral.GetWindowRect(_process.MainWindowHandle, out var data);
		return new Size(data.Right - data.Left, data.Bottom - data.Top);
	}

	/// <summary>
	/// Forcefully closes the application.
	/// </summary>
	/// <param name="timeout"> The optional timeout in milliseconds. If not provided the Timeout value will be used. </param>
	public Application Kill(int? timeout = null)
	{
		try
		{
			var process = _process;

			// See if the process has already shutdown.
			if ((process == null) || process.HasExited)
			{
				return this;
			}

			// OK, no more Mr. Nice Guy time to just kill the process.
			process.Kill();
			process.WaitForExit(timeout ?? (int) Timeout.TotalMilliseconds);
		}
		catch
		{
			// Ignore any errors
		}
		return this;
	}

	public override Application MoveWindow(int x, int y, int width, int height)
	{
		var result = NativeGeneral.MoveWindow(_process.MainWindowHandle, x, y, width, height, true);
		if (!result)
		{
			Debugger.Break();
		}
		return this;
	}

	protected override void Dispose(bool disposing)
	{
		if (AutoClose)
		{
			Close((int) Timeout.TotalMilliseconds);
			Kill((int) Timeout.TotalMilliseconds);
		}

		_process?.Dispose();
		base.Dispose(disposing);
	}

	protected override void Initialize(bool refresh = true, bool bringToFront = true)
	{
		if (refresh)
		{
			//Refresh<Element>(x => false);
			//WaitForComplete();
		}

		if (bringToFront)
		{
			BringToFront();
		}

		NativeGeneral.SetFocus(_process.MainWindowHandle);

		//WaitForComplete();
		base.Initialize(refresh, bringToFront);
	}

	private static Process Start(string executablePath, string arguments = null)
	{
		if (string.IsNullOrWhiteSpace(executablePath))
		{
			throw new ArgumentException("Executable path cannot be empty.", nameof(executablePath));
		}

		if (!File.Exists(executablePath))
		{
			throw new FileNotFoundException("Executable not found.", executablePath);
		}

		var startInfo = new ProcessStartInfo
		{
			FileName = executablePath,
			Arguments = arguments ?? "",
			UseShellExecute = true // usually what people expect
			//CreateNoWindow = true,          // ← uncomment if console should be hidden
			//WindowStyle = ProcessWindowStyle.Minimized,
		};

		var process = Process.Start(startInfo);
		if (process == null)
		{
			throw new ArgumentException("Failed to start the application.");
		}

		try
		{
			// Step 1: Wait until the process has a message loop and is idle (good general signal)
			process.WaitForInputIdle(5000);

			// Step 2: Give it a bit more time + confirm main window actually exists
			// (some apps idle briefly but show window later)
			WaitForMainWindow(process, 2500);
		}
		catch (TimeoutException)
		{
			// Timed out — app is slow, has no GUI, or never idles
		}
		catch (InvalidOperationException ex)
			when (ex.Message.Contains("message loop") || ex.Message.Contains("GUI"))
		{
			// No message loop → console app, service, background process → skip window wait
			// Note: this should never happen as the intent is to automate a GUI application.
		}
		catch
		{
			// Unexpected — log it, but don't crash caller
			// Console.WriteLine($"Wait failed: {ex.Message}");
			Debugger.Break();
		}

		return process;
	}

	private static bool WaitForMainWindow(Process process, int timeoutMs)
	{
		if (timeoutMs <= 0)
		{
			return process.MainWindowHandle != nint.Zero;
		}

		var stopwatch = Stopwatch.StartNew();

		while (stopwatch.ElapsedMilliseconds < timeoutMs)
		{
			process.Refresh();

			if (process.MainWindowHandle != nint.Zero)
			{
				return true;
			}

			if (process.HasExited)
			{
				return false;
			}

			Thread.Sleep(10);
		}

		return false;
	}

	private static IEnumerable<Process> Where(string executablePath, string arguments = null)
	{
		if (string.IsNullOrWhiteSpace(executablePath))
		{
			yield break;
		}

		var targetFileName = Path
			.GetFileName(executablePath)
			.ToLowerInvariant();

		var normalizedArgs = string.IsNullOrWhiteSpace(arguments)
			? null
			: arguments.Trim().ToLowerInvariant();

		foreach (var pd in GetProcesses())
		{
			if ((pd.Name == null) || (pd.Name != targetFileName))
			{
				continue;
			}

			string exePath = null;
			var p = Process.GetProcessById((int) pd.Pid);

			try
			{
				if (p.HasExited)
				{
					continue;
				}

				try
				{
					exePath = p.MainModule?.FileName;
				}
				catch
				{
					/* denied - normal */
				}

				if (string.IsNullOrEmpty(exePath))
				{
					continue;
				}
			}
			catch
			{
				// Ignore
				continue;
			}

			if (!string.Equals(
					Path.GetFileNameWithoutExtension(exePath),
					targetFileName,
					StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			// Optional: stricter full-path match
			// if (!PathsAreEqual(exePath, Path.GetFullPath(executablePath))) continue;

			if (normalizedArgs is null)
			{
				yield return p;
				continue;
			}

			var cmdLine = NativeProcessInfoSafe.TryGetCommandLine(p);
			if (cmdLine?.ToLowerInvariant().Contains(normalizedArgs) == true)
			{
				yield return p;
			}
		}
	}

	#endregion
}