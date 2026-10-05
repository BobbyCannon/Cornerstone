#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Cornerstone.Automation.Desktop;
using Cornerstone.Automation.Desktop.Elements;
using Cornerstone.Automation.Internal;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Platforms.Windows;
using Cornerstone.Platforms.Windows.Native;
using Cornerstone.Runtime;
using Interop.UIAutomationClient;

#endregion

namespace Cornerstone.Automation;

/// <summary>
/// Represents an application that can be automated.
/// </summary>
public class Application : ElementHost
{
	#region Constants

	/// <summary>
	/// Gets the default timeout (in milliseconds).
	/// </summary>
	public const int DefaultTimeout = 60000;

	#endregion

	#region Fields

	private Window _uwpWindow;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an instance of the application.
	/// </summary>
	/// <param name="process"> The process for the application. </param>
	public Application(Process process)
		: this(new SafeProcess(process))
	{
	}

	/// <summary>
	/// Creates an instance of the application.
	/// </summary>
	/// <param name="process"> The safe process for the application. </param>
	public Application(SafeProcess process)
		: base(null, null)
	{
		Process = process;
		Application = this;
		Timeout = TimeSpan.FromMilliseconds(DefaultTimeout);
		TimeProvider = DateTimeProvider.RealTime;
		Keyboard = WindowsInput.Keyboard;
		Mouse = WindowsInput.Mouse;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets a flag to auto close the application when disposed of.
	/// </summary>
	public bool AutoClose { get; set; }

	/// <inheritdoc />
	public override Element FocusedElement => First(x => x.Focused);

	/// <summary>
	/// Gets the handle for this window.
	/// </summary>
	public IntPtr Handle => Process?.MainWindowHandle ?? IntPtr.Zero;

	/// <inheritdoc />
	public override string Id => Process?.Id.ToString();

	/// <summary>
	/// Gets the value indicating that the process is running.
	/// </summary>
	public bool IsRunning => (Process != null) && !Process.HasExited;

	/// <summary>
	/// Gets the location of the application.
	/// </summary>
	public Point Location => Process.GetWindowLocation();

	/// <inheritdoc />
	public override string Name => Handle.ToString();

	/// <summary>
	/// Gets the underlying process for this application.
	/// </summary>
	public SafeProcess Process { get; private set; }

	/// <summary>
	/// Gets the size of the application.
	/// </summary>
	public Size Size => Process.GetWindowSize();

	/// <summary>
	/// Gets or sets a flag to tell the browser to act slower. Defaults to false.
	/// </summary>
	public bool SlowMotion { get; set; }

	/// <summary>
	/// Gets or sets the time out for delay request. Defaults to 60 seconds.
	/// </summary>
	public TimeSpan Timeout { get; set; }

	/// <inheritdoc />
	protected override bool SupportsNativeIdSearch => true;

	/// <inheritdoc />
	protected override bool SupportsNativeSearch => true;

	#endregion

	#region Methods

	/// <summary>
	/// Attaches the application to an existing process.
	/// </summary>
	/// <param name="executablePath"> The path to the executable. </param>
	/// <param name="arguments"> The arguments for the executable. Arguments are optional. </param>
	/// <param name="refresh"> The setting to determine to refresh children now. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <param name="isUwp"> True if we are attaching to a UWP window. </param>
	/// <returns> The instance that represents the application. </returns>
	public static Application Attach(string executablePath, string arguments = null, bool refresh = true, bool bringToFront = true, bool isUwp = false)
	{
		var process = ProcessService.Where(executablePath, arguments).FirstOrDefault();
		return process == null ? null : Attach(process, refresh, bringToFront, isUwp);
	}

	/// <summary>
	/// Attaches the application to an existing process.
	/// </summary>
	/// <param name="handle"> The main window handle of the executable. </param>
	/// <param name="refresh"> The setting to determine to refresh children now. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <returns> The instance that represents the application. </returns>
	public static Application Attach(IntPtr handle, bool refresh = true, bool bringToFront = true)
	{
		var process = ProcessService.Where(x => x.MainWindowHandle == handle).FirstOrDefault();
		return process == null ? null : Attach(process, refresh, bringToFront);
	}

	/// <summary>
	/// Attaches the application to an existing process.
	/// </summary>
	/// <param name="process"> The process to attach to. </param>
	/// <param name="refresh"> The setting to determine to refresh children now. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <returns> The instance that represents the application. </returns>
	public static Application Attach(Process process, bool refresh = true, bool bringToFront = true)
	{
		return process == null ? null : Attach(new SafeProcess(process), refresh, bringToFront);
	}

	/// <summary>
	/// Attaches the application to an existing process.
	/// </summary>
	/// <param name="process"> The process to attach to. </param>
	/// <param name="refresh"> The setting to determine to refresh children now. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <param name="isUwp"> True if we are attaching to a UWP window. </param>
	/// <returns> The instance that represents the application. </returns>
	public static Application Attach(SafeProcess process, bool refresh = true, bool bringToFront = true, bool isUwp = false, bool waitForMainWindow = true)
	{
		var application = new Application(process);
		application.Initialize(isUwp);

		if (waitForMainWindow)
		{
			application.WaitForMainWindow();
		}

		if (refresh)
		{
			application.Refresh<Element>(x => false);
			application.WaitForComplete();
		}

		if (bringToFront)
		{
			application.BringToFront();
		}

		if (application.Handle != IntPtr.Zero)
		{
			NativeGeneral.SetFocus(application.Handle);
		}

		if (waitForMainWindow)
		{
			application.WaitForComplete();
		}

		return application;
	}

	/// <summary>
	/// Attaches the application to an existing process.
	/// </summary>
	/// <param name="executablePath"> The path to the executable. </param>
	/// <param name="arguments"> The arguments for the executable. Arguments are optional. </param>
	/// <param name="refresh"> The setting to determine to refresh children now. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <returns> The instance that represents the application. </returns>
	public static Application AttachOrCreate(string executablePath, string arguments = null, bool refresh = true, bool bringToFront = true)
	{
		return Attach(executablePath, arguments, refresh, bringToFront) ?? Create(executablePath, arguments, refresh, bringToFront);
	}

	/// <summary>
	/// Attaches or creates a new instance of the universal application.
	/// </summary>
	/// <param name="executablePath"> The path to the executable. </param>
	/// <param name="packageFamilyName"> The application package family name. </param>
	/// <returns> The instance that represents the application. </returns>
	public static Application AttachOrCreateStoreApp(string executablePath, string packageFamilyName)
	{
		return Attach(executablePath, isUwp: false) ?? CreateUniversal(executablePath, packageFamilyName);
	}

	/// <summary>
	/// Attaches or creates a new instance of the universal application.
	/// </summary>
	/// <param name="executablePath"> The path to the executable. </param>
	/// <param name="packageFamilyName"> The application package family name. </param>
	/// <returns> The instance that represents the application. </returns>
	public static Application AttachOrCreateUniversal(string executablePath, string packageFamilyName)
	{
		return Attach(executablePath, isUwp: true) ?? CreateUniversal(executablePath, packageFamilyName);
	}

	/// <summary>
	/// Brings the application to the front and makes it the top window.
	/// </summary>
	public Application BringToFront()
	{
		if (_uwpWindow != null)
		{
			_uwpWindow.BringToFront();
			return this;
		}

		Focus();
		NativeGeneral.BringWindowToTop(Handle);
		NativeGeneral.SetForegroundWindow(Handle);
		NativeGeneral.BringToTop(Handle);
		Focus();
		return this;
	}

	/// <summary>
	/// Closes the window.
	/// </summary>
	/// <param name="timeout"> The optional timeout in milliseconds. If not provided the Timeout value will be used. </param>
	public Application Close(int? timeout = null)
	{
		Process.Close(timeout ?? (int) Timeout.TotalMilliseconds);
		return this;
	}

	/// <summary>
	/// Closes all windows my name and closes them.
	/// </summary>
	/// <param name="executablePath"> The path to the executable. </param>
	/// <param name="timeout"> The timeout to wait for the application to close. </param>
	/// <param name="exceptProcessId"> The ID of the process to exclude. </param>
	public static void CloseAll(string executablePath, int timeout = DefaultTimeout, int exceptProcessId = 0)
	{
		List<SafeProcess> processes = null;
		var watch = Stopwatch.StartNew();

		do
		{
			processes?.ForEach(x => x.Dispose());
			processes = ProcessService.Where(executablePath)
				.Where(x => (exceptProcessId == 0) || (x.Id != exceptProcessId))
				.ToList();

			processes.ForEach(x => x.Close());

			if (watch.Elapsed.TotalMilliseconds >= timeout)
			{
				break;
			}
		} while ((processes.Count > 0) && !processes.All(x => x.HasExited));

		processes.ForEach(x => x.Dispose());
	}

	/// <summary>
	/// Creates a new application process.
	/// </summary>
	/// <param name="executablePath"> The path to the executable. </param>
	/// <param name="arguments"> The arguments for the executable. Arguments are optional. </param>
	/// <param name="refresh"> The flag to trigger loading to load state when creating the application. Defaults to true. </param>
	/// <param name="bringToFront"> Bring the process to the front. </param>
	/// <returns> The instance that represents an application. </returns>
	public static Application Create(string executablePath, string arguments = null, bool refresh = true, bool bringToFront = true, bool waitForMainWindow = true)
	{
		var process = ProcessService.Start(executablePath, arguments);
		if (process == null)
		{
			throw new InvalidOperationException("Failed to start the application.");
		}

		return Attach(process, refresh, bringToFront, waitForMainWindow: waitForMainWindow);
	}

	/// <summary>
	/// Creates a new instance of the universal application.
	/// </summary>
	/// <param name="executablePath"> The path to the executable. </param>
	/// <param name="packageFamilyName"> The application package family name. </param>
	/// <returns> The instance that represents the application. </returns>
	public static Application CreateUniversal(string executablePath, string packageFamilyName)
	{
		var process = ProcessService.StartUniversal(executablePath, packageFamilyName);
		return Attach(process, isUwp: true);
	}

	/// <summary>
	/// Checks to see if an application process exist by path and optional arguments.
	/// </summary>
	/// <param name="executablePath"> The path to the executable. </param>
	/// <param name="arguments"> The arguments for the executable. Arguments are optional. </param>
	/// <returns> True if the application exists and false otherwise. </returns>
	public static bool Exists(string executablePath, string arguments = null)
	{
		var processes = ProcessService.Where(executablePath, arguments).ToList();
		processes.ForEach(x => x.Dispose());
		return processes.Any();
	}

	/// <summary>
	/// Sets the application as the focused window.
	/// </summary>
	public Application Focus()
	{
		NativeGeneral.SetFocus(Handle);
		return this;
	}

	/// <summary>
	/// Returns a value indicating if the windows is in front of all other windows.
	/// </summary>
	/// <returns> </returns>
	public bool IsInFront()
	{
		var handle = NativeGeneral.GetForegroundWindow();
		return handle == Process.MainWindowHandle;
	}

	/// <summary>
	/// Forcefully closes the application.
	/// </summary>
	/// <param name="timeout"> The optional timeout in milliseconds. If not provided the Timeout value will be used. </param>
	public Application Kill(int? timeout = null)
	{
		Process.Kill(timeout ?? (int) Timeout.TotalMilliseconds);
		return this;
	}

	/// <summary>
	/// Move the window and resize it.
	/// </summary>
	/// <param name="x"> The x coordinate to move to. </param>
	/// <param name="y"> The y coordinate to move to. </param>
	public Application MoveWindow(int x, int y)
	{
		return MoveWindow(x, y, Size.Width, Size.Height);
	}

	/// <summary>
	/// Move the window and resize it.
	/// </summary>
	/// <param name="x"> The x coordinate to move to. </param>
	/// <param name="y"> The y coordinate to move to. </param>
	/// <param name="width"> The width of the window. </param>
	/// <param name="height"> The height of the window. </param>
	public Application MoveWindow(int x, int y, int width, int height)
	{
		WaitForMainWindow();
		NativeGeneral.ShowWindow(Handle, NativeGeneral.SwRestore);
		NativeGeneral.MoveWindow(Handle, x, y, width, height, true);
		this.WaitUntil(_ =>
				(Location.X == x)
				&& (Location.Y == y)
				&& (Size.Width == width)
				&& (Size.Height == height),
			2000,
			10,
			timeProvider: TimeProvider
		);
		return this;
	}

	/// <summary>
	/// Move the window and resize it.
	/// </summary>
	/// <param name="location"> The location to move to. </param>
	/// <param name="size"> The size of the window. </param>
	public Application MoveWindow(Point location, Size size)
	{
		return MoveWindow(location.X, location.Y, size.Width, size.Height);
	}

	/// <inheritdoc />
	public override ElementHost Refresh<T>(Func<T, bool> condition)
	{
		const int maxAttempts = 3;

		for (var attempt = 0; ; attempt++)
		{
			try
			{
				this.WaitUntil(x =>
				{
					Children.Clear();
					Children.AddRange(GetWindows());
					return Children.Any();
				}, (int) Timeout.TotalMilliseconds, 10, timeProvider: TimeProvider);

				return this;
			}
			catch (COMException)
			{
				if (attempt >= (maxAttempts - 1))
				{
					throw;
				}

				// A window closed while enumerating it. Brief pause then retry.
				Delay(100);
			}
		}
	}

	/// <summary>
	/// Resize the browser to the provided size.
	/// </summary>
	/// <param name="width"> The width to set. </param>
	/// <param name="height"> The height to set. </param>
	public ElementHost Resize(int width, int height)
	{
		var location = Location;
		return MoveWindow(location.X, location.Y, width, height);
	}

	/// <summary>
	/// Waits for the Process to not be busy.
	/// </summary>
	/// <param name="minimumDelay"> The minimum delay in milliseconds to wait. Defaults to 0 milliseconds. </param>
	public override ElementHost WaitForComplete(int minimumDelay = 0)
	{
		Delay(minimumDelay);
		return this;
	}

	/// <summary>
	/// Waits until the process has a main window handle.
	/// </summary>
	public Application WaitForMainWindow()
	{
		try
		{
			Process?.Process?.WaitForInputIdle(5000);
		}
		catch (InvalidOperationException)
		{
		}

		this.WaitUntil(_ =>
		{
			Process?.Process?.Refresh();
			return Handle != IntPtr.Zero;
		}, (int) Timeout.TotalMilliseconds, 50, timeProvider: TimeProvider);

		return this;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected override void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		if (AutoClose)
		{
			Close((int) Timeout.TotalMilliseconds);
			Kill((int) Timeout.TotalMilliseconds);
		}

		_uwpWindow?.Dispose();
		_uwpWindow = null;

		Process?.Dispose();
		Process = null;
	}

	/// <inheritdoc />
	protected override T FindNative<T>(Func<T, bool> condition, bool includeDescendants)
	{
		EnsureWindowsLoaded();

		foreach (var child in Children.OfType<T>())
		{
			if (condition(child))
			{
				return child;
			}
		}

		foreach (var window in Children.OfType<DesktopElement>())
		{
			var match = window.FindByCondition(condition, includeDescendants);
			if (match != null)
			{
				return match;
			}
		}

		return default;
	}

	/// <inheritdoc />
	protected override T FindNativeById<T>(string id, bool includeDescendants)
	{
		EnsureWindowsLoaded();

		foreach (var window in Children.OfType<DesktopElement>())
		{
			if (window is T typedWindow && ((window.Id == id) || (window.Name == id) || (window.AutomationId == id)))
			{
				return typedWindow;
			}

			var match = window.FindById(id, includeDescendants);
			if (match is T typed)
			{
				return typed;
			}
		}

		return default;
	}

	private void EnsureWindowsLoaded()
	{
		if (Children.Count > 0)
		{
			return;
		}

		Children.AddRange(GetWindows());
	}

	/// <summary>
	/// Gets all windows for the process.
	/// </summary>
	/// <returns> The array of windows. </returns>
	internal IEnumerable<Window> GetWindows(ICollection<IntPtr> windowsToIgnore = null)
	{
		if (_uwpWindow != null)
		{
			return [_uwpWindow];
		}

		Process.Process.Refresh();

		return DesktopAutomation.Use(automation =>
		{
			var windows = new List<Window>();
			var seenHandles = new HashSet<IntPtr>();

			void TryAdd(IntPtr handle)
			{
				if ((handle == IntPtr.Zero)
					|| (windowsToIgnore?.Contains(handle) == true)
					|| !seenHandles.Add(handle))
				{
					return;
				}

				try
				{
					var automationElement = automation.ElementFromHandle(handle);
					var element = DesktopElement.Create(automationElement, this, null);
					if (element is Window window)
					{
						windows.Add(window);
					}
				}
				catch
				{
					seenHandles.Remove(handle);
				}
			}

			TryAdd(Process.MainWindowHandle);

			foreach (var handle in EnumerateProcessWindowHandles())
			{
				TryAdd(handle);
			}

			return windows;
		});
	}

	private IEnumerable<IntPtr> EnumerateProcessWindowHandles()
	{
		var handles = new List<IntPtr>();

		try
		{
			foreach (ProcessThread thread in Process.Process.Threads)
			{
				using (thread)
				{
					NativeGeneral.EnumThreadWindows(thread.Id, (hWnd, lParam) =>
					{
						handles.Add(hWnd);
						return true;
					}, IntPtr.Zero);
				}
			}
		}
		catch
		{
			return new IntPtr[0];
		}

		return handles;
	}

	private void Initialize(bool isUwp)
	{
		if (isUwp)
		{
			// Detect UWP
			var windowHandle = InternalExtensions.Refresh(Process, TimeSpan.FromMilliseconds(150));

			if (windowHandle != IntPtr.Zero)
			{
				var element = DesktopAutomation.Use(automation => automation.ElementFromHandle(windowHandle));
				_uwpWindow = new Window(element, this, null);
			}
		}

		if (Process != null)
		{
			//Process.Exited += (sender, args) => OnClosed();
			Process.Process.EnableRaisingEvents = true;
		}
	}

	#endregion
}