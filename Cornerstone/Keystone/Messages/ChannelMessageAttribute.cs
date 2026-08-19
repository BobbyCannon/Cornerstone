#region References

using System;
using Cornerstone.Keystone;

#endregion

namespace Cornerstone.Keystone.Messages;

/// <summary>
/// Marks a channel message type. Cornerstone.Generators emits publish and
/// subscribe helpers on <typeparamref name="TChannel" />.
/// The CLR type is the operation id (no parallel enum).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ChannelMessageAttribute<TChannel> : CornerstoneAttribute
	where TChannel : KeystoneChannel
{
	#region Constructors

	public ChannelMessageAttribute()
	{
		MethodName = string.Empty;
	}

	public ChannelMessageAttribute(string methodName)
	{
		MethodName = methodName ?? string.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Publish / SubscribeTo method name. Empty means infer from the type name.
	/// </summary>
	public string MethodName { get; }

	/// <summary>
	/// When true, the generated publish method is also marked [RelayCommand].
	/// </summary>
	public bool RelayCommand { get; set; }

	#endregion
}
