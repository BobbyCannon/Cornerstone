#region References

using System;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class NameScopeTests : ScopedTestBase
{
	#region Fields

	/*
	`async void` here is intentional since we expect the continuation to be
	executed *synchronously* and behave more like an event handler to make sure that
	that the object graph is completely ready to use after it's built
	rather than have pending continuations queued by SynchronizationContext.
	*/
	private object _found;

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CanRegisterSameElementMoreThanOnce()
	{
		var target = new NameScope();
		var element = new object();

		target.Register("foo", element);
		target.Register("foo", element);

		CornerstoneTest.Same(element, target.Find("foo"));
	}

	[PresentationTestMethod]
	public void CannotRegisterNewElementForCompletedScope()
	{
		var target = new NameScope();
		var element = new object();

		target.Register("foo", element);
		target.Complete();
		Assert.Throws<InvalidOperationException>(() => target.Register("bar", element));
	}

	[PresentationTestMethod]
	public void CannotRegisterNewElementWithExistingName()
	{
		var target = new NameScope();

		target.Register("foo", new object());
		Assert.Throws<ArgumentException>(() => target.Register("foo", new object()));
	}

	[PresentationTestMethod]
	public void ChildScopeFindAsyncShouldFindElementsInParentScopeWhenChildIsCompleted()
	{
		var scope = new NameScope();
		var childScope = new ChildNameScope(scope);
		var element = new object();
		scope.Register("foo", element);
		FindAsync(childScope, "foo");
		CornerstoneTest.IsNull(_found);
		childScope.Complete();
		CornerstoneTest.Same(element, _found);
	}

	[PresentationTestMethod]
	public void ChildScopeFindAsyncShouldPreferOwnElements()
	{
		var scope = new NameScope();
		var childScope = new ChildNameScope(scope);
		var element = new object();
		var childElement = new object();
		FindAsync(childScope, "foo");
		scope.Register("foo", element);
		CornerstoneTest.IsNull(_found);
		childScope.Register("foo", childElement);
		CornerstoneTest.Same(childElement, childScope.Find("foo"));
		childScope.Complete();
		FindAsync(childScope, "foo");
		CornerstoneTest.Same(childElement, childScope.Find("foo"));
	}

	[PresentationTestMethod]
	public void ChildScopeShouldNotFindControlInParentScopeUnlessCompleted()
	{
		var scope = new NameScope();
		var childScope = new ChildNameScope(scope);
		var element = new object();
		scope.Register("foo", element);
		CornerstoneTest.IsNull(childScope.Find("foo"));
		childScope.Complete();
		CornerstoneTest.Same(element, childScope.Find("foo"));
	}

	[PresentationTestMethod]
	public void ChildScopeShouldPreferOwnElements()
	{
		var scope = new NameScope();
		var childScope = new ChildNameScope(scope);
		var element = new object();
		var childElement = new object();
		scope.Register("foo", element);
		childScope.Register("foo", childElement);
		childScope.Complete();
		CornerstoneTest.Same(childElement, childScope.Find("foo"));
	}

	[PresentationTestMethod]
	public void FindAsyncShouldFindControlsAddedEarlier()
	{
		var scope = new NameScope();
		var element = new object();
		scope.Register("foo", element);
		FindAsync(scope, "foo");
		CornerstoneTest.Same(_found, element);
	}

	[PresentationTestMethod]
	public void FindAsyncShouldFindControlsAddedLater()
	{
		var scope = new NameScope();
		var element = new object();

		FindAsync(scope, "foo");
		CornerstoneTest.IsNull(_found);
		scope.Register("foo", element);
		CornerstoneTest.Same(_found, element);
	}

	[PresentationTestMethod]
	public void FindAsyncShouldReturnNullAfterScopeCompletion()
	{
		var scope = new NameScope();
		var element = new object();
		var finished = false;

		async void Find(string name)
		{
			CornerstoneTest.IsNull(await scope.FindAsync(name));
			finished = true;
		}

		Find("foo");
		CornerstoneTest.IsFalse(finished);
		scope.Register("bar", element);
		CornerstoneTest.IsFalse(finished);
		scope.Complete();
		CornerstoneTest.IsTrue(finished);
	}

	[PresentationTestMethod]
	public void RegisterRegistersElement()
	{
		var target = new NameScope();
		var element = new object();

		target.Register("foo", element);

		CornerstoneTest.Same(element, target.Find("foo"));
	}

	private async void FindAsync(INameScope scope, string name)
	{
		_found = await scope.FindAsync(name);
	}

	#endregion
}