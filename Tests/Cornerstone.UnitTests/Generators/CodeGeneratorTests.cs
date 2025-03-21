#region References

using System;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Json;
using Cornerstone.Serialization;
using Cornerstone.Testing;
using Cornerstone.Text;
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
		var actual = CodeGenerator.GetAllCodeCombinations<SerializationSettings>().ToList();
		//AreEqual(expected, actual);
		AreEqual(5760, actual.Count);
		AreEqual(5760, actual
			.Select(x => x.GetHashCode())
			.Distinct()
			.Count()
		);
	}

	[TestMethod]
	public void GenerateUpdateWith()
	{
		var type = typeof(PartialUpdateValue);
		var builder = GenerateUpdateWith(type, type);
		CopyToClipboard(builder.ToString());
		Console.Write(builder.ToString());
	}

	[TestMethod]
	public void ConvertEnumToStaticProperties()
	{
		var type = typeof(JsonTokenType);
		var details = EnumExtensions.GetAllEnumDetails(type);
		var builder = new TextBuilder();

		builder.AppendLine($"public static class {type.Name}");
		builder.AppendLine("{");
		builder.PushIndent();
		foreach (var detail in details)
		{
			builder.AppendLine($"public static string {detail.Value.Name} = \"{detail.Value.Name}\";}}");
		}
		builder.PopIndent();
		builder.AppendLine("}");
		builder.Dump();

	}

	#endregion
}