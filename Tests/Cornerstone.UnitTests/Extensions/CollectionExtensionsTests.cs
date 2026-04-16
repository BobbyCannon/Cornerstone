#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public partial class CollectionExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	[SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
	public void ReconcileDoesNothingWhenBothCollectionsAreEmpty()
	{
		var collection = new PresentationList<int>();
		var expected = new List<int>();
		collection.Reconcile(expected);
		AreEqual(0, collection.Count);
	}

	[TestMethod]
	public void ReconcileHandlesUpdateWithIncludeExcludeSettings()
	{
		// This test assumes Person implements IUpdateable or has properties that UpdateWith respects.
		// Adjust based on your actual UpdateWith behavior.
		var collection = new PresentationList<Person>
		{
			new() { Id = 1, Name = "OldName", Age = 30 }
		};

		var expected = new List<Person>
		{
			new() { Id = 1, Name = "NewName", Age = 40 }
		};

		collection.Reconcile(expected);

		AreEqual(1, collection.Count);
		AreEqual("NewName", collection[0].Name);

		// Age update depends on your IncludeExcludeSettings for UpdateableAction.Updateable
	}

	[TestMethod]
	public void ReconcileLoadsExpectedItemsWhenCollectionIsEmpty()
	{
		var collection = new PresentationList<string>();
		var expected = new List<string> { "Alpha", "Beta", "Gamma" };
		collection.Reconcile(expected);
		AreEqual(3, collection.Count);
		AreEqual("Alpha", collection[0]);
		AreEqual("Beta", collection[1]);
		AreEqual("Gamma", collection[2]);
	}

	[TestMethod]
	public void ReconcileRemovesExtraItemsAddsMissingItemsAndUpdatesExistingOnes()
	{
		var collection = new PresentationList<Person>
		{
			new() { Id = 1, Name = "Alice", Age = 30 },
			new() { Id = 2, Name = "Bob", Age = 25 },
			new() { Id = 3, Name = "Charlie", Age = 35 } // will be removed
		};

		var expected = new List<Person>
		{
			new() { Id = 1, Name = "Alice", Age = 31 }, // update
			new() { Id = 2, Name = "Bob", Age = 25 }, // unchanged
			new() { Id = 4, Name = "Diana", Age = 28 } // add
		};

		collection.Reconcile(expected);
		AreEqual(3, collection.Count);

		var alice = collection.First(x => x.Id == 1);
		AreEqual("Alice", alice.Name);
		AreEqual(31, alice.Age);

		var bob = collection.First(x => x.Id == 2);
		AreEqual("Bob", bob.Name);
		AreEqual(25, bob.Age);

		var diana = collection.First(x => x.Id == 4);
		AreEqual("Diana", diana.Name);
		AreEqual(28, diana.Age);

		// Charlie removed
		IsFalse(collection.Any(x => x.Id == 3));
		AreEqual(["Alice", "Bob", "Diana"], collection.Select(x => x.Name).ToArray());
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
	public void ReconcileThrowsArgumentNullExceptionWhenCollectionIsNull()
	{
		IPresentationList<string> collection = null;
		var expected = new List<string> { "item" };
		ExpectedException<ArgumentNullException>(() => collection.Reconcile(expected));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
	public void ReconcileThrowsArgumentNullExceptionWhenExpectedIsNull()
	{
		var collection = new PresentationList<string>();
		IEnumerable expected = null;
		ExpectedException<ArgumentNullException>(() => collection.Reconcile(expected));
	}

	[TestMethod]
	public void ReconcileUsesCustomDistinctCheckWhenProvided()
	{
		var collection = new PresentationList<Person>
		{
			new() { Id = 1, Name = "Alice" }
		};

		// Custom comparer that only looks at Id
		collection.DistinctCheck = EqualityComparer<Person>.Create((x, y) => x.Id == y.Id, x => x.Id.GetHashCode());

		var expected = new List<Person>
		{
			new() { Id = 1, Name = "Alicia" } // different name but same Id
		};

		collection.Reconcile(expected);

		AreEqual(1, collection.Count);
		AreEqual("Alicia", collection[0].Name); // name was updated
	}

	[TestMethod]
	public void ToSpeedyListAppliesOrderBySettings()
	{
		var source = new List<Person>
		{
			new() { Name = "Charlie", Age = 35 },
			new() { Name = "Alice", Age = 30 },
			new() { Name = "Bob", Age = 25 }
		};
		var orderByName = new OrderBy<Person>(x => x.Name);
		var result = source.ToSpeedyList(null, orderByName);
		AreEqual(3, result.Count);
		AreEqual("Alice", result[0].Name);
		AreEqual("Bob", result[1].Name);
		AreEqual("Charlie", result[2].Name);
	}

	[TestMethod]
	public void ToSpeedyListCreatesPresentationListWithItems()
	{
		var source = new List<string> { "One", "Two", "Three" };
		var result = source.ToSpeedyList();
		IsNotNull(result);
		AreEqual(3, result.Count);
		AreEqual("One", result[0]);
		AreEqual("Two", result[1]);
		AreEqual("Three", result[2]);
	}

	[TestMethod]
	public void ToSpeedyListRespectsProvidedDispatcher()
	{
		var dispatcher = new TestDispatcher(); // assume you have a test double or use a real one if available
		var source = new List<int> { 10, 20 };
		var result = source.ToSpeedyList(dispatcher);
		IsNotNull(result);
		AreEqual(2, result.Count);
	}

	#endregion

	#region Classes

	[Notifiable(["*"])]
	[Updateable(UpdateableAction.All, ["*"])]
	private partial class Person : Notifiable
	{
		#region Properties

		public partial int Age { get; set; }
		public partial int Id { get; set; }
		public partial string Name { get; set; }

		#endregion
	}

	#endregion
}