#region References

using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName.Keystone.Channels;

/// <summary>
/// Channel for the home greet button.
/// </summary>
[SourceReflection]
public partial class GreetingChannel : KeystoneChannel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public GreetingChannel()
	{
	}

	#endregion

	#region Records

	[ChannelMessage<GreetingChannel>]
	public record struct GreetMessage : IChannelMessage;

	#endregion
}
