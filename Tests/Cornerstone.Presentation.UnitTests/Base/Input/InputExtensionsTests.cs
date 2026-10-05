#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class InputExtensionsTests
{
	#region Methods

	[PresentationTestMethod]
	public void InputHitTestShouldUseCoordinatesRelativeToTheSubtreeRoot()
	{
		Border target;
		using var services = new CompositorTestServices(new Size(200, 200))
		{
			TopLevel =
			{
				Content = new StackPanel
				{
					Background = Brushes.White,
					Children =
					{
						new Border
						{
							Width = 100,
							Height = 200,
							Background = Brushes.Red
						},
						(target = new Border
						{
							Width = 100,
							Height = 200,
							Background = Brushes.Green
						})
					},
					Orientation = Orientation.Horizontal
				}
			}
		};

		services.RunJobs();

		var result = target.InputHitTest(new Point(50, 50), false);

		CornerstoneTest.Same(target, result);
	}

	#endregion
}