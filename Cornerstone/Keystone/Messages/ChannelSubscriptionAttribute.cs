#region References

using System;

#endregion

// ReSharper disable once CheckNamespace
namespace Cornerstone.Keystone.Messages;

/// <summary>
/// Instruct Cornerstone.Generators to generate a relay command for the method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ChannelSubscriptionAttribute<TChannelEnum, T2> : CornerstoneAttribute
	where TChannelEnum : Enum
	where T2 : IChannelMessage
{
	#region Constructors

	/// <summary>
	/// Generate a property whose name is derived from the name of this field, with a public getter and setter
	/// </summary>
	/// <param name="value"> </param>
	public ChannelSubscriptionAttribute(TChannelEnum value)
	{
		ChannelId = value;
		MessageType = typeof(T2);
	}

	#endregion

	#region Properties

	public TChannelEnum ChannelId { get; set; }

	public Type MessageType { get; set; }

	#endregion
}

/// <summary>
/// Instruct Cornerstone.Generators to generate a relay command for the method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ChannelSubscriptionAttribute<TChannelEnum> : CornerstoneAttribute
	where TChannelEnum : Enum
{
	#region Constructors

	/// <summary>
	/// Generate a property whose name is derived from the name of this field, with a public getter and setter
	/// </summary>
	/// <param name="value"> </param>
	public ChannelSubscriptionAttribute(TChannelEnum value)
	{
		ChannelId = value;
	}

	#endregion

	#region Properties

	public TChannelEnum ChannelId { get; set; }

	#endregion
}