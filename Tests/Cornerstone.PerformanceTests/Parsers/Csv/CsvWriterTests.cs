#region References

using System.IO;
using Cornerstone.Collections;
using Cornerstone.Parsers.Csv;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Parsers.Csv;

[TestClass]
public class CsvWriterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void StreamWrite()
	{
		using var stringWriter = new StringWriter();
		var writer = new CsvWriter(new CsvOptions(), "First,Name", "Age", "Enabled");
		writer.WriteHeaders(stringWriter);
		writer.WriteLine(stringWriter, "Bobby", 21, true);
		stringWriter.ToString().Dump();

		using var stringReader = new StringReader(stringWriter.ToString());
		var reader = new CsvReader(stringReader, new CsvOptions());
		IsTrue(reader.Read());
		AreEqual(writer.Headers, reader.LineAsArray());
		IsTrue(reader.Read());
		AreEqual(new ReadOnlySet<string>("Bobby", "21", "True"), reader.LineAsArray());
	}

	#endregion
}