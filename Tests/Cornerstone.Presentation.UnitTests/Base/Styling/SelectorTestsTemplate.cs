#region References

using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsTemplate
{
	#region Methods

	[PresentationTestMethod]
	public async Task ControlInTemplateIsMatchedWithCorrectTypeOfAndClassOfTemplatedControl()
	{
		var target = new TestTemplatedControl { Classes = { "foo" } };
		var styleKey = typeof(TestTemplatedControl);

		var border = (Border) target.VisualChildren.Single();
		var selector = default(Selector).OfType(styleKey).Class("foo").Template().OfType<Border>();
		var activator = selector.Match(border).Activator;

		CornerstoneTest.IsNotNull(activator);
		CornerstoneTest.IsTrue(await activator.Take(1));
	}

	[PresentationTestMethod]
	public void ControlInTemplateIsMatchedWithTemplateSelector()
	{
		var target = new TestTemplatedControl();
		var border = (Border) target.GetVisualChildren().Single();
		var selector = default(Selector)
			.OfType(target.GetType())
			.Template()
			.OfType<Border>();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisInstance, selector.Match(border).Result);
	}

	[PresentationTestMethod]
	public void ControlInTemplateIsMatchedWithTypeOfTemplatedControl()
	{
		var target = new TestTemplatedControl();
		var styleKey = typeof(TestTemplatedControl);
		var border = (Border) target.VisualChildren.Single();
		var selector = default(Selector).OfType(styleKey).Template().OfType<Border>();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisInstance, selector.Match(border).Result);
	}

	[PresentationTestMethod]
	public async Task ControlInTemplateIsNotMatchedWithCorrectTypeOfAndWrongClassOfTemplatedControl()
	{
		var target = new TestTemplatedControl { Classes = { "bar" } };

		var border = (Border) target.VisualChildren.Single();
		var selector = default(Selector).OfType(typeof(TestTemplatedControl)).Class("foo").Template().OfType<Border>();
		var activator = selector.Match(border).Activator;

		CornerstoneTest.IsNotNull(activator);
		CornerstoneTest.IsFalse(await activator.Take(1));
	}

	[PresentationTestMethod]
	public void ControlInTemplateOfWrongTypeIsNotMatchedWithTemplateSelector()
	{
		var target = new TestTemplatedControl();
		var border = (Border) target.GetVisualChildren().Single();
		var selector = default(Selector)
			.OfType<Button>()
			.Template()
			.OfType<Border>();

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisInstance, selector.Match(border).Result);
	}

	[PresentationTestMethod]
	public void ControlNotInTemplateIsNotMatchedWithTemplateSelector()
	{
		var target = new TestTemplatedControl();
		var border = (Border) target.GetVisualChildren().Single();
		var selector = default(Selector)
			.OfType(target.GetType())
			.Template()
			.OfType<Border>();

		border.TemplatedParent = null;

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisInstance, selector.Match(border).Result);
	}

	[PresentationTestMethod]
	public void NestedControlInTemplateIsMatchedWithTemplateSelector()
	{
		var target = new TestTemplatedControl();
		var textBlock = (TextBlock) target.VisualChildren.Single().VisualChildren.Single();
		var selector = default(Selector)
			.OfType(target.GetType())
			.Template()
			.OfType<TextBlock>();

		CornerstoneTest.AreEqual(SelectorMatchResult.AlwaysThisInstance, selector.Match(textBlock).Result);
	}

	[PresentationTestMethod]
	public void NestedSelectorIsUnsubscribed()
	{
		var target = new TestTemplatedControl { Classes = { "foo" } };
		var border = (Border) target.VisualChildren.Single();
		var selector = default(Selector).OfType(typeof(TestTemplatedControl)).Class("foo").Template().OfType<Border>();
		var activator = selector.Match(border).Activator;
		CornerstoneTest.IsNotNull(activator);

		using (activator.Subscribe(_ => { }))
		{
			CornerstoneTest.AreEqual(1, target.Classes.ListenerCount);
		}

		CornerstoneTest.AreEqual(0, target.Classes.ListenerCount);
	}

	#endregion

	#region Classes

	private class TestTemplatedControl : TemplatedControl
	{
		#region Constructors

		public TestTemplatedControl()
		{
			VisualChildren.Add(new Border
			{
				TemplatedParent = this,
				Child = new TextBlock
				{
					TemplatedParent = this
				}
			});
		}

		#endregion
	}

	#endregion
}