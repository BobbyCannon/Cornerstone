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
	public void LastIndexOf()
	{
		var buffers = new IStringBuffer[]
		{
			//                   012345678901234567890123456789012345678901234
			new StringGapBuffer("The quick brown fox jumped over the lazy dog."),
			new StringRopeBuffer("The quick brown fox jumped over the lazy dog.")
		};

		foreach (var buffer in buffers)
		{
			Assert.AreEqual(4, buffer.LastIndexOf("quick", 9));
			Assert.AreEqual(4, buffer.LastIndexOf("quick", 8));
			Assert.AreEqual(-1, buffer.LastIndexOf("quick", 7));
		}
	}

	#endregion
}