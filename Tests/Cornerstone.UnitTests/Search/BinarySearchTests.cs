#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Search;

[TestClass]
public class BinarySearchTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Ceil()
	{
		//               0   1   2   3   4
		int[] valueArray = [10, 20, 20, 35, 42];

		AreEqual(35, BinarySearch.FindCeil(valueArray, 21, -1));
		AreEqual(35, BinarySearch.FindCeil(valueArray, 35, -1));
		AreEqual(42, BinarySearch.FindCeil(valueArray, 36, -1));
		AreEqual(42, BinarySearch.FindCeil(valueArray, 42, -1));
		AreEqual(-1, BinarySearch.FindCeil(valueArray, 100, -1));

		AreEqual(20, BinarySearch.FindCeil(valueArray, 20, 100));
		AreEqual(42, BinarySearch.FindCeil(valueArray, 42, 100));
		AreEqual(100, BinarySearch.FindCeil(valueArray, 43, 100));

		AreEqual(1, BinarySearch.FindCeilIndex(valueArray, 20));
		AreEqual(0, BinarySearch.FindCeilIndex(valueArray, 5));
		AreEqual(-1, BinarySearch.FindCeilIndex(valueArray, 50));
		
		SpeedyList<int> valueSpeedyList = [10, 20, 20, 35, 42];

		AreEqual(35, BinarySearch.FindCeil(valueSpeedyList, 21, -1));
		AreEqual(35, BinarySearch.FindCeil(valueSpeedyList, 35, -1));
		AreEqual(42, BinarySearch.FindCeil(valueSpeedyList, 36, -1));
		AreEqual(42, BinarySearch.FindCeil(valueSpeedyList, 42, -1));
		AreEqual(-1, BinarySearch.FindCeil(valueSpeedyList, 100, -1));

		AreEqual(20, BinarySearch.FindCeil(valueSpeedyList, 20, 100));
		AreEqual(42, BinarySearch.FindCeil(valueSpeedyList, 42, 100));
		AreEqual(100, BinarySearch.FindCeil(valueSpeedyList, 43, 100));

		AreEqual(1, BinarySearch.FindCeilIndex(valueSpeedyList, 20));
		AreEqual(0, BinarySearch.FindCeilIndex(valueSpeedyList, 5));
		AreEqual(-1, BinarySearch.FindCeilIndex(valueSpeedyList, 50));
	}

	[TestMethod]
	public void CeilIndexNegativeNumbers()
	{
		int[] valuesArray = [-50, -10, 0, 15, 30];

		AreEqual(0, BinarySearch.FindCeilIndex(valuesArray, -60));
		AreEqual(1, BinarySearch.FindCeilIndex(valuesArray, -20));
		AreEqual(2, BinarySearch.FindCeilIndex(valuesArray, -5));
		AreEqual(3, BinarySearch.FindCeilIndex(valuesArray, 10));
		AreEqual(-1, BinarySearch.FindCeilIndex(valuesArray, 100));
		
		SpeedyList<int> valuesSpeedyList = [-50, -10, 0, 15, 30];

		AreEqual(0, BinarySearch.FindCeilIndex(valuesSpeedyList, -60));
		AreEqual(1, BinarySearch.FindCeilIndex(valuesSpeedyList, -20));
		AreEqual(2, BinarySearch.FindCeilIndex(valuesSpeedyList, -5));
		AreEqual(3, BinarySearch.FindCeilIndex(valuesSpeedyList, 10));
		AreEqual(-1, BinarySearch.FindCeilIndex(valuesSpeedyList, 100));
	}

	[TestMethod]
	public void CeilNegativeNumbers()
	{
		int[] valuesArray = [-50, -10, 0, 15, 30];
		
		AreEqual(-50, BinarySearch.FindCeil(valuesArray, -60, -999));
		AreEqual(-10, BinarySearch.FindCeil(valuesArray, -20, -999));
		AreEqual(0, BinarySearch.FindCeil(valuesArray, -5, -999));
		AreEqual(15, BinarySearch.FindCeil(valuesArray, 10, -999));
		AreEqual(-999, BinarySearch.FindCeil(valuesArray, 100, -999));

		SpeedyList<int> valuesSpeedyList = [-50, -10, 0, 15, 30];
		
		AreEqual(-50, BinarySearch.FindCeil(valuesSpeedyList, -60, -999));
		AreEqual(-10, BinarySearch.FindCeil(valuesSpeedyList, -20, -999));
		AreEqual(0, BinarySearch.FindCeil(valuesSpeedyList, -5, -999));
		AreEqual(15, BinarySearch.FindCeil(valuesSpeedyList, 10, -999));
		AreEqual(-999, BinarySearch.FindCeil(valuesSpeedyList, 100, -999));
	}

	[TestMethod]
	public void CeilWithSingleValue()
	{
		int[] buffer = [10];

		AreEqual(10, BinarySearch.FindCeil(buffer, 9, -1));
		AreEqual(10, BinarySearch.FindCeil(buffer, 10, -1));
		AreEqual(-1, BinarySearch.FindCeil(buffer, 11, -1));

		AreEqual(0, BinarySearch.FindCeilIndex(buffer, 9));
		AreEqual(0, BinarySearch.FindCeilIndex(buffer, 10));
		AreEqual(-1, BinarySearch.FindCeilIndex(buffer, 11));
	}

	[TestMethod]
	public void EmptyCollectionDifferentFallbacks()
	{
		var empty = Array.Empty<int>();

		AreEqual(-999, BinarySearch.FindFloor(empty, 0, -999));
		AreEqual(9999, BinarySearch.FindFloor(empty, 500, 9999));
		AreEqual(-1, BinarySearch.FindFloor(empty, -100, -1));

		AreEqual(-999, BinarySearch.FindCeil(empty, 0, -999));
		AreEqual(777, BinarySearch.FindCeil(empty, -50, 777));

		AreEqual(-1, BinarySearch.FindFloorIndex(empty, 123));
		AreEqual(-1, BinarySearch.FindFloorIndex(empty, -123));

		AreEqual(-1, BinarySearch.FindCeilIndex(empty, 123));
		AreEqual(-1, BinarySearch.FindCeilIndex(empty, -999));
	}

	[TestMethod]
	public void Floor()
	{
		//               0   1   2   3   4
		int[] valueArray = [10, 20, 20, 35, 42];

		AreEqual(10, BinarySearch.FindFloor(valueArray, 18, -1));
		AreEqual(20, BinarySearch.FindFloor(valueArray, 20, -1));

		AreEqual(2, BinarySearch.FindFloorIndex(valueArray, 20));
		AreEqual(3, BinarySearch.FindFloorIndex(valueArray, 36));
		
		SpeedyList<int> valueSpeedyList = [10, 20, 20, 35, 42];

		AreEqual(10, BinarySearch.FindFloor(valueSpeedyList, 18, -1));
		AreEqual(20, BinarySearch.FindFloor(valueSpeedyList, 20, -1));

		AreEqual(2, BinarySearch.FindFloorIndex(valueSpeedyList, 20));
		AreEqual(3, BinarySearch.FindFloorIndex(valueSpeedyList, 36));
	}

	[TestMethod]
	public void FloorAndCeilAtInt32Boundaries()
	{
		int[] values = [int.MinValue, -1000000, 0, 1000000, int.MaxValue];

		// Floor
		AreEqual(int.MinValue, BinarySearch.FindFloor(values, int.MinValue, -1));
		AreEqual(int.MinValue, BinarySearch.FindFloor(values, int.MinValue + 1, -1));
		AreEqual(int.MaxValue, BinarySearch.FindFloor(values, int.MaxValue, -1));
		AreEqual(1000000, BinarySearch.FindFloor(values, int.MaxValue - 1000, -1));

		// Ceil
		AreEqual(int.MinValue, BinarySearch.FindCeil(values, int.MinValue, -1));
		AreEqual(-1000000, BinarySearch.FindCeil(values, int.MinValue + 1, -1));
		AreEqual(int.MaxValue, BinarySearch.FindCeil(values, int.MaxValue - 1000, -1));
		AreEqual(int.MaxValue, BinarySearch.FindCeil(values, int.MaxValue, -1));
	}

	[TestMethod]
	public void FloorIndexAndCeilIndexAtInt32Boundaries()
	{
		int[] values = [int.MinValue, -1000000, 0, 1000000, int.MaxValue];

		// FloorIndex
		AreEqual(0, BinarySearch.FindFloorIndex(values, int.MinValue));
		AreEqual(0, BinarySearch.FindFloorIndex(values, int.MinValue + 500000));
		AreEqual(4, BinarySearch.FindFloorIndex(values, int.MaxValue));

		// CeilIndex
		AreEqual(0, BinarySearch.FindCeilIndex(values, int.MinValue));
		AreEqual(1, BinarySearch.FindCeilIndex(values, int.MinValue + 1));
		AreEqual(4, BinarySearch.FindCeilIndex(values, int.MaxValue - 1));
		AreEqual(4, BinarySearch.FindCeilIndex(values, int.MaxValue));
	}

	[TestMethod]
	public void FloorIndexNegativeNumbers()
	{
		int[] values = [-50, -10, 0, 15, 30];

		AreEqual(-1, BinarySearch.FindFloorIndex(values, -60));
		AreEqual(-1, BinarySearch.FindFloorIndex(values, -51));
		AreEqual(0, BinarySearch.FindFloorIndex(values, -20));
		AreEqual(1, BinarySearch.FindFloorIndex(values, -5));
		AreEqual(4, BinarySearch.FindFloorIndex(values, 100));
	}

	[TestMethod]
	public void FloorNegativeNumbers()
	{
		int[] values = [-50, -10, 0, 15, 30];

		AreEqual(-999, BinarySearch.FindFloor(values, -60, -999));
		AreEqual(-999, BinarySearch.FindFloor(values, -51, -999));
		AreEqual(-50, BinarySearch.FindFloor(values, -20, -999));
		AreEqual(-10, BinarySearch.FindFloor(values, -5, -999));
		AreEqual(30, BinarySearch.FindFloor(values, 100, -999));
	}

	[TestMethod]
	public void FloorWithSingleValue()
	{
		int[] buffer = [10];

		AreEqual(-1, BinarySearch.FindFloor(buffer, 9, -1));
		AreEqual(10, BinarySearch.FindFloor(buffer, 10, -1));
		AreEqual(10, BinarySearch.FindFloor(buffer, 11, -1));

		AreEqual(-1, BinarySearch.FindFloorIndex(buffer, 9));
		AreEqual(0, BinarySearch.FindFloorIndex(buffer, 10));
		AreEqual(0, BinarySearch.FindFloorIndex(buffer, 11));
	}

	[TestMethod]
	public void SingleElementEdgeCases()
	{
		int[] single = [42];

		// Floor
		AreEqual(42, BinarySearch.FindFloor(single, 42, -1));
		AreEqual(42, BinarySearch.FindFloor(single, 50, -1));
		AreEqual(-1, BinarySearch.FindFloor(single, 41, -1));

		// Ceil
		AreEqual(42, BinarySearch.FindCeil(single, 42, -1));
		AreEqual(42, BinarySearch.FindCeil(single, 30, -1));
		AreEqual(-1, BinarySearch.FindCeil(single, 100, -1));

		// Indices
		AreEqual(0, BinarySearch.FindFloorIndex(single, 42));
		AreEqual(0, BinarySearch.FindFloorIndex(single, 100));
		AreEqual(-1, BinarySearch.FindFloorIndex(single, 30));

		AreEqual(0, BinarySearch.FindCeilIndex(single, 42));
		AreEqual(0, BinarySearch.FindCeilIndex(single, 10));
		AreEqual(-1, BinarySearch.FindCeilIndex(single, 100));
	}

	[TestMethod]
	public void WorksWithList()
	{
		var list = new List<int> { 10, 20, 20, 35, 42 };

		AreEqual(20, BinarySearch.FindFloor(list, 20, -1));
		AreEqual(35, BinarySearch.FindCeil(list, 21, -1));
		AreEqual(2, BinarySearch.FindFloorIndex(list, 20));
		AreEqual(1, BinarySearch.FindCeilIndex(list, 20));
	}

	#endregion
}