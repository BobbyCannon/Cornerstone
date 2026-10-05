#region References

using System.Collections.Generic;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Utils;

[TestClass]
public class SafeEnumerableHashSetTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void SetIsCopiedDuringNestedEnumerations()
	{
		var target = new SafeEnumerableHashSet<string>();
		var initialInner = target.Inner;
		var firstItems = new HashSet<string>();
		var secondItems = new HashSet<string>();
		HashSet<string> firstInner;
		HashSet<string> secondInner;

		target.Add("foo");

		foreach (var i in target)
		{
			target.Add("bar");

			firstInner = target.Inner;
			CornerstoneTest.NotSame(initialInner, firstInner);

			foreach (var j in target)
			{
				target.Add("baz");

				secondInner = target.Inner;
				CornerstoneTest.NotSame(firstInner, secondInner);

				secondItems.Add(j);
			}

			firstItems.Add(i);
		}

		CornerstoneTest.AreEqual(new HashSet<string> { "foo" }, firstItems);
		CornerstoneTest.AreEqual(new HashSet<string> { "foo", "bar" }, secondItems);
		CornerstoneTest.AreEqual(new HashSet<string> { "foo", "bar", "baz", "baz" }, target);

		var finalInner = target.Inner;
		target.Add("final");
		CornerstoneTest.Same(finalInner, target.Inner);
	}

	[PresentationTestMethod]
	public void SetIsCopiedOnlyOnceDuringEnumeration()
	{
		var target = new SafeEnumerableHashSet<string>();
		var inner = target.Inner;

		target.Add("foo");

		foreach (var i in target)
		{
			target.Add("bar");
			CornerstoneTest.NotSame(inner, target.Inner);
			inner = target.Inner;
			target.Add("baz");
			CornerstoneTest.Same(inner, target.Inner);
		}

		target.Add("baz");
	}

	[PresentationTestMethod]
	public void SetIsCopiedOutsideEnumeration()
	{
		var target = new SafeEnumerableHashSet<string>();
		var inner = target.Inner;

		target.Add("foo");

		foreach (var i in target)
		{
			CornerstoneTest.Same(inner, target.Inner);
			target.Add("bar");
			CornerstoneTest.NotSame(inner, target.Inner);
			CornerstoneTest.AreEqual("foo", i);
		}

		inner = target.Inner;

		foreach (var i in target)
		{
			target.Add("baz");
			CornerstoneTest.NotSame(inner, target.Inner);
		}

		CornerstoneTest.AreEqual(new HashSet<string> { "foo", "bar", "baz", "baz" }, target);
	}

	[PresentationTestMethod]
	public void SetIsNotCopiedAfterEnumeration()
	{
		var target = new SafeEnumerableHashSet<string>();
		var inner = target.Inner;

		target.Add("foo");

		foreach (var i in target)
		{
			target.Add("bar");
			CornerstoneTest.NotSame(inner, target.Inner);
			inner = target.Inner;
			CornerstoneTest.AreEqual("foo", i);
		}

		target.Add("baz");
		CornerstoneTest.Same(inner, target.Inner);
	}

	[PresentationTestMethod]
	public void SetIsNotCopiedOutsideEnumeration()
	{
		var target = new SafeEnumerableHashSet<string>();
		var inner = target.Inner;

		target.Add("foo");
		target.Add("bar");
		target.Remove("foo");

		CornerstoneTest.Same(inner, target.Inner);
	}

	#endregion
}