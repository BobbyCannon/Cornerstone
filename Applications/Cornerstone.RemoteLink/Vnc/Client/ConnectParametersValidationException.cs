using System;

namespace Cornerstone.RemoteLink.Vnc.Client
{
    public class ConnectParametersValidationException : Exception
    {
        public ConnectParametersValidationException() { }

        public ConnectParametersValidationException(string? message) : base(message) { }

        public ConnectParametersValidationException(string? message, Exception? innerException) : base(message,
            innerException) { }
    }
}
