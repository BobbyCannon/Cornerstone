#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class ControlThemeTests
{
	#region Methods

	[PresentationTestMethod]
	public void ControlThemeCannotBeAddedToControlThemeChildren()
	{
		var target = new ControlTheme(typeof(Button));
		var other = new ControlTheme(typeof(CheckBox));

		Assert.Throws<InvalidOperationException>(() => other.Children.Add(target));
	}

	[PresentationTestMethod]
	public void ControlThemeCannotBeAddedToStyleChildren()
	{
		var target = new ControlTheme(typeof(Button));
		var style = new Style();

		Assert.Throws<InvalidOperationException>(() => style.Children.Add(target));
	}

	[PresentationTestMethod]
	public void ControlThemeCannotBeAddedToStyles()
	{
		var target = new ControlTheme(typeof(Button));
		var styles = new Styles();

		Assert.Throws<InvalidOperationException>(() => styles.Add(target));
	}

	[PresentationTestMethod]
	public void StyleWithDoubleTemplateSelectorCannotBeAddedToChildren()
	{
		var target = new ControlTheme(typeof(Button));
		var child = new Style(x => x.Nesting().Template().OfType<ToggleButton>().Template().OfType<Border>());

		Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
	}

	[PresentationTestMethod]
	public void StyleWithNonTemplateChildSelectorCannotBeAddedToChildren()
	{
		var target = new ControlTheme(typeof(Button));
		var child = new Style(x => x.Nesting().Child().OfType<Border>());

		Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
	}

	[PresentationTestMethod]
	public void StyleWithNonTemplateChildTemplateSelectorCannotBeAddedToChildren()
	{
		var target = new ControlTheme(typeof(Button));
		var child = new Style(x => x.Nesting().Child().Template().OfType<Border>());

		Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
	}

	[PresentationTestMethod]
	public void StyleWithNonTemplateDescendentSelectorCannotBeAddedToChildren()
	{
		var target = new ControlTheme(typeof(Button));
		var child = new Style(x => x.Nesting().Descendant().OfType<Border>());

		Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
	}

	[PresentationTestMethod]
	public void StyleWithoutNestingSelectorCannotBeAddedToChildren()
	{
		var target = new ControlTheme(typeof(Button));
		var child = new Style(x => x.OfType<Button>().Template().OfType<Border>());

		Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
	}

	[PresentationTestMethod]
	public void StyleWithoutSelectorCannotBeAddedToChildren()
	{
		var target = new ControlTheme(typeof(Button));
		var child = new Style();

		Assert.Throws<InvalidOperationException>(() => target.Children.Add(child));
	}

	#endregion
}