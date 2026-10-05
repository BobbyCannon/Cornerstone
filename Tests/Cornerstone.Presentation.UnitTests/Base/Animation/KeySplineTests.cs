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
public class KeySplineTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("1,2F,3,4")]
	[DataRow("Foo,Bar,Fee,Buzz")]
	public void CanHandleInvalidStringKeySplineViaTypeConverter(string input)
	{
		var conv = new KeySplineTypeConverter();

		CornerstoneTest.Throws<Exception>(() => (KeySpline) conv.ConvertFrom(input));
	}

	[PresentationTestMethod]
	[DataRow("1,2 3,4")]
	[DataRow("1 2 3 4")]
	[DataRow("1 2,3 4")]
	[DataRow("1,2,3,4")]
	public void CanParseKeySplineViaTypeConverter(string input)
	{
		var conv = new KeySplineTypeConverter();

		var keySpline = CornerstoneTest.IsAssignableFrom<KeySpline>(conv.ConvertFrom(input));

		CornerstoneTest.AreEqual(1, keySpline.ControlPointX1);
		CornerstoneTest.AreEqual(2, keySpline.ControlPointY1);
		CornerstoneTest.AreEqual(3, keySpline.ControlPointX2);
		CornerstoneTest.AreEqual(4, keySpline.ControlPointY2);
	}

	/*
	To get the test values for the KeySpline test, you can:
	1) Grab the WPF sample for KeySpline animations from https://github.com/microsoft/WPF-Samples/tree/master/Animation/KeySplineAnimations
	2) Add the following xaml somewhere:
		<Button Content="Capture"
				Click="Button_Click"/>
		<ScrollViewer VerticalScrollBarVisibility="Visible">
			<TextBlock Name="CaptureData"
						Text="---"
						TextWrapping="Wrap" />
		</ScrollViewer>
	3) Add the following code to the code behind:
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			CaptureData.Text += string.Format("\n{0} | {1}", myTranslateTransform3D.OffsetX, (TimeSpan)ExampleStoryboard.GetCurrentTime(this));
			CaptureData.Text +=
				"\nKeySpline=\"" + mySplineKeyFrame.KeySpline.ControlPoint1.X.ToString() + "," +
				mySplineKeyFrame.KeySpline.ControlPoint1.Y.ToString() + " " +
				mySplineKeyFrame.KeySpline.ControlPoint2.X.ToString() + "," +
				mySplineKeyFrame.KeySpline.ControlPoint2.Y.ToString() + "\"";
			CaptureData.Text += "\n-----";
		}
	4) Run the app, mess with the slider values, then click the button to capture output values
	**/

	[PresentationTestMethod]
	public void CheckKeySplineHandledproperly()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(RotateTransform.AngleProperty, -2.5d) }, KeyTime = TimeSpan.FromSeconds(0)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(RotateTransform.AngleProperty, 2.5d) },
			KeyTime = TimeSpan.FromSeconds(5),
			KeySpline = new KeySpline(0.1123555056179775,
				0.657303370786517,
				0.8370786516853934,
				0.499999999999999999)
		};

		var animation = new Presentation.Animation.Animation
		{
			Duration = TimeSpan.FromSeconds(5),
			Children = { keyframe1, keyframe2 },
			IterationCount = new IterationCount(5),
			PlaybackDirection = PlaybackDirection.Alternate
		};

		var rotateTransform = new RotateTransform(-2.5);
		var rect = new Rectangle { RenderTransform = rotateTransform };

		var clock = new TestClock();

		animation.RunAsync(rect, clock, CancellationToken.None);

		// position is what you'd expect at end and beginning
		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(rotateTransform.Angle, -2.5);
		clock.Step(TimeSpan.FromSeconds(5));
		CornerstoneTest.AreEqual(rotateTransform.Angle, 2.5);

		// test some points in between end and beginning
		var tolerance = 0.01;
		clock.Step(TimeSpan.Parse("00:00:10.0153932"));
		var expected = -2.4122350198982545;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:11.2655407"));
		expected = -0.37153223002125113;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:12.6158773"));
		expected = 0.3967885416786294;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:14.6495256"));
		expected = 1.8016358493761722;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);
	}

	[PresentationTestMethod]
	public void CheckKeySplineParsingIsCorrect()
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
			Easing = Easing.Parse(
				"0.1123555056179775,0.657303370786517,0.8370786516853934,0.499999999999999999")
		};

		var rotateTransform = new RotateTransform(-2.5);
		var rect = new Rectangle { RenderTransform = rotateTransform };

		var clock = new TestClock();

		animation.RunAsync(rect, clock, CancellationToken.None);

		// position is what you'd expect at end and beginning
		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(rotateTransform.Angle, -2.5);
		clock.Step(TimeSpan.FromSeconds(5));
		CornerstoneTest.AreEqual(rotateTransform.Angle, 2.5);

		// test some points in between end and beginning
		var tolerance = 0.01;
		clock.Step(TimeSpan.Parse("00:00:10.0153932"));
		var expected = -2.4122350198982545;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:11.2655407"));
		expected = -0.37153223002125113;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:12.6158773"));
		expected = 0.3967885416786294;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

		clock.Step(TimeSpan.Parse("00:00:14.6495256"));
		expected = 1.8016358493761722;
		CornerstoneTest.IsTrue(Math.Abs(rotateTransform.Angle - expected) <= tolerance);
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/15704
	[PresentationTestMethod]
	[DataRow(nameof(BackEaseIn))]
	[DataRow(nameof(BackEaseOut))]
	[DataRow(nameof(BackEaseInOut))]
	[DataRow(nameof(ElasticEaseIn))]
	[DataRow(nameof(ElasticEaseOut))]
	[DataRow(nameof(ElasticEaseInOut))]
	public void KeySplineProgressLessThanZeroOrGreaterThanOneWorks(string easingType)
	{
		var easing = Easing.Parse(easingType);

		var animation = new Presentation.Animation.Animation
		{
			Duration = TimeSpan.FromSeconds(1.0),
			Children =
			{
				new KeyFrame
				{
					Cue = new Cue(0.0),
					Setters = { new Setter(TranslateTransform.YProperty, 10.0) }
				},
				new KeyFrame
				{
					Cue = new Cue(1.0),
					Setters = { new Setter(TranslateTransform.YProperty, 20.0) }
				}
			},
			IterationCount = new IterationCount(5),
			PlaybackDirection = PlaybackDirection.Alternate,
			Easing = easing
		};

		var transform = new TranslateTransform(0.0, 50.0);
		var rect = new Rectangle { RenderTransform = transform };

		var clock = new TestClock();

		animation.RunAsync(rect, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(10.0, transform.Y, 0.0001);

		for (var time = TimeSpan.FromSeconds(0.1); time < animation.Duration; time += TimeSpan.FromSeconds(0.1))
		{
			clock.Step(time);
			CornerstoneTest.IsTrue(double.IsFinite(transform.Y));
			CornerstoneTest.AreNotEqual(10.0, transform.Y);
			CornerstoneTest.AreNotEqual(20.0, transform.Y);
		}

		clock.Step(animation.Duration);
		CornerstoneTest.AreEqual(20.0, transform.Y, 0.0001);
	}

	[PresentationTestMethod]
	[DataRow(nameof(BackEaseIn))]
	[DataRow(nameof(BackEaseOut))]
	[DataRow(nameof(BackEaseInOut))]
	[DataRow(nameof(ElasticEaseIn))]
	[DataRow(nameof(ElasticEaseOut))]
	[DataRow(nameof(ElasticEaseInOut))]
	public void KeySplineProgressLessThanZeroOrGreaterThanOneWorksWithSingleKeyFrame(string easingType)
	{
		var easing = Easing.Parse(easingType);

		var animation = new Presentation.Animation.Animation
		{
			Duration = TimeSpan.FromSeconds(1.0),
			Children =
			{
				new KeyFrame
				{
					Cue = new Cue(1.0),
					Setters = { new Setter(TranslateTransform.YProperty, 10.0) }
				}
			},
			IterationCount = new IterationCount(5),
			PlaybackDirection = PlaybackDirection.Alternate,
			Easing = easing
		};

		var transform = new TranslateTransform(0.0, 50.0);
		var rect = new Rectangle { RenderTransform = transform };

		var clock = new TestClock();

		animation.RunAsync(rect, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(50.0, transform.Y, 0.0001);

		for (var time = TimeSpan.FromSeconds(0.1); time < animation.Duration; time += TimeSpan.FromSeconds(0.1))
		{
			clock.Step(time);
			CornerstoneTest.IsTrue(double.IsFinite(transform.Y));
			CornerstoneTest.AreNotEqual(50.0, transform.Y);
			CornerstoneTest.AreNotEqual(10.0, transform.Y);
		}

		clock.Step(animation.Duration);
		CornerstoneTest.AreEqual(10.0, transform.Y, 0.0001);
	}

	[PresentationTestMethod]
	[DataRow(-0.01)]
	[DataRow(1.01)]
	public void KeySplineXValuesCannotBeOutOfRange(double input)
	{
		var keySpline = new KeySpline();
		Assert.Throws<ArgumentException>(() => keySpline.ControlPointX1 = input);
		Assert.Throws<ArgumentException>(() => keySpline.ControlPointX2 = input);
	}

	[PresentationTestMethod]
	[DataRow(0.00)]
	[DataRow(0.50)]
	[DataRow(1.00)]
	public void KeySplineXValuesInRangeDoNotThrow(double input)
	{
		var keySpline = new KeySpline();
		keySpline.ControlPointX1 = input; // no exception will be thrown -- test will fail if exception thrown
		keySpline.ControlPointX2 = input; // no exception will be thrown -- test will fail if exception thrown
	}

	[PresentationTestMethod]
	public void SplineEasingCanBeMutated()
	{
		var easing = new SplineEasing();

		CornerstoneTest.AreEqual(0, easing.Ease(0));
		CornerstoneTest.AreEqual(1, easing.Ease(1));

		easing.X1 = 0.25;
		easing.Y1 = 0.5;
		easing.X2 = 0.75;
		easing.Y2 = 1.0;

		CornerstoneTest.AreNotEqual(0.5, easing.Ease(0.5));
	}

	[PresentationTestMethod]
	public void SplineEasingConstructorAssignsAllControlPoints()
	{
		// y1 != y2 and y2 != 1 (KeySpline default) so both a Y1 overwrite
		// and a missing Y2 assignment would fail this test.
		var easing = new SplineEasing(0.2, 0.8, 0.4, 0.3);

		CornerstoneTest.AreEqual(0.2, easing.X1);
		CornerstoneTest.AreEqual(0.8, easing.Y1);
		CornerstoneTest.AreEqual(0.4, easing.X2);
		CornerstoneTest.AreEqual(0.3, easing.Y2);

		var expected = new SplineEasing(new KeySpline(0.2, 0.8, 0.4, 0.3));
		CornerstoneTest.AreEqual(expected.Ease(0.5), easing.Ease(0.5));
	}

	#endregion
}