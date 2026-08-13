#region References

using System;
using System.Threading;
using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Keystone;

[TestClass]
public class KeystoneBusHistoryTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void HistoryDisabledDoesNotRecord()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		bus.Track(channel);
		bus.InitializeLifecycle();

		IsFalse(bus.IsHistoryEnabled);
		channel.PublishTest(1);
		AreEqual(0, bus.History.Count);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void HistoryEnabledRecordsAfterHandlersWithDurationAndCount()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		var ran = 0;
		channel.SubscribeTest(7, () =>
		{
			ran++;
			Thread.Sleep(1);
		});

		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;

		channel.PublishTest(7);

		AreEqual(1, ran);
		AreEqual(1, bus.History.Count);
		var entry = bus.History[0];
		AreEqual(1, entry.Sequence);
		AreEqual(nameof(TestChannel), entry.ChannelName);
		AreEqual(7, entry.Type);
		AreEqual(1, entry.HandlerCount);
		IsTrue(entry.ElapsedTicks >= 0);
		IsFalse(entry.HadError);
		AreEqual(string.Empty, entry.ErrorMessage);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void HistoryRespectsLimit()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.History.Limit = 2;
		bus.IsHistoryEnabled = true;

		channel.PublishTest(1);
		channel.PublishTest(2);
		channel.PublishTest(3);

		AreEqual(2, bus.History.Count);
		// Unordered list drops oldest from the start when over limit.
		AreEqual(2, bus.History[0].Type);
		AreEqual(3, bus.History[1].Type);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void HistoryCapturesHandlerError()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		channel.SubscribeTest(3, () => throw new InvalidOperationException("boom"));
		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;

		channel.PublishTest(3);

		AreEqual(1, bus.History.Count);
		IsTrue(bus.History[0].HadError);
		AreEqual("boom", bus.History[0].ErrorMessage);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void DisablingHistoryStopsRecording()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;
		channel.PublishTest(1);
		AreEqual(1, bus.History.Count);

		bus.IsHistoryEnabled = false;
		channel.PublishTest(2);
		AreEqual(1, bus.History.Count);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void MessagePayloadNamePreferredForHistoryName()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;

		channel.PublishTest(9, new NamedPayload());

		AreEqual(1, bus.History.Count);
		AreEqual(nameof(NamedPayload), bus.History[0].Name);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void HistoryFilterSkipsNonMatchingTypes()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;
		bus.HistoryFilter = "type:2";

		channel.PublishTest(1);
		channel.PublishTest(2);
		channel.PublishTest(3);

		AreEqual(1, bus.History.Count);
		AreEqual(2, bus.History[0].Type);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void HistoryFilterChannelContains()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;
		bus.HistoryFilter = "channel:Test";

		channel.PublishTest(1);
		AreEqual(1, bus.History.Count);

		bus.HistoryFilter = "channel:Settings";
		channel.PublishTest(2);
		AreEqual(1, bus.History.Count);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void EmptyHistoryFilterRecordsAll()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;
		bus.HistoryFilter = string.Empty;

		channel.PublishTest(1);
		channel.PublishTest(2);
		AreEqual(2, bus.History.Count);

		bus.UninitializeLifecycle();
	}

	#endregion

	#region Classes

	private sealed class NamedPayload : IChannelMessage
	{
	}

	private sealed class TestChannel : KeystoneChannel
	{
		public void PublishTest(int type)
		{
			Publish(type);
		}

		public void PublishTest(int type, IChannelMessage message)
		{
			Publish(type, message);
		}

		public void SubscribeTest(int type, Action handler)
		{
			Subscribe(type, handler);
		}
	}

	#endregion
}
