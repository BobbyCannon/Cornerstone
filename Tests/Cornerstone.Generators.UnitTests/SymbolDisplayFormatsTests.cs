#region References

using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Generators.UnitTests;

[TestClass]
public class SymbolDisplayFormatsTests : GeneratorUnitTest
{
	#region Methods

	[TestMethod]
	public void ValidateFormat()
	{
		var compilation = CSharpCompilation.Create("test")
			.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
		var symbol = compilation.GetSpecialType(SpecialType.System_Int32);

		var fullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName);
		AreEqual("System.Int32", fullyQualifiedName);

		var globalFullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);
		AreEqual("global::System.Int32", globalFullyQualifiedName);
	}

	#endregion
}