#region References

using System;
using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators.CSharpGenerators;
using Cornerstone.Protocols.Osc;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Generators.CodeGenerators.CSharpGenerators;

[TestClass]
public class DateCSharpGeneratorTests : CodeGeneratorTests<DateCSharpGenerator>
{
	#region Methods

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(
			$@"Generators\CodeGenerators\CSharpGenerators\{nameof(DateCSharpGeneratorTests)}.cs",
			EnableFileUpdates || IsDebugging
		);
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetScenarios();
		var settings = CodeGenerator.DefaultWriterSettings;

		foreach (var scenario in scenarios)
		{
			var actual = Generator.GenerateCode(scenario.From.Type, scenario.From.Value, settings);
			AreEqual(scenario.To.Value, actual, scenario.Name);
		}
	}

	[TestMethod]
	public void RunSingleScenario()
	{
	}

	private TestScenario[] GetScenarios()
	{
		var scenarios = new TestScenario[]
		{
			// <Scenarios>
			new("0: DateOnly", DateOnly.MinValue, typeof(DateOnly), "DateOnly.MinValue", typeof(string)),
			new("1: DateOnly", DateOnly.MaxValue, typeof(DateOnly), "DateOnly.MaxValue", typeof(string)),
			new("2: DateOnly", new DateOnly(2023, 10, 31), typeof(DateOnly), "new DateOnly(2023, 10, 31)", typeof(string)),
			new("3: DateOnly?", null, typeof(DateOnly?), "null", typeof(string)),
			new("4: DateOnly?", DateOnly.MinValue, typeof(DateOnly?), "DateOnly.MinValue", typeof(string)),
			new("5: DateOnly?", DateOnly.MaxValue, typeof(DateOnly?), "DateOnly.MaxValue", typeof(string)),
			new("6: DateOnly?", new DateOnly(2023, 10, 31), typeof(DateOnly?), "new DateOnly(2023, 10, 31)", typeof(string)),
			new("7: DateOnly?", null, typeof(DateOnly?), "null", typeof(string)),
			new("8: DateOnly?", DateOnly.MinValue, typeof(DateOnly?), "DateOnly.MinValue", typeof(string)),
			new("9: DateOnly?", DateOnly.MaxValue, typeof(DateOnly?), "DateOnly.MaxValue", typeof(string)),
			new("10: DateOnly?", new DateOnly(2023, 10, 31), typeof(DateOnly?), "new DateOnly(2023, 10, 31)", typeof(string)),
			new("11: DateTime", DateTime.MinValue, typeof(DateTime), "DateTime.MinValue", typeof(string)),
			new("12: DateTime", DateTime.MaxValue, typeof(DateTime), "DateTime.MaxValue", typeof(string)),
			new("13: DateTime", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime), "new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local)", typeof(string)),
			new("14: DateTime", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime), "new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc)", typeof(string)),
			new("15: DateTime", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime), "new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified)", typeof(string)),
			new("16: DateTime?", null, typeof(DateTime?), "null", typeof(string)),
			new("17: DateTime?", DateTime.MinValue, typeof(DateTime?), "DateTime.MinValue", typeof(string)),
			new("18: DateTime?", DateTime.MaxValue, typeof(DateTime?), "DateTime.MaxValue", typeof(string)),
			new("19: DateTime?", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local)", typeof(string)),
			new("20: DateTime?", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc)", typeof(string)),
			new("21: DateTime?", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified)", typeof(string)),
			new("22: DateTime?", null, typeof(DateTime?), "null", typeof(string)),
			new("23: DateTime?", DateTime.MinValue, typeof(DateTime?), "DateTime.MinValue", typeof(string)),
			new("24: DateTime?", DateTime.MaxValue, typeof(DateTime?), "DateTime.MaxValue", typeof(string)),
			new("25: DateTime?", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local)", typeof(string)),
			new("26: DateTime?", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc)", typeof(string)),
			new("27: DateTime?", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime?), "new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified)", typeof(string)),
			new("28: DateTimeOffset", DateTimeOffset.MinValue, typeof(DateTimeOffset), "DateTimeOffset.MinValue", typeof(string)),
			new("29: DateTimeOffset", DateTimeOffset.MaxValue, typeof(DateTimeOffset), "DateTimeOffset.MaxValue", typeof(string)),
			new("30: DateTimeOffset", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset), "new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0))", typeof(string)),
			new("31: DateTimeOffset?", null, typeof(DateTimeOffset?), "null", typeof(string)),
			new("32: DateTimeOffset?", DateTimeOffset.MinValue, typeof(DateTimeOffset?), "DateTimeOffset.MinValue", typeof(string)),
			new("33: DateTimeOffset?", DateTimeOffset.MaxValue, typeof(DateTimeOffset?), "DateTimeOffset.MaxValue", typeof(string)),
			new("34: DateTimeOffset?", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset?), "new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0))", typeof(string)),
			new("35: DateTimeOffset?", null, typeof(DateTimeOffset?), "null", typeof(string)),
			new("36: DateTimeOffset?", DateTimeOffset.MinValue, typeof(DateTimeOffset?), "DateTimeOffset.MinValue", typeof(string)),
			new("37: DateTimeOffset?", DateTimeOffset.MaxValue, typeof(DateTimeOffset?), "DateTimeOffset.MaxValue", typeof(string)),
			new("38: DateTimeOffset?", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset?), "new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0))", typeof(string)),
			new("39: IsoDateTime", IsoDateTime.MinValue, typeof(IsoDateTime), "IsoDateTime.MinValue", typeof(string)),
			new("40: IsoDateTime", IsoDateTime.MaxValue, typeof(IsoDateTime), "IsoDateTime.MaxValue", typeof(string)),
			new("41: IsoDateTime", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3))", typeof(string)),
			new("42: IsoDateTime", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6))", typeof(string)),
			new("43: IsoDateTime?", null, typeof(IsoDateTime?), "null", typeof(string)),
			new("44: IsoDateTime?", IsoDateTime.MinValue, typeof(IsoDateTime?), "IsoDateTime.MinValue", typeof(string)),
			new("45: IsoDateTime?", IsoDateTime.MaxValue, typeof(IsoDateTime?), "IsoDateTime.MaxValue", typeof(string)),
			new("46: IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime?), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3))", typeof(string)),
			new("47: IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime?), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6))", typeof(string)),
			new("48: IsoDateTime?", null, typeof(IsoDateTime?), "null", typeof(string)),
			new("49: IsoDateTime?", IsoDateTime.MinValue, typeof(IsoDateTime?), "IsoDateTime.MinValue", typeof(string)),
			new("50: IsoDateTime?", IsoDateTime.MaxValue, typeof(IsoDateTime?), "IsoDateTime.MaxValue", typeof(string)),
			new("51: IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime?), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3))", typeof(string)),
			new("52: IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime?), "new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6))", typeof(string)),
			new("53: OscTimeTag", OscTimeTag.MinValue, typeof(OscTimeTag), "OscTimeTag.MinValue", typeof(string)),
			new("54: OscTimeTag", OscTimeTag.MaxValue, typeof(OscTimeTag), "OscTimeTag.MaxValue", typeof(string)),
			new("55: OscTimeTag", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag), "new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc))", typeof(string)),
			new("56: OscTimeTag?", null, typeof(OscTimeTag?), "null", typeof(string)),
			new("57: OscTimeTag?", OscTimeTag.MinValue, typeof(OscTimeTag?), "OscTimeTag.MinValue", typeof(string)),
			new("58: OscTimeTag?", OscTimeTag.MaxValue, typeof(OscTimeTag?), "OscTimeTag.MaxValue", typeof(string)),
			new("59: OscTimeTag?", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag?), "new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc))", typeof(string)),
			new("60: OscTimeTag?", null, typeof(OscTimeTag?), "null", typeof(string)),
			new("61: OscTimeTag?", OscTimeTag.MinValue, typeof(OscTimeTag?), "OscTimeTag.MinValue", typeof(string)),
			new("62: OscTimeTag?", OscTimeTag.MaxValue, typeof(OscTimeTag?), "OscTimeTag.MaxValue", typeof(string)),
			new("63: OscTimeTag?", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag?), "new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc))", typeof(string)),
			// </Scenarios>
		};

		return scenarios;
	}

	#endregion
}