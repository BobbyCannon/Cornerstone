#region References

using System;
using System.Collections.Generic;
using System.Reflection;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

[TestClass]
public class FormattingBufferHelperTests
{
	#region Properties

	public static TestRows<int> LargeSizes => new() { 500_000, 1_000_000 };
	public static TestRows<int> SmallSizes => new() { 1, 500, 10_000, 125_000 };

	#endregion

	#region Methods

	[PresentationTestMethod]
	[TestData(nameof(SmallSizes))]
	public void ShouldKeepSmallBufferArrayBuilder(int itemCount)
	{
		var capacity = FillAndClearArrayBuilder(itemCount);

		CornerstoneTest.IsTrue(capacity >= itemCount);
	}

	[PresentationTestMethod]
	[TestData(nameof(SmallSizes))]
	public void ShouldKeepSmallBufferDictionary(int itemCount)
	{
		var capacity = FillAndClearDictionary(itemCount);

		CornerstoneTest.IsTrue(capacity >= itemCount);
	}

	[PresentationTestMethod]
	[TestData(nameof(SmallSizes))]
	public void ShouldKeepSmallBufferList(int itemCount)
	{
		var capacity = FillAndClearList(itemCount);

		CornerstoneTest.IsTrue(capacity >= itemCount);
	}

	[PresentationTestMethod]
	[TestData(nameof(SmallSizes))]
	public void ShouldKeepSmallBufferStack(int itemCount)
	{
		var capacity = FillAndClearStack(itemCount);

		CornerstoneTest.IsTrue(capacity >= itemCount);
	}

	[PresentationTestMethod]
	[TestData(nameof(LargeSizes))]
	public void ShouldResetLargeBufferArrayBuilder(int itemCount)
	{
		var capacity = FillAndClearArrayBuilder(itemCount);

		CornerstoneTest.AreEqual(0, capacity);
	}

	[PresentationTestMethod]
	[TestData(nameof(LargeSizes))]
	public void ShouldResetLargeBufferDictionary(int itemCount)
	{
		var capacity = FillAndClearDictionary(itemCount);

		CornerstoneTest.IsTrue(capacity <= 3); // dictionary trims to the nearest prime starting with 3
	}

	[PresentationTestMethod]
	[TestData(nameof(LargeSizes))]
	public void ShouldResetLargeBufferList(int itemCount)
	{
		var capacity = FillAndClearList(itemCount);

		CornerstoneTest.AreEqual(0, capacity);
	}

	[PresentationTestMethod]
	[TestData(nameof(LargeSizes))]
	public void ShouldResetLargeBufferStack(int itemCount)
	{
		var capacity = FillAndClearStack(itemCount);

		CornerstoneTest.AreEqual(0, capacity);
	}

	private static int FillAndClearArrayBuilder(int itemCount)
	{
		var arrayBuilder = new ArrayBuilder<int>();

		for (var i = 0; i < itemCount; ++i)
		{
			arrayBuilder.AddItem(i);
		}

		FormattingBufferHelper.ClearThenResetIfTooLarge(ref arrayBuilder);

		return arrayBuilder.Capacity;
	}

	private static int FillAndClearDictionary(int itemCount)
	{
		var dictionary = new Dictionary<int, int>();

		for (var i = 0; i < itemCount; ++i)
		{
			dictionary.Add(i, i);
		}

		FormattingBufferHelper.ClearThenResetIfTooLarge(ref dictionary);

		var array = (Array) dictionary.GetType()
			.GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(dictionary)!;

		return array.Length;
	}

	private static int FillAndClearList(int itemCount)
	{
		var list = new List<int>();

		for (var i = 0; i < itemCount; ++i)
		{
			list.Add(i);
		}

		FormattingBufferHelper.ClearThenResetIfTooLarge(list);

		return list.Capacity;
	}

	private static int FillAndClearStack(int itemCount)
	{
		var stack = new Stack<int>();

		for (var i = 0; i < itemCount; ++i)
		{
			stack.Push(i);
		}

		FormattingBufferHelper.ClearThenResetIfTooLarge(stack);

		var array = (Array) stack.GetType()
			.GetField("_array", BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(stack)!;

		return array.Length;
	}

	#endregion
}