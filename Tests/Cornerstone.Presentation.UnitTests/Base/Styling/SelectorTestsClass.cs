#region References

using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsClass
{
	#region Methods

	[PresentationTestMethod]
	public async Task ClassDoesntMatchControlWithoutClass()
	{
		var control = new Control1
		{
			Classes = { "bar" }
		};

		var target = default(Selector).Class("foo");
		var match = target.Match(control);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		CornerstoneTest.IsFalse(await match.Activator.Take(1));
	}

	[PresentationTestMethod]
	public async Task ClassMatchesControlWithClass()
	{
		var control = new Control1
		{
			Classes = { "foo" }
		};

		var target = default(Selector).Class("foo");
		var match = target.Match(control);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		CornerstoneTest.IsTrue(await match.Activator.Take(1));
	}

	[PresentationTestMethod]
	public async Task ClassMatchesControlWithTemplatedParent()
	{
		var control = new Control1
		{
			Classes = { "foo" },
			TemplatedParent = new Button()
		};

		var target = default(Selector).Class("foo");
		var match = target.Match(control);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		CornerstoneTest.IsTrue(await match.Activator.Take(1));
	}

	[PresentationTestMethod]
	public void ClassSelectorShouldHaveCorrectStringRepresentation()
	{
		var target = default(Selector).Class("foo");

		CornerstoneTest.AreEqual(".foo", target.ToString());
	}

	[PresentationTestMethod]
	public async Task ClassTracksAdditions()
	{
		var control = new Control1();

		var target = default(Selector).Class("foo");
		var activator = target.Match(control).Activator;
		CornerstoneTest.IsNotNull(activator);
		var observable = activator.ToObservable();

		CornerstoneTest.IsFalse(await observable.Take(1));
		control.Classes.Add("foo");
		CornerstoneTest.IsTrue(await observable.Take(1));
	}

	[PresentationTestMethod]
	public async Task ClassTracksRemovals()
	{
		var control = new Control1
		{
			Classes = { "foo" }
		};

		var target = default(Selector).Class("foo");
		var activator = target.Match(control).Activator;
		CornerstoneTest.IsNotNull(activator);
		var observable = activator.ToObservable();

		CornerstoneTest.IsTrue(await observable.Take(1));
		control.Classes.Remove("foo");
		CornerstoneTest.IsFalse(await observable.Take(1));
	}

	[PresentationTestMethod]
	public async Task MultipleClasses()
	{
		var control = new Control1();
		var target = default(Selector).Class("foo").Class("bar");
		var activator = target.Match(control).Activator;
		CornerstoneTest.IsNotNull(activator);
		var observable = activator.ToObservable();

		CornerstoneTest.IsFalse(await observable.Take(1));
		control.Classes.Add("foo");
		CornerstoneTest.IsFalse(await observable.Take(1));
		control.Classes.Add("bar");
		CornerstoneTest.IsTrue(await observable.Take(1));
		control.Classes.Remove("bar");
		CornerstoneTest.IsFalse(await observable.Take(1));
	}

	[PresentationTestMethod]
	public void OnlyNotifiesWhenResultChanges()
	{
		// Test for #1698
		var control = new Control1
		{
			Classes = { "foo" }
		};

		var target = default(Selector).Class("foo");
		var activator = target.Match(control).Activator;
		CornerstoneTest.IsNotNull(activator);
		var result = new List<bool>();

		using (activator.Subscribe(x => result.Add(x)))
		{
			control.Classes.Add("bar");
			control.Classes.Remove("foo");
		}

		CornerstoneTest.AreEqual(new[] { true, false }, result);
	}

	[PresentationTestMethod]
	public void PesudoClassSelectorShouldHaveCorrectStringRepresentation()
	{
		var target = default(Selector).Class(":foo");

		CornerstoneTest.AreEqual(":foo", target.ToString());
	}

	#endregion

	#region Classes

	public class Control1 : Control
	{
	}

	#endregion
}