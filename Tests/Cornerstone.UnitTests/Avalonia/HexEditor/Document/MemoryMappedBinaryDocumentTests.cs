#region References

using System;
using System.IO;
using System.Text;
using Cornerstone.Avalonia.HexEditor.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.HexEditor.Document;

[TestClass]
public class MemoryMappedBinaryDocumentTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ReadFromFile()
	{
		var path = Path.Combine(TempDirectory, "MemoryMapped.txt");
		File.WriteAllText(path, "123456ABCDEF");
		var document = new MemoryMappedBinaryDocument(path);
		var actual = new Span<byte>(new byte[4]);

		document.Read(0, actual);
		AreEqual("1234", Encoding.UTF8.GetString(actual));

		document.Read(6, actual);
		AreEqual("ABCD", Encoding.UTF8.GetString(actual));
	}

	#endregion
}