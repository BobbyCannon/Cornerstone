#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Compare;
using Cornerstone.Convert.Converters;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Testing;
using Cornerstone.Text;

#endregion

namespace Cornerstone.UnitTests.Convert.Converters;

public abstract class ConverterTests<T> : CornerstoneUnitTest
	where T : BaseConverter, new()
{
	#region Constructors

	protected ConverterTests()
	{
		Converter = new T();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The converter for this test.
	/// </summary>
	public T Converter { get; }

	#endregion

	#region Methods

	protected void GenerateNewScenarios(string fileName, bool enableTestScenarioCreation)
	{
		GenerateNewScenarios(fileName, enableTestScenarioCreation, Converter.FromTypes.ToArray(), Converter.ToTypes.ToArray());
	}

	protected void GenerateNewScenarios(string fileName, bool enableTestScenarioCreation, Type[] fromTypes, Type[] toTypes)
	{
		if (GetRuntimeInformation().DotNetRuntimeVersion.Major <= 4)
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

		var filePath = $@"{UnitTestsDirectory}\Convert\Converters\{fileName}";
		var nextDecimal = 0.0m;
		var builder = new TextBuilder();
		var scenarioIndex = 0;
		var (fromAll, toAll, fromFiltered, toFiltered) = FilterTypes(fromTypes, toTypes);

		nextDecimal = ProcessTypes(fromAll, toAll, builder, nextDecimal, ref scenarioIndex);
		builder.AppendLine("#if (!NET48)");
		nextDecimal = ProcessTypes(fromAll, toFiltered, builder, nextDecimal, ref scenarioIndex);
		nextDecimal = ProcessTypes(fromFiltered, toAll, builder, nextDecimal, ref scenarioIndex);
		ProcessTypes(fromFiltered, toFiltered, builder, nextDecimal, ref scenarioIndex);
		builder.AppendLine("#endif");

		UpdateFileIfNecessary(filePath, builder);
	}

	protected void TestInvalidValues<T2>(params TestScenarioValue[] invalidValues) where T2 : Enum
	{
		TestInvalidValues([typeof(T2)], invalidValues);
	}

	protected void TestInvalidValues(params TestScenarioValue[] invalidValues)
	{
		TestInvalidValues(Array.Empty<Type>(), invalidValues);
	}

	protected void TestScenarios(params TestScenario[] scenarios)
	{
		var setting = Comparer.Settings;
		setting.DoubleTolerance = 0.01d;
		setting.FloatTolerance = 0.01f;

		for (var index = 0; index < scenarios.Length; index++)
		{
			var scenario = scenarios[index];
			scenario.Name.Dump();

			var result = Converter.TryConvertTo(scenario.From.Value, scenario.From.Type, scenario.To.Type, out var actualTo);
			var message = $"{scenario.Name} - The converter failed to convert.";

			IsTrue(result, message);
			AreEqual(scenario.To.Value, actualTo, message, setting);
		}
	}

	protected void ValidateCanConvert()
	{
		foreach (var fromType in Converter.FromTypes)
		foreach (var toType in Converter.ToTypes)
		{
			IsTrue(Converter.CanConvert(fromType, toType));
		}
	}

	private void AddTypes(ITextBuilder builder, Type fromType, Type toType, ref decimal nextDecimal, ref int scenarioIndex)
	{
		// Handles matching types and invalid combinations
		if (!Converter.CanConvert(fromType, toType))
		{
			return;
		}

		var value = GetBestValueForTesting(fromType, toType, ref nextDecimal);
		var (fromString, toString) = GenerateConvertTestString(Converter, fromType, toType, value);

		// ReSharper disable once UseStringInterpolation
		var line = value == null
			? string.Format(
				"new(\"{0}: {1} -> {2}\", {3}, typeof({1}), {4}, typeof({2})),",
				scenarioIndex++,
				CSharpCodeWriter.GetCodeTypeName(fromType),
				CSharpCodeWriter.GetCodeTypeName(toType),
				fromString,
				toString
			)
			: string.Format(
				"new(\"{0}: {1} -> {2}\", {3}, {4}),",
				scenarioIndex++,
				CSharpCodeWriter.GetCodeTypeName(fromType),
				CSharpCodeWriter.GetCodeTypeName(toType),
				fromString,
				toString
			);

		builder.AppendLine(line);
	}

	private (Type[] fromAll, Type[] toAll, Type[] fromFiltered, Type[] toFiltered) FilterTypes(Type[] fromTypes, Type[] toTypes)
	{
		var fromAll = new List<Type>();
		var toAll = new List<Type>();
		var fromFiltered = new List<Type>();
		var toFiltered = new List<Type>();

		foreach (var fromType in fromTypes)
		{
			if (CheckTypeForClassicFramework(fromType))
			{
				fromAll.Add(fromType);
			}
			else
			{
				fromFiltered.Add(fromType);
			}
		}

		foreach (var toType in toTypes)
		{
			if (CheckTypeForClassicFramework(toType))
			{
				toAll.Add(toType);
			}
			else
			{
				toFiltered.Add(toType);
			}
		}

		return (
			fromAll.ToArray(),
			toAll.ToArray(),
			fromFiltered.ToArray(),
			toFiltered.ToArray()
		);
	}

	private decimal ProcessTypes(Type[] fromTypes, Type[] toTypes, TextBuilder builder, decimal nextDecimal, ref int scenarioIndex)
	{
		foreach (var fromType in fromTypes)
		{
			foreach (var toType in toTypes)
			{
				AddTypes(builder, fromType, toType, ref nextDecimal, ref scenarioIndex);

				// Skip exact same types
				if (toType == fromType)
				{
					continue;
				}

				AddTypes(builder, toType, fromType, ref nextDecimal, ref scenarioIndex);
			}
		}
		return nextDecimal;
	}

	private void TestInvalidValues(Type[] additionalTypes, params TestScenarioValue[] invalidValues)
	{
		var converter = new T();
		var toTypes = converter.ToTypes.ToArray().Combine(additionalTypes);

		foreach (var invalidValue in invalidValues)
		{
			foreach (var toType in toTypes)
			{
				var result = converter.TryConvertTo(invalidValue.Value, invalidValue.Type, toType, out var actual);
				IsFalse(result);
				AreEqual(default, actual);

				// Now reverse the order.
				var toTypeValue = toType.CreateInstance();
				result = converter.TryConvertTo(toTypeValue, toType, invalidValue.Type, out actual);
				IsFalse(result);
				AreEqual(default, actual);
			}
		}
	}

	#endregion
}