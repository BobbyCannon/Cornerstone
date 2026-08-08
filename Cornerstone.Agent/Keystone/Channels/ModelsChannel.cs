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
public partial class ModelsChannel : KeystoneChannel<ModelsChannel.ModelsMessageType>
{
	#region Methods

	[ChannelSubscription<ModelsMessageType>(ModelsMessageType.ModelsUpdated)]
	public void ModelsUpdated()
	{
		Publish(ModelsMessageType.ModelsUpdated);
	}

	[RelayCommand]
	[ChannelSubscription<ModelsMessageType>(ModelsMessageType.RefreshModels)]
	public void RefreshModels()
	{
		Publish(ModelsMessageType.RefreshModels);
	}

	/// <summary>
	/// User intent: desire this model path. Does not load weights.
	/// </summary>
	[ChannelSubscription<ModelsMessageType, SelectModelMessage>(ModelsMessageType.SelectModel)]
	public void SelectModel(string filePath)
	{
		Publish(ModelsMessageType.SelectModel, new SelectModelMessage(filePath));
	}

	/// <summary>
	/// Explicit unload (shutdown / tests). Not required for normal model switch
	/// (EnsureLoaded unloads the previous model first).
	/// </summary>
	[ChannelSubscription<ModelsMessageType>(ModelsMessageType.UnloadModel)]
	public void UnloadModel()
	{
		Publish(ModelsMessageType.UnloadModel);
	}

	#endregion

	#region Records

	public record struct SelectModelMessage(string FilePath) : IChannelMessage;

	#endregion

	#region Enumerations

	public enum ModelsMessageType
	{
		RefreshModels,
		ModelsUpdated,
		SelectModel,
		UnloadModel
	}

	#endregion
}