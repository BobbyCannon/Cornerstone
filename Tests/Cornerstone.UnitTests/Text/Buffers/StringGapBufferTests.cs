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
			AreEqual(4, buffer.LastIndexOf(9, "quick"));
			AreEqual(4, buffer.LastIndexOf(8, "quick"));
			AreEqual(-1, buffer.LastIndexOf(7, "quick"));
		}
	}

	#endregion
}