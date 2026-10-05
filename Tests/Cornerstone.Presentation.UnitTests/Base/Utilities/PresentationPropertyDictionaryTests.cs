#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class PresentationPropertyDictionaryTests
{
	#region Fields

	private static readonly PresentationProperty[] TestProperties;

	#endregion

	#region Constructors

	static PresentationPropertyDictionaryTests()
	{
		TestProperties = new PresentationProperty[100];

		for (var i = 0; i < 100; ++i)
		{
			TestProperties[i] = new StyledProperty<string>(
				$"Test{i}",
				typeof(PresentationPropertyDictionaryTests),
				typeof(PresentationPropertyDictionaryTests),
				new StyledPropertyMetadata<string>());
		}

		Shuffle(TestProperties, 42);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void AddAddsNewValue(int count)
	{
		var target = CreateTarget(count);
		var index = count;
		var property = TestProperties[index];

		target.Add(property, "new");

		CornerstoneTest.AreEqual("new", target[property]);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void AddThrowsIfKeyExists(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var property = TestProperties[index];

		Assert.Throws<ArgumentException>(() => target.Add(property, "new"));
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void ContainsKeyReturnsFalseIfValueDoesNotExist(int count)
	{
		var target = CreateTarget(count);
		var index = count;
		var property = TestProperties[index];

		CornerstoneTest.IsFalse(target.ContainsKey(property));
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void ContainsKeyReturnsTrueIfValueExists(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var property = TestProperties[index];

		CornerstoneTest.IsTrue(target.ContainsKey(property));
	}

	public static TestRows<int> Counts()
	{
		var result = new TestRows<int>();
		result.Add(0);
		result.Add(1);
		result.Add(10);
		result.Add(13);
		result.Add(50);
		result.Add(72);
		return result;
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void GetValueFindsValue(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;

		var value = target.GetValue(index);

		CornerstoneTest.IsNotNull(value);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void GetValueThrowsIfIndexOutOfRange(int count)
	{
		var target = CreateTarget(count);
		var index = count;

		Assert.Throws<IndexOutOfRangeException>(() => target.GetValue(index));
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void IntIndexerFindsValue(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var result = target[index];

		CornerstoneTest.IsNotNull(result);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void IntIndexerThrowsIfIndexOutOfRange(int count)
	{
		var target = CreateTarget(count);
		var index = count;

		Assert.Throws<IndexOutOfRangeException>(() => target[index]);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void PropertyIndexerAddsNewValue(int count)
	{
		var target = CreateTarget(count);
		var index = count;
		var property = TestProperties[index];

		target[property] = "new";

		CornerstoneTest.AreEqual("new", target[property]);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void PropertyIndexerFindsValue(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var property = TestProperties[index];
		var result = target[property];

		CornerstoneTest.AreEqual($"Value{index}", result);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void PropertyIndexerSetsExistingValue(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var property = TestProperties[index];

		CornerstoneTest.AreEqual($"Value{index}", target[property]);

		target[property] = "new";

		CornerstoneTest.AreEqual("new", target[property]);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void PropertyIndexerThrowsIfValueNotFound(int count)
	{
		var target = CreateTarget(count);
		var index = count;
		var property = TestProperties[index];

		Assert.Throws<KeyNotFoundException>(() => target[property]);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void RemoveRemovesValue(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var property = TestProperties[index];

		CornerstoneTest.IsTrue(target.Remove(property));
		CornerstoneTest.IsFalse(target.ContainsKey(property));
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void RemoveReturnsExistingValue(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var property = TestProperties[index];

		CornerstoneTest.IsTrue(target.Remove(property, out var value));
		CornerstoneTest.AreEqual($"Value{index}", value);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void RemoveReturnsFalseIfValueNotPresent(int count)
	{
		var target = CreateTarget(count);
		var index = count;
		var property = TestProperties[index];

		CornerstoneTest.IsFalse(target.Remove(property));
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void TryAddAddsNewValue(int count)
	{
		var target = CreateTarget(count);
		var index = count;
		var property = TestProperties[index];

		CornerstoneTest.IsTrue(target.TryAdd(property, "new"));

		CornerstoneTest.AreEqual("new", target[property]);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void TryAddReturnsFalseIfKeyExists(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var property = TestProperties[index];

		CornerstoneTest.IsFalse(target.TryAdd(property, "new"));
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void TryGetValueFindsValue(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count / 2;
		var property = TestProperties[index];

		CornerstoneTest.IsTrue(target.TryGetValue(property, out var value));
		CornerstoneTest.AreEqual($"Value{index}", value);
	}

	[PresentationTestMethod]
	[TestData(nameof(Counts))]
	public void TryGetValueReturnsFalseIfKeyDoesNotExist(int count)
	{
		if (count == 0)
		{
			return;
		}

		var target = CreateTarget(count);
		var index = count;
		var property = TestProperties[index];

		CornerstoneTest.IsFalse(target.TryGetValue(property, out var value));
		CornerstoneTest.IsNull(value);
	}

	private static PresentationPropertyDictionary<string> CreateTarget(int items)
	{
		var result = new PresentationPropertyDictionary<string>();

		for (var i = 0; i < items; ++i)
		{
			result.Add(TestProperties[i], $"Value{i}");
		}

		return result;
	}

	private static void Shuffle<T>(T[] array, int seed)
	{
		var rng = new Random(seed);

		var n = array.Length;
		while (n > 1)
		{
			var k = rng.Next(n--);
			var temp = array[n];
			array[n] = array[k];
			array[k] = temp;
		}
	}

	#endregion
}