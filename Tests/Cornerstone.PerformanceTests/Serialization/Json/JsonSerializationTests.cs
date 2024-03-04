#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Serialization;
using Cornerstone.Text;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Serialization.Json;

[TestClass]
public class JsonSerializationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ObjectJsonParse()
	{
		var filePath = $"{UnitTestsDirectory}\\Resources\\object.json";
		var expected = GetExpected();
		File.WriteAllText(filePath,
			//JsonConvert.SerializeObject(expected, Formatting.Indented)
			expected.ToJson(new SerializationOptions { TextFormat = TextFormat.Indented })
		);

		var json = File.ReadAllText(filePath);
		var actual = json.FromJson<ObjectJson>();
		AreEqual(expected, actual);
	}

	private ObjectJson GetExpected()
	{
		return new ObjectJson
		{
			Booleans = GetValuesForTesting(typeof(bool)).Cast<bool>().ToArray(),
			Character = '\0',
			CharArray = ['\"', '\\', '/', '\b', '\f', '\n', '\r', '\t'],
			DateTime = new DateTime(2024, 01, 02, 03, 04, 05, 06, DateTimeKind.Unspecified),
			DateTimeLocal = new DateTime(2024, 01, 02, 03, 04, 05, 07, DateTimeKind.Local),
			DateTimeNull = null,
			DateTimeUtc = new DateTime(2024, 01, 02, 03, 04, 05, 08, DateTimeKind.Utc),
			DateTimeOffset = new DateTimeOffset(2024, 01, 02, 03, 04, 05, TimeSpan.FromMinutes(15)),
			DateTimeOffsetNull = null,
			Guid = new Guid("8d6533aa-4103-4071-9341-1d2f64904f7a"),
			InterfaceListString = new List<string> { null, "foo", "bar" },
			ListString = ["hello", null, "world"],
			ShortGuid = new ShortGuid("F071D3BC-0FA9-4D59-A44E-92E29CF98406"),
			TextFormatEnum = TextFormat.Spaced
		};
	}

	#endregion

	#region Classes

	public class ObjectJson
	{
		#region Properties

		public bool[] Booleans { get; set; }

		public char Character { get; set; }

		public char[] CharArray { get; set; }

		public DateTime DateTime { get; set; }

		public DateTime DateTimeLocal { get; set; }

		public DateTime? DateTimeNull { get; set; }

		public DateTimeOffset DateTimeOffset { get; set; }

		public DateTimeOffset? DateTimeOffsetNull { get; set; }

		public DateTime DateTimeUtc { get; set; }

		public Guid Guid { get; set; }

		public IList<string> InterfaceListString { get; set; }

		public List<string> ListString { get; set; }

		public ShortGuid ShortGuid { get; set; }

		public TextFormat TextFormatEnum { get; set; }

		#endregion
	}

	#endregion
}