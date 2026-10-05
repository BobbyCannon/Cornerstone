#region References

using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Sync;

#endregion

namespace Cornerstone.Sample.Keystone.Channels;

[SourceReflection]
[DependencyInjected]
public partial class MeshChannel : KeystoneChannel
{
	#region Methods

	[RelayCommand]
	public void SaveCenter()
	{
		SaveNode(MeshNode.Center);
	}

	[RelayCommand]
	public void SaveEast()
	{
		SaveNode(MeshNode.East);
	}

	[RelayCommand]
	public void SaveNorth()
	{
		SaveNode(MeshNode.North);
	}

	[RelayCommand]
	public void SaveSouth()
	{
		SaveNode(MeshNode.South);
	}

	[RelayCommand]
	public void SaveWest()
	{
		SaveNode(MeshNode.West);
	}

	[RelayCommand]
	public void SyncAll()
	{
		MeshSyncAll();
	}

	[RelayCommand]
	public void SyncEastToCenter()
	{
		SyncLink(MeshLink.EastToCenter);
	}

	[RelayCommand]
	public void SyncEastToSouth()
	{
		SyncLink(MeshLink.EastToSouth);
	}

	[RelayCommand]
	public void SyncNorthToCenter()
	{
		SyncLink(MeshLink.NorthToCenter);
	}

	[RelayCommand]
	public void SyncNorthToEast()
	{
		SyncLink(MeshLink.NorthToEast);
	}

	[RelayCommand]
	public void SyncSouthToCenter()
	{
		SyncLink(MeshLink.SouthToCenter);
	}

	[RelayCommand]
	public void SyncSouthToWest()
	{
		SyncLink(MeshLink.SouthToWest);
	}

	[RelayCommand]
	public void SyncWestToCenter()
	{
		SyncLink(MeshLink.WestToCenter);
	}

	[RelayCommand]
	public void SyncWestToNorth()
	{
		SyncLink(MeshLink.WestToNorth);
	}

	#endregion

	#region Records

	[ChannelMessage<MeshChannel>]
	public record struct MeshSyncAllMessage : IChannelMessage;

	[ChannelMessage<MeshChannel>]
	public record struct SaveNodeMessage(MeshNode Node) : IChannelMessage;

	[ChannelMessage<MeshChannel>]
	public record struct SyncLinkMessage(MeshLink Link) : IChannelMessage;

	#endregion
}
