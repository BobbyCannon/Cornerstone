#region References

using Cornerstone.Collections;
using Cornerstone.Serialization;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Serialization;

public class SpeedyPackWriterTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Message()
	{
		var message = new TestMessage { Id = 123456, Price = 98.75, Version = 2 };
		var buffer = new SpeedyBuffer<byte>();
		SpeedyPackWriter.Write([message.Version, message.Id, message.Price], buffer);
		var actual = new TestMessage();
		IsTrue(actual.TryParse(buffer.AsSpan()));
		AreEqual(message, actual);
	}

	#endregion
}