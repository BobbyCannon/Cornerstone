using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.SecurityTypes;
using Cornerstone.RemoteLink.Vnc.Client.Security;

namespace Cornerstone.RemoteLink.Vnc.Client.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A security type without any security.
    /// </summary>
    public class NoneSecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;

        /// <inhertitdoc />
        public byte Id => (byte)WellKnownSecurityType.None;

        /// <inhertitdoc />
        public string Name => "None";

        /// <inhertitdoc />
        public int Priority => 1; // Anything is better than nothing. xD

        /// <summary>
        /// Initializes a new instance of the <see cref="NoneSecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public NoneSecurityType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _state = context.GetState<ProtocolState>();
        }

        /// <inhertitdoc />
        public Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken = default)
        {
            if (authenticationHandler == null)
                throw new ArgumentNullException(nameof(authenticationHandler));

            cancellationToken.ThrowIfCancellationRequested();

            // Nothing to do.

            // The server will not answer with a SecurityResult message in earlier protocol versions.
            bool expectSecurityResult = _state.ProtocolVersion >= RfbProtocolVersion.RFB_3_8;

            return Task.FromResult(new AuthenticationResult(null, expectSecurityResult));
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
