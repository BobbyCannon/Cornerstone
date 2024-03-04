#region References

using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class TextBuilderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AppendWithIndent()
	{
		var document = new TextBuilder();
		document.Append("public class Test()");
		document.AppendLine();
		document.AppendLineThenPushIndent('{');
		document.AppendLine("public string Name { get; set; }");
		document.Append("public ");
		document.Append("bool Enabled ");
		document.AppendLineThenPopIndent("{ get; }");
		document.AppendLineThenPushIndent();
		document.AppendLine("public override string ToString()");
		document.AppendLineThenPushIndent("{");
		document.AppendLineThenPopIndent("return \"Test\";");
		document.AppendLineThenPopIndent('}');
		document.Append('}');

		var expected = @"public class Test()
{
	public string Name { get; set; }
	public bool Enabled { get; }

	public override string ToString()
	{
		return ""Test"";
	}
}";

		AreEqual(expected, document.ToString());
	}

	[TestMethod]
	public void Constructors()
	{
		var scenarios = new[] { null, string.Empty, "Hello World" };

		foreach (var scenario in scenarios)
		{
			var actual = new TextBuilder(scenario);
			AreEqual(scenario ?? string.Empty, actual.ToString());
		}
	}

	[TestMethod]
	public void Length()
	{
		var document = new TextBuilder();
		AreEqual(0, document.Length);
		document.Append('\r');
		AreEqual(1, document.Length);
	}

	[TestMethod]
	public void Trim()
	{
		var scenarios = new Dictionary<string, string>
		{
			{ "  2", "2" },
			{ "  2    ", "2" },
			{ " \t 2   \r\n    ", "2" }
		};

		var document = new TextBuilder();

		foreach (var scenario in scenarios)
		{
			document.Clear();
			document.Append(scenario.Key);
			document.Trim();

			AreEqual(scenario.Value, document.ToString());
		}

		document.Clear();
		document.Append("\r\n");
		AreEqual(2, document.Length);
		IsTrue((bool) document.GetMemberValue("_endsWithNewline"));

		document.Trim();
		IsFalse((bool) document.GetMemberValue("_endsWithNewline"));
		AreEqual(0, document.Length);
	}

	#endregion
}