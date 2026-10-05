#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class UserControlTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldBeStyledAsUserControl()
	{
		var target = new UserControl();
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<UserControl>())
				{
					Setters =
					{
						new Setter(TemplatedControl.TemplateProperty, GetTemplate())
					}
				}
			},
			Child = target
		};

		CornerstoneTest.IsNotNull(target.Template);
	}

	private static FuncControlTemplate GetTemplate()
	{
		return new FuncControlTemplate<UserControl>((parent, scope) =>
		{
			return new Border
			{
				Background = new SolidColorBrush(0xffffffff),
				Child = new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty]
				}.RegisterInNameScope(scope)
			};
		});
	}

	#endregion
}