#region References

using System;
using System.Drawing;
#if WINDOWS
using Cornerstone.Automation.Platforms.Windows;
#endif

#endregion

namespace Cornerstone.Automation;

public abstract class Application : IDisposable
{
	#region Constants

	/// <summary>
	/// Gets the default timeout (in milliseconds).
	/// </summary>
	public const int DefaultTimeout = 60000;

	#endregion

	#region Constructors

	protected Application()
	{
		Timeout = TimeSpan.FromMilliseconds(DefaultTimeout);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets a flag to auto close the application when disposed of.
	/// </summary>
	public bool AutoClose { get; set; }

	/// <summary>
	/// Gets the location of the application.
	/// </summary>
	public abstract Point Location { get; }

	/// <summary>
	/// Gets the size of the application.
	/// </summary>
	public abstract Size Size { get; }

	/// <summary>
	/// Gets or sets the timeout for delay request. Defaults to 60 seconds.
	/// </summary>
	public TimeSpan Timeout { get; set; }

	#endregion

	#region Methods

	public static Application Attach(string executablePath, string arguments = null)
	{
		#if WINDOWS
		var response = WindowsApplication.Attach(executablePath, arguments);
		response.Initialize();
		return response;
		#else
		throw new NotSupportedException();
		#endif
	}

	public static Application AttachOrCreate(string executablePath, string arguments = null)
	{
		#if WINDOWS
		var response = WindowsApplication.AttachOrCreate(executablePath, arguments);
		response.Initialize();
		return response;
		#else
		throw new NotSupportedException();
		#endif
	}

	/// <summary>
	/// Brings the application to the front and makes it the top window.
	/// </summary>
	public virtual Application BringToFront()
	{
		return this;
	}

	public static Application Create(string executablePath, string arguments = null)
	{
		#if WINDOWS
		var response = WindowsApplication.Create(executablePath, arguments);
		response.Initialize();
		return response;
		#else
		throw new NotSupportedException();
		#endif
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Move the window and resize it.
	/// </summary>
	/// <param name="x"> The x coordinate to move to. </param>
	/// <param name="y"> The y coordinate to move to. </param>
	/// <param name="width"> The width of the window. </param>
	/// <param name="height"> The height of the window. </param>
	public abstract Application MoveWindow(int x, int y, int width, int height);

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
	}

	/// <summary>
	/// Initialize the application on create / attach.
	/// </summary>
	/// <param name="refresh"> The setting to determine to refresh children now. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	protected virtual void Initialize(bool refresh = true, bool bringToFront = true)
	{
	}

	#endregion
}