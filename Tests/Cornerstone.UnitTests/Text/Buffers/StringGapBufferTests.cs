#region References

using Cornerstone.Text.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Buffers;

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
			AreEqual(4, buffer.LastIndexOf("quick", 9));
			AreEqual(4, buffer.LastIndexOf("quick", 8));
			AreEqual(-1, buffer.LastIndexOf("quick", 7));
		}
	}

	#endregion
}