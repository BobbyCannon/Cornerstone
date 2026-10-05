#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Styling.Activators;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsNesting
{
	#region Methods

	[PresentationTestMethod]
	public void AddingChildWithNoNestingSelectorFails()
	{
		var parent = new Style(x => x.OfType<Control1>());
		var child = new Style(x => x.Class("foo"));

		Assert.Throws<InvalidOperationException>(() => parent.Children.Add(child));
	}

	[PresentationTestMethod]
	public void AddingCombinatorSelectorChildWithNoNestingSelectorFails()
	{
		var parent = new Style(x => x.OfType<Control1>());
		var child = new Style(x => x.Class("foo").Descendant().Class("bar"));

		Assert.Throws<InvalidOperationException>(() => parent.Children.Add(child));
	}

	[PresentationTestMethod]
	public void AddingOrSelectorChildWithNoNestingSelectorFails()
	{
		var parent = new Style(x => x.OfType<Control1>());
		var child = new Style(x => Selectors.Or(
			x.Nesting().Class("foo"),
			x.Class("bar")));

		Assert.Throws<InvalidOperationException>(() => parent.Children.Add(child));
	}

	[PresentationTestMethod]
	public void CanAddChildWithoutNestingSelectorToStyleWithoutSelector()
	{
		var parent = new Style();
		var child = new Style(x => x.Class("foo"));

		parent.Children.Add(child);
	}

	[PresentationTestMethod]
	public void DoubleNestingClassDoesntMatchGrandparentOfTypeSelector()
	{
		var control = new Control2
		{
			Classes = { "foo", "bar" }
		};

		Style parent;
		Style nested;
		var grandparent = new Style(x => x.OfType<Control1>())
		{
			Children =
			{
				(parent = new Style(x => x.Nesting().Class("foo"))
				{
					Children =
					{
						(nested = new Style(x => x.Nesting().Class("bar")))
					}
				})
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, match.Result);
	}

	[PresentationTestMethod]
	public void DoubleNestingClassMatches()
	{
		var control = new Control1
		{
			Classes = { "foo", "bar" }
		};

		Style parent;
		Style nested;
		var grandparent = new Style(x => x.OfType<Control1>())
		{
			Children =
			{
				(parent = new Style(x => x.Nesting().Class("foo"))
				{
					Children =
					{
						(nested = new Style(x => x.Nesting().Class("bar")))
					}
				})
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);

		var sink = new ActivatorSink(match.Activator);

		CornerstoneTest.IsTrue(sink.Active);
		control.Classes.Remove("foo");
		CornerstoneTest.IsFalse(sink.Active);
	}

	[PresentationTestMethod]
	public void NestingClassDoesntMatchParentOfTypeSelector()
	{
		var control = new Control2();
		Style nested;
		var parent = new Style(x => x.OfType<Control1>())
		{
			Children =
			{
				(nested = new Style(x => x.Nesting().Class("foo")))
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, match.Result);
	}

	[PresentationTestMethod]
	public void NestingClassMatches()
	{
		var control = new Control1 { Classes = { "foo" } };
		Style nested;
		var parent = new Style(x => x.OfType<Control1>())
		{
			Children =
			{
				(nested = new Style(x => x.Nesting().Class("foo")))
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);

		var sink = new ActivatorSink(match.Activator);

		CornerstoneTest.IsTrue(sink.Active);
		control.Classes.Clear();
		CornerstoneTest.IsFalse(sink.Active);
	}

	[PresentationTestMethod]
	public void NestingNotClassMatches()
	{
		var control = new Control1 { Classes = { "foo" } };
		Style nested;
		var parent = new Style(x => x.OfType<Control1>())
		{
			Children =
			{
				(nested = new Style(x => x.Nesting().Not(y => y.Class("foo"))))
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);

		var sink = new ActivatorSink(match.Activator);

		CornerstoneTest.IsFalse(sink.Active);
		control.Classes.Clear();
		CornerstoneTest.IsTrue(sink.Active);
	}

	[PresentationTestMethod]
	public void NestingWithNoParentSelectorFails()
	{
		var control = new Control1();
		Style nested;
		var parent = new Style
		{
			Children =
			{
				(nested = new Style(x => x.Nesting().Class("foo")))
			}
		};

		Assert.Throws<InvalidOperationException>(() => nested.Selector!.Match(control, parent));
	}

	[PresentationTestMethod]
	public void NestingWithNoParentStyleFails()
	{
		var control = new Control1();
		var style = new Style(x => x.Nesting().OfType<Control1>());

		Assert.Throws<InvalidOperationException>(() => style.Selector!.Match(control, null));
	}

	[PresentationTestMethod]
	public void OrNestingChildOfTypeDoesntMatchParentOfTypeSelector()
	{
		var control = new Control1();
		var panel = new DockPanel { Children = { control } };
		Style nested;
		var parent = new Style(x => x.OfType<Panel>())
		{
			Children =
			{
				(nested = new Style(x => Selectors.Or(
					x.Nesting().Child().OfType<Control1>(),
					x.Nesting().Child().OfType<Control1>())))
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisInstance, match.Result);
	}

	[PresentationTestMethod]
	public void OrNestingChildOfTypeMatches()
	{
		var control = new Control1 { Classes = { "foo" } };
		var panel = new Panel { Children = { control } };
		Style nested;
		var parent = new Style(x => x.OfType<Panel>())
		{
			Children =
			{
				(nested = new Style(x => Selectors.Or(
					x.Nesting().Child().OfType<Control1>(),
					x.Nesting().Child().OfType<Control1>())))
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisInstance, match.Result);
	}

	[PresentationTestMethod]
	public void OrNestingClassDoesntMatchParentOfTypeSelector()
	{
		var control = new Control2();
		Style nested;
		var parent = new Style(x => x.OfType<Control1>())
		{
			Children =
			{
				(nested = new Style(x => Selectors.Or(
					x.Nesting().Class("foo"),
					x.Nesting().Class("bar"))))
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, match.Result);
	}

	[PresentationTestMethod]
	public void OrNestingClassMatches()
	{
		var control = new Control1 { Classes = { "foo" } };
		Style nested;
		var parent = new Style(x => x.OfType<Control1>())
		{
			Children =
			{
				(nested = new Style(x => Selectors.Or(
					x.Nesting().Class("foo"),
					x.Nesting().Class("bar"))))
			}
		};

		var match = nested.Selector!.Match(control, parent);
		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);

		var sink = new ActivatorSink(match.Activator);

		CornerstoneTest.IsTrue(sink.Active);
		control.Classes.Clear();
		CornerstoneTest.IsFalse(sink.Active);
	}

	#endregion

	#region Classes

	public class Control1 : Control
	{
	}

	public class Control2 : Control
	{
	}

	private class ActivatorSink : IStyleActivatorSink
	{
		#region Constructors

		public ActivatorSink(IStyleActivator source)
		{
			source.Subscribe(this);
			Active = source.GetIsActive();
		}

		#endregion

		#region Properties

		public bool Active { get; private set; }

		#endregion

		#region Methods

		public void OnNext(bool value)
		{
			Active = value;
		}

		#endregion
	}

	#endregion
}