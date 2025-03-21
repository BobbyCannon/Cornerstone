#region References

using System;
using System.IO;
using Cornerstone.FileSystem;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.FileSystem;

[TestClass]
public class StorageServiceTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void WriteAllText()
	{
		Directory.Delete(TempDirectory, true);

		var tempFile = Path.Join(TempDirectory, "Folder1", "Folder2", "Folder3", "Test.txt");
		var expected = Guid.NewGuid().ToString();

		var service = new FileService();
		service.WriteAllText(tempFile, expected);

		var actual = File.ReadAllText(tempFile);
		actual.Dump();

		AreEqual(expected, actual);
	}

	#endregion
}