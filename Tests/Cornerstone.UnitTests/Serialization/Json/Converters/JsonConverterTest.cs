#region References

using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Text;
using CodeGenerator = Cornerstone.Generators.CodeGenerator;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Converters;

public class JsonConverterTest<T> : JsonConverterTest
	where T : JsonConverter, new()
{
	#region Constructors

	protected JsonConverterTest()
	{
		Converter = new T();
	}

	#endregion

	#region Properties

	public T Converter { get; }

	#endregion

	#region Methods

	protected void GenerateNewScenarios(string fileName, bool enableTestScenarioCreation)
	{
		if (!enableTestScenarioCreation)
		{
			return;
		}

		var filePath = $@"{UnitTestsDirectory}\Serialization\Json\Converters\{fileName}";
		var builder = new TextBuilder();
		var allTypes = TypeExtensions.AddNullables(Converter.GetSupportedTypes());
		var scenarioIndex = 0;
		var settings = Serializer.DefaultSettings;

		foreach (var type in allTypes)
		{
			var values = GetValuesForTesting(type);

			foreach (var value in values)
			{
				// new("bool", true, typeof(bool), "true"),
				var json = Converter.GetJsonString(value, settings);
				var line = string.Format(
					"new(\"{0} {1}\", {2}, typeof({1}), \"{3}\"),",
					scenarioIndex++,
					CSharpCodeWriter.GetCodeTypeName(type),
					CSharpCodeWriter.GenerateCode(value),
					json.Escape()
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

		var filePath = $@"{UnitTestsDirectory}\Serialization\Json\Converters\{fileName}";
		var builder = new TextBuilder();
		var scenarioIndex = 0;
		var serializerSettings = Serializer.DefaultSettings;
		var writerSettings = CodeGenerator.DefaultWriterOptions;

		foreach (var value in values)
		{
			// new("bool", true, typeof(bool), "true"),
			var json = Converter.GetJsonString(value, serializerSettings);
			var line = string.Format(
				"new(\"{0} {1}\", {2}, typeof({1}), \"{3}\"),",
				scenarioIndex++,
				CSharpCodeWriter.GetCodeTypeName(value.GetType()),
				CSharpCodeWriter.GenerateCode(value, CodeLanguage.CSharp, writerSettings),
				json.Escape()
			);

			builder.AppendLine(line);
		}

		UpdateFileIfNecessary(filePath, builder);
	}

	#endregion
}

public class JsonConverterTest : CornerstoneUnitTest
{
	#region Constructors

	static JsonConverterTest()
	{
		ConverterSettings = CodeGenerator.GetAllCodeCombinations<SerializationOptions>().ToArray();
	}

	#endregion

	#region Properties

	public static SerializationOptions[] ConverterSettings { get; }

	#endregion
}