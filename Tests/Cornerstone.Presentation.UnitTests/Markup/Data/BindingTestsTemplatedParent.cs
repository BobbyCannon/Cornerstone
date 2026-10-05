#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsTemplatedParent : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void OneWayBindingShouldBeSetUp()
	{
		var source = new Button
		{
			Template = new FuncControlTemplate<Button>((parent, _) =>
				new ContentPresenter
				{
					[~ContentPresenter.ContentProperty] = new Binding
					{
						Mode = BindingMode.OneWay,
						RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
						Path = "Content"
					}
				})
		};

		source.ApplyTemplate();

		var target = (ContentPresenter) source.GetVisualChildren().Single();

		CornerstoneTest.IsNull(target.Content);
		source.Content = "foo";
		CornerstoneTest.AreEqual("foo", target.Content);
		source.Content = "bar";
		CornerstoneTest.AreEqual("bar", target.Content);
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldBeSetUp()
	{
		var source = new Button
		{
			Template = new FuncControlTemplate<Button>((parent, _) =>
				new ContentPresenter
				{
					[~ContentPresenter.ContentProperty] = new Binding
					{
						Mode = BindingMode.TwoWay,
						RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
						Path = "Content"
					}
				})
		};

		source.ApplyTemplate();

		var target = (ContentPresenter) source.GetVisualChildren().Single();

		CornerstoneTest.IsNull(target.Content);
		source.Content = "foo";
		CornerstoneTest.AreEqual("foo", target.Content);
		target.Content = "bar";
		CornerstoneTest.AreEqual("bar", source.Content);
	}

	#endregion
}