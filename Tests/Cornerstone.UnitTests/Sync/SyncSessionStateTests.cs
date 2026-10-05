#region References

using System;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncSessionStateTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CouldNotStartFactoryDoesNotMarkStarted()
	{
		var sessionId = Guid.NewGuid();
		var session = SyncSession.CouldNotStart(sessionId, "Full", this);
		AreEqual(sessionId, session.SessionId);
		AreEqual("Full", session.SyncType);
		IsTrue(session.State.HasFlag(SyncSessionState.CouldNotStart));
		IsFalse(session.SyncStarted);
		IsFalse(session.SyncRunning);
	}

	[TestMethod]
	public void ElapsedIsZeroUntilStartedThenFollowsClock()
	{
		var session = new SyncSession(this);
		AreEqual(TimeSpan.Zero, session.Elapsed);
		session.Start(Guid.NewGuid(), "Full", new SyncSettings());
		IncrementTime(seconds: 5);
		IsTrue(session.Elapsed >= TimeSpan.FromSeconds(5));
	}

	[TestMethod]
	public void NewSessionPercentIsZero()
	{
		var session = new SyncSession(this);
		AreEqual(0m, session.Percent);
		IsFalse(session.SyncConfigured);
	}

	[TestMethod]
	public void ShowProgressRequiresRunningPastThreshold()
	{
		var session = new SyncSession(this);
		session.ShowProgressThreshold = TimeSpan.FromSeconds(5);
		IsFalse(session.ShowProgress);
		session.UpdateState(SyncSessionState.Started);
		IsFalse(session.ShowProgress);
	}

	[TestMethod]
	public void SuccessfulAndCancelledAreIndependentOfCompleted()
	{
		var session = new SyncSession(this);
		session.UpdateState(SyncSessionState.Started);
		session.UpdateState(SyncSessionState.Successful);
		IsTrue(session.SyncSuccessful);
		IsFalse(session.SyncCompleted);

		session.UpdateState(SyncSessionState.Completed);
		IsTrue(session.SyncSuccessful);
		IsTrue(session.SyncCompleted);

		var cancelled = new SyncSession(this);
		cancelled.UpdateState(SyncSessionState.Started);
		cancelled.UpdateState(SyncSessionState.Cancelled);
		cancelled.UpdateState(SyncSessionState.Completed);
		IsTrue(cancelled.SyncCancelled);
		IsTrue(cancelled.SyncCompleted);
		IsFalse(cancelled.SyncSuccessful);
	}

	[TestMethod]
	public void SyncRunningFollowsStartedAndCompleted()
	{
		var session = new SyncSession(this);
		IsFalse(session.SyncRunning);
		IsFalse(session.SyncCompleted);
		IsFalse(session.SyncSuccessful);
		IsFalse(session.SyncCancelled);

		session.UpdateState(SyncSessionState.Started);
		IsTrue(session.SyncStarted);
		IsTrue(session.SyncRunning);

		session.UpdateState(SyncSessionState.Pulling);
		IsTrue(session.SyncRunning);

		session.UpdateState(SyncSessionState.Pushing);
		IsTrue(session.SyncRunning);

		session.UpdateState(SyncSessionState.Completed);
		IsTrue(session.SyncCompleted);
		IsFalse(session.SyncRunning);
	}

	[TestMethod]
	public void WaitForSyncStateTimeout()
	{
		var session = new SyncSession(this);
		IsFalse(session.WaitForSyncState(SyncSessionState.Completed, TimeSpan.Zero));
		session.UpdateState(SyncSessionState.Completed);
		IsTrue(session.WaitForSyncState(SyncSessionState.Completed, TimeSpan.Zero));
	}

	#endregion
}