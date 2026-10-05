#region References

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Automation.Web;

/// <summary>
/// Loopback HTTP server for automation tests. Serves mapped pages so the browser can
/// navigate with a real http origin (links, location, cookies, fetch) without a deployed site.
/// </summary>
public sealed class HtmlFixtureServer : IDisposable
{
	#region Fields

	private readonly ConcurrentDictionary<string, FixtureResource> _resources;
	private readonly HttpListener _listener;
	private readonly CancellationTokenSource _cancellation;
	private readonly Task _listenTask;
	private bool _disposed;

	#endregion

	#region Constructors

	private HtmlFixtureServer(int port)
	{
		Port = port;
		BaseUri = new Uri($"http://127.0.0.1:{port}/");
		_resources = new ConcurrentDictionary<string, FixtureResource>(StringComparer.OrdinalIgnoreCase);
		_listener = new HttpListener();
		_listener.Prefixes.Add(BaseUri.ToString());
		_cancellation = new CancellationTokenSource();
		_listener.Start();
		_listenTask = Task.Run(() => ListenAsync(_cancellation.Token));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Base address, always http on 127.0.0.1.
	/// </summary>
	public Uri BaseUri { get; }

	/// <summary>
	/// TCP port the listener bound.
	/// </summary>
	public int Port { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Starts a server on a free loopback port.
	/// </summary>
	public static HtmlFixtureServer Start()
	{
		return new HtmlFixtureServer(GetFreeTcpPort());
	}

	/// <summary>
	/// Starts a server and runs the configure callback before returning.
	/// </summary>
	public static HtmlFixtureServer Start(Action<HtmlFixtureServer> configure)
	{
		var server = Start();
		configure?.Invoke(server);
		return server;
	}

	/// <summary>
	/// Absolute URI for a mapped path.
	/// </summary>
	public Uri GetUri(string path)
	{
		return new Uri(BaseUri, NormalizePath(path).TrimStart('/'));
	}

	/// <summary>
	/// Maps a GET path to a body. Path is like "/home.html".
	/// </summary>
	public HtmlFixtureServer Map(string path, string content, string contentType = null)
	{
		var normalized = NormalizePath(path);
		_resources[normalized] = new FixtureResource
		{
			StatusCode = 200,
			ContentType = contentType ?? GuessContentType(normalized),
			Body = content ?? string.Empty
		};
		return this;
	}

	/// <summary>
	/// Maps a GET path to a 302 redirect.
	/// </summary>
	public HtmlFixtureServer MapRedirect(string path, string location)
	{
		var normalized = NormalizePath(path);
		var target = location.StartsWith("http", StringComparison.OrdinalIgnoreCase)
			? location
			: GetUri(location).ToString();
		_resources[normalized] = new FixtureResource
		{
			StatusCode = 302,
			Location = target,
			ContentType = "text/plain",
			Body = string.Empty
		};
		return this;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_cancellation.Cancel();

		try
		{
			_listener.Stop();
		}
		catch (ObjectDisposedException)
		{
		}

		_listener.Close();

		try
		{
			_listenTask.Wait(TimeSpan.FromSeconds(2));
		}
		catch (AggregateException)
		{
		}

		_cancellation.Dispose();
	}

	private async Task ListenAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
		{
			HttpListenerContext context;

			try
			{
				context = await _listener.GetContextAsync().ConfigureAwait(false);
			}
			catch (HttpListenerException)
			{
				break;
			}
			catch (ObjectDisposedException)
			{
				break;
			}

			try
			{
				Handle(context);
			}
			catch
			{
				try
				{
					context.Response.StatusCode = 500;
					context.Response.Close();
				}
				catch
				{
				}
			}
		}
	}

	private void Handle(HttpListenerContext context)
	{
		var path = NormalizePath(context.Request.Url?.AbsolutePath ?? "/");
		if ((path == "/") && _resources.ContainsKey("/index.html"))
		{
			path = "/index.html";
		}

		if (!_resources.TryGetValue(path, out var resource))
		{
			Write(context.Response, 404, "text/plain; charset=utf-8", "Not Found");
			return;
		}

		if (resource.StatusCode is >= 300 and < 400)
		{
			context.Response.StatusCode = resource.StatusCode;
			context.Response.RedirectLocation = resource.Location;
			context.Response.Close();
			return;
		}

		Write(context.Response, resource.StatusCode, resource.ContentType, resource.Body);
	}

	private static void Write(HttpListenerResponse response, int statusCode, string contentType, string body)
	{
		var bytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
		response.StatusCode = statusCode;
		response.ContentType = contentType;
		response.ContentLength64 = bytes.Length;
		response.OutputStream.Write(bytes, 0, bytes.Length);
		response.Close();
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "/";
		}

		path = path.Trim();
		if (!path.StartsWith('/'))
		{
			path = "/" + path;
		}

		return path;
	}

	private static string GuessContentType(string path)
	{
		return Path.GetExtension(path).ToLowerInvariant() switch
		{
			".html" or ".htm" => "text/html; charset=utf-8",
			".js" => "text/javascript; charset=utf-8",
			".css" => "text/css; charset=utf-8",
			".json" => "application/json; charset=utf-8",
			".txt" => "text/plain; charset=utf-8",
			_ => "application/octet-stream"
		};
	}

	private static int GetFreeTcpPort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint) listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	#endregion

	#region Classes

	private sealed class FixtureResource
	{
		public int StatusCode { get; set; }

		public string ContentType { get; set; }

		public string Body { get; set; }

		public string Location { get; set; }
	}

	#endregion
}
