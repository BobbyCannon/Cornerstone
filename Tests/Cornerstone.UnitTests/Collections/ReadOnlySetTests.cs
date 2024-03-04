#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
[SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
public class ReadOnlySetTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Constructors()
	{
		var actual = new ReadOnlySet<int>(1, 2, 3);
		IsTrue(actual.IsReadOnly);
		IsNotNull(actual);
		AreEqual(3, actual.Count);

		var actual2 = new ReadOnlySet<int>(new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, new[] { 7, 8, 9 });
		var expected2 = new ReadOnlySet<int>(1, 2, 3, 4, 5, 6, 7, 8, 9);
		AreEqual(expected2, actual2);
	}

	[TestMethod]
	public void Empty()
	{
		var expected = new ReadOnlySet<int>();
		AreEqual(expected, ReadOnlySet<int>.Empty);
	}

	[TestMethod]
	public void NotSupportedMembers()
	{
		var actual = new ReadOnlySet<int>(1, 2, 3);
		IsTrue(actual.IsReadOnly);
		IsNotNull(actual);

		ExpectedException<NotSupportedException>(actual.Clear);
		ExpectedException<NotSupportedException>(() => ((ICollection<int>) actual).Add(1));
		ExpectedException<NotSupportedException>(() => ((ISet<int>) actual).Add(1));
		ExpectedException<NotSupportedException>(() => actual.Remove(1));
		ExpectedException<NotSupportedException>(() => actual.ExceptWith(new[] { 2, 3, 4 }));
		ExpectedException<NotSupportedException>(() => actual.IntersectWith(new[] { 2, 3, 4 }));
		ExpectedException<NotSupportedException>(() => actual.SymmetricExceptWith(new[] { 2, 3, 4 }));
		ExpectedException<NotSupportedException>(() => actual.UnionWith(new[] { 2, 3, 4 }));
	}

	[TestMethod]
	public void Overlaps()
	{
		var actual = new ReadOnlySet<int>(1, 2, 3);
		var overlap = new ReadOnlySet<int>(2);
		var nonOverlap = new ReadOnlySet<int>(4);
		IsTrue(overlap.Overlaps(actual));
		IsTrue(actual.Overlaps(overlap));
		IsFalse(nonOverlap.Overlaps(actual));
		IsFalse(actual.Overlaps(nonOverlap));
	}

	[TestMethod]
	public void SetEquals()
	{
		var actual = new ReadOnlySet<int>(1, 2, 3);
		var overlap = new ReadOnlySet<int>(1, 2, 3);
		IsTrue(actual.SetEquals(overlap));
		IsTrue(overlap.SetEquals(actual));
	}

	[TestMethod]
	public void Subset()
	{
		var actual = new ReadOnlySet<int>(1, 2, 3);
		var other = new ReadOnlySet<int>(2, 3);
		IsTrue(other.IsSubsetOf(actual));
		IsFalse(actual.IsSubsetOf(other));
		IsTrue(other.IsProperSubsetOf(actual));
		IsFalse(actual.IsProperSubsetOf(other));
	}

	[TestMethod]
	public void Superset()
	{
		var actual = new ReadOnlySet<int>(1, 2, 3);
		var other = new ReadOnlySet<int>(2, 3);
		IsFalse(other.IsSupersetOf(actual));
		IsTrue(actual.IsSupersetOf(other));
		IsFalse(other.IsProperSupersetOf(actual));
		IsTrue(actual.IsProperSupersetOf(other));
	}

	#endregion
}