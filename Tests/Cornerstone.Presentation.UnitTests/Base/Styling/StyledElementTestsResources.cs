#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class StyledElementTestsResources
{
	#region Methods

	[PresentationTestMethod]
	public void AddingResourceShouldCallRaiseResourceChangedOnLogicalChildren()
	{
		Border child;

		var target = new ContentControl
		{
			Content = child = new Border(),
			Template = ContentControlTemplate()
		};

		var raisedOnTarget = false;
		var raisedOnChild = false;

		target.Measure(Size.Infinity);
		target.ResourcesChanged += (_, __) => raisedOnTarget = true;
		child.ResourcesChanged += (_, __) => raisedOnChild = true;

		target.Resources.Add("foo", "bar");

		CornerstoneTest.IsTrue(raisedOnTarget);
		CornerstoneTest.IsTrue(raisedOnChild);
	}

	[PresentationTestMethod]
	public void AddingResourceToNestedStyleShouldRaiseResourceChanged()
	{
		Style style;
		var target = new StyledElement
		{
			Styles =
			{
				(style = new Style())
			}
		};

		var raised = false;

		target.ResourcesChanged += (_, __) => raised = true;
		style.Resources.Add("foo", "bar");

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void AddingResourceToStylesShouldRaiseResourceChanged()
	{
		var target = new Decorator();
		var raised = false;

		target.ResourcesChanged += (_, __) => raised = true;
		target.Styles.Resources.Add("foo", "bar");

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void FindResourceShouldFindApplicationResource()
	{
		Control target;

		var app = new Application
		{
			Resources =
			{
				{ "foo", "foo-value" }
			}
		};

		var root = new TestRoot
		{
			Child = target = new Control(),
			StylingParent = app
		};

		CornerstoneTest.AreEqual("foo-value", target.FindResource("foo"));
	}

	[PresentationTestMethod]
	public void FindResourceShouldFindApplicationStyleResource()
	{
		Control target;

		var app = new Application
		{
			Styles =
			{
				new Style
				{
					Resources =
					{
						{ "foo", "foo-value" }
					}
				}
			},
			Resources =
			{
				{ "bar", "bar-value" }
			}
		};

		var root = new TestRoot
		{
			Child = target = new Control(),
			StylingParent = app
		};

		CornerstoneTest.AreEqual("foo-value", target.FindResource("foo"));
	}

	[PresentationTestMethod]
	public void FindResourceShouldFindControlResource()
	{
		var target = new StyledElement
		{
			Resources =
			{
				{ "foo", "foo-value" }
			}
		};

		CornerstoneTest.AreEqual("foo-value", target.FindResource("foo"));
	}

	[PresentationTestMethod]
	public void FindResourceShouldFindControlResourceInParent()
	{
		Control target;

		var root = new Decorator
		{
			Resources =
			{
				{ "foo", "foo-value" }
			},
			Child = target = new Control()
		};

		CornerstoneTest.AreEqual("foo-value", target.FindResource("foo"));
	}

	[PresentationTestMethod]
	public void FindResourceShouldFindStyleResource()
	{
		var target = new StyledElement
		{
			Styles =
			{
				new Style
				{
					Resources =
					{
						{ "foo", "foo-value" }
					}
				}
			},
			Resources =
			{
				{ "bar", "bar-value" }
			}
		};

		CornerstoneTest.AreEqual("foo-value", target.FindResource("foo"));
	}

	[PresentationTestMethod]
	public void FindResourceShouldFindStylesResource()
	{
		var target = new StyledElement
		{
			Styles =
			{
				new Styles
				{
					Resources =
					{
						{ "foo", "foo-value" }
					}
				}
			},
			Resources =
			{
				{ "bar", "bar-value" }
			}
		};

		CornerstoneTest.AreEqual("foo-value", target.FindResource("foo"));
	}

	private static IControlTemplate ContentControlTemplate()
	{
		return new FuncControlTemplate<ContentControl>((x, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
			}.RegisterInNameScope(scope));
	}

	#endregion
}