#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Testing;
using Cornerstone.Threading;
using Cornerstone.UnitTests.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Client;

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
public class OrderByTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DefaultOrderBy()
	{
		var list = new List<int> { 5, 3, 2, 4, 1 };
		var order = new OrderBy<int>();
		var actual = order.Process(list.AsQueryable());

		AreEqual(new[] { 1, 2, 3, 4, 5 }, actual.ToArray());
	}

	[TestMethod]
	public void DefaultOrderByForComparableObject()
	{
		var list = new List<SampleAccount>
		{
			new() { FirstName = "John" },
			new() { FirstName = "Jane" },
			new() { FirstName = "Bob" }
		};
		var order = new OrderBy<SampleAccount>();
		var actual = order.Process(list.AsQueryable());

		AreEqual(new[] { list[2], list[1], list[0] }, actual.ToArray());
	}

	[TestMethod]
	public void DefaultOrderByForString()
	{
		var list = new List<string> { "C", "b", "A" };
		var order = new OrderBy<string>();
		var actual = order.Process(list.AsQueryable());

		AreEqual(new[] { "A", "b", "C" }, actual.ToArray());
	}

	[TestMethod]
	public void DefaultOrderByForStringNumbers()
	{
		var list = new List<string> { "11", "2", "1" };
		var order = new OrderBy<string>();
		var actual = order.Process(list.AsQueryable());

		AreEqual(new[] { "1", "11", "2" }, actual.ToArray());
	}

	[TestMethod]
	public void GetInsertIndex()
	{
		var scenarios = new List<(OrderBy<ClientAccount>[] order, (string[] existing, string insert, int expected)[] results)>
		{
			(
				[
					new OrderBy<ClientAccount>(x => x.Name == "Zoe", true),
					new OrderBy<ClientAccount>(x => x.Name == "Zane", true),
					new OrderBy<ClientAccount>(x => x.Name)
				],
				[
					(["Zane"], "Zoe", 0),
					(["Zoe"], "Zane", 1),
					(["Zoe", "Zane"], "Bob", 2),
					(["Zoe", "Zane", "Jack"], "Chad", 2)
				]
			)
		};

		for (var index = 0; index < scenarios.Count; index++)
		{
			$"Scenario {index}".Dump();

			var scenario = scenarios[index];

			foreach (var t in scenario.results)
			{
				$"{t.insert}".Dump();

				var insert = new ClientAccount { Name = t.insert };
				var existing = t.existing.Select(x => new ClientAccount { Name = x }).ToList();
				var actual = OrderBy<ClientAccount>.GetInsertIndex(existing, insert, scenario.order);

				AreEqual(t.expected, actual);
			}
		}
	}

	[TestMethod]
	public void InvalidDefaultOrderBy()
	{
		ExpectedException<InvalidOperationException>(
			() => _ = new OrderBy<Lockable>(),
			"The type must implement IComparable to use this constructor."
		);
	}

	[TestMethod]
	public void OrderUsingManyOrderBy()
	{
		var scenarios = new List<(OrderBy<ClientAccount>[] order, (string[] unordered, string[] expected)[] results)>
		{
			(
				[
					new OrderBy<ClientAccount>(x => x.Name == "Zoe", true),
					new OrderBy<ClientAccount>(x => x.Name == "Zane", true),
					new OrderBy<ClientAccount>(x => x.Name)
				],
				[
					(["Zane", "Zoe"], ["Zoe", "Zane"]),
					(["Bob", "Zane", "Zoe"], ["Zoe", "Zane", "Bob"]),
					(["Bob", "Zane", "Joe", "Zoe"], ["Zoe", "Zane", "Bob", "Joe"])
				]
			)
		};

		for (var index = 0; index < scenarios.Count; index++)
		{
			$"Scenario {index}".Dump();

			var scenario = scenarios[index];

			foreach (var t in scenario.results)
			{
				var unordered = t.unordered.Select(x => new ClientAccount { Name = x }).ToList();
				var actual = OrderBy<ClientAccount>.OrderCollection(unordered, scenario.order);
				var expected = t.expected.Select(x => new ClientAccount { Name = x }).ToList();

				AreEqual(expected, actual);
			}
		}
	}

	#endregion
}