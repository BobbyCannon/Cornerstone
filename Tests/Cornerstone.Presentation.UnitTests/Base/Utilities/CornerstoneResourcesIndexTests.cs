#region References

using System;
using System.IO;
using System.Text;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class CornerstoneResourcesIndexTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldCombinedSamePhysicalPathResources()
	{
		using var memoryStream = new MemoryStream();

		var resourceBytes = Encoding.UTF8.GetBytes("resource-data");
		CornerstoneResourcesIndexReaderWriter.WriteResources(memoryStream, new[]
		{
			new CornerstoneResourcesEntry
			{
				Path = "app.xaml",
				SystemPath = "app.ico",
				Size = resourceBytes.Length,
				Open = () => new MemoryStream(resourceBytes)
			},
			new CornerstoneResourcesEntry
			{
				Path = "!__CornerstoneDefaultWindowIcon",
				SystemPath = "app.ico",
				Size = resourceBytes.Length,
				Open = () => new MemoryStream(resourceBytes)
			}
		});

		memoryStream.Seek(4, SeekOrigin.Begin); // skip 4 bytes for "index size" field.

		var index = CornerstoneResourcesIndexReaderWriter.ReadIndex(memoryStream);

		CornerstoneTest.AreEqual("app.xaml", index[0].Path);
		CornerstoneTest.AreEqual(0, index[0].Offset);
		CornerstoneTest.AreEqual(resourceBytes.Length, index[0].Size);

		CornerstoneTest.AreEqual("!__CornerstoneDefaultWindowIcon", index[1].Path);
		CornerstoneTest.AreEqual(0, index[1].Offset);
		CornerstoneTest.AreEqual(resourceBytes.Length, index[1].Size);
	}

	[PresentationTestMethod]
	public void ShouldWriteAndReadTheSameResources()
	{
		using var memoryStream = new MemoryStream();

		var fooBytes = Encoding.UTF8.GetBytes("foo");
		var booBytes = Encoding.UTF8.GetBytes("boo");
		CornerstoneResourcesIndexReaderWriter.WriteResources(memoryStream,
			new[]
			{
				new CornerstoneResourcesEntry
				{
					Path = "foo.xaml", Size = fooBytes.Length, Open = () => new MemoryStream(fooBytes)
				},
				new CornerstoneResourcesEntry
				{
					Path = "boo.xaml", Size = booBytes.Length, Open = () => new MemoryStream(booBytes)
				}
			});

		memoryStream.Seek(4, SeekOrigin.Begin); // skip 4 bytes for "index size" field.

		var index = CornerstoneResourcesIndexReaderWriter.ReadIndex(memoryStream);
		var resourcesBasePosition = memoryStream.Position;

		Span<byte> buffer = stackalloc byte[index[0].Size];

		CornerstoneTest.AreEqual("foo.xaml", index[0].Path);
		CornerstoneTest.AreEqual(0, index[0].Offset);
		CornerstoneTest.AreEqual(fooBytes.Length, index[0].Size);

		memoryStream.Seek(resourcesBasePosition + index[0].Offset, SeekOrigin.Begin);
		memoryStream.ReadExactly(buffer);
		CornerstoneTest.AreEqual(fooBytes, buffer.ToArray());

		CornerstoneTest.AreEqual("boo.xaml", index[1].Path);
		CornerstoneTest.AreEqual(fooBytes.Length, index[1].Offset);
		CornerstoneTest.AreEqual(booBytes.Length, index[1].Size);

		memoryStream.Seek(resourcesBasePosition + index[1].Offset, SeekOrigin.Begin);
		memoryStream.ReadExactly(buffer);
		CornerstoneTest.AreEqual(booBytes, buffer.ToArray());
	}

	#endregion
}