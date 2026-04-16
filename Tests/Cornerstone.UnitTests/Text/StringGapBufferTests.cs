#region References

using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class StringGapBufferTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
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

	[TestMethod]
	public void SubString()
	{
		var actual = new StringGapBuffer("012345679");
		AreEqual("123", actual.Substring(1, 3));
		AreEqual("34", actual.Substring(3, 2));
	}

	#endregion
}