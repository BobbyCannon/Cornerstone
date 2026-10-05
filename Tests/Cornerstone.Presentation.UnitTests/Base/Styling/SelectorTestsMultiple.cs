#region References

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsMultiple
{
	#region Methods

	[PresentationTestMethod]
	public void ControlWithClassDescendentOfControlWithTwoClasses()
	{
		var textBlock = new TextBlock();
		var control = new Button { Content = textBlock };

		control.ApplyTemplate();

		var selector = default(Selector)
			.OfType<Button>()
			.Class("foo")
			.Class("bar")
			.Descendant()
			.OfType<TextBlock>()
			.Class("baz");

		var values = new List<bool>();
		var match = selector.Match(textBlock);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		match.Activator.Subscribe(x => values.Add(x));

		CornerstoneTest.AreEqual(new[] { false }, values);
		control.Classes.AddRange(new[] { "foo", "bar" });
		CornerstoneTest.AreEqual(new[] { false }, values);
		textBlock.Classes.Add("baz");
		CornerstoneTest.AreEqual(new[] { false, true }, values);
	}

	[PresentationTestMethod]
	public void NamedClassTemplateChildOfControl()
	{
		var template = new FuncControlTemplate((parent, scope) =>
		{
			return new Border
			{
				Name = "border"
			}.RegisterInNameScope(scope);
		});

		var control = new Button
		{
			Template = template
		};

		control.ApplyTemplate();

		var selector = default(Selector)
			.OfType<Button>()
			.Template()
			.Name("border")
			.Class("foo");

		var border = (Border) control.VisualChildren.Single();
		var values = new List<bool>();
		var match = selector.Match(border);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		match.Activator.Subscribe(x => values.Add(x));

		CornerstoneTest.AreEqual(new[] { false }, values);
		border.Classes.AddRange(new[] { "foo" });
		CornerstoneTest.AreEqual(new[] { false, true }, values);
		border.Classes.Remove("foo");
		CornerstoneTest.AreEqual(new[] { false, true, false }, values);
	}

	[PresentationTestMethod]
	public void NamedOfTypeTemplateChildOfControlWithTwoClassesWrongType()
	{
		var template = new FuncControlTemplate((parent, scope) =>
		{
			return new Border
			{
				Name = "border"
			}.RegisterInNameScope(scope);
		});

		var control = new Button
		{
			Template = template
		};

		control.ApplyTemplate();

		var selector = default(Selector)
			.OfType<Button>()
			.Class("foo")
			.Class("bar")
			.Template()
			.OfType<TextBlock>()
			.Name("baz");

		var border = (Border) control.VisualChildren.Single();
		var values = new List<bool>();
		var match = selector.Match(border);

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, match.Result);
	}

	[PresentationTestMethod]
	public void NamedTemplateChildOfControlWithTwoClasses()
	{
		var template = new FuncControlTemplate((parent, scope) =>
		{
			return new Border
			{
				Name = "border"
			}.RegisterInNameScope(scope);
		});

		var control = new Button
		{
			Template = template
		};

		control.ApplyTemplate();

		var selector = default(Selector)
			.OfType<Button>()
			.Class("foo")
			.Class("bar")
			.Template()
			.Name("border");

		var border = (Border) control.VisualChildren.Single();
		var values = new List<bool>();
		var match = selector.Match(border);

		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);
		CornerstoneTest.IsNotNull(match.Activator);
		match.Activator.Subscribe(x => values.Add(x));

		CornerstoneTest.AreEqual(new[] { false }, values);
		control.Classes.AddRange(new[] { "foo", "bar" });
		CornerstoneTest.AreEqual(new[] { false, true }, values);
		control.Classes.Remove("foo");
		CornerstoneTest.AreEqual(new[] { false, true, false }, values);
	}

	[PresentationTestMethod]
	public async Task NestedPropertyEquals()
	{
		var control = new Canvas();
		var parent = new Border { Child = control };

		var target = default(Selector)
			.OfType<Border>()
			.PropertyEquals(Border.TagProperty, "foo")
			.Child()
			.OfType<Canvas>()
			.PropertyEquals(Canvas.TagProperty, "bar");

		var match = target.Match(control);
		CornerstoneTest.AreEqual(SelectorMatchResult.Sometimes, match.Result);

		var activator = match.Activator;

		CornerstoneTest.IsNotNull(activator);
		CornerstoneTest.IsFalse(await activator.Take(1));
		control.Tag = "bar";
		CornerstoneTest.IsFalse(await activator.Take(1));
		parent.Tag = "foo";
		CornerstoneTest.IsTrue(await activator.Take(1));
	}

	[PresentationTestMethod]
	public void TargetTypeChild()
	{
		var selector = default(Selector)
			.OfType<Button>()
			.Child()
			.OfType<TextBlock>();

		CornerstoneTest.AreEqual(typeof(TextBlock), selector.TargetType);
	}

	[PresentationTestMethod]
	public void TargetTypeDescendant()
	{
		var selector = default(Selector)
			.OfType<Button>()
			.Descendant()
			.OfType<TextBlock>();

		CornerstoneTest.AreEqual(typeof(TextBlock), selector.TargetType);
	}

	[PresentationTestMethod]
	public void TargetTypeIsClass()
	{
		var selector = default(Selector)
			.Is<Button>()
			.Class("foo");

		CornerstoneTest.AreEqual(typeof(Button), selector.TargetType);
	}

	[PresentationTestMethod]
	public void TargetTypeOfType()
	{
		var selector = default(Selector).OfType<Button>();

		CornerstoneTest.AreEqual(typeof(Button), selector.TargetType);
	}

	[PresentationTestMethod]
	public void TargetTypeOfTypeClass()
	{
		var selector = default(Selector)
			.OfType<Button>()
			.Class("foo");

		CornerstoneTest.AreEqual(typeof(Button), selector.TargetType);
	}

	[PresentationTestMethod]
	public void TargetTypeTemplate()
	{
		var selector = default(Selector)
			.OfType<Button>()
			.Template()
			.OfType<TextBlock>();

		CornerstoneTest.AreEqual(typeof(TextBlock), selector.TargetType);
	}

	#endregion
}