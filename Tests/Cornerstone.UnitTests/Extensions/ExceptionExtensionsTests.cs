#region References

using System;
using System.IO;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class ExceptionExtensionsTests : CornerstoneUnitTest
{
	#region Fields

	private TextWriter _original;
	private StringWriter _stringWriter;

	#endregion

	#region Properties

	public TestContext TestContext { get; set; }

	#endregion

	#region Methods

	[TestMethod]
	public void Dump()
	{
		var scenarios = new (string Value, string Prefix, string Expected)[]
		{
			("Hello World", null, "Hello World\r\n"),
			("Bar", "Foo", "FooBar\r\n"),
			(null, null, "\r\n"),
			(null, "Prefix", "Prefix\r\n")
		};

		foreach (var scenario in scenarios)
		{
			scenario.Value.Dump(scenario.Prefix);
			var actual = _stringWriter.ToString();
			AreEqual(scenario.Expected, actual);

			TestCleanup();
			TestInitialize();
		}
	}

	[TestCleanup]
	public override void TestCleanup()
	{
		Console.SetOut(_original);
		base.TestCleanup();
	}

	[TestInitialize]
	public override void TestInitialize()
	{
		_stringWriter = new StringWriter();
		_original = Console.Out;
		Console.SetOut(_stringWriter);

		base.TestInitialize();
	}

	#endregion
}