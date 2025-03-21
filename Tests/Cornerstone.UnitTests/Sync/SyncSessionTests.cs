#region References

using System;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncSessionTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CouldNotStart()
	{
		var sessionId = new Guid("7C871549-337F-4D41-B342-5982941A52B1");
		var actual = SyncSession.CouldNotStart(null, sessionId, null, this);
		AreEqual(SyncSessionState.CouldNotStart, actual.State);
		AreEqual(sessionId, actual.SessionId);
		AreEqual(TimeSpan.Zero, actual.Elapsed);
		AreEqual(0, actual.Percent);
	}

	#endregion
}