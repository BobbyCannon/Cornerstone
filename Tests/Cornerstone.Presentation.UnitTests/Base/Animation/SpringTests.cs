#region References

using System;
using System.Threading;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Animation.Easings;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Animation;

[TestClass]
public class SpringTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("1,2F,3,4")]
	[DataRow("Foo,Bar,Fee,Buzz")]
	public void CanHandleInvalidStringViaTypeConverter(string input)
	{
		var conv = new SpringTypeConverter();

		CornerstoneTest.Throws<Exception>(() => (Spring) conv.ConvertFrom(input));
	}

	[PresentationTestMethod]
	[DataRow("1,2 3,4")]
	public void CanParseSpringViaTypeConverter(string input)
	{
		var conv = new SpringTypeConverter();

		var spring = CornerstoneTest.IsAssignableFrom<Spring>(conv.ConvertFrom(input));

		CornerstoneTest.IsNotNull(spring);
		CornerstoneTest.AreEqual(1, spring.Mass);
		CornerstoneTest.AreEqual(2, spring.Stiffness);
		CornerstoneTest.AreEqual(3, spring.Damping);
		CornerstoneTest.AreEqual(4, spring.InitialVelocity);
	}

	[PresentationTestMethod]
	public void CheckSpringEasingHandledproperly()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(RotateTransform.AngleProperty, -2.5d) }, KeyTime = TimeSpan.FromSeconds(0)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(RotateTransform.AngleProperty, 2.5d) }, KeyTime = TimeSpan.FromSeconds(5)
		};

		var animation = new Presentation.Animation.Animation
		{
			Duration = TimeSpan.FromSeconds(5),
			Children = { keyframe1, keyframe2 },
			IterationCount = new IterationCount(5),
			PlaybackDirection = PlaybackDirection.Alternate,
			Easing = new SpringEasing(1, 10, 1)
		};

		var rotateTransform = new RotateTransform(-2.5);
		var rect = new Rectangle { RenderTransform = rotateTransform };

		var clock = new TestClock();
		animation.RunAsync(rect, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(-2.5, rotateTransform.Angle);
		clock.Step(TimeSpan.FromSeconds(5));
		CornerstoneTest.AreEqual(5.522828945000075, rotateTransform.Angle);

		var tolerance = 0.01;
		clock.Step(TimeSpan.Parse("00:00:10.0153932"));
		var expected = -2.499763294237805;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:11.2655407"));
		expected = -1.1011448950348934;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:12.6158773"));
		expected = 2.1264981706749007;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:14.6495256"));
		expected = 5.4337608446234782;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);
	}

	[PresentationTestMethod]
	public void SplineEasingCanBeMutated()
	{
		var easing = new SpringEasing(1, 1, 1);

		CornerstoneTest.AreEqual(0, easing.Ease(0));
		CornerstoneTest.AreEqual(0.34029984660829826, easing.Ease(1));

		easing.Mass = 2;
		easing.Stiffness = 2;
		easing.Damping = 2;
		easing.InitialVelocity = 1;

		CornerstoneTest.AreNotEqual(0.05136985716812037, easing.Ease(0.5));
	}

	#endregion
}