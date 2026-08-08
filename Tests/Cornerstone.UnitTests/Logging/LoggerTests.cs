#region References

using System;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Logging;

[TestClass]
public class LoggerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CapacityMustBePowerOfTwoAtLeastFour()
	{
		ExpectedException<ArgumentException>(() => _ = new Logger(2));
		ExpectedException<ArgumentException>(() => _ = new Logger(10));
		_ = new Logger(4);
		_ = new Logger(64);
	}

	[TestMethod]
	public void ClearResetsCountWithoutChangingCapacity()
	{
		var logger = new Logger(8, this);
		logger.Write(LogLevel.Information, "a");
		logger.Write(LogLevel.Information, "b");

		logger.Clear();

		AreEqual(0, logger.Count);
		AreEqual(8, logger.Capacity);
		AreEqual(0UL, logger.DroppedCount);
		AreEqual(0, logger.Snapshot().Length);
	}

	[TestMethod]
	public void IndependentInstancesDoNotShareState()
	{
		var first = new Logger(8, this);
		var second = new Logger(8, this);

		first.Write(LogLevel.Information, "only-first");

		AreEqual(1, first.Count);
		AreEqual(0, second.Count);
		AreEqual("only-first", first.Snapshot()[0].Message);
	}

	[TestMethod]
	public void LevelFilterDoesNotAdvanceCount()
	{
		var logger = new Logger(8, this, LogLevel.Warning);
		logger.Write(LogLevel.Debug, "ignored");
		logger.Write(LogLevel.Information, "ignored");
		logger.Write(LogLevel.Warning, "kept");

		AreEqual(1, logger.Count);
		AreEqual("kept", logger.Snapshot()[0].Message);
	}

	[TestMethod]
	public void MinimumLevelNoneDisablesAllWrites()
	{
		var logger = new Logger(8, this)
		{
			MinimumLevel = LogLevel.None
		};

		logger.Write(LogLevel.Critical, "nope");
		AreEqual(0, logger.Count);
	}

	[TestMethod]
	public void ParallelWritersStayWithinCapacity()
	{
		var logger = new Logger(64, this);

		Parallel.For(0, 1000, i => logger.Write(LogLevel.Information, i.ToString()));

		IsTrue(logger.Count <= logger.Capacity);
		IsTrue(logger.DroppedCount > 0);
		AreEqual(logger.Capacity, logger.Count);
	}

	[TestMethod]
	public void RecycledSlotDoesNotRetainOldExceptionOrMessage()
	{
		var logger = new Logger(4, this);
		var exception = new InvalidOperationException("old");

		logger.Write(LogLevel.Error, Guid.Empty, "old-message", exception);
		logger.Write(LogLevel.Information, "a");
		logger.Write(LogLevel.Information, "b");
		logger.Write(LogLevel.Information, "c");
		logger.Write(LogLevel.Information, "d"); // drops oldest (old-message)

		var snapshot = logger.Snapshot();
		AreEqual(4, snapshot.Length);
		IsFalse(snapshot.Any(x => x.Message == "old-message"));
		IsFalse(snapshot.Any(x => x.Exception != null));
		AreEqual("a", snapshot[0].Message);
		AreEqual("d", snapshot[3].Message);
	}

	[TestMethod]
	public void SequenceIsMonotonicAcrossWrap()
	{
		var logger = new Logger(4, this);

		for (var i = 0; i < 10; i++)
		{
			logger.Write(LogLevel.Information, i.ToString());
		}

		var sequences = logger.Snapshot().Select(x => x.Sequence).ToArray();
		AreEqual(4, sequences.Length);
		AreEqual(7UL, sequences[0]);
		AreEqual(8UL, sequences[1]);
		AreEqual(9UL, sequences[2]);
		AreEqual(10UL, sequences[3]);
	}

	[TestMethod]
	public void SnapshotIsOldestToNewest()
	{
		var logger = new Logger(8, this);
		logger.Write(LogLevel.Information, "first");
		logger.Write(LogLevel.Warning, "second");
		logger.Write(LogLevel.Error, "third");

		var snapshot = logger.Snapshot();
		AreEqual(3, snapshot.Length);
		AreEqual("first", snapshot[0].Message);
		AreEqual("second", snapshot[1].Message);
		AreEqual("third", snapshot[2].Message);
		AreEqual(LogLevel.Error, snapshot[2].Level);
	}

	[TestMethod]
	public void WrapDropsOldestAndTracksDroppedCount()
	{
		var logger = new Logger(4, this);

		for (var i = 0; i < 7; i++)
		{
			logger.Write(LogLevel.Information, i.ToString());
		}

		AreEqual(4, logger.Count);
		AreEqual(3UL, logger.DroppedCount);

		var snapshot = logger.Snapshot();
		AreEqual("3", snapshot[0].Message);
		AreEqual("4", snapshot[1].Message);
		AreEqual("5", snapshot[2].Message);
		AreEqual("6", snapshot[3].Message);
	}

	[TestMethod]
	public void WriteWithSessionAndTimestamp()
	{
		var logger = new Logger(8, this);
		var sessionId = Guid.NewGuid();
		var timestamp = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

		logger.Write(LogLevel.Information, sessionId, "sync", timestamp);

		var entry = logger.Snapshot()[0];
		AreEqual(sessionId, entry.SessionId);
		AreEqual(timestamp, entry.Timestamp);
		AreEqual("sync", entry.Message);
	}

	#endregion
}
