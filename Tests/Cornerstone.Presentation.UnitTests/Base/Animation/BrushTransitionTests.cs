#region References

using System;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Animation;

[TestClass]
public class BrushTransitionTests
{
	#region Methods

	[PresentationTestMethod]
	public void LinearGradientBrushOpacityIsInteroplated()
	{
		Test(0, new LinearGradientBrush { Opacity = 0 }, new LinearGradientBrush { Opacity = 0 });
		Test(0, new LinearGradientBrush { Opacity = 0 }, new LinearGradientBrush { Opacity = 1 });
		Test(0.5, new LinearGradientBrush { Opacity = 0 }, new LinearGradientBrush { Opacity = 1 });
		Test(0.5, new LinearGradientBrush { Opacity = 0.5 }, new LinearGradientBrush { Opacity = 0.5 });
		Test(1, new LinearGradientBrush { Opacity = 1 }, new LinearGradientBrush { Opacity = 1 });
	}

	[PresentationTestMethod]
	public void SolidColorBrushOpacityIsInteroplated()
	{
		Test(0, new SolidColorBrush { Opacity = 0 }, new SolidColorBrush { Opacity = 0 });
		Test(0, new SolidColorBrush { Opacity = 0 }, new SolidColorBrush { Opacity = 1 });
		Test(0.5, new SolidColorBrush { Opacity = 0 }, new SolidColorBrush { Opacity = 1 });
		Test(0.5, new SolidColorBrush { Opacity = 0.5 }, new SolidColorBrush { Opacity = 0.5 });
		Test(1, new SolidColorBrush { Opacity = 1 }, new SolidColorBrush { Opacity = 1 });

		// TODO: investigate why this case fails.
		//Test2(1, new SolidColorBrush { Opacity = 0 }, new SolidColorBrush { Opacity = 1 });
	}

	private static void Test(double progress, IBrush oldBrush, IBrush newBrush)
	{
		var clock = new TestClock();
		var border = new Border { Background = oldBrush };
		var sut = new BrushTransition
		{
			Duration = TimeSpan.FromSeconds(1), Property = Border.BackgroundProperty
		};

		sut.Apply(border, clock, oldBrush, newBrush);
		clock.Pulse(TimeSpan.Zero);
		clock.Pulse(sut.Duration * progress);

		CornerstoneTest.IsNotNull(border.Background);
		CornerstoneTest.AreEqual(oldBrush.Opacity + ((newBrush.Opacity - oldBrush.Opacity) * progress), border.Background.Opacity);
	}

	#endregion
}