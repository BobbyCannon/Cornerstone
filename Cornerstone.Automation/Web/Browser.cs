#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Automation.Web.Browsers;
using Cornerstone.Automation.Desktop.Elements;
using Cornerstone.Automation.Internal;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using System.Text.Json.Nodes;

#endregion

namespace Cornerstone.Automation.Web;

/// <summary>
/// Out-of-process browser host (Chrome, Edge, Firefox) on the web JS model.
/// </summary>
public abstract class Browser : WebAutomation, IScrollableElement, IDisposable
{
	#region Constants

	/// <summary>
	/// Gets the missing Cornerstone error.
	/// </summary>
	public const string CornerstoneNotDefinedMessage = "Cornerstone is not defined";

	/// <summary>
	/// Gets the missing Cornerstone error.
	/// </summary>
	[Obsolete("Use CornerstoneNotDefinedMessage.")]
	public const string SpeedyNotDefinedMessage = CornerstoneNotDefinedMessage;

	#endregion

	#region Fields

	private string _lastUri;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the Browser class.
	/// </summary>
	protected Browser(Application application, ICollection<IntPtr> windowsToIgnore = null, bool requireVisibleWindow = true)
	{
		Application = application ?? throw new ArgumentNullException(nameof(application));
		AutoRefresh = true;
		TimeProvider = application.TimeProvider ?? DateTimeProvider.RealTime;

		if (!requireVisibleWindow)
		{
			return;
		}

		var watch = Stopwatch.StartNew();

		do
		{
			Window?.Dispose();
			Window = application.GetWindows(windowsToIgnore).FirstOrDefault();
		} while (Window is not { Visible: true } && (watch.Elapsed <= Timeout));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the current active element.
	/// </summary>
	public WebElement ActiveElement
	{
		get
		{
			var id = ExecuteScript("document.activeElement.id");
			if (string.IsNullOrWhiteSpace(id))
			{
				return null;
			}

			return FirstOrDefaultDescendants(x => x.Id == id);
		}
	}

	/// <summary>
	/// The desktop process host for this browser window.
	/// </summary>
	public Application Application { get; }

	/// <summary>
	/// Gets or sets a flag to auto close the browser when disposed of. Defaults to false.
	/// </summary>
	public bool AutoClose
	{
		get => Application.AutoClose;
		set => Application.AutoClose = value;
	}

	/// <summary>
	/// Gets the type of the browser.
	/// </summary>
	public abstract BrowserType BrowserType { get; }

	/// <inheritdoc />
	public double HorizontalScrollPercent => (ScrollableElement ??= GetScrollableElement())?.HorizontalScrollPercent ?? 0;

	/// <summary>
	/// Gets the ID of the browser.
	/// </summary>
	public override string Id => (Application?.Handle.ToInt32() ?? 0).ToString();

	/// <summary>
	/// Gets the value indicating if the browser is closed.
	/// </summary>
	public bool IsClosed => Application is not { IsRunning: true };

	/// <inheritdoc />
	public bool IsScrollable => (ScrollableElement ??= GetScrollableElement())?.IsScrollable ?? false;

	/// <summary>
	/// Gets the location of the browser.
	/// </summary>
	public Point Location => Application.Location;

	/// <summary>
	/// Gets the raw HTML of the page.
	/// </summary>
	public virtual string RawHtml => GetHtml();

	/// <summary>
	/// Gets the size of the browser.
	/// </summary>
	public Size Size => Application.Size;

	/// <summary>
	/// Gets or sets the timeout for delay request. Defaults to 60 seconds.
	/// </summary>
	public TimeSpan Timeout
	{
		get => Application.Timeout;
		set => Application.Timeout = value;
	}

	/// <summary>
	/// Gets the URI of the current page.
	/// </summary>
	public string Uri => GetBrowserUri();

	/// <inheritdoc />
	public double VerticalScrollPercent => (ScrollableElement ??= GetScrollableElement())?.VerticalScrollPercent ?? 0;

	/// <summary>
	/// The main windows for the browser.
	/// </summary>
	public Window Window { get; private set; }

	/// <summary>
	/// Gets the scrollable element to
	/// </summary>
	protected IScrollableElement ScrollableElement { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Attach browsers for each type provided.
	/// </summary>
	/// <param name="type"> The type of the browser to attach to. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	public static IEnumerable<Browser> AttachBrowsers(BrowserType type = BrowserType.All, bool bringToFront = true)
	{
		var response = new List<Browser>();

		if ((type & BrowserType.Chrome) == BrowserType.Chrome)
		{
			response.Add(Chrome.Attach(bringToFront));
		}

		if ((type & BrowserType.Edge) == BrowserType.Edge)
		{
			response.Add(Edge.Attach(bringToFront));
		}

		if ((type & BrowserType.Firefox) == BrowserType.Firefox)
		{
			response.Add(Firefox.Attach(bringToFront));
		}

		return response;
	}

	/// <summary>
	/// Attach or create browsers for each type provided.
	/// </summary>
	/// <param name="type"> The type of the browser to attach to or create. </param>
	public static IEnumerable<Browser> AttachOrCreate(BrowserType type = BrowserType.All)
	{
		var response = new List<Browser>();

		if ((type & BrowserType.Chrome) == BrowserType.Chrome)
		{
			var chrome = Chrome.AttachOrCreate();
			response.Add(chrome);
		}

		if ((type & BrowserType.Edge) == BrowserType.Edge)
		{
			var edge = Edge.AttachOrCreate();
			response.Add(edge);
		}

		if ((type & BrowserType.Firefox) == BrowserType.Firefox)
		{
			var firefox = Firefox.AttachOrCreate();
			response.Add(firefox);
		}

		return response;
	}

	/// <summary>
	/// Attach process as a browser.
	/// </summary>
	/// <param name="process"> The process of the browser to attach to. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	/// <returns> The browser if successfully attached or otherwise null. </returns>
	public static Browser AttachToBrowser(Process process, bool bringToFront = true)
	{
		return Chrome.Attach(process, bringToFront) ?? Edge.Attach(process, bringToFront) ?? Firefox.Attach(process, bringToFront);
	}

	/// <summary>
	/// Brings the application to the front and makes it the top window.
	/// </summary>
	public virtual Browser BringToFront()
	{
		Application.BringToFront();
		return this;
	}

	/// <summary>
	/// Releases the browser debugger connection and optionally closes the process.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Closes the browser.
	/// </summary>
	public void Close()
	{
		Application?.Close();
	}

	/// <summary>
	/// Closes all browsers of the provided type.
	/// </summary>
	/// <param name="type"> The type of the browser to close. </param>
	public static void CloseBrowsers(BrowserType type = BrowserType.All)
	{
		if ((type & BrowserType.Chrome) == BrowserType.Chrome)
		{
			Application.CloseAll(Chrome.BrowserName);
		}

		if ((type & BrowserType.Edge) == BrowserType.Edge)
		{
			Application.CloseAll(Edge.BrowserName);
		}

		if ((type & BrowserType.Firefox) == BrowserType.Firefox)
		{
			Application.CloseAll(Firefox.BrowserName);
		}
	}

	/// <summary>
	/// Create browsers for each type provided.
	/// </summary>
	/// <param name="type"> The type of the browser to create. </param>
	/// <param name="bringToFront"> The option to bring the application to the front. This argument is optional and defaults to true. </param>
	public static IEnumerable<Browser> CreateBrowsers(BrowserType type = BrowserType.All, bool bringToFront = true)
	{
		var response = new List<Browser>();

		if ((type & BrowserType.Chrome) == BrowserType.Chrome)
		{
			response.Add(Chrome.Create(bringToFront));
		}

		if ((type & BrowserType.Edge) == BrowserType.Edge)
		{
			response.Add(Edge.Create());
		}

		if ((type & BrowserType.Firefox) == BrowserType.Firefox)
		{
			response.Add(Firefox.Create(bringToFront));
		}

		return response;
	}

	/// <summary>
	/// Execute JavaScript code in the current document.
	/// </summary>
	/// <param name="script"> The script to run. </param>
	/// <param name="expectResponse"> The script will return response. </param>
	/// <returns> The response from the script. </returns>
	public string ExecuteScript(string script, bool expectResponse = true)
	{
		var response = ExecuteJavaScriptAsync(script).AwaitResults();

		if (response?.Contains(CornerstoneNotDefinedMessage) == true)
		{
			InjectTestScriptAsync().AwaitResults();
			return ExecuteJavaScriptAsync(script).AwaitResults();
		}

		return response;
	}

	/// <summary>
	/// Sets the browser as the focused window.
	/// </summary>
	public Browser Focus()
	{
		Window.Focus();
		Application.Focus();
		return this;
	}

	/// <summary>
	/// Process an action against a new instance of each browser type provided at the same time (parallel).
	/// </summary>
	/// <param name="action"> The action to perform against each browser. </param>
	/// <param name="type"> The type of the browser to process against. </param>
	/// <param name="timeout"> The timeout to wait for browsers to complete </param>
	public static void ForAllBrowsers(Action<Browser> action, BrowserType type, TimeSpan timeout)
	{
		var types = type.GetTypeArray();
		var tasks = types.Select(x => Task.Run(() =>
			{
				using var browser = AttachOrCreate(x).First();
				browser.Application.Timeout = timeout;
				action(browser);
			})
		).ToArray();

		// Add 20% time to ensure the thread doesn't time out first.
		var milliseconds = (int) (timeout.TotalMilliseconds * 1.2);
		if (milliseconds < 10000)
		{
			milliseconds = 10000;
		}

		var wait = Task.WaitAll(tasks, milliseconds);
		if (wait)
		{
			return;
		}

		// If we did timeout then capture the exceptions and throw a timeout error
		var errors = tasks.Where(x => x.Exception != null).Select(x => x.Exception.Message);
		var error = string.Join(Environment.NewLine, errors);

		if (string.IsNullOrWhiteSpace(error))
		{
			throw new CornerstoneException($"{nameof(ForAllBrowsers)} has timed out.");
		}

		throw new CornerstoneException($"{nameof(ForAllBrowsers)} has timed out.{error}");
	}

	/// <summary>
	/// Process an action against a new instance of each browser type provided (serial).
	/// </summary>
	/// <param name="action"> The action to perform against each browser. </param>
	/// <param name="type"> The type of the browser to process against. </param>
	public static void ForEachBrowser(Action<Browser> action, BrowserType type = BrowserType.All)
	{
		var browsers = AttachOrCreate(type);
		foreach (var browser in browsers)
		{
			using (browser)
			{
				action(browser);
			}
		}
	}

	/// <summary>
	/// Gets the HTML displayed in the browser.
	/// </summary>
	public string GetHtml()
	{
		return ExecuteScript("document.documentElement.outerHTML")
			.Replace("<input id=\"cornerstoneResult\" type=\"hidden\" value=\"\">", "")
			.Replace("<input id=\"speedyResult\" type=\"hidden\" value=\"\">", "");
	}

	

	/// <summary>
	/// Move the window and resize it.
	/// </summary>
	/// <param name="x"> The x coordinate to move to. </param>
	/// <param name="y"> The y coordinate to move to. </param>
	public virtual Browser MoveWindow(int x, int y)
	{
		Application.Focus();
		Application.MoveWindow(x, y);
		Window.Focus();
		Window.Move(x, y);
		return this;
	}

	/// <summary>
	/// Move the window and resize it.
	/// </summary>
	/// <param name="x"> The x coordinate to move to. </param>
	/// <param name="y"> The y coordinate to move to. </param>
	/// <param name="width"> The width of the window. </param>
	/// <param name="height"> The height of the window. </param>
	public virtual Browser MoveWindow(int x, int y, int width, int height)
	{
		Application.Focus();
		Application.MoveWindow(x, y, width, height);
		Window.Focus();
		Window.Move(x, y, width, height);
		return this;
	}

	/// <summary>
	/// Move the window and resize it.
	/// </summary>
	/// <param name="location"> The location to move to. </param>
	/// <param name="size"> The size of the window. </param>
	public virtual Browser MoveWindow(Point location, Size size)
	{
		Application.Focus();
		Application.MoveWindow(location, size);
		Window.Focus();
		Window.Move(location, size);
		return this;
	}

	/// <summary>
	/// Navigates the browser to the provided URI.
	/// </summary>
	/// <param name="uri"> The URI to navigate to. </param>
	/// <param name="expectedUri"> The expected URI to navigate to. </param>
	public Browser NavigateTo(string uri, string expectedUri = null)
	{
		if (uri == null)
		{
			throw new ArgumentNullException(nameof(uri));
		}

		_lastUri = Uri;

		try
		{
			//LogManager.Write("Navigating to " + expectedUri + ".", LogLevel.Verbose);
			BrowserNavigateTo(uri);

			if (_lastUri.Equals(uri, StringComparison.OrdinalIgnoreCase))
			{
				Refresh();
				return this;
			}

			WaitForNavigation(expectedUri ?? uri);
			return this;
		}
		finally
		{
			_lastUri = uri;
		}
	}

	/// <inheritdoc />
	public void Refresh()
	{
		RefreshAsync().AwaitResults();
	}

	public override async Task RefreshAsync<T>(Func<T, bool> condition, System.Threading.CancellationToken? cancellationToken = null)
	{
		WaitForComplete();
		await base.RefreshAsync(condition, cancellationToken);
	}

	/// <summary>
	/// Removes the element from the page. * Experimental
	/// </summary>
	/// <param name="element"> The element to remove. </param>
	public bool RemoveElement(WebElement element)
	{
		ExecuteJavaScriptAsync($"Cornerstone.removeElement(\'{element.Id}\',{element.GetFrameIdInsert()});").AwaitResults();
		return Children.Remove(element);
	}

	/// <summary>
	/// Removes an attribute from an element.
	/// </summary>
	/// <param name="element"> The element to remove the attribute from. </param>
	/// <param name="name"> The name of the attribute to remove. </param>
	public void RemoveElementAttribute(WebElement element, string name)
	{
		ExecuteJavaScriptAsync($"Cornerstone.removeElementAttribute(\'{element.Id}\',{element.GetFrameIdInsert()},\'{name}\');").AwaitResults();
	}

	/// <summary>
	/// Resize the browser to the provided size.
	/// </summary>
	/// <param name="width"> The width to set. </param>
	/// <param name="height"> The height to set. </param>
	public Browser Resize(int width, int height)
	{
		Application.Resize(width, height);
		return this;
	}

	/// <summary>
	/// Scroll the browser window.
	/// </summary>
	/// <param name="horizontalPercent"> The percentage to scroll horizontally. </param>
	/// <param name="verticalPercent"> The percentage to scroll vertically. </param>
	public void Scroll(double horizontalPercent, double verticalPercent)
	{
		try
		{
			var e = ScrollableElement ??= GetScrollableElement();
			e?.Scroll(horizontalPercent, verticalPercent);
		}
		catch (Exception)
		{
			Application.Children.Clear();

			var e = ScrollableElement = GetScrollableElement();
			e?.Scroll(horizontalPercent, verticalPercent);
		}
	}

	/// <summary>
	/// Sets the HTML to display in the browser.
	/// </summary>
	/// <param name="html"> The HTML to apply to the browser. </param>
	public void SetHtml(string html)
	{
		var innerHtml = CleanupScriptForJavascriptString(html);
		ExecuteScript($"document.open(); document.write('{innerHtml}'); document.close();", false);
	}

	/// <inheritdoc />
	public Browser WaitForComplete(int minimumDelay = 0)
	{
		this.WaitUntil(_ => ExecuteJavaScriptAsync("document.readyState === 'complete'").AwaitResults().Equals("true", StringComparison.OrdinalIgnoreCase), 1000, 10, timeProvider: TimeProvider);
		Application?.WaitForComplete(minimumDelay);
		return this;
	}

	/// <summary>
	/// Wait for the browser page to redirect to a provided URI.
	/// </summary>
	/// <param name="uri"> The expected URI to land on. Defaults to empty string if not provided. </param>
	/// <param name="timeout"> The timeout before giving up on the redirect. Defaults to Timeout if not provided. </param>
	/// <param name="refresh"> An optional flag to refresh the child elements. Defaults to true. </param>
	public Browser WaitForNavigation(string uri = null, TimeSpan? timeout = null, bool refresh = true)
	{
		timeout ??= Application.Timeout;

		if (uri == null)
		{
			if (!this.WaitUntil(_ => Uri != _lastUri, (int) timeout.Value.TotalMilliseconds, 10))
			{
				throw new CornerstoneException($"Browser never completed navigated away from {Uri}.");
			}
		}
		else
		{
			string alternateUri = null;
			if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
			{
				alternateUri = $"https://{uri.Substring(7)}";
			}
			else if (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				alternateUri = $"http://{uri.Substring(8)}";
			}

			if (!this.WaitUntil(_ =>
					{
						var current = Uri;
						return current.StartsWith(uri, StringComparison.OrdinalIgnoreCase)
							|| ((alternateUri != null) && current.StartsWith(alternateUri, StringComparison.OrdinalIgnoreCase));
					},
					(int) timeout.Value.TotalMilliseconds, 10))
			{
				throw new CornerstoneException($"Browser never completed navigation to {uri}. Current URI is {Uri}.");
			}
		}

		if (refresh)
		{
			Refresh();
		}

		return this;
	}

	/// <summary>
	/// Browser implementation of navigate to
	/// </summary>
	/// <param name="uri"> The URI to navigate to. </param>
	protected abstract void BrowserNavigateTo(string uri);

	protected override string GetUri()
	{
		return GetBrowserUri();
	}

	protected override void NavigateTo(string uri)
	{
		BrowserNavigateTo(uri);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		if (AutoClose)
		{
			Application?.Close();
		}

		Application?.Dispose();
	}

	/// <summary>
	/// Reads the current URI directly from the browser.
	/// </summary>
	/// <returns> The current URI that was read from the browser. </returns>
	protected abstract string GetBrowserUri();

	/// <summary>
	/// Gets the scrollable element to scroll browser content.
	/// </summary>
	/// <returns> </returns>
	protected abstract IScrollableElement GetScrollableElement();

	#endregion
}