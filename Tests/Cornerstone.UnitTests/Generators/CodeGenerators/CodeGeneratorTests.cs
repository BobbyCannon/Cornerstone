#region References

using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Text;
using CodeGenerator = Cornerstone.Generators.CodeGenerators.CodeGenerator;

#endregion

namespace Cornerstone.UnitTests.Generators.CodeGenerators;

public class CodeGeneratorTests<T> : CornerstoneUnitTest
    where T : CodeGenerator, new()
{
    #region Constructors

    public CodeGeneratorTests()
    {
        Generator = new T();
    }

    #endregion

    #region Properties

    public T Generator { get; }

    #endregion

    #region Methods

    protected void GenerateNewScenarios(string fileName, bool enableTestScenarioCreation)
    {
		if (!enableTestScenarioCreation)
		{
			return;
		}

        var filePath = $@"{UnitTestsDirectory}\Generators\CodeGenerators\{fileName}";
        var builder = new TextBuilder();
        var allTypes = TypeExtensions.AddNullables(Generator.GetSupportedTypes());
        var scenarioIndex = 0;

        foreach (var type in allTypes)
        {
            var values = GetValuesForTesting(type);

            foreach (var value in values)
            {
                // public TestScenario(string name, object from, Type fromType, object to, Type toType)
                var code = CSharpCodeWriter.GenerateCode(value);
                var line = string.Format(
                    "new(\"{0}: {1}\", {2}, typeof({1}), \"{3}\", typeof(string)),",
                    scenarioIndex++,
                    CSharpCodeWriter.GetCodeTypeName(type),
                    code, code.Escape()
                );

                builder.AppendLine(line);
            }
        }

        UpdateFileIfNecessary(filePath, builder);
    }
	
	protected void GenerateNewScenarios(string fileName, bool enableTestScenarioCreation, params object[] values)
    {
		if (!enableTestScenarioCreation)
		{
			return;
		}

        var filePath = $@"{UnitTestsDirectory}\Generators\CodeGenerators\{fileName}";
        var builder = new TextBuilder();
        var scenarioIndex = 0;
		var settings = Cornerstone.Generators.CodeGenerator.DefaultWriterOptions;

        foreach (var value in values)
        {
            // public TestScenario(string name, object from, Type fromType, object to, Type toType)
            var type = value.GetType();
            var code = CSharpCodeWriter.GenerateCode(value, CodeLanguage.CSharp, settings);
            var line = string.Format(
                "new(\"{0}: {1}\", {2}, typeof({1}), \"{3}\", typeof(string)),",
                scenarioIndex++,
                CSharpCodeWriter.GetCodeTypeName(type),
                code, code.Escape()
            );

            builder.AppendLine(line);
        }

        UpdateFileIfNecessary(filePath, builder);
    }

    #endregion
}