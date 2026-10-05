#region References

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
public class SelectorTestsChild
{
	#region Methods

	[PresentationTestMethod]
	public void ChildDoesntMatchControlWhenItHasNoParent()
	{
		var control = new TestLogical3();
		var selector = default(Selector).OfType<TestLogical1>().Child().OfType<TestLogical3>();

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisInstance, selector.Match(control).Result);
	}

	[PresentationTestMethod]
	public void ChildDoesntMatchControlWhenItIsGrandchildOfType()
	{
		var grandparent = new TestLogical1();
		var parent = new TestLogical2();
		var child = new TestLogical3();

		parent.LogicalParent = grandparent;
		child.LogicalParent = parent;

		var selector = default(Selector).OfType<TestLogical1>().Child().OfType<TestLogical3>();

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisInstance, selector.Match(child).Result);
	}

	[PresentationTestMethod]
	public void ChildMatchesControlWhenItIsChildOfType()
	{
		var parent = new TestLogical1();
		var child = new TestLogical2();

		child.LogicalParent = parent;

		var selector = default(Selector).OfType<TestLogical1>().Child().OfType<TestLogical2>();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisInstance, selector.Match(child).Result);
	}

	[PresentationTestMethod]
	public async Task ChildMatchesControlWhenItIsChildOfTypeAndClass()
	{
		var parent = new TestLogical1();
		var child = new TestLogical2();

		child.LogicalParent = parent;

		var selector = default(Selector).OfType<TestLogical1>().Class("foo").Child().OfType<TestLogical2>();
		var activator = selector.Match(child).Activator;

		CornerstoneTest.IsNotNull(activator);
		CornerstoneTest.IsFalse(await activator.Take(1));
		parent.Classes.Add("foo");
		CornerstoneTest.IsTrue(await activator.Take(1));
		parent.Classes.Remove("foo");
		CornerstoneTest.IsFalse(await activator.Take(1));
	}

	[PresentationTestMethod]
	public void ChildSelectorShouldHaveCorrectStringRepresentation()
	{
		var selector = default(Selector).OfType<TestLogical1>().Child().OfType<TestLogical3>();

		CornerstoneTest.AreEqual("TestLogical1 > TestLogical3", selector.ToString());
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