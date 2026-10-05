using System;

namespace Cornerstone.RemoteLink.Vnc.Client.Protocol
{
    public class HandshakeFailedException : RfbProtocolException
    {
        public HandshakeFailedException() { }

        public HandshakeFailedException(string? message) : base(message) { }

        public HandshakeFailedException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
