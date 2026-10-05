#region References

using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Keystone.Channels;

[SourceReflection]
[DependencyInjected]
public partial class SyncChannel : KeystoneChannel
{
	#region Methods

	[RelayCommand]
	public void AddSampleAccount()
	{
		AddClientAccount("Sample User", "sample@cornerstone.local");
	}

	[RelayCommand]
	public void ApplyDatabases()
	{
		ApplyDatabaseKinds();
	}

	[RelayCommand]
	public void InjectAccounts()
	{
		InjectAccountBatch();
	}

	[RelayCommand]
	public void ModifyAccounts()
	{
		ModifyAccountBatch();
	}

	public void StartSync(string syncType)
	{
		SyncRequest(syncType);
	}

	[RelayCommand]
	public void StartSyncAll()
	{
		StartSync(SampleSyncClient.SyncAll);
	}

	#endregion

	#region Records

	[ChannelMessage<SyncChannel>]
	public record struct AddClientAccountMessage(string Name, string Email) : IChannelMessage;

	[ChannelMessage<SyncChannel>]
	public record struct ApplyDatabaseKindsMessage : IChannelMessage;

	[ChannelMessage<SyncChannel>]
	public record struct InjectAccountBatchMessage : IChannelMessage;

	[ChannelMessage<SyncChannel>]
	public record struct ModifyAccountBatchMessage : IChannelMessage;

	[ChannelMessage<SyncChannel>]
	public record struct SyncCompletedMessage(SyncSession Session) : IChannelMessage;

	[ChannelMessage<SyncChannel>]
	public record struct SyncRequestMessage(string SyncType) : IChannelMessage;

	#endregion
}
