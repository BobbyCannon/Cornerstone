#region References

using System;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Json;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Sync;
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
		var type = typeof(TokenData<,>);
		var builder = GenerateUpdateWith(type);
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