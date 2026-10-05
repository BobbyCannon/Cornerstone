#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsOr
{
	#region Methods

	[PresentationTestMethod]
	public void OrSelectorDoesntMatchControlOfIncorrectType()
	{
		var target = Selectors.Or(
			default(Selector).OfType<Control1>(),
			default(Selector).OfType<Control2>().Class("bar"));
		var control = new Control3();

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void OrSelectorDoesntMatchControlWithIncorrectName()
	{
		var target = Selectors.Or(
			default(Selector).OfType<Control1>().Name("foo"),
			default(Selector).OfType<Control2>().Name("foo"));
		var control = new Control1 { Name = "bar" };

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisInstance, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void OrSelectorMatchesControlOfCorrectType()
	{
		var target = Selectors.Or(
			default(Selector).OfType<Control1>(),
			default(Selector).OfType<Control2>().Class("bar"));
		var control = new Control1();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void OrSelectorMatchesControlOfCorrectTypeWithClass()
	{
		var target = Selectors.Or(
			default(Selector).OfType<Control1>(),
			default(Selector).OfType<Control2>().Class("bar"));
		var control = new Control2();

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void OrSelectorShouldHaveCorrectStringRepresentation()
	{
		var target = Selectors.Or(
			default(Selector).OfType<Control1>().Class("foo"),
			default(Selector).OfType<Control2>().Class("bar"));

		CornerstoneTest.AreEqual("Control1.foo, Control2.bar", target.ToString());
	}

	[PresentationTestMethod]
	public void ReturnsCommonTargetType()
	{
		var target = Selectors.Or(
			default(Selector).OfType<Control1>().Class("foo"),
			default(Selector).OfType<Control2>().Class("bar"));

		CornerstoneTest.AreEqual(typeof(Control), target.TargetType);
	}

	[PresentationTestMethod]
	public void ReturnsCorrectTargetTypeWhenTypesSame()
	{
		var target = Selectors.Or(
			default(Selector).OfType<Control1>().Class("foo"),
			default(Selector).OfType<Control1>().Class("bar"));

		CornerstoneTest.AreEqual(typeof(Control1), target.TargetType);
	}

	[PresentationTestMethod]
	public void ReturnsNullTargetTypeWhenASelectorHasNoTargetType()
	{
		var target = Selectors.Or(
			default(Selector).OfType<Control1>().Class("foo"),
			default(Selector).Class("bar"));

		CornerstoneTest.AreEqual(null, target.TargetType);
	}

	[PresentationTestMethod]
	public void ValidateNestingSelectorChecksChildrenWhenParentIsAnOrSelector()
	{
		var target = Selectors.Or(
			default(Selector).Class("foo"),
			default(Selector).Class("bar")
		).Name("baz");

		Assert.Throws<InvalidOperationException>(() => target.ValidateNestingSelector(false));

		target = Selectors.Or(
			default(Selector).Nesting().Class("foo"),
			default(Selector).Nesting().Class("bar")
		).Name("baz");

		target.ValidateNestingSelector(false);
	}

	#endregion

	#region Classes

	public class Control1 : Control
	{
	}

	public class Control2 : Control
	{
	}

	public class Control3 : Control
	{
	}

	#endregion
}