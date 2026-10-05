#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Automation.Desktop.Elements;
using Cornerstone.Automation.Internal;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;


#endregion

namespace Cornerstone.Automation.Web.Browsers;

/// <summary>
/// Represents a chromium based browser.
/// </summary>
public abstract class ChromiumBrowser : Browser
{
	#region Constants

	/// <summary>
	/// The debugging argument for starting the browser.
	/// "C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9223 --user-data-dir="C:\temp\chrome-debug" --profile-directory=Cornerstone --remote-allow-origins=*
	/// </summary>
	private const string DebugArgument = "--remote-debugging-port={0} --user-data-dir=\"{1}\" --profile-directory=Cornerstone --remote-allow-origins=* --enable-automation --no-first-run --disable-extensions --disable-features=PrivacySandboxSettings4";

	#endregion

	#region Fields

	private static readonly HttpClient _client;
	private readonly int _debugPort;
	private static readonly JsonSerializerOptions JsonOptions;
	private volatile bool _pageReady;
	private bool _pageDomainEnabled;
	private int _requestId;
	private ClientWebSocket _socket;
	private readonly ConcurrentDictionary<string, JsonNode> _socketResponses;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the Chrome class.
	/// </summary>
	/// <param name="port"> The debug port number for the browser. </param>
	/// <param name="application"> The window of the existing browser. </param>
	/// <param name="windowsToIgnore"> The windows to ignore. Optional. </param>
	protected ChromiumBrowser(int port, Application application, ICollection<IntPtr> windowsToIgnore = null, bool requireVisibleWindow = true)
		: base(application, windowsToIgnore, requireVisibleWindow)
	{
		_debugPort = port;
		_requestId = 0;
		_socketResponses = new ConcurrentDictionary<string, JsonNode>();
	}

	static ChromiumBrowser()
	{
		_client = new HttpClient();
		JsonOptions = Serializer.CreateOptions(o => o.WriteIndented = true);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Navigates the browser to the provided URI.
	/// </summary>
	/// <param name="uri"> The URI to navigate to. </param>
	protected override void BrowserNavigateTo(string uri)
	{
		EnsurePageDomain();
		_pageReady = false;

		var request = new Request
		{
			Id = _requestId++,
			Method = "Page.navigate",
			Params = new JsonObject { ["url"] = uri }
		};

		if (Window != null)
		{
			Application.Focus();
			Window.Focus();
		}

		SendRequestAndReadResponse(request, x => x.GetInt("id") == request.Id);
		Utility.WaitUntil(() => _pageReady || IsDocumentReady(), (int) Application.Timeout.TotalMilliseconds, 25, timeProvider: TimeProvider);
	}

	/// <summary>
	/// Connect to the Chrome browser debugger port.
	/// </summary>
	/// <exception cref="Exception"> All debugging sessions are taken. </exception>
	protected void Connect()
	{
		var sessions = new List<RemoteSessionsResponse>();
		RemoteSessionsResponse session = null;

		Utility.WaitUntil(() =>
		{
			try
			{
				sessions.AddRange(GetAvailableSessions());
				if (sessions.Count == 0)
				{
					throw new CornerstoneException("All debugging sessions are taken.");
				}

				session = sessions.FirstOrDefault(IsDebuggablePage);
				return session != null;
			}
			catch (WebException)
			{
				return false;
			}
		}, Application.Timeout, 250, timeProvider: TimeProvider);

		if (session == null)
		{
			throw new CornerstoneException("Could not find a valid debugger enabled page. Make sure you close the debugger tools.");
		}

		var sessionWsEndpoint = new Uri(session.WebSocketDebuggerUrl);
		_socket = new ClientWebSocket();

		if (!_socket.ConnectAsync(sessionWsEndpoint, CancellationToken.None).Wait(Application.Timeout))
		{
			throw new CornerstoneException("Failed to connect to the server.");
		}

		Task.Run(() =>
		{
			while (_socket != null)
			{
				if (!ReadResponseAsync())
				{
					if (_socket == null)
					{
						break;
					}

					_socket?.Dispose();
					_socket = new ClientWebSocket();

					if (!_socket.ConnectAsync(sessionWsEndpoint, CancellationToken.None).Wait(Application.Timeout))
					{
						throw new CornerstoneException("Failed to connect to the server.");
					}

					_pageDomainEnabled = false;
					EnsurePageDomain();
				}
			}
		});

		EnsurePageDomain();
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_socket?.Dispose();
			_socket = null;
		}

		base.Dispose(disposing);
	}

	/// <summary>
	/// Execute JavaScript code in the current document.
	/// </summary>
	/// <param name="script"> The code script to execute. </param>
	/// <param name="expectResponse"> The script will return response. </param>
	/// <returns> The response from the execution. </returns>
	public override Task<string> ExecuteJavaScriptAsync(string script)
	{
		return Task.FromResult(EvaluateJavaScript(script));
	}

	private string EvaluateJavaScript(string script, bool expectResponse = true)
	{
		var request = new Request
		{
			Id = _requestId++,
			Method = "Runtime.evaluate",
			Params = new JsonObject
			{
				["expression"] = script,
				["objectGroup"] = "console",
				["includeCommandLineAPI"] = true,
				["returnByValue"] = expectResponse
			}
		};

		var data = SendRequestAndReadResponse(request, x => x.GetInt("id") == request.Id);
		if (data.Contains(CornerstoneNotDefinedMessage))
		{
			return CornerstoneNotDefinedMessage;
		}

		var response = data.AsJsonNode();
		var result = response?["result"]?["result"];
		if (result == null)
		{
			return data;
		}

		if (result is JsonValue jsonValue)
		{
			return jsonValue.AsString();
		}

		if ((result is JsonObject jsonObject) && jsonObject.TryGetPropertyValue("value", out var value))
		{
			if (value is JsonArray or JsonObject)
			{
				return value.ToJsonString();
			}

			return value.AsString();
		}

		return result.ToJsonString(JsonOptions);
	}

	/// <summary>
	/// Reads the current URI directly from the browser.
	/// </summary>
	/// <returns> The current URI that was read from the browser. </returns>
	protected override string GetBrowserUri()
	{
		//LogManager.Write("First browser's URI.", LogLevel.Verbose);

		var request = new Request
		{
			Id = _requestId++,
			Method = "DOM.getDocument"
		};

		var response = SendRequestAndReadResponse(request, x => x.GetInt("id") == request.Id);
		var documentUrl = response.AsJsonNode()?["result"]?["root"]?.GetString("documentURL");
		if (string.IsNullOrEmpty(documentUrl))
		{
			throw new CornerstoneException("Failed to get the URI.");
		}

		return documentUrl;
	}

	/// <summary>
	/// Gets the debug arguments for the chromium browser.
	/// </summary>
	/// <param name="port"> The debug port number for the browser. </param>
	/// <returns> The command arguments for the browser. </returns>
	private static bool IsDebuggablePage(RemoteSessionsResponse session)
	{
		return (session != null)
			&& !string.IsNullOrWhiteSpace(session.WebSocketDebuggerUrl)
			&& !string.IsNullOrWhiteSpace(session.Url)
			&& !session.Url.Contains("omnibox", StringComparison.OrdinalIgnoreCase);
	}

	protected static int GetFreeTcpPort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint) listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	protected static string GetDebugArguments(int port, bool headless = false, string userDataDirectory = null)
	{
		var tempPath = userDataDirectory ?? Path.Combine(Path.GetTempPath(), "Cornerstone.Automation");
		Directory.CreateDirectory(tempPath);
		var arguments = string.Format(DebugArgument, port, tempPath);
		if (headless)
		{
			arguments += " --headless=new --disable-gpu --hide-scrollbars --mute-audio about:blank";
		}

		return arguments;
	}

	protected static string ResolveExecutable(string fileName, string relativeInstallPath)
	{
		if (File.Exists(fileName))
		{
			return fileName;
		}

		var roots = new[]
		{
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
		};

		foreach (var root in roots)
		{
			if (string.IsNullOrWhiteSpace(root))
			{
				continue;
			}

			var candidate = Path.Combine(root, relativeInstallPath);
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		return fileName;
	}

	/// <inheritdoc />
	protected override IScrollableElement GetScrollableElement()
	{
		return Application.FirstOrDefault<Document>(x => x.IsScrollable);
	}

	private List<RemoteSessionsResponse> GetAvailableSessions()
	{
		var location = $"http://127.0.0.1:{_debugPort}/json";
		var data = _client.GetStringAsync(location).Result;
		var sessions = data.FromJson<List<RemoteSessionsResponse>>();

		if (sessions == null)
		{
			throw new CornerstoneException("Could not get available sessions.");
		}

		sessions.RemoveAll(x => x.Url.StartsWith("chrome-extension"));
		sessions.RemoveAll(x => x.Url.StartsWith("chrome-devtools"));
		sessions.RemoveAll(x => x.Url.StartsWith("chrome-untrusted"));
		sessions.RemoveAll(x => x.Url.Contains("omnibox", StringComparison.OrdinalIgnoreCase));
		sessions.RemoveAll(x => x.Title.Contains("https://ntp.msn.com/edge/ntp/service-worker.js"));

		if (sessions.Count > 1)
		{
			Debug.WriteLine("\r\nToo many sessions?");

			sessions.ForEach(x =>
			{
				Debug.WriteLine(x.Title);
				Debug.WriteLine(x.Url);
				Debug.WriteLine(x.DevtoolsFrontendUrl);
				Debug.WriteLine(x.WebSocketDebuggerUrl);
			});
		}

		return sessions;
	}

	private bool ReadResponse(int id)
	{
		return Utility.WaitUntil(() => _socketResponses.ContainsKey(id.ToString()), Application.Timeout, 1, timeProvider: TimeProvider);
	}

	private bool ReadResponseAsync()
	{
		var buffer = new ArraySegment<byte>(new byte[131072]);
		var builder = new StringBuilder();

		try
		{
			WebSocketReceiveResult result;

			if ((_socket.State == WebSocketState.Aborted) || (_socket.State == WebSocketState.Closed))
			{
				return false;
			}

			do
			{
				result = _socket.ReceiveAsync(buffer, CancellationToken.None).Result;
				var data = new byte[result.Count];
				Array.Copy(buffer.Array, 0, data, 0, data.Length);
				builder.Append(Encoding.UTF8.GetString(data));
			} while (!result.EndOfMessage);

			var response = builder.ToString().AsJsonNode();
			if (response?["method"] != null)
			{
				var method = response.GetString("method");
				if ((method == "Page.loadEventFired")
					|| (method == "Page.frameStoppedLoading")
					|| (method == "Page.domContentEventFired"))
				{
					_pageReady = true;
				}

				return true;
			}

			if (response?["id"] != null)
			{
				var id = response.GetString("id") ?? response.GetInt("id").ToString();
				_socketResponses.TryAdd(id, response);
			}

			return true;
		}
		catch (ObjectDisposedException)
		{
			return false;
		}
		catch (AggregateException)
		{
			return false;
		}
		catch
		{
			return false;
		}
	}

	private void EnsurePageDomain()
	{
		if (_pageDomainEnabled)
		{
			return;
		}

		var request = new Request
		{
			Id = _requestId++,
			Method = "Page.enable"
		};
		SendRequestAndReadResponse(request, x => x.GetInt("id") == request.Id);
		_pageDomainEnabled = true;
	}

	private bool IsDocumentReady()
	{
		try
		{
			var state = EvaluateJavaScript("document.readyState")?.Trim('"');
			return (state == "complete") || (state == "interactive");
		}
		catch
		{
			return false;
		}
	}

	private bool SendRequest<T>(T request)
	{
		var json = request.ToJson();
		//LogManager.Write("Debugger Request: " + json, LogLevel.Verbose);
		var jsonBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
		return _socket.SendAsync(jsonBuffer, WebSocketMessageType.Text, true, CancellationToken.None).Wait(Application.Timeout);
	}

	private string SendRequestAndReadResponse(Request request, Func<JsonNode, bool> action)
	{
		if (SendRequest(request) && ReadResponse(request.Id))
		{
			_socketResponses.TryRemove(request.Id.ToString(), out var response);
			return response == null ? string.Empty : response.ToJsonString();
		}

		request.Id++;
		return SendRequestAndReadResponse(request, action);
	}

	#endregion

	#region Classes

	[Serializable]
	[DataContract]
	internal class RemoteSessionsResponse
	{
		#region Properties

		[DataMember]
		public string DevtoolsFrontendUrl { get; set; }

		[DataMember]
		public string FaviconUrl { get; set; }

		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string ThumbnailUrl { get; set; }

		[DataMember]
		public string Title { get; set; }

		[DataMember]
		public string Url { get; set; }

		[DataMember]
		public string WebSocketDebuggerUrl { get; set; }

		#endregion
	}

	private class Request
	{
		#region Properties

		public int Id { get; set; }
		public string Method { get; set; }
		public object Params { get; set; }

		#endregion
	}

	#endregion
}