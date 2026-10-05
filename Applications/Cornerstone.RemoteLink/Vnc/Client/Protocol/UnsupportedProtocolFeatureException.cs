using System;

namespace Cornerstone.RemoteLink.Vnc.Client.Protocol
{
    public class UnsupportedProtocolFeatureException : RfbProtocolException
    {
        public UnsupportedProtocolFeatureException() { }

        public UnsupportedProtocolFeatureException(string? message) : base(message) { }

        public UnsupportedProtocolFeatureException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
