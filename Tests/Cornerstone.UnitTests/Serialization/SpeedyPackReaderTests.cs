#region References

using System;
using Cornerstone.Serialization;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Serialization;

public class SpeedyPackReaderTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Message()
	{
		var message = new TestMessage { Id = 123456, Price = 98.75, Version = 2 };
		var packed = SpeedyPacket.Pack([message.Version, message.Id, message.Price]);
		var actual = new TestMessage();
		IsTrue(actual.TryParse(packed));
		AreEqual(message, actual);
	}

	#endregion
}

public sealed class TestMessage
{
	#region Fields

	public int Id;
	public double Price;
	public byte Version;

	#endregion

	#region Methods

	public bool TryParse(ReadOnlySpan<byte> data)
	{
		var reader = new SpeedyPackReader(data);
		if (reader.Header.Length != 3)
		{
			return false;
		}

		if (!reader.TryPeekHeaderType(out var type)
			|| !SpeedyPack.IsByte(type)
			|| !reader.TryReadByte(out Version))
		{
			return false;
		}

		// Version 1
		if (!reader.TryPeekHeaderType(out type)
			|| !SpeedyPack.IsInt32(type)
			|| !reader.TryReadInt32(out Id))
		{
			return false;
		}

		if (!reader.TryPeekHeaderType(out type)
			|| !SpeedyPack.IsDouble(type)
			|| !reader.TryReadDouble(out Price))
		{
			return false;
		}

		return true;
	}

	#endregion
}