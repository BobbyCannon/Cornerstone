#region References

using System.Text;
using Cornerstone.Text.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Buffers;

[TestClass]
public class StringBufferTextReaderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Read()
	{
		var expected = "the quick brown foxed jumped";

		ForEachBuffers(buffer =>
			{
				using var reader = new StringBufferTextReader(buffer);
				var actual = new StringBuilder();
				var partialBuffer = new char[5];
				int count;

				while ((count = reader.Read(partialBuffer, 0, partialBuffer.Length)) > 0)
				{
					actual.Append(new string(partialBuffer, 0, count));
				}

				AreEqual(expected, actual.ToString());
			},
			expected
		);
	}
	
	[TestMethod]
	public void ReadBlock()
	{
		//              0    1    2    3    4    5 
		//              0123456789012345678901234567
		var expected = "the quick brown foxed jumped";

		ForEachBuffers(buffer =>
			{
				using var reader = new StringBufferTextReader(buffer);
				var actual = new StringBuilder();
				var partialBuffer = new char[5];
				int count;

				while ((count = reader.ReadBlock(partialBuffer, 0, partialBuffer.Length)) > 0)
				{
					actual.Append(new string(partialBuffer, 0, count));
				}

				AreEqual(expected, actual.ToString());
			},
			expected
		);
	}
	
	[TestMethod]
	public void ReadToEnd()
	{
		//              0    1    2    3    4    5 
		//              0123456789012345678901234567
		var expected = "the quick brown foxed jumped";

		ForEachBuffers(buffer =>
			{
				using var reader = new StringBufferTextReader(buffer);
				var actual = reader.ReadToEnd();
				AreEqual(expected, actual);
			},
			expected
		);
	}

	#endregion
}