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
		channel.Publish(new MessageA());
		AreEqual(0, bus.History.Count);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void HistoryEnabledRecordsAfterHandlersWithDurationAndCount()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		var ran = 0;
		channel.Subscribe<MessageB>(_ =>
		{
			ran++;
			Thread.Sleep(1);
		});

		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;

		channel.Publish(new MessageB());

		AreEqual(1, ran);
		AreEqual(1, bus.History.Count);
		var entry = bus.History[0];
		AreEqual(1, entry.Sequence);
		AreEqual(nameof(TestChannel), entry.ChannelName);
		AreEqual(nameof(MessageB), entry.Type);
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

		channel.Publish(new MessageA());
		channel.Publish(new MessageB());
		channel.Publish(new MessageC());

		AreEqual(2, bus.History.Count);
		AreEqual(nameof(MessageB), bus.History[0].Type);
		AreEqual(nameof(MessageC), bus.History[1].Type);

		bus.UninitializeLifecycle();
	}

	[TestMethod]
	public void HistoryCapturesHandlerError()
	{
		var bus = new KeystoneBus();
		var channel = new TestChannel();
		channel.Subscribe<MessageA>(_ => throw new InvalidOperationException("boom"));
		bus.Track(channel);
		bus.InitializeLifecycle();
		bus.IsHistoryEnabled = true;

		channel.Publish(new MessageA());

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
		channel.Publish(new MessageA());
		AreEqual(1, bus.History.Count);

		bus.IsHistoryEnabled = false;
		channel.Publish(new MessageB());
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

		channel.Publish(new NamedPayload());

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
		bus.HistoryFilter = "type:MessageB";

		channel.Publish(new MessageA());
		channel.Publish(new MessageB());
		channel.Publish(new MessageC());

		AreEqual(1, bus.History.Count);
		AreEqual(nameof(MessageB), bus.History[0].Type);

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

		channel.Publish(new MessageA());
		AreEqual(1, bus.History.Count);

		bus.HistoryFilter = "channel:Settings";
		channel.Publish(new MessageB());
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

		channel.Publish(new MessageA());
		channel.Publish(new MessageB());
		AreEqual(2, bus.History.Count);

		bus.UninitializeLifecycle();
	}

	#endregion

	#region Classes

	private readonly record struct MessageA : IChannelMessage;

	private readonly record struct MessageB : IChannelMessage;

	private readonly record struct MessageC : IChannelMessage;

	private sealed class NamedPayload : IChannelMessage
	{
	}

	private sealed class TestChannel : KeystoneChannel
	{
	}

	#endregion
}
