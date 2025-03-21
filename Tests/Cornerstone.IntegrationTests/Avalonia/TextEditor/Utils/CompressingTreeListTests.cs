#region References

using System;
using System.Linq;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitAssert = NUnit.Framework.Assert;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Utils;

[TestClass]
public class CompressingTreeListTests: CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddRepeated()
	{
		var list = new CompressingTreeList<int>((a, b) => a == b);
		list.Add(42);
		list.Add(42);
		list.Add(42);
		list.Insert(0, 42);
		list.Insert(1, 42);
		AreEqual(new[] { 42, 42, 42, 42, 42 }, list.ToArray());
	}

	[TestMethod]
	public void CheckAdd10BillionElements()
	{
		const int billion = 1000000000;
		var list = new CompressingTreeList<string>(string.Equals);
		list.InsertRange(0, billion, "A");
		list.InsertRange(1, billion, "B");
		AreEqual(2 * billion, list.Count);
		NUnitAssert.Throws<OverflowException>(delegate { list.InsertRange(2, billion, "C"); });
	}

	[TestMethod]
	public void EmptyTreeList()
	{
		var list = new CompressingTreeList<string>(string.Equals);
		AreEqual(0, list.Count);
		foreach (var v in list)
		{
			Assert.Fail();
		}
		var arr = new string[0];
		list.CopyTo(arr, 0);
	}

	[TestMethod]
	public void RemoveAtEnd()
	{
		var list = new CompressingTreeList<int>((a, b) => a == b);
		for (var i = 1; i <= 3; i++)
		{
			list.InsertRange(list.Count, 2, i);
		}
		AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
		list.RemoveRange(3, 3);
		AreEqual(new[] { 1, 1, 2 }, list.ToArray());
	}

	[TestMethod]
	public void RemoveAtStart()
	{
		var list = new CompressingTreeList<int>((a, b) => a == b);
		for (var i = 1; i <= 3; i++)
		{
			list.InsertRange(list.Count, 2, i);
		}
		AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
		list.RemoveRange(0, 1);
		AreEqual(new[] { 1, 2, 2, 3, 3 }, list.ToArray());
	}

	[TestMethod]
	public void RemoveAtStart2()
	{
		var list = new CompressingTreeList<int>((a, b) => a == b);
		for (var i = 1; i <= 3; i++)
		{
			list.InsertRange(list.Count, 2, i);
		}
		AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
		list.RemoveRange(0, 3);
		AreEqual(new[] { 2, 3, 3 }, list.ToArray());
	}

	[TestMethod]
	public void RemoveRange()
	{
		var list = new CompressingTreeList<int>((a, b) => a == b);
		for (var i = 1; i <= 3; i++)
		{
			list.InsertRange(list.Count, 2, i);
		}
		AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
		list.RemoveRange(1, 4);
		AreEqual(new[] { 1, 3 }, list.ToArray());
		list.Insert(1, 1);
		list.InsertRange(2, 2, 2);
		list.Insert(4, 1);
		AreEqual(new[] { 1, 1, 2, 2, 1, 3 }, list.ToArray());
		list.RemoveRange(2, 2);
		AreEqual(new[] { 1, 1, 1, 3 }, list.ToArray());
	}

	[TestMethod]
	public void Transform()
	{
		var list = new CompressingTreeList<int>((a, b) => a == b);
		list.AddRange([0, 1, 1, 0]);
		var calls = 0;
		list.Transform(i =>
		{
			calls++;
			return i + 1;
		});
		AreEqual(3, calls);
		AreEqual(new[] { 1, 2, 2, 1 }, list.ToArray());
	}

	[TestMethod]
	public void TransformRange()
	{
		var list = new CompressingTreeList<int>((a, b) => a == b);
		list.AddRange([0, 1, 1, 1, 0, 0]);
		list.TransformRange(2, 3, i => 0);
		AreEqual(new[] { 0, 1, 0, 0, 0, 0 }, list.ToArray());
	}

	[TestMethod]
	public void TransformToZero()
	{
		var list = new CompressingTreeList<int>((a, b) => a == b);
		list.AddRange([0, 1, 1, 0]);
		list.Transform(i => 0);
		AreEqual(new[] { 0, 0, 0, 0 }, list.ToArray());
	}

	#endregion
}