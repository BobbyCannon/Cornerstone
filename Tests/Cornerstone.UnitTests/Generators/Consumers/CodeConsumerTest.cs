#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Testing;
using Cornerstone.Text;

#endregion

namespace Cornerstone.UnitTests.Generators.Consumers;

public abstract class CodeWriterTest<T> : CornerstoneUnitTest
	where T : new()
{
	#region Constructors

	protected CodeWriterTest()
	{
		Consumer = new T();
	}

	#endregion

	#region Properties

	public T Consumer { get; }

	#endregion

	#region Methods

	protected void GenerateNewScenarios(string fileName, bool enableTestScenarioCreation, params Type[] types)
	{
		if (RuntimeInformation.DotNetRuntimeVersion.Major <= 4)
		{
			// Do NOT generate scenarios on .NET48
			"We do not generate scenarios on .NET48".Dump();
			return;
		}

		if (!enableTestScenarioCreation)
		{
			"Not generating scenarios due to not being enabled".Dump();
			return;
		}

		var filePath = $@"{UnitTestsDirectory}\Generators\Consumers\{fileName}";
		var builder = new TextBuilder();
		var allTypes = TypeExtensions.AddNullables(types);

		foreach (var type in allTypes)
		{
			var values = GetValuesForTesting(type);

			foreach (var value in values)
			{
				var code = CSharpCodeWriter.GenerateCode(value);
				var line = string.Format(
					"new(\"{0}\", {1}, typeof({0}), \"{2}\"),",
					CSharpCodeWriter.GetCodeTypeName(type),
					code, code.Escape()
				);

				builder.AppendLine(line);
			}
		}

		UpdateFileIfNecessary(filePath, builder);
	}

	#endregion
}