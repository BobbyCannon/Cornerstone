#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Remote.Protocol;

#endregion

namespace Cornerstone.VisualStudio.Protocol;

public sealed class EditorConnection : IDisposable
{
	#region Fields

	private readonly ICornerstoneRemoteTransportConnection _connection;
	private readonly object _gate;
	private readonly Dictionary<int, TaskCompletionSource<object>> _pendingById;
	private readonly SemaphoreSlim _sendGate;
	private int _disposed;
	private int _nextRequestId;

	#endregion

	#region Constructors

	private EditorConnection(ICornerstoneRemoteTransportConnection connection)
	{
		_connection = connection;
		_gate = new object();
		_pendingById = new Dictionary<int, TaskCompletionSource<object>>();
		_sendGate = new SemaphoreSlim(1, 1);
		_disposed = 0;
		_nextRequestId = 0;
		_connection.OnMessage += OnMessage;
		_connection.OnException += OnException;
	}

	#endregion

	#region Events

	/// <summary>
	/// The socket failed. Callers drop this connection and open another.
	/// </summary>
	public event Action<EditorConnection, Exception> ConnectionLost;

	/// <summary>
	/// The host started or finished reading one assembly.
	/// </summary>
	public event Action<MetadataProgressMessage> Progress;

	#endregion

	#region Methods

	public static async Task<EditorConnection> ConnectAsync(int port)
	{
		var transport = new BsonTcpTransport(new DefaultMessageTypeResolver(
			typeof(GetCompletionsRequestMessage).GetTypeInfo().Assembly));
		var connection = await transport.Connect(IPAddress.Loopback, port).ConfigureAwait(false);
		return new EditorConnection(connection);
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		_connection.OnMessage -= OnMessage;
		_connection.OnException -= OnException;
		try
		{
			_connection.Dispose();
		}
		catch (Exception)
		{
			// The socket is already gone.
		}

		FailAll(new ObjectDisposedException(nameof(EditorConnection)));
	}

	public Task<GetCompletionsResponseMessage> GetCompletionsAsync(string text, int caret)
	{
		return GetCompletionsAsync(text, caret, string.Empty, null, null);
	}

	public Task<GetCompletionsResponseMessage> GetCompletionsAsync(
		string text,
		int caret,
		string assemblyName,
		IList<string> extraClassNames,
		IList<string> assemblyPaths)
	{
		var request = new GetCompletionsRequestMessage();
		request.Text = text ?? string.Empty;
		request.Caret = caret;
		request.AssemblyName = assemblyName ?? string.Empty;
		if (extraClassNames != null)
		{
			request.ExtraClassNames.AddRange(extraClassNames);
		}

		if (assemblyPaths != null)
		{
			request.AssemblyPaths.AddRange(assemblyPaths);
		}

		return SendAsync<GetCompletionsResponseMessage>(request, "GetCompletions", TimeSpan.FromSeconds(60));
	}

	public Task<GoToDefinitionResponseMessage> GoToDefinitionAsync(
		string text,
		int caret,
		string assemblyName,
		IList<string> assemblyPaths)
	{
		var request = new GoToDefinitionRequestMessage();
		request.Text = text ?? string.Empty;
		request.Caret = caret;
		request.AssemblyName = assemblyName ?? string.Empty;
		if (assemblyPaths != null)
		{
			request.AssemblyPaths.AddRange(assemblyPaths);
		}

		return SendAsync<GoToDefinitionResponseMessage>(request, "GoToDefinition", TimeSpan.FromSeconds(60));
	}

	public Task<ConvertGridDefinitionsResponseMessage> ConvertGridDefinitionsAsync(string text, int caret)
	{
		var request = new ConvertGridDefinitionsRequestMessage();
		request.Text = text ?? string.Empty;
		request.Caret = caret;
		return SendAsync<ConvertGridDefinitionsResponseMessage>(request, "ConvertGridDefinitions", TimeSpan.FromSeconds(10));
	}

	public Task<LookupNamespaceResponseMessage> LookupNamespaceAsync(
		string text,
		string typeName,
		bool hasAlias,
		string alias,
		IList<string> assemblyPaths)
	{
		var request = new LookupNamespaceRequestMessage();
		request.Text = text ?? string.Empty;
		request.TypeName = typeName ?? string.Empty;
		request.HasAlias = hasAlias;
		request.Alias = alias ?? string.Empty;
		if (assemblyPaths != null)
		{
			request.AssemblyPaths.AddRange(assemblyPaths);
		}

		return SendAsync<LookupNamespaceResponseMessage>(request, "LookupNamespace", TimeSpan.FromSeconds(60));
	}

	public Task<EnsureMetadataResponseMessage> EnsureMetadataAsync(IList<string> assemblyPaths, string solutionDirectory)
	{
		return EnsureMetadataAsync(assemblyPaths, solutionDirectory, null);
	}

	public Task<EnsureMetadataResponseMessage> EnsureMetadataAsync(IList<string> assemblyPaths, string solutionDirectory, IList<string> targetPaths)
	{
		var request = new EnsureMetadataRequestMessage();
		request.SolutionDirectory = solutionDirectory ?? string.Empty;
		if (assemblyPaths != null)
		{
			request.AssemblyPaths.AddRange(assemblyPaths);
		}

		if (targetPaths != null)
		{
			request.TargetPaths.AddRange(targetPaths);
		}

		return SendAsync<EnsureMetadataResponseMessage>(request, "EnsureMetadata", TimeSpan.FromMinutes(3));
	}

	public Task<BuildEnterIndentResponseMessage> BuildEnterIndentAsync(
		string line,
		int caretIndex,
		string previousLine,
		int indentSize,
		string newLine)
	{
		var request = new BuildEnterIndentRequestMessage();
		request.Line = line ?? string.Empty;
		request.CaretIndex = caretIndex;
		request.PreviousLine = previousLine ?? string.Empty;
		request.IndentSize = indentSize;
		request.NewLine = newLine ?? string.Empty;
		return SendAsync<BuildEnterIndentResponseMessage>(request, "BuildEnterIndent", TimeSpan.FromSeconds(10));
	}

	private async Task<TResponse> SendAsync<TResponse>(object request, string operation, TimeSpan replyTimeout) where TResponse : class
	{
		var call = request as IEditorCall;
		if (call == null)
		{
			throw new InvalidOperationException("Editor request " + operation + " has no request id.");
		}

		var pending = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		int id;
		lock (_gate)
		{
			_nextRequestId++;
			if (_nextRequestId == 0)
			{
				_nextRequestId++;
			}

			id = _nextRequestId;
			call.RequestId = id;
			_pendingById[id] = pending;
		}

		try
		{
			await _sendGate.WaitAsync().ConfigureAwait(false);
			try
			{
				var send = _connection.Send(request);
				var sent = await Task.WhenAny(send, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
				if (sent != send)
				{
					throw new EditorCallTimeoutException(
						"The editor process did not accept " + operation + ".",
						requestAccepted: false);
				}

				await send.ConfigureAwait(false);
			}
			finally
			{
				_sendGate.Release();
			}

			var finished = await Task.WhenAny(pending.Task, Task.Delay(replyTimeout)).ConfigureAwait(false);
			if (finished != pending.Task)
			{
				throw new EditorCallTimeoutException(
					"The editor process did not answer " + operation + ".",
					requestAccepted: true);
			}

			return (TResponse) await pending.Task.ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is EditorCallTimeoutException || IsTransportFailure(ex))
		{
			lock (_gate)
			{
				_pendingById.Remove(id);
			}

			throw;
		}
		finally
		{
			lock (_gate)
			{
				_pendingById.Remove(id);
			}
		}
	}

	private static bool IsTransportFailure(Exception exception)
	{
		return (exception is IOException) ||
			(exception is SocketException) ||
			(exception is ObjectDisposedException) ||
			(exception is EndOfStreamException);
	}

	private void FailAll(Exception exception)
	{
		List<TaskCompletionSource<object>> pending;
		lock (_gate)
		{
			if (_pendingById.Count == 0)
			{
				return;
			}

			pending = new List<TaskCompletionSource<object>>(_pendingById.Values);
			_pendingById.Clear();
		}

		for (var i = 0; i < pending.Count; i++)
		{
			// A shared exception instance grows a new stack on every await.
			pending[i].TrySetException(new ObjectDisposedException(nameof(EditorConnection), exception.Message));
		}
	}

	private void OnException(ICornerstoneRemoteTransportConnection connection, Exception exception)
	{
		FailAll(exception);
		var lost = ConnectionLost;
		if (lost != null)
		{
			lost(this, exception);
		}
	}

	private void OnMessage(ICornerstoneRemoteTransportConnection connection, object message)
	{
		var progress = message as MetadataProgressMessage;
		if (progress != null)
		{
			var report = Progress;
			if (report != null)
			{
				report(progress);
			}

			return;
		}

		var call = message as IEditorCall;
		if (call == null)
		{
			return;
		}

		TaskCompletionSource<object> pending;
		lock (_gate)
		{
			if (!_pendingById.TryGetValue(call.RequestId, out pending))
			{
				return;
			}
		}

		pending.TrySetResult(message);
	}

	#endregion
}

/// <summary>
/// The editor host did not take the request, or took it and did not answer in time.
/// A request that was accepted leaves the socket up so a late reply and other calls can finish.
/// </summary>
public sealed class EditorCallTimeoutException : TimeoutException
{
	#region Constructors

	public EditorCallTimeoutException(string message, bool requestAccepted)
		: base(message)
	{
		RequestAccepted = requestAccepted;
	}

	#endregion

	#region Properties

	public bool RequestAccepted { get; }

	#endregion
}
