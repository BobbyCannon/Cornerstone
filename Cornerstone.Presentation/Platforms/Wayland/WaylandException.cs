using System;
using Cornerstone.Presentation.Wayland.Server.Interop;

namespace Cornerstone.Presentation.Wayland;

public class WaylandException : Exception
{
    public WaylandException()
    {
    }

    public WaylandException(string? message) : base(message)
    {
    }

    public WaylandException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class WaylandPollException : WaylandException
{
    public WaylandPollException() : base("poll failed")
    {
    }

    public WaylandPollException(string? message) : base(message)
    {
    }

    public WaylandPollException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class WaylandNetworkException : WaylandException
{
    public WaylandNetworkException()
    {
    }

    public WaylandNetworkException(string? message) : base(message)
    {
    }

    public WaylandNetworkException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class WaylandFlushException : WaylandNetworkException
{
    public WaylandFlushException()
    {
    }

    public WaylandFlushException(string? message) : base(message)
    {
    }

    public WaylandFlushException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    internal WaylandFlushException(UnsafeNativeMethods.Errno errno) : base("wl_display_flush failed, errno: " + errno)
    {
    }
}

public class WaylandReadException : WaylandNetworkException
{
    public WaylandReadException()
    {
    }

    public WaylandReadException(string? message) : base(message)
    {
    }

    public WaylandReadException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    internal WaylandReadException(UnsafeNativeMethods.Errno errno) : base("wl_display_read_events failed, errno: " + errno)
    {
    }
}

public class WaylandProtocolErrorException : WaylandException
{
    public WaylandProtocolErrorException() : base("protocol error")
    {
    }

    public WaylandProtocolErrorException(string? message) : base(message)
    {
    }

    public WaylandProtocolErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    internal WaylandProtocolErrorException(uint errorCode, string errorMessage) : base($"protocol error: {errorCode} {errorMessage}")
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public uint ErrorCode { get; }
    public string? ErrorMessage { get; }
}
