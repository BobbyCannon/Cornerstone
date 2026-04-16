#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cornerstone.Data;
using Cornerstone.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class PartialUpdateTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddOrUpdateSupportsTypeConversion()
	{
		var partial = new PartialUpdate();
		partial.AddOrUpdate("Age", "42");
		IsTrue(partial.TryGet(out int age, "Age"));
		AreEqual(42, age);
	}

	[TestMethod]
	public void CaseInsensitiveKeysAreHandledCorrectly()
	{
		var dict = new Dictionary<string, JsonElement>
		{
			["name"] = Serializer.FromJson<JsonElement>("\"CaseInsensitive\"")
		};

		var partial = PartialUpdate.FromDictionary(dict);

		IsTrue(partial.TryGet(out string value, "Name"));
		AreEqual("CaseInsensitive", value);
	}

	[TestMethod]
	public void FromDictionaryHandlesPrimitivesCorrectly()
	{
		var dict = new Dictionary<string, JsonElement>
		{
			["Name"] = Serializer.FromJson<JsonElement>("\"John\""),
			["Age"] = Serializer.FromJson<JsonElement>("42"),
			["IsActive"] = Serializer.FromJson<JsonElement>("true")
		};

		var partial = PartialUpdate.FromDictionary(dict);

		IsTrue(partial.TryGet(out string name, "Name"));
		AreEqual("John", name);

		IsTrue(partial.TryGet(out int age, "Age"));
		AreEqual(42, age);

		IsTrue(partial.TryGet(out bool active, "IsActive"));
		IsTrue(active);
	}

	[TestMethod]
	public void FromDictionaryWithGenericTypeUsesPropertyCache()
	{
		var dict = new Dictionary<string, JsonElement>
		{
			["Name"] = Serializer.FromJson<JsonElement>("\"Alice\""),
			["Age"] = Serializer.FromJson<JsonElement>("30"),
			["UnknownProp"] = Serializer.FromJson<JsonElement>("\"extra\"")
		};

		var partial = PartialUpdate<TestModel>.FromDictionary(dict);

		IsTrue(partial.TryGet(out string name, "Name"));
		AreEqual("Alice", name);

		IsTrue(partial.TryGet(out int age, "Age"));
		AreEqual(30, age);

		IsTrue(partial.TryGet(out string extra, "UnknownProp"));
		AreEqual("extra", extra);
	}

	[TestMethod]
	public void FromJsonElementHandlesPrimitivesCorrectly()
	{
		var json = """{"Name": "John", "Age": 42, "IsActive": true}""";
		var element = Serializer.FromJson<JsonElement>(json);

		var partial = PartialUpdate.FromJsonElement(element);

		IsTrue(partial.TryGet(out string name, "Name"));
		AreEqual("John", name);

		IsTrue(partial.TryGet(out int age, "Age"));
		AreEqual(42, age);

		IsTrue(partial.TryGet(out bool active, "IsActive"));
		IsTrue(active);
	}

	[TestMethod]
	public void FromJsonElementWithGenericTypeUsesPropertyCache()
	{
		var json = """{"Name":"Bob","Age":25}""";
		var element = Serializer.FromJson<JsonElement>(json);
		var partial = PartialUpdate<TestModel>.FromJsonElement(element);

		IsTrue(partial.TryGet(out string name, "Name"));
		AreEqual("Bob", name);

		IsTrue(partial.TryGet(out int age, "Age"));
		AreEqual(25, age);
	}

	[TestMethod]
	public void GivenEmptyDictionaryReturnsEmptyPartialUpdate()
	{
		var result = PartialUpdate.FromDictionary(new Dictionary<string, JsonElement>());
		IsNotNull(result);
		AreEqual(0, result.Updates.Count);
	}

	[TestMethod]
	public void GivenNullDictionaryReturnsNewPartialUpdate()
	{
		var result = PartialUpdate.FromDictionary(null);
		IsNotNull(result);
		AreEqual(0, result.Updates.Count);
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
	public void JsonHandlesNullPartialUpdate()
	{
		PartialUpdate<TestModel> original = null;
		var json = original.ToJson();
		var deserialized = json.FromJson<PartialUpdate<TestModel>>();
		IsNull(deserialized);
	}

	[TestMethod]
	public void JsonRoundtripPreservesValues()
	{
		var original = new PartialUpdate<TestModel>();
		original.AddOrUpdate("Name", "Charlie");
		original.AddOrUpdate("Age", 35);

		var json = original.ToJson();
		var deserialized = json.FromJson<PartialUpdate<TestModel>>();

		IsNotNull(deserialized);
		IsTrue(deserialized.TryGet(out string name, "Name"));
		AreEqual("Charlie", name);
		IsTrue(deserialized.TryGet(out int age, "Age"));
		AreEqual(35, age);
	}

	[TestMethod]
	public void ToDictionaryReturnsCorrectValues()
	{
		var partial = new PartialUpdate();
		partial.AddOrUpdate("Name", "Test");
		partial.AddOrUpdate("Age", 99);

		var dict = partial.ToDictionary();

		AreEqual(2, dict.Count);
		AreEqual("Test", dict["Name"]);
		AreEqual(99, dict["Age"]);
	}

	[TestMethod]
	public void TryGetReturnsFalseForMissingKey()
	{
		var partial = new PartialUpdate();
		IsFalse(partial.TryGet<string>(out _, "Missing"));
	}

	[TestMethod]
	public void TryUpdateInvokesOnlyWhenKeyExists()
	{
		var partial = new PartialUpdate();
		partial.AddOrUpdate("Name", "Original");

		var called = false;
		partial.TryUpdate<string>(_ => { called = true; }, "Name");

		IsTrue(called);
		IsTrue(partial.TryGet(out string name, "Name"));
		AreEqual("Original", name);
	}

	#endregion

	#region Classes

	public class TestModel
	{
		#region Properties

		public int Age { get; set; }
		public DateTime BirthDate { get; set; }
		public object DynamicValue { get; set; }
		public string Name { get; set; }

		#endregion
	}

	#endregion
}