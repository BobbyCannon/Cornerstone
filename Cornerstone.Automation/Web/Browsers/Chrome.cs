#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

#endregion

namespace Cornerstone.Automation.Web.Browsers;

/// <summary>
/// Represents an Chrome browser.
/// </summary>
public class Chrome : ChromiumBrowser
{
	#region Constants

	/// <summary>
	/// The name of the browser.
	/// </summary>
	public const string BrowserName = "chrome.exe";

	/// <summary>
	/// The debug port for the browser.
	/// </summary>
	public const int DebugPort = 9222;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the Chrome class.
	/// </summary>
	/// <param name="application"> The window of the existing browser. </param>
	/// <param name="windowsToIgnore"> The windows to ignore. Optional. </param>
	private Chrome(Application application, ICollection<IntPtr> windowsToIgnore = null, bool requireVisibleWindow = true, int? port = null)
		: base(port ?? DebugPort, application, windowsToIgnore, requireVisibleWindow)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the type of the browser.
	/// </summary>
	public override BrowserType BrowserType => BrowserType.Chrome;

	#endregion

	#region Methods

	/// <summary>
	/// Attempts to attach to an existing browser.
	/// </summary>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <returns> The browser instance or null if not found. </returns>
	public static Browser Attach(bool bringToFront = true)
	{
		var application = Application.Attach(BrowserName, GetDebugArguments(DebugPort), false, bringToFront);
		if (application == null)
		{
			return null;
		}

		var browser = new Chrome(application);
		browser.Connect();
		browser.Refresh();
		return browser;
	}

	/// <summary>
	/// Attempts to attach to an existing browser.
	/// </summary>
	/// <param name="process"> The process to attach to. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <returns> The browser instance or null if not found. </returns>
	public static Browser Attach(Process process, bool bringToFront = true)
	{
		if (process.ProcessName != BrowserName)
		{
			return null;
		}

		if (!Application.Exists(BrowserName, GetDebugArguments(DebugPort)))
		{
			throw new ArgumentException("The process was not started with the debug arguments.", nameof(process));
		}

		var application = Application.Attach(process, false, bringToFront);
		var browser = new Chrome(application);
		browser.Connect();
		browser.Refresh();
		return browser;
	}

	/// <summary>
	/// Attempts to attach to an existing browser. If one is not found then create and return a new one.
	/// </summary>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <returns> The browser instance. </returns>
	public static Browser AttachOrCreate(bool bringToFront = true)
	{
		return Attach(bringToFront) ?? Create(bringToFront);
	}

	/// <summary>
	/// Attempts to create a new browser. If one is not found then we'll make sure it was started with the
	/// remote debugger argument. If not an exception will be thrown.
	/// </summary>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <returns> The browser instance. </returns>
	public static Browser Create(bool bringToFront = true)
	{
		if (Application.Exists(BrowserName) && !Application.Exists(BrowserName, GetDebugArguments(DebugPort)))
		{
			throw new CornerstoneException("The first instance of Chrome was not started with the remote debugger enabled.");
		}

		Chrome browser;

		// See if chrome is already running
		var application = Application.Attach(BrowserName, GetDebugArguments(DebugPort), false, bringToFront);
		if (application != null)
		{
			try
			{
				Application.Create(BrowserName, GetDebugArguments(DebugPort), false, bringToFront).Dispose();
			}
			catch
			{
				// Process may close during creation causing error.
			}

			browser = new Chrome(application);
		}
		else
		{
			application = Application.Create(BrowserName, GetDebugArguments(DebugPort), false, bringToFront);
			browser = new Chrome(application);
		}

		browser.Connect();
		browser.NavigateTo("about:blank");
		return browser;
	}

	/// <summary>
	/// Starts an isolated headless Chrome with its own debug port and user-data directory.
	/// Does not attach to an existing interactive Chrome instance.
	/// </summary>
	/// <param name="port"> Remote debugging port. Defaults to 9333. </param>
	/// <returns> The browser instance. </returns>
	public static Browser CreateHeadless(int port = 0)
	{
		if (port <= 0)
		{
			port = GetFreeTcpPort();
		}

		var userDataDirectory = Path.Combine(Path.GetTempPath(), "Cornerstone.Automation", $"headless-chrome-{port}-{Guid.NewGuid():N}");
		var arguments = GetDebugArguments(port, headless: true, userDataDirectory: userDataDirectory);
		var executable = ResolveExecutable(BrowserName, Path.Combine("Google", "Chrome", "Application", BrowserName));
		var application = Application.Create(executable, arguments, refresh: false, bringToFront: false, waitForMainWindow: false);
		application.AutoClose = true;
		application.Timeout = TimeSpan.FromSeconds(15);

		var browser = new Chrome(application, requireVisibleWindow: false, port: port);
		browser.Connect();
		browser.Refresh();
		return browser;
	}

	#endregion
}