#region References

using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

#endregion

namespace Cornerstone.Generators.UnitTests;

public class SymbolDisplayFormatsTests : GeneratorUnitTest
{
	#region Methods

	[Test]
	public void ValidateFormat()
	{
		var compilation = CSharpCompilation.Create("test")
			.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
		var symbol = compilation.GetSpecialType(SpecialType.System_Int32);

		var fullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName);
		Assert.That("System.Int32" == fullyQualifiedName, actualExpression: fullyQualifiedName);
		
		var globalFullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);
		Assert.That("global::System.Int32" == globalFullyQualifiedName, actualExpression: globalFullyQualifiedName);
	}

	#endregion
}