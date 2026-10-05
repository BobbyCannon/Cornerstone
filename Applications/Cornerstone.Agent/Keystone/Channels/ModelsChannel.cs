#region References

using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Keystone.Channels;

[SourceReflection]
[DependencyInjected]
public partial class ModelsChannel : KeystoneChannel
{
	#region Methods

	[RelayCommand]
	public void RefreshModels()
	{
		Publish(new RefreshModelsMessage());
	}

	#endregion

	#region Records

	[ChannelMessage<ModelsChannel>]
	public record struct ModelsUpdatedMessage : IChannelMessage;

	[ChannelMessage<ModelsChannel>(RelayCommand = true)]
	public record struct RefreshModelsMessage : IChannelMessage;

	[ChannelMessage<ModelsChannel>]
	public record struct SelectModelMessage(string FilePath) : IChannelMessage;

	[ChannelMessage<ModelsChannel>]
	public record struct UnloadModelMessage : IChannelMessage;

	#endregion
}
