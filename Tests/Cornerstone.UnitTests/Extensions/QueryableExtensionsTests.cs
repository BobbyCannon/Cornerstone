#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QueryableExtensions = Cornerstone.Extensions.QueryableExtensions;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class QueryableExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetInsertIndexReturnsCorrectPositionForMultipleOrderBys()
	{
		var list = new List<Person>
		{
			new() { Id = 1, Name = "Alice", Age = 30 },
			new() { Id = 2, Name = "Alice", Age = 25 },
			new() { Id = 3, Name = "Bob", Age = 40 }
		};

		var orderByName = new OrderBy<Person>(x => x.Name);
		var orderByAge = new OrderBy<Person>(x => x.Age);
		var newItem = new Person { Name = "Alice", Age = 28 };
		var index = QueryableExtensions.GetInsertIndex(list, newItem, orderByName, orderByAge);

		AreEqual(2, index); // After Alice(30) but before Bob
	}

	[TestMethod]
	public void GetInsertIndexReturnsCorrectPositionForSingleOrderByAscending()
	{
		var list = new List<Person>
		{
			new() { Id = 1, Name = "Alice", Age = 30 },
			new() { Id = 3, Name = "Charlie", Age = 35 },
			new() { Id = 5, Name = "Eve", Age = 28 }
		};

		var orderByName = new OrderBy<Person>(x => x.Name);
		var item = new Person { Name = "Bob" };
		var index = QueryableExtensions.GetInsertIndex(list, item, orderByName);
		AreEqual(1, index); // Should insert between Alice and Charlie
	}

	[TestMethod]
	public void GetInsertIndexReturnsCorrectPositionForSingleOrderByDescending()
	{
		var list = new List<Person>
		{
			new() { Name = "Eve", Age = 28 },
			new() { Name = "Charlie", Age = 35 },
			new() { Name = "Alice", Age = 30 }
		};

		var orderByNameDesc = new OrderBy<Person>(x => x.Name, true);
		var item = new Person { Name = "Bob" };
		var index = QueryableExtensions.GetInsertIndex(list, item, orderByNameDesc);
		AreEqual(2, index); // Bob comes after Charlie in descending order
	}

	[TestMethod]
	public void GetInsertIndexReturnsListCountWhenItemShouldBeAppended()
	{
		var list = new List<Person>
		{
			new() { Name = "Alice" },
			new() { Name = "Bob" }
		};

		var orderByName = new OrderBy<Person>(x => x.Name);
		var newItem = new Person { Name = "Charlie" };
		var index = QueryableExtensions.GetInsertIndex(list, newItem, orderByName);
		AreEqual(2, index);
	}

	[TestMethod]
	public void GetInsertIndexThrowsArgumentExceptionWhenNoOrderByProvided()
	{
		var list = new List<Person>();
		var item = new Person();
		ExpectedException<ArgumentException>(() => QueryableExtensions.GetInsertIndex(list, item));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
	public void GetInsertIndexThrowsArgumentNullExceptionWhenListIsNull()
	{
		IList<Person> list = null!;
		var orderBy = new OrderBy<Person>(x => x.Name);
		ExpectedException<ArgumentNullException>(() => QueryableExtensions.GetInsertIndex(list, new Person(), orderBy));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "UseCollectionExpression")]
	public void OrderOnEnumerableAppliesMultipleOrderBys()
	{
		var source = new List<Person>
		{
			new() { Name = "Alice", Age = 30 },
			new() { Name = "Bob", Age = 25 },
			new() { Name = "Alice", Age = 25 }
		};

		var orderByName = new OrderBy<Person>(x => x.Name);
		var orderByAge = new OrderBy<Person>(x => x.Age);
		var result = QueryableExtensions.Order(source, new[] { orderByName, orderByAge }).ToList();
		AreEqual("Alice", result[0].Name);
		AreEqual(25, result[0].Age);
		AreEqual("Alice", result[1].Name);
		AreEqual(30, result[1].Age);
		AreEqual("Bob", result[2].Name);
	}

	[TestMethod]
	public void OrderOnEnumerableAppliesSingleOrderByAscending()
	{
		var source = new List<Person>
		{
			new() { Name = "Charlie" },
			new() { Name = "Alice" },
			new() { Name = "Bob" }
		};

		var orderByName = new OrderBy<Person>(x => x.Name);
		var result = QueryableExtensions.Order(source, [orderByName]).ToList();
		AreEqual("Alice", result[0].Name);
		AreEqual("Bob", result[1].Name);
		AreEqual("Charlie", result[2].Name);
	}

	[TestMethod]
	public void OrderOnEnumerableReturnsOriginalWhenNoOrderByProvided()
	{
		var source = new List<int> { 5, 3, 8, 1 };
		var result = source.Order(null!);
		AreEqual(4, result.Count());

		// Order is not guaranteed to be changed when no OrderBy is supplied
	}

	[TestMethod]
	public void OrderOnQueryableAppliesDescendingOrderBy()
	{
		var query = new List<Person>
		{
			new() { Age = 30 },
			new() { Age = 25 }
		}.AsQueryable();

		var orderByAgeDesc = new OrderBy<Person>(x => x.Age, true);
		var result = QueryableExtensions.Order(query, [orderByAgeDesc]).ToList();
		AreEqual(30, result[0].Age);
		AreEqual(25, result[1].Age);
	}

	[TestMethod]
	public void OrderOnQueryableAppliesSingleOrderBy()
	{
		var query = new List<Person>
		{
			new() { Name = "Charlie" },
			new() { Name = "Alice" }
		}.AsQueryable();

		var orderByName = new OrderBy<Person>(x => x.Name);
		var result = QueryableExtensions.Order(query, [orderByName]).ToList();
		AreEqual("Alice", result[0].Name);
		AreEqual("Charlie", result[1].Name);
	}

	[TestMethod]
	public void OrderOnQueryableThrowsWhenNoOrderByProvided()
	{
		var query = new List<Person>().AsQueryable();
		ExpectedException<ArgumentNullException>(() => QueryableExtensions.Order(query, null));
		ExpectedException<InvalidOperationException>(() => QueryableExtensions.Order(query, []));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
	public void OrderOnQueryableThrowsWhenOrderBysIsNullOrEmpty()
	{
		var persons = new List<Person>().AsEnumerable();
		ExpectedException<ArgumentNullException>(() => QueryableExtensions.Order(persons, null));
		ExpectedException<InvalidOperationException>(() => QueryableExtensions.Order(persons, []));

		var query = new List<Person>().AsQueryable();
		ExpectedException<ArgumentNullException>(() => QueryableExtensions.Order(query, null));
		ExpectedException<InvalidOperationException>(() => QueryableExtensions.Order(query, []));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
	public void OrderOnQueryableThrowsWhenQueryIsNull()
	{
		IQueryable<Person> query = null!;
		var orderBy = new OrderBy<Person>(x => x.Name);
		ExpectedException<ArgumentNullException>(() => QueryableExtensions.Order(query, [orderBy]));
	}

	#endregion

	#region Classes

	private class Person
	{
		#region Properties

		public int Age { get; set; }
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;

		#endregion
	}

	#endregion
}