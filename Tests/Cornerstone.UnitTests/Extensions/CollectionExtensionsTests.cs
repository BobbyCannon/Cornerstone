﻿#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class CollectionExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddRange()
	{
		var hashset = new HashSet<int>();
		hashset.AddRange(1, 2, 3, 4);
		AreEqual(new[] { 1, 2, 3, 4 }, hashset);
	}

	[TestMethod]
	public void AsReadOnly()
	{
		var expected = new ReadOnlySet<int>(1, 2, 3, 4);
		var scenarios = new IEnumerable<int>[]
		{
			new[] { 1, 2, 3, 4 },
			new Collection<int> { 1, 2, 3, 4 },
			new List<int> { 1, 2, 3, 4 }
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.ToReadOnlySet();
			AreEqual(expected, actual);
		}
	}

	[TestMethod]
	public void Reconcile()
	{
		IList list = new List<object> { "1", 1, true, DateTime.MinValue};
		IEnumerable expected = new object[] { "2", 2, false };

		list.Reconcile(expected);

		AreEqual(3, list.Count);
		AreEqual("2,2,False", string.Join(",", list.ToObjectArray()));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	public void ToHashSet()
	{
		var scenarios = new IEnumerable<string>[]
		{
			new[] { "Apple", "apple", "Bob", "bob" },
			new HashSet<string>(new[] { "Apple", "apple", "Bob", "bob" }),
			new HashSet<string>(new[] { "Apple", "apple", "Bob", "bob" }, StringComparer.CurrentCulture),
			new HashSet<string>(new[] { "Apple", "apple", "Bob", "bob" }, StringComparer.Ordinal)
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.ToHashSet("Apple");
			AreEqual(new[] { "Apple", "apple", "Bob", "bob" }, actual);
			actual = scenario.ToHashSet("bOb");
			AreEqual(new[] { "Apple", "apple", "Bob", "bob", "bOb" }, actual);
			IsTrue(actual.Contains("bob"));
			IsFalse(actual.Contains("BOB"));
		}
	}

	[TestMethod]
	public void ToHashSetCaseSensitive()
	{
		var scenarios = new IEnumerable<string>[]
		{
			new HashSet<string>(new[] { "Apple", "apple", "Bob", "bob" }, StringComparer.CurrentCultureIgnoreCase),
			new HashSet<string>(new[] { "Apple", "apple", "Bob", "bob" }, StringComparer.InvariantCultureIgnoreCase),
			new HashSet<string>(new[] { "Apple", "apple", "Bob", "bob" }, StringComparer.OrdinalIgnoreCase)
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.ToHashSet("Apple");

			AreEqual(new[] { "Apple", "Bob" }, actual);
			IsTrue(actual.Contains("Apple"));
			IsTrue(actual.Contains("apple"));
			IsTrue(actual.Contains("APPLE"));
		}
	}

	#endregion
}