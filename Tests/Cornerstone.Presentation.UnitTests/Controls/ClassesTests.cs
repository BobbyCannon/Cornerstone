#region References

using System;
using Cornerstone.Presentation.Controls.StyleClasses;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ClassesTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClearShouldNotRemovePseudoclasses()
	{
		var target = new Classes("foo", "bar");

		((IPseudoClasses) target).Add(":baz");

		target.Clear();

		CornerstoneTest.AreEqual(new[] { ":baz" }, target);
	}

	[PresentationTestMethod]
	public void DuplicatesShouldNotBeAdded()
	{
		var target = new Classes();

		target.Add("foo");
		target.Add("foo");

		CornerstoneTest.AreEqual(new[] { "foo" }, target);
	}

	[PresentationTestMethod]
	public void DuplicatesShouldNotBeAddedViaAddRange()
	{
		var target = new Classes();

		target.Add("foo");
		target.AddRange(new[] { "foo", "bar" });

		CornerstoneTest.AreEqual(new[] { "foo", "bar" }, target);
	}

	[PresentationTestMethod]
	public void DuplicatesShouldNotBeAddedViaPseudoclasses()
	{
		var target = new Classes();
		var ps = (IPseudoClasses) target;

		ps.Add(":foo");
		ps.Add(":foo");

		CornerstoneTest.AreEqual(new[] { ":foo" }, target);
	}

	[PresentationTestMethod]
	public void DuplicatesShouldNotBeInserted()
	{
		var target = new Classes();

		target.Add("foo");
		target.Insert(0, "foo");

		CornerstoneTest.AreEqual(new[] { "foo" }, target);
	}

	[PresentationTestMethod]
	public void DuplicatesShouldNotBeInsertedViaInsertRange()
	{
		var target = new Classes();

		target.Add("foo");
		target.InsertRange(1, new[] { "foo", "bar" });

		CornerstoneTest.AreEqual(new[] { "foo", "bar" }, target);
	}

	[PresentationTestMethod]
	public void ListenersCanBeAddedByListener()
	{
		var classes = new Classes();
		var listener1 = new ClassesChangedListener(() => { });
		var listener2 = new ClassesChangedListener(() => classes.AddListener(listener1));

		classes.AddListener(listener2);
		classes.Add("bar");
	}

	[PresentationTestMethod]
	public void ListenersCanBeRemovedByListener()
	{
		var classes = new Classes();
		var listener1 = new ClassesChangedListener(() => { });
		var listener2 = new ClassesChangedListener(() => classes.RemoveListener(listener1));

		classes.AddListener(listener1);
		classes.AddListener(listener2);
		classes.Add("bar");
	}

	[PresentationTestMethod]
	public void RemoveAllShouldRemoveClasses()
	{
		var target = new Classes("foo", "bar", "baz");

		target.RemoveAll(new[] { "bar", "baz" });

		CornerstoneTest.AreEqual(new[] { "foo" }, target);
	}

	[PresentationTestMethod]
	public void ReplaceShouldNotAcceptPseudoclasses()
	{
		var target = new Classes();

		Assert.Throws<ArgumentException>(() => target.Replace(new[] { ":qux" }));
	}

	[PresentationTestMethod]
	public void ReplaceShouldNotReplacePseudoclasses()
	{
		var target = new Classes("foo", "bar");

		((IPseudoClasses) target).Add(":baz");

		target.Replace(new[] { "qux" });

		CornerstoneTest.AreEqual(new[] { ":baz", "qux" }, target);
	}

	[PresentationTestMethod]
	public void ShouldNotBeAbleToAddPseudoclass()
	{
		var target = new Classes();

		Assert.Throws<ArgumentException>(() => target.Add(":foo"));
	}

	[PresentationTestMethod]
	public void ShouldNotBeAbleToAddPseudoclassesViaAddRange()
	{
		var target = new Classes();

		Assert.Throws<ArgumentException>(() => target.AddRange(new[] { "foo", ":bar" }));
	}

	[PresentationTestMethod]
	public void ShouldNotBeAbleToInsertPseudoclass()
	{
		var target = new Classes();

		Assert.Throws<ArgumentException>(() => target.Insert(0, ":foo"));
	}

	[PresentationTestMethod]
	public void ShouldNotBeAbleToInsertPseudoclassesViaInsertRange()
	{
		var target = new Classes();

		Assert.Throws<ArgumentException>(() => target.InsertRange(0, new[] { "foo", ":bar" }));
	}

	[PresentationTestMethod]
	public void ShouldNotBeAbleToRemovePseudoclass()
	{
		var target = new Classes();

		Assert.Throws<ArgumentException>(() => target.Remove(":foo"));
	}

	[PresentationTestMethod]
	public void ShouldNotBeAbleToRemovePseudoclassViaRemoveAt()
	{
		var target = new Classes();

		((IPseudoClasses) target).Add(":foo");

		Assert.Throws<ArgumentException>(() => target.RemoveAt(0));
	}

	[PresentationTestMethod]
	public void ShouldNotBeAbleToRemovePseudoclassesViaRemoveAll()
	{
		var target = new Classes();

		Assert.Throws<ArgumentException>(() => target.RemoveAll(new[] { "foo", ":bar" }));
	}

	[PresentationTestMethod]
	public void ShouldNotBeAbleToRemovePseudoclassesViaRemoveRange()
	{
		var target = new Classes();

		Assert.Throws<ArgumentException>(() => target.RemoveRange(0, 1));
	}

	#endregion

	#region Classes

	private class ClassesChangedListener : IClassesChangedListener
	{
		#region Fields

		private readonly Action _action;

		#endregion

		#region Constructors

		public ClassesChangedListener(Action action)
		{
			_action = action;
		}

		#endregion

		#region Methods

		public void Changed()
		{
			_action();
		}

		#endregion
	}

	#endregion
}