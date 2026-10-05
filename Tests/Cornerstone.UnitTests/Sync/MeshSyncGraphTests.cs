#region References

using Cornerstone.Sample.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class MeshSyncGraphTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void SpokeLastWriteWinsThenRimCopies()
	{
		var graph = new MeshSyncGraph(this, RuntimeInformation, Dispatcher);
		try
		{
			graph.Prepare();
			IncrementTime(seconds: 5);
			graph.SaveName(MeshNode.North, "FromNorth");

			var spoke = graph.Sync(MeshLink.NorthToCenter);
			IsTrue(spoke.SyncSuccessful, spoke.State.ToString());
			AreEqual("FromNorth", graph.ReadCard(MeshNode.Center).Name);
			AreEqual("Account", graph.ReadCard(MeshNode.East).Name);

			var rim = graph.Sync(MeshLink.NorthToEast);
			IsTrue(rim.SyncSuccessful, rim.State.ToString());
			AreEqual("FromNorth", graph.ReadCard(MeshNode.East).Name);
		}
		finally
		{
			graph.Dispose();
		}
	}

	#endregion
}
