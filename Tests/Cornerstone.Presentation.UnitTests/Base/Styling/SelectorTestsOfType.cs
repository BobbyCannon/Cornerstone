#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsOfType
{
	#region Methods

	[PresentationTestMethod]
	public void OfTypeClassDoesntMatchControlOfWrongType()
	{
		var control = new Control2();
		var target = default(Selector).OfType<Control1>().Class("foo");

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void OfTypeDoesntMatchControlOfWrongType()
	{
		var control = new Control2();
		var target = default(Selector).OfType<Control1>();

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void OfTypeMatchesControlOfCorrectType()
	{
		var control = new Control1();
		var target = default(Selector).OfType<Control1>();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void OfTypeMatchesControlWithTemplatedParent()
	{
		var control = new Control1 { TemplatedParent = new Button() };
		var target = default(Selector).OfType<Control1>();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
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