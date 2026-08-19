#region References

using System;

#endregion

namespace Cornerstone.Keystone;

/// <summary>
/// Generates SubscribeTo / UnsubscribeTo calls for On* handlers that take a
/// [ChannelMessage] payload. Call base.InitializeLifecycle / UninitializeLifecycle.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ChannelHandlersAttribute : CornerstoneAttribute
{
}
