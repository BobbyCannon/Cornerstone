#region References

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization;

[TestClass]
public partial class SerializerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CreateOptionsCanEnableIndentAndPascalCase()
	{
		var sample = new NamingSample { DisplayName = "Alpha", ItemCount = 3 };

		var options = Serializer.CreateOptions(o =>
		{
			o.WriteIndented = true;
			o.PropertyNamingPolicy = null;
			o.DictionaryKeyPolicy = null;
		});

		var json = sample.ToJson(options);

		IsTrue(json.Contains('\n') || json.Contains("\r\n"));
		IsTrue(json.Contains("\"DisplayName\""));
		IsTrue(json.Contains("\"ItemCount\""));
		IsFalse(json.Contains("\"displayName\""));
	}

	[TestMethod]
	public void CreateOptionsCanUseSnakeCaseNaming()
	{
		var sample = new NamingSample { DisplayName = "Beta", ItemCount = 7 };

		var options = Serializer.CreateOptions(o =>
		{
			o.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
			o.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
		});

		var json = sample.ToJson(options);

		IsTrue(json.Contains("\"display_name\""));
		IsTrue(json.Contains("\"item_count\""));
	}

	[TestMethod]
	public void CreateOptionsInheritsEncoderFromBase()
	{
		var options = Serializer.CreateOptions();
		AreEqual(Serializer.SerializationOptions.Encoder, options.Encoder);
		AreEqual(Serializer.SerializationOptions.DefaultIgnoreCondition, options.DefaultIgnoreCondition);
		AreEqual(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, options.Encoder);
	}

	[TestMethod]
	public void DefaultToJsonIsCompactCamelCase()
	{
		var sample = new NamingSample { DisplayName = "Gamma", ItemCount = 1 };
		var json = sample.ToJson();

		IsFalse(json.Contains('\n'));
		IsTrue(json.Contains("\"displayName\""));
	}

	[TestMethod]
	public void FromJsonWithOptionsRoundTrips()
	{
		var sample = new NamingSample { DisplayName = "Delta", ItemCount = 9 };
		var options = Serializer.CreateOptions(o =>
		{
			o.PropertyNamingPolicy = null;
			o.DictionaryKeyPolicy = null;
		});

		var json = sample.ToJson(options);
		var restored = json.FromJson<NamingSample>(options);

		AreEqual(sample.DisplayName, restored.DisplayName);
		AreEqual(sample.ItemCount, restored.ItemCount);
	}

	[TestMethod]
	public void InnerJson()
	{
		// Do not call Serializer.AddTypeInfoResolvers here. Global SerializationOptions becomes
		// read-only after the first serialize/deserialize (other tests in this suite often run first).
		// Test is covered by DefaultJsonTypeInfoResolver already registered on SerializationOptions.
		var testInner = new Test
		{
			Json = "{\"Foo\":\"Bar\"}"
		};
		var test = new Test
		{
			Json = testInner.ToRawJson()
		};

		//var expected = "{\"json\":\"{\\u0022json\\u0022:\\u0022{\\\\u0022Foo\\\\u0022:\\\\u0022Bar\\\\u0022}\\u0022}\"}";
		var expected = "{\"json\":\"{\\\"json\\\":\\\"{\\\\\\\"Foo\\\\\\\":\\\\\\\"Bar\\\\\\\"}\\\"}\"}";
		var actual = test.ToRawJson();
		AreEqual(expected, actual);

		var bytes = Encoding.Unicode.GetBytes(actual);
		var content = Encoding.Unicode.GetString(bytes);
		var actualTest = content.FromJson<Test>();
		AreEqual(test, actualTest);

		var packed = actualTest.ToSpeedyPacket().ToByteArray().ToArray();
		var unpacked = (Test) SpeedyPack.Unpack(packed, typeof(Test));
		AreEqual(test, unpacked);

		var syncObject = SyncObject.ToSyncObject(test);
		var fromSyncObject = syncObject.ToSyncModel();
		AreEqual(test, fromSyncObject);
	}

	[TestMethod]
	public void ToJsonFileWritesIndentedContent()
	{
		var path = Path.Combine(Path.GetTempPath(), $"cornerstone-serializer-{Guid.NewGuid():N}.json");
		try
		{
			var sample = new NamingSample { DisplayName = "Epsilon", ItemCount = 2 };
			var options = Serializer.CreateOptions(o =>
			{
				o.WriteIndented = true;
				o.PropertyNamingPolicy = null;
				o.DictionaryKeyPolicy = null;
			});

			Serializer.ToJsonFile(path, sample, options);

			var onDisk = File.ReadAllText(path);
			IsTrue(onDisk.Contains('\n') || onDisk.Contains("\r\n"));
			IsTrue(onDisk.Contains("\"DisplayName\""));
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	#endregion

	#region Classes

	public class NamingSample
	{
		#region Properties

		public string DisplayName { get; set; }

		public int ItemCount { get; set; }

		#endregion
	}

	[SourceReflection]
	[Packable(1, [nameof(Json)])]
	public partial class Test : SyncModel
	{
		#region Properties

		public string Json { get; set; }

		#endregion
	}

	#endregion
}