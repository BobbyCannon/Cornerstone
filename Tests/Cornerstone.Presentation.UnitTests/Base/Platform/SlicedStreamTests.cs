#region References

using System.IO;
using Cornerstone.Presentation.Platform.Internal;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Platform;

[TestClass]
public class SlicedStreamTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(2, SeekOrigin.Begin, 22, 2, 9)]
	[DataRow(2, SeekOrigin.Current, 22, 17, 24)]
	[DataRow(-2, SeekOrigin.End, 22, 40, 47)]
	public void SeekWorks(
		long offset,
		SeekOrigin origin,
		long startingUnderlyingPosition,
		long expectedPosition,
		long expectedUnderlyingPosition)
	{
		var memoryStream = new MemoryStream(new byte[1024]);
		var slicedStream = new SlicedStream(memoryStream, 7, 42);
		memoryStream.Position = startingUnderlyingPosition;

		slicedStream.Seek(offset, origin);

		CornerstoneTest.AreEqual(expectedPosition, slicedStream.Position);
		CornerstoneTest.AreEqual(expectedUnderlyingPosition, memoryStream.Position);
	}

	#endregion
}