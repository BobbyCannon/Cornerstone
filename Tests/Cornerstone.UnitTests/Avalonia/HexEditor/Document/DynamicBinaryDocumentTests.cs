#region References

using System;
using Cornerstone.Avalonia.HexEditor.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.HexEditor.Document;

[TestClass]
public class DynamicBinaryDocumentTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void WriteByteOutsideBoundary()
	{
		var document = new DynamicBinaryDocument();
		document.Write(3, new ReadOnlySpan<byte>([1, 2, 3]));
		var actual = document.ToArray();
		AreEqual(new byte[] { 0, 0, 0, 1, 2, 3 }, actual);
	}

	[TestMethod]
	public void WriteBytes()
	{
		var document = new DynamicBinaryDocument();
		document.Write(0, new ReadOnlySpan<byte>([1, 2, 3]));
		var actual = document.ToArray();
		AreEqual(new byte[] { 1, 2, 3 }, actual);

		document.Write(0, new Span<byte>([4, 5, 6]));
		actual = document.ToArray();
		AreEqual(new byte[] { 4, 5, 6 }, actual);

		document.Write(0, [7, 8, 9, 10]);
		actual = document.ToArray();
		AreEqual(new byte[] { 7, 8, 9, 10 }, actual);
	}

	#endregion
}