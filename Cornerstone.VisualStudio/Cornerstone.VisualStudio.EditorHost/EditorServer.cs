#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Remote.Protocol;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;
using Cornerstone.VisualStudio.Protocol;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

public sealed class EditorServer : IDisposable
{
	#region Fields

	private readonly List<ICornerstoneRemoteTransportConnection> _connections;
	private readonly SemaphoreSlim _engineGate;
	private readonly CompletionEngine _engine;
	private readonly object _progressLock;
	private readonly Queue<MetadataProgressMessage> _progressQueue;
	private readonly SemaphoreSlim _sendGate;
	private bool _progressPump;
	private IDisposable _listener;
	private readonly Metadata _metadata;

	#endregion

	#region Constructors

	public EditorServer(Metadata metadata)
	{
		_metadata = metadata ?? new Metadata();
		_engine = new CompletionEngine();
		_connections = new List<ICornerstoneRemoteTransportConnection>();
		_engineGate = new SemaphoreSlim(1, 1);
		_progressLock = new object();
		_progressQueue = new Queue<MetadataProgressMessage>();
		_progressPump = false;
		_sendGate = new SemaphoreSlim(1, 1);
		AssemblyLoadProgress.Changed += OnAssemblyProgress;
	}

	#endregion

	#region Properties

	public int Port { get; private set; }

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_listener != null)
		{
			_listener.Dispose();
			_listener = null;
		}

		lock (_connections)
		{
			foreach (var connection in _connections)
			{
				connection.Dispose();
			}

			_connections.Clear();
		}
	}

	[RequiresUnreferencedCode("Bson uses reflection")]
	public void Start()
	{
		Start(ReservePort());
	}

	[RequiresUnreferencedCode("Bson uses reflection")]
	public void Start(int port)
	{
		Port = port;
		var transport = new BsonTcpTransport(new DefaultMessageTypeResolver(
			typeof(GetCompletionsRequestMessage).GetTypeInfo().Assembly));
		_listener = transport.Listen(IPAddress.Loopback, Port, OnConnected);
	}

	private void OnConnected(ICornerstoneRemoteTransportConnection connection)
	{
		lock (_connections)
		{
			_connections.Add(connection);
		}

		connection.OnMessage += OnMessage;
	}

	private void OnMessage(ICornerstoneRemoteTransportConnection connection, object message)
	{
		// Return to the socket reader before any metadata walk. Enter indent does not
		// use the completion engine, so it can be answered while a completion is still loading.
		var indentRequest = message as BuildEnterIndentRequestMessage;
		if (indentRequest != null)
		{
			Queue(() => SendEnterIndentAsync(connection, indentRequest));
			return;
		}

		var definitionRequest = message as GoToDefinitionRequestMessage;
		if (definitionRequest != null)
		{
			Queue(() => SendGoToDefinitionAsync(connection, definitionRequest));
			return;
		}

		var ensureRequest = message as EnsureMetadataRequestMessage;
		if (ensureRequest != null)
		{
			Queue(() => SendEnsureMetadataAsync(connection, ensureRequest));
			return;
		}

		var namespaceRequest = message as LookupNamespaceRequestMessage;
		if (namespaceRequest != null)
		{
			Queue(() => SendLookupNamespaceAsync(connection, namespaceRequest));
			return;
		}

		var gridRequest = message as ConvertGridDefinitionsRequestMessage;
		if (gridRequest != null)
		{
			Queue(() => SendConvertGridDefinitionsAsync(connection, gridRequest));
			return;
		}

		var request = message as GetCompletionsRequestMessage;
		if (request == null)
		{
			return;
		}

		Queue(() => SendCompletionsAsync(connection, request));
	}

	private static void Queue(Func<Task> work)
	{
		Task.Run(async () =>
		{
			try
			{
				await work().ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception);
			}
		});
	}

	private async Task SendEnterIndentAsync(ICornerstoneRemoteTransportConnection connection, BuildEnterIndentRequestMessage request)
	{
		var response = new BuildEnterIndentResponseMessage();
		response.RequestId = request.RequestId;
		try
		{
			var previous = EnterIndent.LeadingWhitespace(request.PreviousLine);
			response.Insertion = EnterIndent.BuildInsertion(
				request.Line,
				request.CaretIndex,
				previous,
				request.IndentSize,
				request.NewLine);
		}
		catch (Exception exception)
		{
			response.Error = exception.Message;
		}

		await SendReplyAsync(connection, response).ConfigureAwait(false);
	}

	private async Task SendCompletionsAsync(ICornerstoneRemoteTransportConnection connection, GetCompletionsRequestMessage request)
	{
		var response = new GetCompletionsResponseMessage();
		response.RequestId = request.RequestId;
		Metadata metadata;
		try
		{
			metadata = ResolveKnownMetadata(request.AssemblyPaths);
			if ((metadata == null) && (request.AssemblyPaths != null) && (request.AssemblyPaths.Count > 0))
			{
				var hit = await MetadataCache.GetHitAsync(request.AssemblyPaths, null).ConfigureAwait(false);
				metadata = hit == null ? null : hit.Metadata;
			}
		}
		catch (Exception exception)
		{
			response.Error = exception.Message;
			await SendReplyAsync(connection, response).ConfigureAwait(false);
			return;
		}

		await _engineGate.WaitAsync().ConfigureAwait(false);
		try
		{
			var text = request.Text ?? string.Empty;
			var set = _engine.GetCompletions(
				metadata,
				text,
				request.Caret,
				request.AssemblyName,
				request.ExtraClassNames);
			if (set != null)
			{
				response.StartPosition = set.StartPosition;
				foreach (var completion in set.Completions)
				{
					response.DisplayTexts.Add(completion.DisplayText);
					response.Items.Add(ToItem(completion));
				}
			}
		}
		catch (Exception exception)
		{
			response.Error = exception.Message;
		}
		finally
		{
			_engineGate.Release();
		}

		await SendReplyAsync(connection, response).ConfigureAwait(false);
	}

	private async Task SendGoToDefinitionAsync(ICornerstoneRemoteTransportConnection connection, GoToDefinitionRequestMessage request)
	{
		var response = new GoToDefinitionResponseMessage();
		response.RequestId = request.RequestId;
		Metadata metadata;
		try
		{
			metadata = ResolveKnownMetadata(request.AssemblyPaths);
		}
		catch (Exception exception)
		{
			response.Error = exception.Message;
			await SendReplyAsync(connection, response).ConfigureAwait(false);
			return;
		}

		await _engineGate.WaitAsync().ConfigureAwait(false);
		try
		{
			var target = XamlGoToDefinitionResolver.Resolve(
				_engine,
				metadata,
				request.Text ?? string.Empty,
				request.Caret,
				request.AssemblyName);
			response.Kind = (int) target.Kind;
			response.TypeFullName = target.TypeFullName ?? string.Empty;
			response.MemberName = target.MemberName ?? string.Empty;
			response.ClassName = target.ClassName ?? string.Empty;
			response.MethodName = target.MethodName ?? string.Empty;
			response.DocumentOffset = target.DocumentOffset;
		}
		catch (Exception exception)
		{
			response.Error = exception.Message;
		}
		finally
		{
			_engineGate.Release();
		}

		await SendReplyAsync(connection, response).ConfigureAwait(false);
	}

	private void OnAssemblyProgress(MetadataProgressMessage message)
	{
		if (message == null)
		{
			return;
		}

		var start = false;
		lock (_progressLock)
		{
			_progressQueue.Enqueue(message);
			if (!_progressPump)
			{
				_progressPump = true;
				start = true;
			}
		}

		if (start)
		{
			Queue(PumpProgressAsync);
		}
	}

	private async Task PumpProgressAsync()
	{
		while (true)
		{
			MetadataProgressMessage message;
			lock (_progressLock)
			{
				if (_progressQueue.Count == 0)
				{
					_progressPump = false;
					return;
				}

				message = _progressQueue.Dequeue();
			}

			try
			{
				await SendProgressAsync(message).ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception);
			}
		}
	}

	private async Task SendProgressAsync(MetadataProgressMessage message)
	{
		ICornerstoneRemoteTransportConnection[] connections;
		lock (_connections)
		{
			connections = new ICornerstoneRemoteTransportConnection[_connections.Count];
			_connections.CopyTo(connections);
		}

		for (var i = 0; i < connections.Length; i++)
		{
			await SendReplyAsync(connections[i], message).ConfigureAwait(false);
		}
	}

	private async Task SendReplyAsync(ICornerstoneRemoteTransportConnection connection, object response)
	{
		await _sendGate.WaitAsync().ConfigureAwait(false);
		try
		{
			await connection.Send(response).ConfigureAwait(false);
		}
		finally
		{
			_sendGate.Release();
		}
	}

	private async Task SendEnsureMetadataAsync(ICornerstoneRemoteTransportConnection connection, EnsureMetadataRequestMessage request)
	{
		var response = new EnsureMetadataResponseMessage();
		response.RequestId = request.RequestId;
		try
		{
			MetadataCache.UseSolutionDirectory(request.SolutionDirectory);
			var hit = await MetadataCache.GetHitAsync(request.AssemblyPaths, request.TargetPaths).ConfigureAwait(false);
			response.Seconds = hit.Seconds;
			response.FromCache = hit.FromCache ? 1 : 0;
			response.ReadCount = hit.ReadCount;
		}
		catch (Exception exception)
		{
			response.Error = exception.Message;
		}

		await SendReplyAsync(connection, response).ConfigureAwait(false);
	}

	private async Task SendConvertGridDefinitionsAsync(ICornerstoneRemoteTransportConnection connection, ConvertGridDefinitionsRequestMessage request)
	{
		var response = new ConvertGridDefinitionsResponseMessage();
		response.RequestId = request.RequestId;
		try
		{
			GridDefinitionsSuggestion.Conversion conversion;
			if (GridDefinitionsSuggestion.TryConvert(request.Text, request.Caret, out conversion))
			{
				response.CanConvert = 1;
				response.AttributeName = conversion.AttributeName;
				response.AttributeValue = conversion.AttributeValue;
				response.DisplayText = conversion.DisplayText;
				response.Insertion = conversion.Insertion;
				response.RemoveStart = conversion.RemoveStart;
				response.RemoveLength = conversion.RemoveLength;
				response.InsertAt = conversion.InsertAt;
			}
		}
		catch (Exception exception)
		{
			response.Error = exception.Message;
		}

		await SendReplyAsync(connection, response).ConfigureAwait(false);
	}

	private async Task SendLookupNamespaceAsync(ICornerstoneRemoteTransportConnection connection, LookupNamespaceRequestMessage request)
	{
		var response = new LookupNamespaceResponseMessage();
		response.RequestId = request.RequestId;
		try
		{
			// A lightbulb must not wait behind a metadata load. The load keeps the only
			// gate, and this call's timeout would otherwise drop the shared connection.
			Metadata metadata;
			if ((request.AssemblyPaths == null) || (request.AssemblyPaths.Count == 0))
			{
				metadata = _metadata;
			}
			else if (!MetadataCache.TryGetReady(request.AssemblyPaths, out metadata))
			{
				await SendReplyAsync(connection, response).ConfigureAwait(false);
				return;
			}

			int kind;
			string xmlNamespace;
			string alias;
			NamespaceSuggestion.Find(
				metadata,
				request.TypeName,
				request.Text,
				request.HasAlias,
				request.Alias,
				out kind,
				out xmlNamespace,
				out alias);
			response.Kind = kind;
			response.XmlNamespace = xmlNamespace ?? string.Empty;
			response.Alias = alias ?? string.Empty;
		}
		catch (Exception exception)
		{
			response.Error = exception.Message;
		}

		await SendReplyAsync(connection, response).ConfigureAwait(false);
	}

	/// <summary>
	/// Metadata already on hand for these assemblies. Does not wait for a load in progress.
	/// </summary>
	private Metadata ResolveKnownMetadata(IList<string> paths)
	{
		if ((paths == null) || (paths.Count == 0))
		{
			return _metadata;
		}

		Metadata metadata;
		if (MetadataCache.TryGetReady(paths, out metadata) ||
			MetadataCache.TryAssembleKnown(paths, out metadata))
		{
			return metadata;
		}

		return null;
	}

	private static CompletionItemMessage ToItem(Completion completion)
	{
		var item = new CompletionItemMessage();
		item.DisplayText = completion.DisplayText ?? string.Empty;
		item.InsertText = completion.InsertText ?? string.Empty;
		item.Description = completion.Description ?? string.Empty;
		item.Suffix = completion.Suffix ?? string.Empty;
		item.Kind = (int) completion.Kind;
		item.Priority = completion.Priority;
		item.TriggerCompletionAfterInsert = completion.TriggerCompletionAfterInsert;
		item.RecommendedCursorOffset = completion.RecommendedCursorOffset ?? -1;
		item.DeleteTextOffset = completion.DeleteTextOffset ?? -1;
		return item;
	}

	private static int ReservePort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint) listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	#endregion
}