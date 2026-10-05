#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsName
{
	#region Methods

	[PresentationTestMethod]
	public void NameDoesntMatchControlOfWrongName()
	{
		var control = new Control1 { Name = "foo" };
		var target = default(Selector).Name("bar");

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisInstance, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void NameDoesntMatchControlWithTemplatedParent()
	{
		var control = new Control1 { TemplatedParent = new Button() };
		var target = default(Selector).Name("foo");
		var activator = target.Match(control);

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisInstance, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void NameHasCorrectStringRepresentation()
	{
		var target = default(Selector).Name("foo");

		CornerstoneTest.AreEqual("#foo", target.ToString());
	}

	[PresentationTestMethod]
	public void NameMatchesControlWithCorrectName()
	{
		var control = new Control1 { Name = "foo" };
		var target = default(Selector).Name("foo");

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisInstance, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public void TypeAndNameHasCorrectStringRepresentation()
	{
		var target = default(Selector).OfType<Control1>().Name("foo");

		CornerstoneTest.AreEqual("Control1#foo", target.ToString());
	}

	#endregion

	#region Classes

	public class Control1 : Control
	{
	}

	#endregion
}