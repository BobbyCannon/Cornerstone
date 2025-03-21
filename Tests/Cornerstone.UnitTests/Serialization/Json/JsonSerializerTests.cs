#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Cornerstone.Attributes;
using Cornerstone.Collections;
using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Newtonsoft;
using Cornerstone.Protocols.Osc;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Testing;
using Cornerstone.Text;
using Cornerstone.Text.Buffers;
using Cornerstone.UnitTests.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json;

[TestClass]
public class JsonSerializerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ArrayOfBooleans()
	{
		var array = new[] { false, true };
		var json = array.ToJson();
		AreEqual("[false,true]", json);
		var actual = json.FromJson<bool[]>();
		AreEqual(array, actual);
	}

	[TestMethod]
	public void AttributeTests()
	{
		var sample = new SampleWithAttribute
		{
			FirstName = "John",
			LastName = "Doe",
			PasswordHash = "384516324565136"
		};
		var expected = "{\"FirstName\":\"John\",\"LastName\":\"Doe\"}";
		var actual = sample.ToJson();
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void Characters()
	{
		var scenarios = new Dictionary<char, string>
		{
			{ '\0', "\"\\u0000\"" },
			{ '\b', "\"\\b\"" },
			{ '\u001b', "\"\\u001B\"" },
			{ '\u001f', "\"\\u001F\"" },
			{ '\u007F', "\"\\u007F\"" }
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.Key.ToJson();
			AreEqual(scenario.Value, actual);
			var actual2 = actual.FromJson<char>();
			AreEqual(scenario.Key, actual2);
		}
	}

	[TestMethod]
	public void Complex()
	{
		var json = "{\"Array\":[{\"Entry\": 1}]}";
		var expected = new JsonObject();
		expected.PropertyName("Array");
		var firstArray = expected.StartObject(typeof(JsonArray));
		var arrayEntry1 = new JsonObject();
		arrayEntry1.WriteProperty("Entry", new JsonNumber(1));
		firstArray.WriteObject(arrayEntry1);
		expected.CompleteObject();

		var actual = JsonSerializer.Parse(json);
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void ConvertWithNestedQuotes()
	{
		var dictionary = new Dictionary<string, string>
		{
			{
				"a",
				new Dictionary<string, string>
				{
					{
						"b",
						new Dictionary<string, string>
						{
							{ "c", "d" }
						}.ToJson()
					}
				}.ToJson()
			}
		};

		var json = dictionary.ToJson();
		var scenarios = new[] { '\"', '\'' };

		foreach (var scenario in scenarios)
		{
			var scenarioJson = json.Replace('\"', scenario);
			var jsonObject = (JsonObject) JsonSerializer.Parse(scenarioJson);
			AreEqual(1, jsonObject.Keys.Count());

			var actual = (JsonString) jsonObject["a"];
			var expected = "{\"b\":\"{\\\"c\\\":\\\"d\\\"}\"}".Replace('\"', scenario);
			AreEqual(expected, actual.Value, () => actual.Value);

			jsonObject = (JsonObject) JsonSerializer.Parse(actual.Value);
			actual = (JsonString) jsonObject["b"];
			expected = "{\"c\":\"d\"}".Replace('\"', scenario);
			AreEqual(expected, actual.Value, () => actual.Value);
		}
	}

	[TestMethod]
	public void DateAndTimes()
	{
		var scenarios = new SerializationScenario[]
		{
			new("DateTime (unspecified)", new DateTime(2024, 01, 02), typeof(DateTime), "\"2024-01-02T00:00:00Z\""),
			new("DateTime (utc)", new DateTime(2024, 01, 02, 0, 0, 0, DateTimeKind.Utc), typeof(DateTime), "\"2024-01-02T00:00:00Z\""),
			#if (NET48)
			new("DateTime (utc) 2", new DateTime(2024, 01, 02, 03, 04, 05, 06, DateTimeKind.Utc), typeof(DateTime), "\"2024-01-02T03:04:05.0060000Z\""),
			new("DateTime (local)", new DateTime(2024, 01, 02, 03, 04, 05, 06, DateTimeKind.Local), typeof(DateTime), "\"2024-01-02T08:04:05.0060000Z\""),
			#else
			new("DateTime (utc) 2", new DateTime(2024, 01, 02, 03, 04, 05, 06, 07, DateTimeKind.Utc), typeof(DateTime), "\"2024-01-02T03:04:05.0060070Z\""),
			new("DateTime (local)", new DateTime(2024, 01, 02, 03, 04, 05, 06, 07, DateTimeKind.Local), typeof(DateTime), "\"2024-01-02T08:04:05.0060070Z\""),
			#endif
		};

		foreach (var scenario in scenarios)
		{
			scenario.Name.Dump();
			var json = scenario.Value.ToJson();
			AreEqual(scenario.Expected, json);
			var actual = json.FromJson(scenario.Type);
			AreEqual(scenario.Value, actual);
		}
	}

	[TestMethod]
	public void Enums()
	{
		foreach (var serializer in GetSerializers())
		{
			serializer.GetType().FullName.Dump();
			AreEqual("9", serializer.ToJson(ConsoleColor.Blue));
			AreEqual("null", serializer.ToJson<ConsoleColor?>(null));
			AreEqual("\"Blue\"", serializer.ToJson(ConsoleColor.Blue, new SerializationSettings { EnumFormat = EnumFormat.Name }));
			AreEqual(ConsoleColor.Blue, serializer.FromJson<ConsoleColor>("9"));
			AreEqual(ConsoleColor.Blue, serializer.FromJson<ConsoleColor>("\"Blue\""));
			AreEqual(null, serializer.FromJson<ConsoleColor?>("null"));
		}
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(EnableFileUpdates || IsDebugging,
			Activator.AllTypes.Combine([typeof(Version)])
		);
	}

	[TestMethod]
	public void GetConverter()
	{
		AreEqual(typeof(EnumJsonConverter), JsonSerializer.GetConverter(typeof(ConsoleColor)).GetType());
		AreEqual(typeof(EnumJsonConverter), JsonSerializer.GetConverter(typeof(ConsoleColor?)).GetType());
	}

	[TestMethod]
	public void IgnoreSerialization()
	{
		var expected = new SampleWithIgnoredMembers
		{
			FirstName = "First",
			LastName = "Last",
			Nickname = "Nick"
		};

		var expectedJson = "{\"FirstName\":\"First\",\"LastName\":\"Last\"}";
		var actualJson = expected.ToJson();
		AreEqual(expectedJson, actualJson);

		var incoming = "{\"FirstName\":\"First\",\"LastName\":\"Last\",\"Nickname\":\"Nick\"}";
		var actual = incoming.FromJson<SampleWithIgnoredMembers>();
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void JsonValues()
	{
		AreEqual("\"ABC\"", new JsonString("ABC").ToJson());
	}

	[TestMethod]
	public void Lists()
	{
		var scenarios = new Dictionary<string, object>
		{
			{ "[1,2,3,4]", new List<int> { 1, 2, 3, 4 } },
			{ "[5,6,7,8]", new SpeedyList<int> { 5, 6, 7, 8 } },
			{ "[6,7,8,[1,2,3]]", new SpeedyList<object> { 6, 7, 8, new[] { 1, 2, 3 } } }
		};

		var serializer = new JsonSerializer();

		foreach (var scenario in scenarios)
		{
			var actual = serializer.ToJson(scenario.Value);
			AreEqual(scenario.Key, actual);

			var actualObject = serializer.FromJson(actual, scenario.Value.GetType());
			AreEqual(scenario.Value, actualObject);
		}
	}

	[TestMethod]
	public void OverriddenPropertyUsingNewOperator()
	{
		var sample = new Sample2 { BoolFalse = 42 };
		var expected = "{\"BoolFalse\":42,\"BoolNullable\":null,\"Foreground\":9,\"IsEnabled\":true,\"Selection\":{\"Item1\":12,\"Item2\":321}}";

		foreach (var serializer in GetSerializers())
		{
			serializer.GetType().FullName.Dump();
			var actual = serializer.ToJson(sample);
			AreEqual(expected, actual);
		}
	}

	[TestMethod]
	public void Recursion()
	{
		var grandpa = new Person { Name = "Grandpa" };
		var dad = new Person { Name = "Dad" };
		var son = new Person { Name = "Son" };
		var expected = @"{
	""Children"": [
		{
			""Children"": [
				{
					""Children"": null,
					""Name"": ""Son""
				}
			],
			""Name"": ""Dad""
		}
	],
	""Name"": ""Grandpa"",
	""Parent"": null
}";
		var settings = new SerializationSettings
		{
			TextFormat = TextFormat.Indented
		};

		grandpa.Children = [dad];
		dad.Parent = grandpa;
		dad.Children = [son];
		son.Parent = dad;

		var actual = grandpa.ToJson(settings);
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void Recursion2()
	{
		var dynamic = new
		{
			Name = "Parent",
			Version = new Version(1, 2, 3, 4),
			Child = new
			{
				Name = "Child",
				Version = new Version(1, 2, 3, 4)
			}
		};

		var expected = "{\"Child\":{\"Name\":\"Child\",\"Version\":\"1.2.3.4\"},\"Name\":\"Parent\",\"Version\":\"1.2.3.4\"}";
		var actual = dynamic.ToJson(new SerializationSettings
		{
			IgnoreReadOnly = false
		});
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void Recursion3()
	{
		var person1 = new Person { Name = "John" };
		var person2 = new Person { Name = "Jane", Children = [person1] };

		var options = new SerializationSettings { TextFormat = TextFormat.Indented };
		var data = new
		{
			Person1 = person1,
			Person2 = person2
		};

		var expected = """
						{
							"Person1": {
								"Children": null,
								"Name": "John",
								"Parent": null
							},
							"Person2": {
								"Children": [
									{
										"Children": null,
										"Name": "John",
										"Parent": null
									}
								],
								"Name": "Jane",
								"Parent": null
							}
						}
						""";
		var actual = data.ToJson(options);
		AreEqual(expected, actual);

		// Circular reference should not be serialized
		person2.Parent = person1;

		expected = """
					{
						"Person1": {
							"Children": null,
							"Name": "John",
							"Parent": null
						},
						"Person2": {
							"Children": [
								{
									"Children": null,
									"Name": "John",
									"Parent": null
								}
							],
							"Name": "Jane"
						}
					}
					""";
		actual = data.ToJson(options);
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetScenarios();

		foreach (var serializer in GetSerializers())
		{
			if (IsDebugging)
			{
				serializer.GetType().FullName.Dump();
			}

			foreach (var scenario in scenarios)
			{
				if (IsDebugging)
				{
					$"\t{scenario.Name}".Dump();
				}

				if (!IsScenarioSupported(serializer, scenario))
				{
					if (IsDebugging)
					{
						"\t\tSkipping due to not supported...".Dump();
					}
					continue;
				}

				var actual = serializer.ToJson(scenario.Value);
				var expected = GetExpected(serializer, scenario);
				AreEqual(expected, actual, $"{serializer.GetType().Name}-{scenario.Name}");
			}
		}
	}

	[TestMethod]
	public void RunSingleScenario()
	{
		var scenario = GetScenarios()[75];

		foreach (var serializer in GetSerializers())
		{
			serializer.GetType().FullName.Dump();
			scenario.Name.Dump();
			var actual = serializer.ToJson(scenario.Value);
			var expected = GetExpected(serializer, scenario);
			AreEqual(expected, actual, $"{serializer.GetType().Name}-{scenario.Name}");
		}
	}

	[TestMethod]
	public void SampleTest()
	{
		var jss = new SerializationSettings { TextFormat = TextFormat.None };
		var js = new JsonSerializer();
		var sample = new Sample();
		var actual = js.ToJson(sample, jss);
		var expected = "{\"BoolFalse\":false,\"BoolNullable\":null,\"Foreground\":9,\"IsEnabled\":true,\"Selection\":{\"Item1\":12,\"Item2\":321}}";
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void Tuple()
	{
		var scenarios = new Dictionary<object, (string none, string indented)>
		{
			{ (456, 321), ("{\"Item1\":456,\"Item2\":321}", "{\r\n\t\"Item1\": 456,\r\n\t\"Item2\": 321\r\n}") },
			{
				(
					new DateTime(2023, 12, 31),
					new DateTimeOffset(2024, 01, 02, 03, 04, 05, 06, TimeSpan.FromMinutes(60))
				),
				(
					"{\"Item1\":\"2023-12-31T00:00:00Z\",\"Item2\":\"2024-01-02T03:04:05.0060000+01:00\"}",
					"{\r\n\t\"Item1\": \"2023-12-31T00:00:00Z\",\r\n\t\"Item2\": \"2024-01-02T03:04:05.0060000+01:00\"\r\n}"
				)
			},
			{ new Tuple<TextFormat>(TextFormat.Spaced), ("{\"Item1\":2}", "{\r\n\t\"Item1\": 2\r\n}") },
			{ new Tuple<int, int>(456, 321), ("{\"Item1\":456,\"Item2\":321}", "{\r\n\t\"Item1\": 456,\r\n\t\"Item2\": 321\r\n}") },
			{
				("Bob", new Guid("8D6533AA-4103-4071-9341-1D2F64904F7A")),
				(
					"{\"Item1\":\"Bob\",\"Item2\":\"8d6533aa-4103-4071-9341-1d2f64904f7a\"}",
					"{\r\n\t\"Item1\": \"Bob\",\r\n\t\"Item2\": \"8d6533aa-4103-4071-9341-1d2f64904f7a\"\r\n}"
				)
			}
		};

		var scenarioIndex = 1;

		foreach (var scenario in scenarios)
		{
			scenarioIndex++.Dump();
			var expected = scenario.Key;
			var json = expected.ToJson();
			AreEqual(scenario.Value.none, json);
			var actual = json.FromJson(expected.GetType());
			AreEqual(expected, actual);
			json = expected.ToJson(new SerializationSettings { TextFormat = TextFormat.Indented });
			AreEqual(scenario.Value.indented, json, () => json);
			actual = json.FromJson(expected.GetType());
			AreEqual(expected, actual);
		}
	}

	[TestMethod]
	public void UpdateableShouldRestrictJson()
	{
		var account = GetModel<SampleAccount>();
		account.Id = 42;
		account.SyncId = new Guid("9DCCF850-CBED-431D-9068-EA2B1F50F7A4");
		account.CreatedOn = StartDateTime;
		account.ModifiedOn = StartDateTime.AddSeconds(1);
		account.LastClientUpdate = StartDateTime.AddSeconds(3);
		account.IsDeleted = true;

		var scenarios = new Dictionary<UpdateableAction, string>
		{
			{ UpdateableAction.None, "{\"CreatedOn\":\"2000-01-02T03:04:00Z\",\"EmailAddress\":\"john@doe.com\",\"FirstName\":\"John\",\"Id\":42,\"IsDeleted\":true,\"LastClientUpdate\":\"2000-01-02T03:04:03Z\",\"LastName\":\"Doe\",\"ModifiedOn\":\"2000-01-02T03:04:01Z\",\"SyncId\":\"9dccf850-cbed-431d-9068-ea2b1f50f7a4\"}" },
			{ UpdateableAction.PropertyChangeTracking, "{\"CreatedOn\":\"2000-01-02T03:04:00Z\",\"EmailAddress\":\"john@doe.com\",\"FirstName\":\"John\",\"Id\":42,\"IsDeleted\":true,\"LastClientUpdate\":\"2000-01-02T03:04:03Z\",\"LastName\":\"Doe\",\"ModifiedOn\":\"2000-01-02T03:04:01Z\",\"SyncId\":\"9dccf850-cbed-431d-9068-ea2b1f50f7a4\"}" },
			{ UpdateableAction.SyncIncomingAdd, "{\"CreatedOn\":\"2000-01-02T03:04:00Z\",\"EmailAddress\":\"john@doe.com\",\"FirstName\":\"John\",\"IsDeleted\":true,\"LastName\":\"Doe\",\"ModifiedOn\":\"2000-01-02T03:04:01Z\",\"SyncId\":\"9dccf850-cbed-431d-9068-ea2b1f50f7a4\"}" },
			{ UpdateableAction.SyncIncomingUpdate, "{\"CreatedOn\":\"2000-01-02T03:04:00Z\",\"EmailAddress\":\"john@doe.com\",\"FirstName\":\"John\",\"IsDeleted\":true,\"LastName\":\"Doe\",\"ModifiedOn\":\"2000-01-02T03:04:01Z\"}" },
			{ UpdateableAction.SyncOutgoing, "{\"CreatedOn\":\"2000-01-02T03:04:00Z\",\"EmailAddress\":\"john@doe.com\",\"FirstName\":\"John\",\"IsDeleted\":true,\"LastName\":\"Doe\",\"ModifiedOn\":\"2000-01-02T03:04:01Z\",\"SyncId\":\"9dccf850-cbed-431d-9068-ea2b1f50f7a4\"}" },
			{ UpdateableAction.UnwrapProxyEntity, "{\"CreatedOn\":\"2000-01-02T03:04:00Z\",\"EmailAddress\":\"john@doe.com\",\"FirstName\":\"John\",\"Id\":42,\"IsDeleted\":true,\"LastClientUpdate\":\"2000-01-02T03:04:03Z\",\"LastName\":\"Doe\",\"ModifiedOn\":\"2000-01-02T03:04:01Z\",\"SyncId\":\"9dccf850-cbed-431d-9068-ea2b1f50f7a4\"}" },
			{ UpdateableAction.Updateable, "{\"CreatedOn\":\"2000-01-02T03:04:00Z\",\"EmailAddress\":\"john@doe.com\",\"FirstName\":\"John\",\"Id\":42,\"IsDeleted\":true,\"LastClientUpdate\":\"2000-01-02T03:04:03Z\",\"LastName\":\"Doe\",\"ModifiedOn\":\"2000-01-02T03:04:01Z\",\"SyncId\":\"9dccf850-cbed-431d-9068-ea2b1f50f7a4\"}" }
		};

		var allActions = EnumExtensions.GetEnumValues<UpdateableAction>();
		var missing = scenarios.Keys.Where(x => !allActions.Contains(x)).ToList();
		AreEqual(0, missing.Count);

		foreach (var item in scenarios)
		{
			item.Key.Dump();

			var actual = account.ToJson(new SerializationSettings { UpdateableAction = item.Key });
			AreEqual(item.Value, actual);
		}
	}

	protected IEnumerable<IJsonSerializer> GetSerializers()
	{
		yield return new JsonSerializer();
		yield return new JsonSerializer(x => new TextJsonConsumer(new StringGapBuffer(), x));
		yield return new NewtonsoftJsonSerializer();
	}

	private (Type[] all, Type[] filtered) FilterTypes(Type[] types)
	{
		var all = new List<Type>();
		var filtered = new List<Type>();

		foreach (var fromType in types)
		{
			if (CheckTypeForClassicFramework(fromType))
			{
				all.Add(fromType);
			}
			else
			{
				filtered.Add(fromType);
			}
		}

		return (all.ToArray(), filtered.ToArray());
	}

	private void GenerateNewScenarios(bool enableTestScenarioCreation, params Type[] types)
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

		var filePath = $@"{UnitTestsDirectory}\Serialization\Json\{nameof(JsonSerializerTests)}.cs";
		var builder = new TextBuilder();
		var scenarioIndex = 0;
		var baseSerializer = new JsonSerializer();
		var (all, filtered) = FilterTypes(types);

		ProcessTypes(all, baseSerializer, ref scenarioIndex, builder);
		builder.AppendLine("#if (!NET48)");
		ProcessTypes(filtered, baseSerializer, ref scenarioIndex, builder);
		builder.AppendLine("#endif");

		UpdateFileIfNecessary(filePath, builder);
	}

	private string GetExpected(IJsonSerializer jsonSerializer, SerializationScenario scenario)
	{
		if (jsonSerializer is JsonSerializer)
		{
			return scenario.Expected;
		}

		// The comment is Cornerstones expected, the returns is Newtonsoft expected value.
		return scenario.Value switch
		{
			Version { Major: 99999, Minor: 99999, Build: 99999, Revision: 99999 } => "{\"Major\":99999,\"Minor\":99999,\"Build\":99999,\"Revision\":99999}",
			Version { Major: 12345, Minor: 12345, Build: 12345, Revision: 12345 } => "{\"Major\":12345,\"Minor\":12345,\"Build\":12345,\"Revision\":12345}",
			Version { Major: 1, Minor: 2, Build: 3, Revision: 4 } => "{\"Major\":1,\"Minor\":2,\"Build\":3,\"Revision\":4}",
			Version { Major: 1, Minor: 2, Build: 3 } => "{\"Major\":1,\"Minor\":2,\"Build\":3,\"Revision\":-1}",
			Version { Major: 1, Minor: 2 } => "{\"Major\":1,\"Minor\":2,\"Build\":-1,\"Revision\":-1}",
			Version { Major: 0, Minor: 0 } => "{\"Major\":0,\"Minor\":0,\"Build\":-1,\"Revision\":-1}",
			JsonString { Value: "" } => "{\"Value\":\"\"}",
			DateTime dValue when dValue == new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local) => "\"2023-10-31T12:01:02-04:00\"",
			DateTime dValue when dValue == new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified) => "\"2023-10-31T12:01:04\"",
			_ => scenario.Expected
		};
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	[SuppressMessage("ReSharper", "RedundantCast")]
	private SerializationScenario[] GetScenarios()
	{
		var scenarios = new SerializationScenario[]
		{
			// <Scenarios>
			new("0: bool", true, typeof(bool), "true"),
			new("1: bool", false, typeof(bool), "false"),
			new("2: bool?", null, typeof(bool?), "null"),
			new("3: bool?", true, typeof(bool?), "true"),
			new("4: bool?", false, typeof(bool?), "false"),
			new("5: byte", byte.MinValue, typeof(byte), "0"),
			new("6: byte", byte.MinValue, typeof(byte), "0"),
			new("7: byte", byte.MaxValue, typeof(byte), "255"),
			new("8: byte?", byte.MinValue, typeof(byte?), "0"),
			new("9: byte?", byte.MinValue, typeof(byte?), "0"),
			new("10: byte?", byte.MaxValue, typeof(byte?), "255"),
			new("11: byte?", null, typeof(byte?), "null"),
			new("12: sbyte", sbyte.MinValue, typeof(sbyte), "-128"),
			new("13: sbyte", 0, typeof(sbyte), "0"),
			new("14: sbyte", sbyte.MaxValue, typeof(sbyte), "127"),
			new("15: sbyte?", sbyte.MinValue, typeof(sbyte?), "-128"),
			new("16: sbyte?", 0, typeof(sbyte?), "0"),
			new("17: sbyte?", sbyte.MaxValue, typeof(sbyte?), "127"),
			new("18: sbyte?", null, typeof(sbyte?), "null"),
			new("19: short", short.MinValue, typeof(short), "-32768"),
			new("20: short", 0, typeof(short), "0"),
			new("21: short", short.MaxValue, typeof(short), "32767"),
			new("22: short?", short.MinValue, typeof(short?), "-32768"),
			new("23: short?", 0, typeof(short?), "0"),
			new("24: short?", short.MaxValue, typeof(short?), "32767"),
			new("25: short?", null, typeof(short?), "null"),
			new("26: ushort", ushort.MinValue, typeof(ushort), "0"),
			new("27: ushort", ushort.MinValue, typeof(ushort), "0"),
			new("28: ushort", ushort.MaxValue, typeof(ushort), "65535"),
			new("29: ushort?", ushort.MinValue, typeof(ushort?), "0"),
			new("30: ushort?", ushort.MinValue, typeof(ushort?), "0"),
			new("31: ushort?", ushort.MaxValue, typeof(ushort?), "65535"),
			new("32: ushort?", null, typeof(ushort?), "null"),
			new("33: int", int.MinValue, typeof(int), "-2147483648"),
			new("34: int", 0, typeof(int), "0"),
			new("35: int", int.MaxValue, typeof(int), "2147483647"),
			new("36: int?", int.MinValue, typeof(int?), "-2147483648"),
			new("37: int?", 0, typeof(int?), "0"),
			new("38: int?", int.MaxValue, typeof(int?), "2147483647"),
			new("39: int?", null, typeof(int?), "null"),
			new("40: uint", uint.MinValue, typeof(uint), "0"),
			new("41: uint", uint.MinValue, typeof(uint), "0"),
			new("42: uint", uint.MaxValue, typeof(uint), "4294967295"),
			new("43: uint?", uint.MinValue, typeof(uint?), "0"),
			new("44: uint?", uint.MinValue, typeof(uint?), "0"),
			new("45: uint?", uint.MaxValue, typeof(uint?), "4294967295"),
			new("46: uint?", null, typeof(uint?), "null"),
			new("47: long", long.MinValue, typeof(long), "-9223372036854775808"),
			new("48: long", 0, typeof(long), "0"),
			new("49: long", long.MaxValue, typeof(long), "9223372036854775807"),
			new("50: long?", long.MinValue, typeof(long?), "-9223372036854775808"),
			new("51: long?", 0, typeof(long?), "0"),
			new("52: long?", long.MaxValue, typeof(long?), "9223372036854775807"),
			new("53: long?", null, typeof(long?), "null"),
			new("54: ulong", ulong.MinValue, typeof(ulong), "0"),
			new("55: ulong", ulong.MinValue, typeof(ulong), "0"),
			new("56: ulong", ulong.MaxValue, typeof(ulong), "18446744073709551615"),
			new("57: ulong?", ulong.MinValue, typeof(ulong?), "0"),
			new("58: ulong?", ulong.MinValue, typeof(ulong?), "0"),
			new("59: ulong?", ulong.MaxValue, typeof(ulong?), "18446744073709551615"),
			new("60: ulong?", null, typeof(ulong?), "null"),
			new("61: IntPtr", IntPtr.MinValue, typeof(IntPtr), "-9223372036854775808"),
			new("62: IntPtr", (IntPtr) 0, typeof(IntPtr), "0"),
			new("63: IntPtr", IntPtr.MaxValue, typeof(IntPtr), "9223372036854775807"),
			new("64: IntPtr?", IntPtr.MinValue, typeof(IntPtr?), "-9223372036854775808"),
			new("65: IntPtr?", (IntPtr) 0, typeof(IntPtr?), "0"),
			new("66: IntPtr?", IntPtr.MaxValue, typeof(IntPtr?), "9223372036854775807"),
			new("67: IntPtr?", null, typeof(IntPtr?), "null"),
			new("68: UIntPtr", UIntPtr.MinValue, typeof(UIntPtr), "0"),
			new("69: UIntPtr", UIntPtr.MinValue, typeof(UIntPtr), "0"),
			new("70: UIntPtr", UIntPtr.MaxValue, typeof(UIntPtr), "18446744073709551615"),
			new("71: UIntPtr?", UIntPtr.MinValue, typeof(UIntPtr?), "0"),
			new("72: UIntPtr?", UIntPtr.MinValue, typeof(UIntPtr?), "0"),
			new("73: UIntPtr?", UIntPtr.MaxValue, typeof(UIntPtr?), "18446744073709551615"),
			new("74: UIntPtr?", null, typeof(UIntPtr?), "null"),
			new("75: Int128", Int128.MinValue, typeof(Int128), "-170141183460469231731687303715884105728"),
			new("76: Int128", 0, typeof(Int128), "0"),
			new("77: Int128", Int128.MaxValue, typeof(Int128), "170141183460469231731687303715884105727"),
			new("78: Int128?", Int128.MinValue, typeof(Int128?), "-170141183460469231731687303715884105728"),
			new("79: Int128?", 0, typeof(Int128?), "0"),
			new("80: Int128?", Int128.MaxValue, typeof(Int128?), "170141183460469231731687303715884105727"),
			new("81: Int128?", null, typeof(Int128?), "null"),
			new("82: UInt128", UInt128.MinValue, typeof(UInt128), "0"),
			new("83: UInt128", UInt128.MinValue, typeof(UInt128), "0"),
			new("84: UInt128", UInt128.MaxValue, typeof(UInt128), "340282366920938463463374607431768211455"),
			new("85: UInt128?", UInt128.MinValue, typeof(UInt128?), "0"),
			new("86: UInt128?", UInt128.MinValue, typeof(UInt128?), "0"),
			new("87: UInt128?", UInt128.MaxValue, typeof(UInt128?), "340282366920938463463374607431768211455"),
			new("88: UInt128?", null, typeof(UInt128?), "null"),
			new("89: decimal", decimal.MinValue, typeof(decimal), "-79228162514264337593543950335.0"),
			new("90: decimal", (decimal) 0m, typeof(decimal), "0.0"),
			new("91: decimal", decimal.MaxValue, typeof(decimal), "79228162514264337593543950335.0"),
			new("92: decimal?", decimal.MinValue, typeof(decimal?), "-79228162514264337593543950335.0"),
			new("93: decimal?", (decimal) 0m, typeof(decimal?), "0.0"),
			new("94: decimal?", decimal.MaxValue, typeof(decimal?), "79228162514264337593543950335.0"),
			new("95: decimal?", null, typeof(decimal?), "null"),
			new("96: double", double.MinValue, typeof(double), "-1.7976931348623157E+308"),
			new("97: double", double.NegativeZero, typeof(double), "-0.0"),
			new("98: double", double.MaxValue, typeof(double), "1.7976931348623157E+308"),
			new("99: double", double.NegativeInfinity, typeof(double), "\"-Infinity\""),
			new("100: double", double.PositiveInfinity, typeof(double), "\"Infinity\""),
			new("101: double", double.NaN, typeof(double), "\"NaN\""),
			new("102: double", double.NegativeZero, typeof(double), "-0.0"),
			new("103: double", double.Pi, typeof(double), "3.141592653589793"),
			new("104: double?", double.MinValue, typeof(double?), "-1.7976931348623157E+308"),
			new("105: double?", double.NegativeZero, typeof(double?), "-0.0"),
			new("106: double?", double.MaxValue, typeof(double?), "1.7976931348623157E+308"),
			new("107: double?", double.NegativeInfinity, typeof(double?), "\"-Infinity\""),
			new("108: double?", double.PositiveInfinity, typeof(double?), "\"Infinity\""),
			new("109: double?", double.NaN, typeof(double?), "\"NaN\""),
			new("110: double?", double.NegativeZero, typeof(double?), "-0.0"),
			new("111: double?", double.Pi, typeof(double?), "3.141592653589793"),
			new("112: double?", null, typeof(double?), "null"),
			new("113: float", float.MinValue, typeof(float), "-3.4028235E+38"),
			new("114: float", (float) 0f, typeof(float), "-0.0"),
			new("115: float", float.MaxValue, typeof(float), "3.4028235E+38"),
			new("116: float", float.NegativeInfinity, typeof(float), "\"-Infinity\""),
			new("117: float", float.PositiveInfinity, typeof(float), "\"Infinity\""),
			new("118: float", float.NaN, typeof(float), "\"NaN\""),
			new("119: float", (float) -0f, typeof(float), "-0.0"),
			new("120: float", float.Pi, typeof(float), "3.1415927"),
			new("121: float?", float.MinValue, typeof(float?), "-3.4028235E+38"),
			new("122: float?", (float) 0f, typeof(float?), "-0.0"),
			new("123: float?", float.MaxValue, typeof(float?), "3.4028235E+38"),
			new("124: float?", float.NegativeInfinity, typeof(float?), "\"-Infinity\""),
			new("125: float?", float.PositiveInfinity, typeof(float?), "\"Infinity\""),
			new("126: float?", float.NaN, typeof(float?), "\"NaN\""),
			new("127: float?", (float) -0f, typeof(float?), "-0.0"),
			new("128: float?", float.Pi, typeof(float?), "3.1415927"),
			new("129: float?", null, typeof(float?), "null"),
			new("130: char", '\0', typeof(char), "\"\\u0000\""),
			new("131: char", '\uFFFF', typeof(char), "\"\\uFFFF\""),
			new("132: char?", '\0', typeof(char?), "\"\\u0000\""),
			new("133: char?", '\uFFFF', typeof(char?), "\"\\uFFFF\""),
			new("134: char?", null, typeof(char?), "null"),
			new("135: string", "", typeof(string), "\"\""),
			new("136: string", "", typeof(string), "\"\""),
			new("137: string", " ", typeof(string), "\" \""),
			new("138: string", null, typeof(string), "null"),
			new("139: StringBuilder", new StringBuilder(""), typeof(StringBuilder), "\"\""),
			new("140: StringBuilder", null, typeof(StringBuilder), "null"),
			new("141: TextBuilder", new TextBuilder(""), typeof(TextBuilder), "\"\""),
			new("142: TextBuilder", null, typeof(TextBuilder), "null"),
			new("143: GapBuffer<char>", new GapBuffer<char>(""), typeof(GapBuffer<char>), "\"\""),
			new("144: GapBuffer<char>", null, typeof(GapBuffer<char>), "null"),
			new("145: JsonString", new JsonString(""), typeof(JsonString), "\"\""),
			new("146: JsonString", null, typeof(JsonString), "null"),
			new("147: DateTime", DateTime.MinValue, typeof(DateTime), "\"0001-01-01T00:00:00\""),
			new("148: DateTime", DateTime.MaxValue, typeof(DateTime), "\"9999-12-31T23:59:59.9999999\""),
			new("149: DateTime", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime), "\"2023-10-31T16:01:02Z\""),
			new("150: DateTime", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime), "\"2023-10-31T12:01:03Z\""),
			new("151: DateTime", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime), "\"2023-10-31T12:01:04Z\""),
			new("152: DateTime?", null, typeof(DateTime?), "null"),
			new("153: DateTime?", DateTime.MinValue, typeof(DateTime?), "\"0001-01-01T00:00:00\""),
			new("154: DateTime?", DateTime.MaxValue, typeof(DateTime?), "\"9999-12-31T23:59:59.9999999\""),
			new("155: DateTime?", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime?), "\"2023-10-31T16:01:02Z\""),
			new("156: DateTime?", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime?), "\"2023-10-31T12:01:03Z\""),
			new("157: DateTime?", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime?), "\"2023-10-31T12:01:04Z\""),
			new("158: DateTimeOffset", DateTimeOffset.MinValue, typeof(DateTimeOffset), "\"0001-01-01T00:00:00+00:00\""),
			new("159: DateTimeOffset", DateTimeOffset.MaxValue, typeof(DateTimeOffset), "\"9999-12-31T23:59:59.9999999+00:00\""),
			new("160: DateTimeOffset", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset), "\"2023-10-31T12:01:02+01:02\""),
			new("161: DateTimeOffset?", null, typeof(DateTimeOffset?), "null"),
			new("162: DateTimeOffset?", DateTimeOffset.MinValue, typeof(DateTimeOffset?), "\"0001-01-01T00:00:00+00:00\""),
			new("163: DateTimeOffset?", DateTimeOffset.MaxValue, typeof(DateTimeOffset?), "\"9999-12-31T23:59:59.9999999+00:00\""),
			new("164: DateTimeOffset?", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset?), "\"2023-10-31T12:01:02+01:02\""),
			new("165: IsoDateTime", IsoDateTime.MinValue, typeof(IsoDateTime), "\"0001-01-01T00:00:00.0000000Z\""),
			new("166: IsoDateTime", IsoDateTime.MaxValue, typeof(IsoDateTime), "\"9999-12-31T23:59:59.9999999Z\""),
			new("167: IsoDateTime", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime), "\"2023-10-31T12:01:02.0000000-04:00/PT1H2M3.000S\""),
			new("168: IsoDateTime", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime), "\"2023-10-31T12:01:03.0000000Z/PT4H5M6.000S\""),
			new("169: IsoDateTime?", null, typeof(IsoDateTime?), "null"),
			new("170: IsoDateTime?", IsoDateTime.MinValue, typeof(IsoDateTime?), "\"0001-01-01T00:00:00.0000000Z\""),
			new("171: IsoDateTime?", IsoDateTime.MaxValue, typeof(IsoDateTime?), "\"9999-12-31T23:59:59.9999999Z\""),
			new("172: IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime?), "\"2023-10-31T12:01:02.0000000-04:00/PT1H2M3.000S\""),
			new("173: IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime?), "\"2023-10-31T12:01:03.0000000Z/PT4H5M6.000S\""),
			new("174: OscTimeTag", OscTimeTag.MinValue, typeof(OscTimeTag), "\"1900-01-01T00:00:00.0000000Z\""),
			new("175: OscTimeTag", OscTimeTag.MaxValue, typeof(OscTimeTag), "\"2036-02-07T06:28:16.0000000Z\""),
			new("176: OscTimeTag", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag), "\"2023-10-31T12:01:02.0000000Z\""),
			new("177: OscTimeTag?", null, typeof(OscTimeTag?), "null"),
			new("178: OscTimeTag?", OscTimeTag.MinValue, typeof(OscTimeTag?), "\"1900-01-01T00:00:00.0000000Z\""),
			new("179: OscTimeTag?", OscTimeTag.MaxValue, typeof(OscTimeTag?), "\"2036-02-07T06:28:16.0000000Z\""),
			new("180: OscTimeTag?", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag?), "\"2023-10-31T12:01:02.0000000Z\""),
			new("181: TimeSpan", TimeSpan.MinValue, typeof(TimeSpan), "\"-10675199.02:48:05.4775808\""),
			new("182: TimeSpan", TimeSpan.MaxValue, typeof(TimeSpan), "\"10675199.02:48:05.4775807\""),
			new("183: TimeSpan", TimeSpan.Zero, typeof(TimeSpan), "\"00:00:00\""),
			new("184: TimeSpan", new TimeSpan(1, 2, 3, 4, 5, 6), typeof(TimeSpan), "\"1.02:03:04.0050060\""),
			new("185: TimeSpan?", TimeSpan.MinValue, typeof(TimeSpan?), "\"-10675199.02:48:05.4775808\""),
			new("186: TimeSpan?", TimeSpan.MaxValue, typeof(TimeSpan?), "\"10675199.02:48:05.4775807\""),
			new("187: TimeSpan?", TimeSpan.Zero, typeof(TimeSpan?), "\"00:00:00\""),
			new("188: TimeSpan?", new TimeSpan(1, 2, 3, 4, 5, 6), typeof(TimeSpan?), "\"1.02:03:04.0050060\""),
			new("189: TimeSpan?", null, typeof(TimeSpan?), "null"),
			new("190: Guid", Guid.Empty, typeof(Guid), "\"00000000-0000-0000-0000-000000000000\""),
			new("191: Guid", Guid.Parse("6dcefb3f-4b1c-40fd-827e-58d31767e4a8"), typeof(Guid), "\"6dcefb3f-4b1c-40fd-827e-58d31767e4a8\""),
			new("192: Guid", Guid.Parse("00000000-0000-0000-0000-000000000001"), typeof(Guid), "\"00000000-0000-0000-0000-000000000001\""),
			new("193: Guid", Guid.Parse("10000000-0000-0000-0000-000000000000"), typeof(Guid), "\"10000000-0000-0000-0000-000000000000\""),
			new("194: Guid?", null, typeof(Guid?), "null"),
			new("195: Guid?", Guid.Empty, typeof(Guid?), "\"00000000-0000-0000-0000-000000000000\""),
			new("196: Guid?", Guid.Parse("6dcefb3f-4b1c-40fd-827e-58d31767e4a8"), typeof(Guid?), "\"6dcefb3f-4b1c-40fd-827e-58d31767e4a8\""),
			new("197: Guid?", Guid.Parse("00000000-0000-0000-0000-000000000001"), typeof(Guid?), "\"00000000-0000-0000-0000-000000000001\""),
			new("198: Guid?", Guid.Parse("10000000-0000-0000-0000-000000000000"), typeof(Guid?), "\"10000000-0000-0000-0000-000000000000\""),
			new("199: ShortGuid", ShortGuid.Empty, typeof(ShortGuid), "\"00000000-0000-0000-0000-000000000000\""),
			new("200: ShortGuid", ShortGuid.Parse("P_vObRxL_UCCfljTF2fkqA"), typeof(ShortGuid), "\"6dcefb3f-4b1c-40fd-827e-58d31767e4a8\""),
			new("201: ShortGuid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAAQ"), typeof(ShortGuid), "\"00000000-0000-0000-0000-000000000001\""),
			new("202: ShortGuid", ShortGuid.Parse("AAAAEAAAAAAAAAAAAAAAAA"), typeof(ShortGuid), "\"10000000-0000-0000-0000-000000000000\""),
			new("203: ShortGuid?", null, typeof(ShortGuid?), "null"),
			new("204: ShortGuid?", ShortGuid.Empty, typeof(ShortGuid?), "\"00000000-0000-0000-0000-000000000000\""),
			new("205: ShortGuid?", ShortGuid.Parse("P_vObRxL_UCCfljTF2fkqA"), typeof(ShortGuid?), "\"6dcefb3f-4b1c-40fd-827e-58d31767e4a8\""),
			new("206: ShortGuid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAAQ"), typeof(ShortGuid?), "\"00000000-0000-0000-0000-000000000001\""),
			new("207: ShortGuid?", ShortGuid.Parse("AAAAEAAAAAAAAAAAAAAAAA"), typeof(ShortGuid?), "\"10000000-0000-0000-0000-000000000000\""),
			new("208: Version", new Version(0, 0), typeof(Version), "\"0.0\""),
			new("209: Version", new Version(1, 2), typeof(Version), "\"1.2\""),
			new("210: Version", new Version(1, 2, 3), typeof(Version), "\"1.2.3\""),
			new("211: Version", new Version(1, 2, 3, 4), typeof(Version), "\"1.2.3.4\""),
			new("212: Version", new Version(12345, 12345, 12345, 12345), typeof(Version), "\"12345.12345.12345.12345\""),
			new("213: Version", new Version(99999, 99999, 99999, 99999), typeof(Version), "\"99999.99999.99999.99999\""),
			new("214: Version", null, typeof(Version), "null"),
			#if (!NET48)
			new("215: DateOnly", DateOnly.MinValue, typeof(DateOnly), "\"0001-01-01\""),
			new("216: DateOnly", DateOnly.MaxValue, typeof(DateOnly), "\"9999-12-31\""),
			new("217: DateOnly", new DateOnly(2023, 10, 31), typeof(DateOnly), "\"2023-10-31\""),
			new("218: DateOnly?", null, typeof(DateOnly?), "null"),
			new("219: DateOnly?", DateOnly.MinValue, typeof(DateOnly?), "\"0001-01-01\""),
			new("220: DateOnly?", DateOnly.MaxValue, typeof(DateOnly?), "\"9999-12-31\""),
			new("221: DateOnly?", new DateOnly(2023, 10, 31), typeof(DateOnly?), "\"2023-10-31\""),
			new("222: TimeOnly", TimeOnly.MinValue, typeof(TimeOnly), "\"00:00:00\""),
			new("223: TimeOnly", TimeOnly.MaxValue, typeof(TimeOnly), "\"23:59:59.9999999\""),
			new("224: TimeOnly", TimeOnly.Parse("11:01:02.0030040"), typeof(TimeOnly), "\"11:01:02.003004\""),
			new("225: TimeOnly?", TimeOnly.MinValue, typeof(TimeOnly?), "\"00:00:00\""),
			new("226: TimeOnly?", TimeOnly.MaxValue, typeof(TimeOnly?), "\"23:59:59.9999999\""),
			new("227: TimeOnly?", TimeOnly.Parse("11:01:02.0030040"), typeof(TimeOnly?), "\"11:01:02.003004\""),
			new("228: TimeOnly?", null, typeof(TimeOnly?), "null"),
			#endif
			// </Scenarios>
		};

		return scenarios;
	}

	private bool IsScenarioSupported(IJsonSerializer serializer, SerializationScenario scenario)
	{
		if (scenario.Value == null)
		{
			return true;
		}

		// todo: figure out what do with these values
		return serializer is not NewtonsoftJsonSerializer
			|| (!scenario.Value.Equals(char.MinValue)
				&& !scenario.Value.Equals(char.MaxValue)
				&& scenario.Value is not
					(StringBuilder
					or TextBuilder
					or GapBuffer<char>
					)
				#if (!NET48)
				&& !scenario.Value.Equals(float.NegativeZero)
				&& !scenario.Value.Equals(double.NegativeZero)
				#endif
			);
	}

	private void ProcessTypes(Type[] types, JsonSerializer baseSerializer, ref int scenarioIndex, TextBuilder builder)
	{
		foreach (var type in types)
		{
			var values = GetValuesForTesting(type);

			foreach (var value in values)
			{
				// public SerializationScenario(string name, object value, [Type] type, string expected)
				var code = baseSerializer.ToJson(value);
				var line = string.Format(
					"new(\"{0}: {1}\", {2}, typeof({1}), \"{3}\"),",
					scenarioIndex++,
					CSharpCodeWriter.GetCodeTypeName(type),
					CSharpCodeWriter.GenerateCode(value),
					code.Escape()
				);

				builder.AppendLine(line);
			}
		}
	}

	#endregion

	#region Classes

	public class Person
	{
		#region Properties

		public Person[] Children { get; set; }

		public string Name { get; set; }

		public Person Parent { get; set; }

		#endregion
	}

	public class Sample
	{
		#region Constructors

		public Sample()
		{
			IsEnabled = true;
			BoolFalse = false;
			BoolNullable = null;
			Foreground = ConsoleColor.Blue;
			Selection = new(12, 321);
		}

		#endregion

		#region Properties

		public bool BoolFalse { get; set; }

		public bool? BoolNullable { get; set; }

		public ConsoleColor? Foreground { get; set; }

		public bool IsEnabled { get; set; }

		public (int start, int length) Selection { get; set; }

		#endregion
	}

	public class Sample2 : Sample
	{
		#region Properties

		public new int BoolFalse { get; set; }

		#endregion
	}

	[SerializedModel(nameof(FirstName), nameof(LastName))]
	public class SampleWithAttribute
	{
		#region Properties

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string PasswordHash { get; set; }

		#endregion
	}

	public class SampleWithIgnoredMembers
	{
		#region Properties

		public string FirstName { get; set; }

		public string LastName { get; set; }

		[SerializationIgnore]
		public string Nickname { get; set; }

		#endregion
	}

	#endregion
}