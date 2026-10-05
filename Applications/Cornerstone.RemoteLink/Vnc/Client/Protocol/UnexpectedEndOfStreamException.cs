using System;

namespace Cornerstone.RemoteLink.Vnc.Client.Protocol
{
    public class UnexpectedEndOfStreamException : RfbProtocolException
    {
        public UnexpectedEndOfStreamException() { }

        public UnexpectedEndOfStreamException(string? message) : base(message) { }

        public UnexpectedEndOfStreamException(string? message, Exception? innerException) : base(message,
            innerException) { }
    }
}
