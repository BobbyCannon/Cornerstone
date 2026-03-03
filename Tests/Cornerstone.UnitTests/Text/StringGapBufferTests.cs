#region References

using Cornerstone.Text;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Text;

public class StringGapBufferTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Constructors()
	{
		var actual = new StringGapBuffer();
		AreEqual(256, actual.Capacity);

		actual = new StringGapBuffer(10);
		AreEqual(10, actual.Capacity);

		actual = new StringGapBuffer("Hello World", 1);
		AreEqual("Hello World", actual.ToString());
		AreEqual(23, actual.Capacity);

		actual = new StringGapBuffer("Hello World");
		AreEqual("Hello World", actual.ToString());
		AreEqual(256, actual.Capacity);
	}

	[Test]
	public void SubString()
	{
		var actual = new StringGapBuffer("012345679");
		AreEqual("123", actual.SubString(1, 3));
		AreEqual("34", actual.SubString(3, 2));
	}

	#endregion
}