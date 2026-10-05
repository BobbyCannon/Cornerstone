using System;

namespace Cornerstone.RemoteLink.Vnc.Client.Protocol
{
    public class RfbProtocolException : Exception
    {
        public RfbProtocolException() { }

        public RfbProtocolException(string? message) : base(message) { }

        public RfbProtocolException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
