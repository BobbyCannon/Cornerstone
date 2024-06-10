#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cornerstone.Net;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Text.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Buffers;

[TestClass]
public class TextWriterStringBufferTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void TextWriter()
	{
		var builder = new StringBuilder();
		var writer = new StringWriter(builder);
		var buffer = new TextWriterStringBuffer(writer);
		var consumer = new TextJsonConsumer(buffer, Serializer.DefaultSettings);

		var scenarios = new Dictionary<string, object>
		{
			{ "true", true },
			{ "\"ce502b7b-b7b9-493c-9fcf-760409b2b638\"", new Guid("ce502b7b-b7b9-493c-9fcf-760409b2b638") },
			{ "{\"Password\":\"abc123\",\"UserName\":\"john\"}", new Credential("john", "abc123") },
			{ "null", null },
			{ "false", false }
		};

		foreach (var x in scenarios)
		{
			builder.Clear();
			consumer.WriteObject(x.Value);
			AreEqual(x.Key, builder.ToString());
		}
	}

	#endregion
}