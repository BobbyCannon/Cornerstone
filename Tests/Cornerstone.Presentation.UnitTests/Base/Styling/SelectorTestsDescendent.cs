#region References

using System.Reactive.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsDescendant
{
	#region Methods

	[PresentationTestMethod]
	public async Task DescendantDoesntMatchControlWhenItIsDescendantOfTypeButWrongClass()
	{
		var grandparent = new TestLogical1();
		var parent = new TestLogical2();
		var child = new TestLogical3();

		grandparent.Classes.Add("bar");
		parent.LogicalParent = grandparent;
		parent.Classes.Add("foo");
		child.LogicalParent = parent;

		var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendant().OfType<TestLogical3>();
		var activator = selector.Match(child).Activator;

		CornerstoneTest.IsNotNull(activator);
		CornerstoneTest.IsFalse(await activator.Take(1));
	}

	[PresentationTestMethod]
	public async Task DescendantMatchesAnyAncestor()
	{
		var grandparent = new TestLogical1();
		var parent = new TestLogical1();
		var child = new TestLogical3();

		parent.LogicalParent = grandparent;
		child.LogicalParent = parent;

		var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendant().OfType<TestLogical3>();
		var activator = selector.Match(child).Activator;
		CornerstoneTest.IsNotNull(activator);
		var observable = activator.ToObservable();

		CornerstoneTest.IsFalse(await observable.Take(1));
		parent.Classes.Add("foo");
		CornerstoneTest.IsTrue(await observable.Take(1));
		grandparent.Classes.Add("foo");
		CornerstoneTest.IsTrue(await observable.Take(1));
		parent.Classes.Remove("foo");
		CornerstoneTest.IsTrue(await observable.Take(1));
		grandparent.Classes.Remove("foo");
		CornerstoneTest.IsFalse(await observable.Take(1));
	}

	[PresentationTestMethod]
	public void DescendantMatchesControlWhenItIsChildOfType()
	{
		var parent = new TestLogical1();
		var child = new TestLogical2();

		child.LogicalParent = parent;

		var selector = default(Selector).OfType<TestLogical1>().Descendant().OfType<TestLogical2>();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisInstance, selector.Match(child).Result);
	}

	[PresentationTestMethod]
	public void DescendantMatchesControlWhenItIsDescendantOfType()
	{
		var grandparent = new TestLogical1();
		var parent = new TestLogical2();
		var child = new TestLogical3();

		parent.LogicalParent = grandparent;
		child.LogicalParent = parent;

		var selector = default(Selector).OfType<TestLogical1>().Descendant().OfType<TestLogical3>();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisInstance, selector.Match(child).Result);
	}

	[PresentationTestMethod]
	public async Task DescendantMatchesControlWhenItIsDescendantOfTypeAndClass()
	{
		var grandparent = new TestLogical1();
		var parent = new TestLogical2();
		var child = new TestLogical3();

		grandparent.Classes.Add("foo");
		parent.LogicalParent = grandparent;
		child.LogicalParent = parent;

		var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendant().OfType<TestLogical3>();
		var activator = selector.Match(child).Activator;

		CornerstoneTest.IsNotNull(activator);
		CornerstoneTest.IsTrue(await activator.Take(1));
	}

	[PresentationTestMethod]
	public void DescendantSelectorShouldHaveCorrectStringRepresentation()
	{
		var selector = default(Selector).OfType<TestLogical1>().Class("foo").Descendant().OfType<TestLogical3>();

		CornerstoneTest.AreEqual("TestLogical1.foo TestLogical3", selector.ToString());
	}

	#endregion

	#region Classes

	public abstract class TestLogical : Control
	{
		#region Properties

		public ILogical LogicalParent
		{
			get => Parent;
			set => ((ISetLogicalParent) this).SetParent(value);
		}

		#endregion
	}

	public class TestLogical1 : TestLogical
	{
	}

	public class TestLogical2 : TestLogical
	{
	}

	public class TestLogical3 : TestLogical
	{
	}

	#endregion
}