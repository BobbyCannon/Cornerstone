#region References

using System;
using System.Linq;
using Cornerstone.Serialization;
using Cornerstone.UnitTests.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeGenerator = Cornerstone.Generators.CodeGenerator;

#endregion

namespace Cornerstone.UnitTests.Generators;

[TestClass]
public class CodeGeneratorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void PropertyCombinations()
	{
		var actual = CodeGenerator.GetAllCodeCombinations<SerializationOptions>().ToList();
		//AreEqual(expected, actual);
		AreEqual(4608, actual.Count);
		AreEqual(4608, actual
			.Select(x => x.GetHashCode())
			.Distinct()
			.Count()
		);
	}

	[TestMethod]
	public void GenerateUpdateWith()
	{
		var type = typeof(SettingsFileTests.SampleSettings);
		var builder = GenerateUpdateWith(type);
		CopyToClipboard(builder.ToString());
		Console.Write(builder.ToString());
	}

	#endregion
}