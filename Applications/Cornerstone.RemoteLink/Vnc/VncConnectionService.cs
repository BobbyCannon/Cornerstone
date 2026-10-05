#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Reflection;
using Cornerstone.RemoteLink.Vnc.Client;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.Implementation.Services.Transports;
using Cornerstone.RemoteLink.Vnc.Client.Rendering;
using Cornerstone.RemoteLink.Vnc.Client.Security;
using Cornerstone.Runtime;
using Microsoft.Extensions.Logging.Abstractions;

#endregion

namespace Cornerstone.RemoteLink.Vnc;

[SourceReflection]
[DependencyInjected(TypeLifetime.Singleton)]
public class VncConnectionService
{
	#region Fields

	private readonly VncClient _client;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public VncConnectionService()
	{
		_client = new VncClient(NullLoggerFactory.Instance);
	}

	#endregion

	#region Methods

	public Task<RfbConnection> ConnectAsync(string host, int port, IAuthenticationHandler authenticationHandler, IRenderTarget renderTarget, CancellationToken cancellationToken)
	{
		if (authenticationHandler == null)
		{
			throw new ArgumentNullException(nameof(authenticationHandler));
		}

		var parameters = new ConnectParameters
		{
			TransportParameters = new TcpTransportParameters
			{
				Host = host,
				Port = port
			},
			AuthenticationHandler = authenticationHandler,
			InitialRenderTarget = renderTarget,
			MaxReconnectAttempts = 0
		};

		return _client.ConnectAsync(parameters, cancellationToken);
	}

	#endregion
}
