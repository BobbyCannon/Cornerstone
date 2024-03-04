#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Protocols.Osc;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Generators.Consumers;

[TestClass]
public class CSharpCodeWriterTests : CodeWriterTest<CSharpCodeWriter>
{
	#region Methods

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(
			$"{nameof(CSharpCodeWriterTests)}.cs",
			EnableFileUpdates || IsDebugging,
			ArrayExtensions.CombineArrays(
				Activator.BooleanTypes,
				Activator.DateTypes,
				Activator.TimeTypes,
				Activator.GuidTypes
			)
		);
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetScenarios();

		for (var index = 0; index < scenarios.Length; index++)
		{
			var scenario = scenarios[index];
			scenario.Name.Dump($"[{index}] : ");

			Consumer.Reset();
			Consumer.AppendObject(scenario.Value);
			var actual = Consumer.ToString();
			AreEqual(scenario.Expected, actual, $"[{index}] : {scenario.Name}");
		}
	}

	[TestMethod]
	public void RunSingleScenario()
	{
		var index = 22;
		var scenarios = GetScenarios();
		var scenario = scenarios[index];
		scenario.Name.Dump($"[{index}] : ");
		Consumer.Reset();
		Consumer.AppendObject(scenario.Value);
		var actual = Consumer.ToString();
		AreEqual(scenario.Expected, actual, $"[{index}] : {scenario.Name}");
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	private SerializationScenario[] GetScenarios()
	{
		var scenarios = new SerializationScenario[]
		{
			// <Scenarios>
			new("bool", true, typeof(bool), "true"),
			new("bool", false, typeof(bool), "false"),
			new("bool?", null, typeof(bool?), "null"),
			new("bool?", true, typeof(bool?), "true"),
			new("bool?", false, typeof(bool?), "false"),
			new("bool?", null, typeof(bool?), "null"),
			new("bool?", true, typeof(bool?), "true"),
			new("bool?", false, typeof(bool?), "false"),
			new("DateOnly", DateOnly.MinValue, typeof(DateOnly), "DateOnly.MinValue"),
			new("DateOnly", DateOnly.MaxValue, typeof(DateOnly), "DateOnly.MaxValue"),
			new("DateOnly", new DateOnly(2023, 10, 31), typeof(DateOnly), "new DateOnly(2023, 10, 31)"),
			new("DateOnly?", null, typeof(DateOnly?), "null"),
			new("DateOnly?", DateOnly.MinValue, typeof(DateOnly?), "DateOnly.MinValue"),
			new("DateOnly?", DateOnly.MaxValue, typeof(DateOnly?), "DateOnly.MaxValue"),
			new("DateOnly?", new DateOnly(2023, 10, 31), typeof(DateOnly?), "new DateOnly(2023, 10, 31)"),
			new("DateOnly?", null, typeof(DateOnly?), "null"),
			new("DateOnly?", DateOnly.MinValue, typeof(DateOnly?), "DateOnly.MinValue"),
			new("DateOnly?", DateOnly.MaxValue, typeof(DateOnly?), "DateOnly.MaxValue"),
			new("DateOnly?", new DateOnly(2023, 10, 31), typeof(DateOnly?), "new DateOnly(2023, 10, 31)"),
			new("DateTime", DateTime.MinValue, typeof(DateTime), "DateTime.MinValue"),
			new("DateTime", DateTime.MaxValue, typeof(DateTime), "DateTime.MaxValue"),
			new("DateTime", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime), "new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local)"),
			new("DateTime", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime), "new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc)"),
			new("DateTime", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime), "new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified)"),
			new("DateTime?", null, typeof(DateTime?), "null"),
			new("DateTime?", DateTime.MinValue, typeof(DateTime?), "DateTime.MinValue"),
			new("DateTime?", DateTime.MaxValue, typeof(DateTime?), "DateTime.MaxValue"),
			new("DateTime?", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local)"),
			new("DateTime?", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc)"),
			new("DateTime?", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified)"),
			new("DateTime?", null, typeof(DateTime?), "null"),
			new("DateTime?", DateTime.MinValue, typeof(DateTime?), "DateTime.MinValue"),
			new("DateTime?", DateTime.MaxValue, typeof(DateTime?), "DateTime.MaxValue"),
			new("DateTime?", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local)"),
			new("DateTime?", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc)"),
			new("DateTime?", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified)"),
			new("DateTimeOffset", DateTimeOffset.MinValue, typeof(DateTimeOffset), "DateTimeOffset.MinValue"),
			new("DateTimeOffset", DateTimeOffset.MaxValue, typeof(DateTimeOffset), "DateTimeOffset.MaxValue"),
			new("DateTimeOffset", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset), "new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0))"),
			new("DateTimeOffset?", null, typeof(DateTimeOffset?), "null"),
			new("DateTimeOffset?", DateTimeOffset.MinValue, typeof(DateTimeOffset?), "DateTimeOffset.MinValue"),
			new("DateTimeOffset?", DateTimeOffset.MaxValue, typeof(DateTimeOffset?), "DateTimeOffset.MaxValue"),
			new("DateTimeOffset?", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset?), "new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0))"),
			new("DateTimeOffset?", null, typeof(DateTimeOffset?), "null"),
			new("DateTimeOffset?", DateTimeOffset.MinValue, typeof(DateTimeOffset?), "DateTimeOffset.MinValue"),
			new("DateTimeOffset?", DateTimeOffset.MaxValue, typeof(DateTimeOffset?), "DateTimeOffset.MaxValue"),
			new("DateTimeOffset?", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset?), "new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0))"),
			new("IsoDateTime", IsoDateTime.MinValue, typeof(IsoDateTime), "IsoDateTime.MinValue"),
			new("IsoDateTime", IsoDateTime.MaxValue, typeof(IsoDateTime), "IsoDateTime.MaxValue"),
			new("IsoDateTime", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3))"),
			new("IsoDateTime", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6))"),
			new("IsoDateTime?", null, typeof(IsoDateTime?), "null"),
			new("IsoDateTime?", IsoDateTime.MinValue, typeof(IsoDateTime?), "IsoDateTime.MinValue"),
			new("IsoDateTime?", IsoDateTime.MaxValue, typeof(IsoDateTime?), "IsoDateTime.MaxValue"),
			new("IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime?), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3))"),
			new("IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime?), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6))"),
			new("IsoDateTime?", null, typeof(IsoDateTime?), "null"),
			new("IsoDateTime?", IsoDateTime.MinValue, typeof(IsoDateTime?), "IsoDateTime.MinValue"),
			new("IsoDateTime?", IsoDateTime.MaxValue, typeof(IsoDateTime?), "IsoDateTime.MaxValue"),
			new("IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime?), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3))"),
			new("IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime?), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6))"),
			new("OscTimeTag", OscTimeTag.MinValue, typeof(OscTimeTag), "OscTimeTag.MinValue"),
			new("OscTimeTag", OscTimeTag.MaxValue, typeof(OscTimeTag), "OscTimeTag.MaxValue"),
			new("OscTimeTag", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag), "new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc))"),
			new("OscTimeTag?", null, typeof(OscTimeTag?), "null"),
			new("OscTimeTag?", OscTimeTag.MinValue, typeof(OscTimeTag?), "OscTimeTag.MinValue"),
			new("OscTimeTag?", OscTimeTag.MaxValue, typeof(OscTimeTag?), "OscTimeTag.MaxValue"),
			new("OscTimeTag?", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag?), "new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc))"),
			new("OscTimeTag?", null, typeof(OscTimeTag?), "null"),
			new("OscTimeTag?", OscTimeTag.MinValue, typeof(OscTimeTag?), "OscTimeTag.MinValue"),
			new("OscTimeTag?", OscTimeTag.MaxValue, typeof(OscTimeTag?), "OscTimeTag.MaxValue"),
			new("OscTimeTag?", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag?), "new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc))"),
			new("TimeOnly", TimeOnly.MinValue, typeof(TimeOnly), "TimeOnly.MinValue"),
			new("TimeOnly", TimeOnly.MaxValue, typeof(TimeOnly), "TimeOnly.MaxValue"),
			new("TimeOnly", TimeOnly.Parse("11:01:02.0030040"), typeof(TimeOnly), "TimeOnly.Parse(\"11:01:02.0030040\")"),
			new("TimeOnly?", TimeOnly.MinValue, typeof(TimeOnly?), "TimeOnly.MinValue"),
			new("TimeOnly?", TimeOnly.MaxValue, typeof(TimeOnly?), "TimeOnly.MaxValue"),
			new("TimeOnly?", TimeOnly.Parse("11:01:02.0030040"), typeof(TimeOnly?), "TimeOnly.Parse(\"11:01:02.0030040\")"),
			new("TimeOnly?", null, typeof(TimeOnly?), "null"),
			new("TimeOnly?", TimeOnly.MinValue, typeof(TimeOnly?), "TimeOnly.MinValue"),
			new("TimeOnly?", TimeOnly.MaxValue, typeof(TimeOnly?), "TimeOnly.MaxValue"),
			new("TimeOnly?", TimeOnly.Parse("11:01:02.0030040"), typeof(TimeOnly?), "TimeOnly.Parse(\"11:01:02.0030040\")"),
			new("TimeOnly?", null, typeof(TimeOnly?), "null"),
			new("TimeSpan", TimeSpan.MinValue, typeof(TimeSpan), "TimeSpan.MinValue"),
			new("TimeSpan", TimeSpan.MaxValue, typeof(TimeSpan), "TimeSpan.MaxValue"),
			new("TimeSpan", TimeSpan.Zero, typeof(TimeSpan), "TimeSpan.Zero"),
			new("TimeSpan", new TimeSpan(1,2, 3, 4, 5, 6), typeof(TimeSpan), "new TimeSpan(1,2, 3, 4, 5, 6)"),
			new("TimeSpan?", TimeSpan.MinValue, typeof(TimeSpan?), "TimeSpan.MinValue"),
			new("TimeSpan?", TimeSpan.MaxValue, typeof(TimeSpan?), "TimeSpan.MaxValue"),
			new("TimeSpan?", TimeSpan.Zero, typeof(TimeSpan?), "TimeSpan.Zero"),
			new("TimeSpan?", new TimeSpan(1,2, 3, 4, 5, 6), typeof(TimeSpan?), "new TimeSpan(1,2, 3, 4, 5, 6)"),
			new("TimeSpan?", null, typeof(TimeSpan?), "null"),
			new("TimeSpan?", TimeSpan.MinValue, typeof(TimeSpan?), "TimeSpan.MinValue"),
			new("TimeSpan?", TimeSpan.MaxValue, typeof(TimeSpan?), "TimeSpan.MaxValue"),
			new("TimeSpan?", TimeSpan.Zero, typeof(TimeSpan?), "TimeSpan.Zero"),
			new("TimeSpan?", new TimeSpan(1,2, 3, 4, 5, 6), typeof(TimeSpan?), "new TimeSpan(1,2, 3, 4, 5, 6)"),
			new("TimeSpan?", null, typeof(TimeSpan?), "null"),
			new("Guid", Guid.Empty, typeof(Guid), "Guid.Empty"),
			new("Guid", Guid.Parse("6dcefb3f-4b1c-40fd-827e-58d31767e4a8"), typeof(Guid), "Guid.Parse(\"6dcefb3f-4b1c-40fd-827e-58d31767e4a8\")"),
			new("Guid", Guid.Parse("00000000-0000-0000-0000-000000000001"), typeof(Guid), "Guid.Parse(\"00000000-0000-0000-0000-000000000001\")"),
			new("Guid", Guid.Parse("10000000-0000-0000-0000-000000000000"), typeof(Guid), "Guid.Parse(\"10000000-0000-0000-0000-000000000000\")"),
			new("Guid?", null, typeof(Guid?), "null"),
			new("Guid?", Guid.Empty, typeof(Guid?), "Guid.Empty"),
			new("Guid?", Guid.Parse("6dcefb3f-4b1c-40fd-827e-58d31767e4a8"), typeof(Guid?), "Guid.Parse(\"6dcefb3f-4b1c-40fd-827e-58d31767e4a8\")"),
			new("Guid?", Guid.Parse("00000000-0000-0000-0000-000000000001"), typeof(Guid?), "Guid.Parse(\"00000000-0000-0000-0000-000000000001\")"),
			new("Guid?", Guid.Parse("10000000-0000-0000-0000-000000000000"), typeof(Guid?), "Guid.Parse(\"10000000-0000-0000-0000-000000000000\")"),
			new("Guid?", null, typeof(Guid?), "null"),
			new("Guid?", Guid.Empty, typeof(Guid?), "Guid.Empty"),
			new("Guid?", Guid.Parse("6dcefb3f-4b1c-40fd-827e-58d31767e4a8"), typeof(Guid?), "Guid.Parse(\"6dcefb3f-4b1c-40fd-827e-58d31767e4a8\")"),
			new("Guid?", Guid.Parse("00000000-0000-0000-0000-000000000001"), typeof(Guid?), "Guid.Parse(\"00000000-0000-0000-0000-000000000001\")"),
			new("Guid?", Guid.Parse("10000000-0000-0000-0000-000000000000"), typeof(Guid?), "Guid.Parse(\"10000000-0000-0000-0000-000000000000\")"),
			new("ShortGuid", ShortGuid.Empty, typeof(ShortGuid), "ShortGuid.Empty"),
			new("ShortGuid", ShortGuid.Parse("P_vObRxL_UCCfljTF2fkqA"), typeof(ShortGuid), "ShortGuid.Parse(\"P_vObRxL_UCCfljTF2fkqA\")"),
			new("ShortGuid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAAQ"), typeof(ShortGuid), "ShortGuid.Parse(\"AAAAAAAAAAAAAAAAAAAAAQ\")"),
			new("ShortGuid", ShortGuid.Parse("AAAAEAAAAAAAAAAAAAAAAA"), typeof(ShortGuid), "ShortGuid.Parse(\"AAAAEAAAAAAAAAAAAAAAAA\")"),
			new("ShortGuid?", null, typeof(ShortGuid?), "null"),
			new("ShortGuid?", ShortGuid.Empty, typeof(ShortGuid?), "ShortGuid.Empty"),
			new("ShortGuid?", ShortGuid.Parse("P_vObRxL_UCCfljTF2fkqA"), typeof(ShortGuid?), "ShortGuid.Parse(\"P_vObRxL_UCCfljTF2fkqA\")"),
			new("ShortGuid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAAQ"), typeof(ShortGuid?), "ShortGuid.Parse(\"AAAAAAAAAAAAAAAAAAAAAQ\")"),
			new("ShortGuid?", ShortGuid.Parse("AAAAEAAAAAAAAAAAAAAAAA"), typeof(ShortGuid?), "ShortGuid.Parse(\"AAAAEAAAAAAAAAAAAAAAAA\")"),
			new("ShortGuid?", null, typeof(ShortGuid?), "null"),
			new("ShortGuid?", ShortGuid.Empty, typeof(ShortGuid?), "ShortGuid.Empty"),
			new("ShortGuid?", ShortGuid.Parse("P_vObRxL_UCCfljTF2fkqA"), typeof(ShortGuid?), "ShortGuid.Parse(\"P_vObRxL_UCCfljTF2fkqA\")"),
			new("ShortGuid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAAQ"), typeof(ShortGuid?), "ShortGuid.Parse(\"AAAAAAAAAAAAAAAAAAAAAQ\")"),
			new("ShortGuid?", ShortGuid.Parse("AAAAEAAAAAAAAAAAAAAAAA"), typeof(ShortGuid?), "ShortGuid.Parse(\"AAAAEAAAAAAAAAAAAAAAAA\")"),
			// </Scenarios>
		};

		return scenarios;
	}

	#endregion
}