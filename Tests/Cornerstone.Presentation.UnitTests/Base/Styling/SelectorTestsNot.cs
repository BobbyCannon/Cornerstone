#region References

using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsNot
{
	#region Methods

	[PresentationTestMethod]
	public async Task NotClassDoesntMatchControlWithClass()
	{
		var control = new Control1
		{
			Classes = { "foo" }
		};

		var target = default(Selector).Not(x => x.Class("foo"));
		var match = target.Match(control);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		CornerstoneTest.IsFalse(await match.Activator.Take(1));
	}

	[PresentationTestMethod]
	public async Task NotClassMatchesControlWithoutClass()
	{
		var control = new Control1
		{
			Classes = { "bar" }
		};

		var target = default(Selector).Not(x => x.Class("foo"));
		var match = target.Match(control);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		CornerstoneTest.IsTrue(await match.Activator.Take(1));
	}

	[PresentationTestMethod]
	public void NotOfTypeDoesntMatchControlOfCorrectType()
	{
		var control = new Control2();
		var target = default(Selector).Not(x => x.OfType<Control1>());

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void NotOfTypeMatchesControlOfIncorrectType()
	{
		var control = new Control1();
		var target = default(Selector).Not(x => x.OfType<Control1>());

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void NotSelectorShouldHaveCorrectStringRepresentation()
	{
		var target = default(Selector).Not(x => x.Class("foo"));

		CornerstoneTest.AreEqual(":not(.foo)", target.ToString());
	}

	[PresentationTestMethod]
	public void OfTypeNotClassDoesntMatchControlOfWrongType()
	{
		var control = new Control2
		{
			Classes = { "foo" }
		};

		var target = default(Selector).OfType<Control1>().Not(x => x.Class("foo"));
		var match = target.Match(control);

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, match.Result);
	}

	[PresentationTestMethod]
	public async Task OfTypeNotClassMatchesControlWithoutClass()
	{
		var control = new Control1
		{
			Classes = { "bar" }
		};

		var target = default(Selector).OfType<Control1>().Not(x => x.Class("foo"));
		var match = target.Match(control);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		CornerstoneTest.IsTrue(await match.Activator.Take(1));
	}

	[PresentationTestMethod]
	public void ReturnsCorrectTargetType()
	{
		var target = default(Selector).OfType<Control1>().Not(x => x.Class("foo"));

		CornerstoneTest.AreEqual(typeof(Control1), target.TargetType);
	}

	#endregion

	#region Classes

	public class Control1 : Control
	{
	}

	public class Control2 : Control
	{
	}

	#endregion
}