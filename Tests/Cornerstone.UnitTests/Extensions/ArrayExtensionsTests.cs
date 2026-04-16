#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class ArrayExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CombineArraysCombinesMultipleArraysCorrectly()
	{
		var array1 = new[] { 1, 2, 3 };
		var array2 = new[] { 4, 5 };
		var array3 = new[] { 6, 7, 8, 9 };

		var result = ArrayExtensions.CombineArrays(array1, array2, array3);

		AreEqual(9, result.Length);
		AreEqual(1, result[0]);
		AreEqual(2, result[1]);
		AreEqual(3, result[2]);
		AreEqual(4, result[3]);
		AreEqual(5, result[4]);
		AreEqual(6, result[5]);
		AreEqual(7, result[6]);
		AreEqual(8, result[7]);
		AreEqual(9, result[8]);
	}

	[TestMethod]
	public void CombineArraysHandlesDifferentTypes()
	{
		var array1 = new[] { "Hello", "World" };
		var array2 = new[] { "Test" };
		var array3 = Array.Empty<string>();
		var result = ArrayExtensions.CombineArrays(array1, array2, array3);

		AreEqual(3, result.Length);
		AreEqual("Hello", result[0]);
		AreEqual("World", result[1]);
		AreEqual("Test", result[2]);
	}

	[TestMethod]
	public void CombineArraysPreservesOrderOfInputArrays()
	{
		var array1 = new[] { 'A', 'B' };
		var array2 = new[] { 'C' };
		var array3 = new[] { 'D', 'E', 'F' };
		var result = ArrayExtensions.CombineArrays(array1, array2, array3);

		AreEqual(['A', 'B', 'C', 'D', 'E', 'F'], result);
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "UseCollectionExpression")]
	public void CombineArraysReturnsEmptyArrayWhenAllArraysAreEmpty()
	{
		var result = ArrayExtensions.CombineArrays(Array.Empty<int>(), Array.Empty<int>());
		AreEqual(0, result.Length);
	}

	[TestMethod]
	public void CombineArraysReturnsEmptyArrayWhenNoArraysProvided()
	{
		var result = ArrayExtensions.CombineArrays<int>();
		AreEqual(0, result.Length);
	}

	[TestMethod]
	public void IterateListConvertsICollectionToArray()
	{
		var hashSet = new HashSet<int> { 10, 20, 30 };
		var result = hashSet.IterateList();

		IsTrue(result is List<object>);
		AreEqual(3, result.Count);

		// Order is not guaranteed in HashSet, so we check content only
		IsTrue(result.Contains(10));
		IsTrue(result.Contains(20));
		IsTrue(result.Contains(30));
	}

	[TestMethod]
	public void IterateListConvertsNonCollectionEnumerableToList()
	{
		var enumerable = GetTestEnumerable();
		var result = enumerable.IterateList();
		IsTrue(result is List<object>);
		AreEqual(4, result.Count);
		AreEqual(100, result[0]);
		AreEqual("Test", result[1]);
		AreEqual(true, result[2]);
		AreEqual(42.5, result[3]);
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
	public void IterateListHandlesNullEnumerable()
	{
		IEnumerable enumerable = null;
		var result = enumerable.IterateList();
		IsNotNull(result);
		AreEqual(0, result.Count);
	}

	[TestMethod]
	public void IterateListReturnsOriginalListWhenInputIsAlreadyIList()
	{
		var originalList = new List<string> { "Apple", "Banana", "Cherry" };
		var result = originalList.IterateList();
		IsTrue(result is List<string>);
		AreEqual(3, result.Count);
		AreEqual("Apple", result[0]);
		AreEqual("Banana", result[1]);
		AreEqual("Cherry", result[2]);
	}

	// Helper method to simulate a non-ICollection enumerable
	private IEnumerable GetTestEnumerable()
	{
		yield return 100;
		yield return "Test";
		yield return true;
		yield return 42.5;
	}

	#endregion
}