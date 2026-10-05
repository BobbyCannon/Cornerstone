#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Protocol;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// One editor-host process for this Visual Studio session. Completion, Go to Definition,
/// and Enter indent are answered there. The UI thread must not wait on this type.
/// </summary>
internal static class EditorHostSession
{
	#region Fields

	private static readonly object Gate = new object();
	private static Process _process;
	private static EditorConnection _connection;
	private static Task<EditorConnection> _connecting;
	private static int _port;
	private static int _connectionGeneration;
	private static int _abandonedGeneration;
	private static int _timeoutsSinceSuccess;
	private static int _hostGeneration;
	private static int _shutDown;

	#endregion

	#region Methods

	public static EditorHostStatus CurrentStatus()
	{
		var status = new EditorHostStatus();
		status.Activity = string.Empty;
		lock (Gate)
		{
			status.Port = _port;
			var process = _process;
			if (process == null)
			{
				status.State = "Not started";
				return status;
			}

			try
			{
				if (process.HasExited)
				{
					status.State = "Exited";
					status.Detail = "code " + process.ExitCode;
					return status;
				}

				status.ProcessId = process.Id;
			}
			catch (InvalidOperationException)
			{
				status.State = "Not started";
				return status;
			}
		}

		status.State = _connection == null ? "Starting" : "Running";
		if (status.Port > 0)
		{
			status.Detail = "port " + status.Port;
		}

		return status;
	}

	/// <summary>
	/// Editor-host row and gear tooltip. State is Loading while metadata work is in flight.
	/// </summary>
	internal static EditorHostStatus DisplayStatus()
	{
		var host = CurrentStatus();
		if (EditorHostActivity.IsWorking && (host.State != "Exited"))
		{
			host.State = "Loading";
		}

		var activity = EditorHostActivity.StatusText;
		if (!string.IsNullOrEmpty(activity) && !string.Equals(activity, "Idle", StringComparison.Ordinal))
		{
			host.Activity = activity;
		}

		return host;
	}

	internal static string HostToolTip()
	{
		var host = DisplayStatus();
		var text = "Editor host: " + host.State;
		if (!string.IsNullOrEmpty(host.Activity))
		{
			return text + " - " + host.Activity;
		}

		if (!string.IsNullOrEmpty(host.Detail))
		{
			return text + " - " + host.Detail;
		}

		return text;
	}

	public static Task<GetCompletionsResponseMessage> GetCompletionsAsync(
		string text,
		int caret,
		string assemblyName,
		IList<string> extraClassNames,
		IList<string> assemblyPaths)
	{
		return CallAsync(connection => connection.GetCompletionsAsync(text, caret, assemblyName, extraClassNames, assemblyPaths));
	}

	public static Task<GoToDefinitionResponseMessage> GoToDefinitionAsync(
		string text,
		int caret,
		string assemblyName,
		IList<string> assemblyPaths)
	{
		return CallAsync(connection => connection.GoToDefinitionAsync(text, caret, assemblyName, assemblyPaths));
	}

	public static Task<ConvertGridDefinitionsResponseMessage> ConvertGridDefinitionsAsync(string text, int caret)
	{
		return CallAsync(connection => connection.ConvertGridDefinitionsAsync(text, caret));
	}

	public static Task<LookupNamespaceResponseMessage> LookupNamespaceAsync(
		string text,
		string typeName,
		bool hasAlias,
		string alias,
		IList<string> assemblyPaths)
	{
		return CallAsync(connection => connection.LookupNamespaceAsync(text, typeName, hasAlias, alias, assemblyPaths));
	}

	public static Task<EnsureMetadataResponseMessage> EnsureMetadataAsync(IList<string> assemblyPaths, string solutionDirectory)
	{
		return EnsureMetadataAsync(assemblyPaths, solutionDirectory, null);
	}

	public static Task<EnsureMetadataResponseMessage> EnsureMetadataAsync(IList<string> assemblyPaths, string solutionDirectory, IList<string> targetPaths)
	{
		return CallAsync(connection => connection.EnsureMetadataAsync(assemblyPaths, solutionDirectory, targetPaths));
	}

	public static Task<BuildEnterIndentResponseMessage> BuildEnterIndentAsync(
		string line,
		int caretIndex,
		string previousLine,
		int indentSize,
		string newLine)
	{
		return CallAsync(connection => connection.BuildEnterIndentAsync(line, caretIndex, previousLine, indentSize, newLine));
	}

	private static async Task<T> CallAsync<T>(Func<EditorConnection, Task<T>> call)
	{
		EditorHostActivity.Enter();
		try
		{
			var connection = await EnsureConnectedAsync().ConfigureAwait(false);
			try
			{
				var result = await call(connection).ConfigureAwait(false);
				NoteSuccess();
				return result;
			}
			catch (EditorCallTimeoutException ex)
			{
				Log.Warning(ex, "Editor host timed out");
				// The host already has the request. Dropping the socket here cancels every
				// other call on it, including a metadata load that is still allowed to run.
				if (!ex.RequestAccepted)
				{
					AbandonAfterTimeout();
				}

				throw;
			}
			catch (TimeoutException ex)
			{
				Log.Warning(ex, "Editor host timed out");
				AbandonAfterTimeout();
				throw;
			}
			catch (Exception ex) when (IsTransportFailure(ex))
			{
				Log.Debug(ex, "Editor host connection failed");
				DropConnection(killProcess: HasExited(ProcessUnderLock()));
				throw;
			}
		}
		finally
		{
			EditorHostActivity.Exit();
		}
	}

	private static Process ProcessUnderLock()
	{
		lock (Gate)
		{
			return _process;
		}
	}

	private static void NoteSuccess()
	{
		lock (Gate)
		{
			_timeoutsSinceSuccess = 0;
		}
	}

	private static bool IsTransportFailure(Exception exception)
	{
		return (exception is IOException) ||
			(exception is ObjectDisposedException) ||
			(exception is InvalidOperationException);
	}

	private static Task<EditorConnection> EnsureConnectedAsync()
	{
		lock (Gate)
		{
			if (_connection != null)
			{
				return Task.FromResult(_connection);
			}

			if (_connecting == null)
			{
				_connecting = ConnectNewAsync();
			}

			return _connecting;
		}
	}

	private static async Task<EditorConnection> ConnectNewAsync()
	{
		try
		{
			Process process;
			int port;
			lock (Gate)
			{
				process = _process;
				port = _port;
			}

			if ((process == null) || HasExited(process))
			{
				port = await StartProcessAsync().ConfigureAwait(false);
			}

			var connection = await ConnectWithRetryAsync(port).ConfigureAwait(false);
			connection.ConnectionLost += OnConnectionLost;
			connection.Progress += OnMetadataProgress;
			lock (Gate)
			{
				_connectionGeneration++;
				_connection = connection;
				_connecting = null;
				return _connection;
			}
		}
		catch (Exception)
		{
			lock (Gate)
			{
				_connecting = null;
			}

			throw;
		}
	}

	private static async Task<EditorConnection> ConnectWithRetryAsync(int port)
	{
		Exception last = null;
		for (var attempt = 0; attempt < 20; attempt++)
		{
			try
			{
				return await EditorConnection.ConnectAsync(port).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				last = ex;
				await Task.Delay(50).ConfigureAwait(false);
			}
		}

		throw last ?? new InvalidOperationException("Editor host did not accept a connection.");
	}

	private static async Task<int> StartProcessAsync()
	{
		int generation;
		lock (Gate)
		{
			if (_shutDown != 0)
			{
				throw new InvalidOperationException("Editor host is shut down.");
			}

			_hostGeneration++;
			generation = _hostGeneration;
		}

		var extensionDirectory = Path.GetDirectoryName(typeof(EditorHostSession).Assembly.Location);
		var hostDirectory = Path.Combine(extensionDirectory, "EditorHost");
		var hostAssembly = Path.Combine(hostDirectory, "Cornerstone.VisualStudio.EditorHost.dll");
		var runtimeConfig = Path.Combine(hostDirectory, "Cornerstone.VisualStudio.EditorHost.runtimeconfig.json");
		var depsFile = Path.Combine(hostDirectory, "Cornerstone.VisualStudio.EditorHost.deps.json");
		if (!File.Exists(hostAssembly))
		{
			throw new FileNotFoundException("Editor host was not deployed with the extension.", hostAssembly);
		}

		var ready = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var process = new Process();
		process.StartInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = "exec --runtimeconfig \"" + runtimeConfig + "\" --depsfile \"" + depsFile + "\" \"" + hostAssembly + "\"",
			WorkingDirectory = hostDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		process.EnableRaisingEvents = true;
		process.OutputDataReceived += (_, e) =>
		{
			if (string.IsNullOrEmpty(e.Data))
			{
				return;
			}

			if (int.TryParse(e.Data, out var reported) && (reported > 0))
			{
				ready.TrySetResult(reported);
			}
		};
		process.ErrorDataReceived += (_, e) =>
		{
			// Handler exists so the stderr pipe is drained. An unread pipe fills and blocks the host.
		};
		process.Exited += OnProcessExited;
		if (!process.Start())
		{
			throw new InvalidOperationException("Failed to start the editor host.");
		}

		ExtensionProcessLifetime.Track(process);
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		var finished = await Task.WhenAny(ready.Task, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
		if (finished != ready.Task)
		{
			ExtensionProcessLifetime.Kill(process);
			throw new TimeoutException("Editor host did not report a port.");
		}

		var port = await ready.Task.ConfigureAwait(false);
		var superseded = false;
		lock (Gate)
		{
			if ((_shutDown != 0) || (generation != _hostGeneration))
			{
				superseded = true;
			}
			else
			{
				_process = process;
				_port = port;
			}
		}

		if (superseded)
		{
			ExtensionProcessLifetime.Kill(process);
			throw new InvalidOperationException("Editor host start was cancelled.");
		}

		return port;
	}

	/// <summary>
	/// Stops the editor host so it releases files under the extension directory.
	/// </summary>
	internal static void Shutdown()
	{
		lock (Gate)
		{
			_shutDown = 1;
		}

		DropConnection(killProcess: true);
	}

	/// <summary>
	/// One missed reply drops the socket. The process stays so a metadata load already
	/// in flight can finish and the next call reconnects. A second miss kills it.
	/// </summary>
	private static void AbandonAfterTimeout()
	{
		var kill = false;
		lock (Gate)
		{
			if (_abandonedGeneration == _connectionGeneration)
			{
				return;
			}

			_abandonedGeneration = _connectionGeneration;
			_timeoutsSinceSuccess++;
			kill = (_timeoutsSinceSuccess >= 2) || HasExited(_process);
		}

		DropConnection(kill);
	}

	private static void DropConnection(bool killProcess)
	{
		EditorConnection connection;
		Process process = null;
		lock (Gate)
		{
			connection = _connection;
			_connection = null;
			_connecting = null;
			if (killProcess)
			{
				_hostGeneration++;
				process = _process;
				_process = null;
				_port = 0;
				_timeoutsSinceSuccess = 0;
			}
		}

		Detach(connection);
		ExtensionProcessLifetime.Kill(process);
	}

	private static void OnMetadataProgress(MetadataProgressMessage message)
	{
		if (message == null)
		{
			return;
		}

		if (message.Total > 0)
		{
			EditorHostActivity.Report(message.Phase, message.AssemblyName, message.Index, message.Total);
			return;
		}

		EditorHostActivity.SetMessage(string.IsNullOrEmpty(message.AssemblyName)
			? "Loading completion metadata"
			: "Reading metadata of " + message.AssemblyName);
	}

	private static void OnConnectionLost(EditorConnection connection, Exception exception)
	{
		lock (Gate)
		{
			if (!ReferenceEquals(_connection, connection))
			{
				return;
			}

			_connection = null;
			_connecting = null;
		}

		Detach(connection);
	}

	private static void OnProcessExited(object sender, EventArgs e)
	{
		var exited = sender as Process;
		EditorConnection connection = null;
		lock (Gate)
		{
			if (!ReferenceEquals(_process, exited))
			{
				return;
			}

			_process = null;
			_port = 0;
			connection = _connection;
			_connection = null;
			_connecting = null;
		}

		Detach(connection);
	}

	private static void Detach(EditorConnection connection)
	{
		if (connection == null)
		{
			return;
		}

		connection.ConnectionLost -= OnConnectionLost;
		connection.Progress -= OnMetadataProgress;
		try
		{
			connection.Dispose();
		}
		catch (Exception)
		{
			// Already closed.
		}
	}

	private static bool HasExited(Process process)
	{
		if (process == null)
		{
			return true;
		}

		try
		{
			return process.HasExited;
		}
		catch (InvalidOperationException)
		{
			return true;
		}
	}

	#endregion
}
