﻿#region References

using System.Linq;
using Cornerstone.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Client;

#pragma warning disable CA1861

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
public class FilteredSpeedyListTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FilterCollectionShouldFilter()
	{
		var completeList = new SpeedyList<int>(null, new OrderBy<int>(x => x));
		var filteredList = new FilteredSpeedyList<int>(completeList, x => (x % 3) == 0);

		completeList.Add(5);
		completeList.Add(1);
		completeList.Add(4);
		completeList.Add(2);
		completeList.Add(3);

		completeList.Insert(1, 7);
		completeList.Insert(4, 6);
		completeList.Insert(3, 9);
		completeList.Insert(7, 8);

		// We should have only been alerted with anything divisible by 3
		AreEqual(3, filteredList.Count);
		AreEqual(new[] { 3, 6, 9 }, filteredList.ToArray());

		completeList.Remove(3);

		// Filter collection should have changed
		AreEqual(2, filteredList.Count);
		AreEqual(new[] { 6, 9 }, filteredList.ToArray());

		completeList.Remove(1);
		completeList.Remove(2);
		completeList.Remove(7);
		completeList.Remove(8);

		// Nothing should have changed
		AreEqual(2, filteredList.Count);
		AreEqual(new[] { 6, 9 }, filteredList.ToArray());

		completeList.Add(3);

		// 3 should have been re-added
		AreEqual(3, filteredList.Count);
		AreEqual(new[] { 3, 6, 9 }, filteredList.ToArray());

		completeList.Clear();

		AreEqual(0, completeList.Count);
		AreEqual(0, filteredList.Count);
	}

	[TestMethod]
	public void FilterCollectionShouldFilterAndOrder()
	{
		var completeList = new SpeedyList<ClientAccount>(null, new OrderBy<ClientAccount>(x => x.Name));
		var filteredList = new FilteredSpeedyList<ClientAccount>(completeList, x => !x.IsDeleted);

		var a1 = completeList.Add(new ClientAccount { Name = "John", IsDeleted = false });
		var a2 = completeList.Add(new ClientAccount { Name = "Jack", IsDeleted = false });
		var a3 = completeList.Add(new ClientAccount { Name = "Adam", IsDeleted = false });

		AreEqual(3, filteredList.Count);
		AreEqual(new[] { "Adam", "Jack", "John" },
			filteredList.Select(x => x.Name).ToArray()
		);

		a3.IsDeleted = true;
		filteredList.RefreshFilter();

		AreEqual(2, filteredList.Count);
		AreEqual(new[] { "Jack", "John" },
			filteredList.Select(x => x.Name).ToArray()
		);

		a3.IsDeleted = false;
		a3.Name = "Zack";
		filteredList.RefreshFilter();

		AreEqual(3, filteredList.Count);
		AreEqual(new[] { "Jack", "John", "Zack" },
			filteredList.Select(x => x.Name).ToArray()
		);

		completeList.Clear();

		AreEqual(0, completeList.Count);
		AreEqual(0, filteredList.Count);
	}

	#endregion
}