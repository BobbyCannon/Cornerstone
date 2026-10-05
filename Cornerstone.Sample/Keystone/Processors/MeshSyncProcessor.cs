#region References

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone.Channels;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Keystone.Processors;

[SourceReflection]
[DependencyInjected]
public partial class MeshSyncProcessor : AppProcessor
{
	#region Fields

	private readonly MeshSyncGraph _graph;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public MeshSyncProcessor(
		AppBus bus,
		AppState state,
		IDateTimeProvider dateTimeProvider,
		IRuntimeInformation runtimeInformation,
		IDispatcher dispatcher
	)
		: base(bus, state)
	{
		_graph = new MeshSyncGraph(dateTimeProvider, runtimeInformation, dispatcher);
	}

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		Bus.Mesh.SubscribeToSaveNode(OnSaveNode);
		Bus.Mesh.SubscribeToSyncLink(OnSyncLink);
		Bus.Mesh.SubscribeToMeshSyncAll(OnSyncAll);
		base.InitializeLifecycle();
	}

	public override void StartLifecycle()
	{
		_graph.Prepare();
		State.Mesh.IsAvailable = true;
		SetBusy(false);
		CopyCards();
		State.Mesh.LastStatus = "Ready. Edit a display name, save that database, then sync a red link.";
		base.StartLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		Bus.Mesh.UnsubscribeToSaveNode(OnSaveNode);
		Bus.Mesh.UnsubscribeToSyncLink(OnSyncLink);
		Bus.Mesh.UnsubscribeToMeshSyncAll(OnSyncAll);
		_graph.Dispose();
		base.UninitializeLifecycle();
	}

	private void CopyCards()
	{
		Copy(MeshNode.Center);
		Copy(MeshNode.North);
		Copy(MeshNode.East);
		Copy(MeshNode.South);
		Copy(MeshNode.West);
	}

	private void Copy(MeshNode node)
	{
		var card = _graph.ReadCard(node);
		switch (node)
		{
			case MeshNode.Center:
				State.Mesh.CenterName = card.Name;
				State.Mesh.CenterModifiedOn = card.ModifiedOn;
				break;
			case MeshNode.North:
				State.Mesh.NorthName = card.Name;
				State.Mesh.NorthModifiedOn = card.ModifiedOn;
				break;
			case MeshNode.East:
				State.Mesh.EastName = card.Name;
				State.Mesh.EastModifiedOn = card.ModifiedOn;
				break;
			case MeshNode.South:
				State.Mesh.SouthName = card.Name;
				State.Mesh.SouthModifiedOn = card.ModifiedOn;
				break;
			case MeshNode.West:
				State.Mesh.WestName = card.Name;
				State.Mesh.WestModifiedOn = card.ModifiedOn;
				break;
		}
	}

	private string NameFor(MeshNode node)
	{
		switch (node)
		{
			case MeshNode.Center:
				return State.Mesh.CenterName;
			case MeshNode.North:
				return State.Mesh.NorthName;
			case MeshNode.East:
				return State.Mesh.EastName;
			case MeshNode.South:
				return State.Mesh.SouthName;
			case MeshNode.West:
				return State.Mesh.WestName;
			default:
				return string.Empty;
		}
	}

	private void OnSaveNode(MeshChannel.SaveNodeMessage message)
	{
		if (!State.Mesh.CanRunWork)
		{
			return;
		}

		var node = message.Node;
		var name = NameFor(node);
		RunWork(() =>
		{
			_graph.SaveName(node, name);
			return $"Saved {name} on {node}.";
		});
	}

	private void OnSyncAll(MeshChannel.MeshSyncAllMessage _)
	{
		if (!State.Mesh.CanRunWork)
		{
			return;
		}

		RunWork(() =>
		{
			foreach (var link in _graph.SyncAllOrder)
			{
				var status = Describe(link, _graph.Sync(link));
				if (status != null)
				{
					return status;
				}
			}

			return "Synced all eight links.";
		});
	}

	private void OnSyncLink(MeshChannel.SyncLinkMessage message)
	{
		if (!State.Mesh.CanRunWork)
		{
			return;
		}

		var link = message.Link;
		RunWork(() => Describe(link, _graph.Sync(link)) ?? $"Synced {link}.");
	}

	private void RunWork(Func<string> work)
	{
		SetBusy(true);
		State.Mesh.LastStatus = "Working...";
		Task.Run(() =>
		{
			string status;
			TimeSpan elapsed;
			try
			{
				var watch = Stopwatch.StartNew();
				status = work();
				watch.Stop();
				elapsed = watch.Elapsed;
			}
			catch (Exception ex)
			{
				status = ex.Message;
				elapsed = TimeSpan.Zero;
			}

			try
			{
				CopyCards();
			}
			catch (Exception ex)
			{
				status = $"{status} Refresh failed: {ex.Message}";
			}

			State.Mesh.LastElapsed = elapsed;
			State.Mesh.LastStatus = status;
			SetBusy(false);
		});
	}

	private void SetBusy(bool busy)
	{
		State.Mesh.CanRunWork = State.Mesh.IsAvailable && !busy;
	}

	private static string Describe(MeshLink link, SyncSession session)
	{
		if ((session != null) && session.SyncSuccessful)
		{
			return null;
		}

		var builder = new StringBuilder();
		builder.Append(link);
		builder.Append(" failed");
		if (session == null)
		{
			return builder.ToString();
		}

		builder.Append(" (");
		builder.Append(session.State);
		builder.Append(')');
		foreach (var issue in session.SyncIssues)
		{
			builder.Append(' ');
			builder.Append(issue.Message);
			break;
		}

		return builder.ToString();
	}

	#endregion
}
