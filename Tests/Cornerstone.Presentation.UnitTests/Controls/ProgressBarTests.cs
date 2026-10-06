#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ProgressBarTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DeclaredPropertiesRoundTrip()
	{
		var target = new ProgressBar();

		CornerstoneTest.IsFalse(target.IsIndeterminate);
		CornerstoneTest.IsFalse(target.ShowProgressText);
		CornerstoneTest.AreEqual("{1:0}%", target.ProgressTextFormat);
		CornerstoneTest.AreEqual(Orientation.Horizontal, target.Orientation);
		CornerstoneTest.AreEqual(TimeSpan.FromSeconds(1.5), target.IndeterminateDuration);
		CornerstoneTest.AreEqual(0, target.Percentage);
		CornerstoneTest.IsNotNull(target.TemplateSettings);
		CornerstoneTest.IsTrue(target.Classes.Contains(":horizontal"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":vertical"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":indeterminate"));

		target.IsIndeterminate = true;
		target.ShowProgressText = true;
		target.ProgressTextFormat = "{0}";
		target.Orientation = Orientation.Vertical;
		target.IndeterminateDuration = TimeSpan.FromSeconds(2);

		CornerstoneTest.IsTrue(target.IsIndeterminate);
		CornerstoneTest.IsTrue(target.ShowProgressText);
		CornerstoneTest.AreEqual("{0}", target.ProgressTextFormat);
		CornerstoneTest.AreEqual(Orientation.Vertical, target.Orientation);
		CornerstoneTest.AreEqual(TimeSpan.FromSeconds(2), target.IndeterminateDuration);
		CornerstoneTest.IsTrue(target.Classes.Contains(":vertical"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":horizontal"));
		CornerstoneTest.IsTrue(target.Classes.Contains(":indeterminate"));
	}

	[PresentationTestMethod]
	public void DeterminateIndicatorWidthFollowsPercentage()
	{
		var indicator = new Indicator();
		var target = new ProgressBar
		{
			Value = 25,
			Template = indicator.Template
		};

		Arrange(target, 200, 20);

		CornerstoneTest.AreEqual(25, target.Percentage);
		CornerstoneTest.AreEqual(50, indicator.Border.Width);
		CornerstoneTest.IsTrue(double.IsNaN(indicator.Border.Height));

		target.Value = 100;

		CornerstoneTest.AreEqual(100, target.Percentage);
		CornerstoneTest.AreEqual(200, indicator.Border.Width);
	}

	[PresentationTestMethod]
	public void EmptyRangeFillsTheTrack()
	{
		var indicator = new Indicator();
		var target = new ProgressBar
		{
			Minimum = 5,
			Maximum = 5,
			Value = 5,
			Template = indicator.Template
		};

		Arrange(target, 80, 10);

		CornerstoneTest.AreEqual(100, target.Percentage);
		CornerstoneTest.AreEqual(80, indicator.Border.Width);
	}

	[PresentationTestMethod]
	public void IndeterminateIndicatorUsesTrackFraction()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var indicator = new Indicator();
		var target = new ProgressBar
		{
			IsIndeterminate = true,
			Template = indicator.Template
		};

		Arrange(target, 200, 20);

		var barWidth = 200d * 0.4;
		var barWidth2 = 200d * 0.6;
		CornerstoneTest.AreEqual(barWidth, target.TemplateSettings.ContainerWidth);
		CornerstoneTest.AreEqual(barWidth2, target.TemplateSettings.Container2Width);
		CornerstoneTest.AreEqual(barWidth * -1.8, target.TemplateSettings.ContainerAnimationStartPosition);
		CornerstoneTest.AreEqual(barWidth * 3.0, target.TemplateSettings.ContainerAnimationEndPosition);
		CornerstoneTest.AreEqual(barWidth2 * -1.5, target.TemplateSettings.Container2AnimationStartPosition);
		CornerstoneTest.AreEqual(barWidth2 * 1.66, target.TemplateSettings.Container2AnimationEndPosition);
		CornerstoneTest.AreEqual(-200, target.TemplateSettings.IndeterminateStartingOffset);
		CornerstoneTest.AreEqual(200, target.TemplateSettings.IndeterminateEndingOffset);
		CornerstoneTest.AreEqual(barWidth, indicator.Border.Width);
		CornerstoneTest.AreEqual(0, target.Percentage);
	}

	[PresentationTestMethod]
	public void IndicatorMarginShrinksTheFilledWidth()
	{
		var indicator = new Indicator();
		var target = new ProgressBar
		{
			Value = 100,
			Template = indicator.Template
		};

		Arrange(target, 200, 20);
		indicator.Border.Margin = new Thickness(10, 0, 30, 0);
		target.Value = 50;

		CornerstoneTest.AreEqual(50, target.Percentage);
		CornerstoneTest.AreEqual(80, indicator.Border.Width);
	}

	[PresentationTestMethod]
	public void ValueAtMinimumClearsTheIndicator()
	{
		var indicator = new Indicator();
		var target = new ProgressBar
		{
			Minimum = 10,
			Maximum = 30,
			Value = 10,
			Template = indicator.Template
		};

		Arrange(target, 100, 16);

		CornerstoneTest.AreEqual(0, target.Percentage);
		CornerstoneTest.AreEqual(0, indicator.Border.Width);
	}

	[PresentationTestMethod]
	public void VerticalIndeterminateStartsAboveTheTrack()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var indicator = new Indicator();
		var target = new ProgressBar
		{
			IsIndeterminate = true,
			Orientation = Orientation.Vertical,
			Template = indicator.Template
		};

		Arrange(target, 16, 100);

		var transform = indicator.Border.RenderTransform as TranslateTransform;
		CornerstoneTest.IsNotNull(transform);
		CornerstoneTest.AreEqual(0, transform.X);
		CornerstoneTest.AreEqual(-100, transform.Y);
		CornerstoneTest.AreEqual(40, indicator.Border.Height);
	}

	[PresentationTestMethod]
	public void VerticalIndicatorHeightFollowsPercentage()
	{
		var indicator = new Indicator();
		var target = new ProgressBar
		{
			Orientation = Orientation.Vertical,
			Value = 50,
			Template = indicator.Template
		};

		Arrange(target, 16, 100);

		CornerstoneTest.AreEqual(50, target.Percentage);
		CornerstoneTest.IsTrue(double.IsNaN(indicator.Border.Width));
		CornerstoneTest.AreEqual(50, indicator.Border.Height);
		CornerstoneTest.IsTrue(target.Classes.Contains(":vertical"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":horizontal"));
	}

	private static void Arrange(ProgressBar target, double width, double height)
	{
		target.Measure(new Size(width, height));
		target.Arrange(new Rect(0, 0, width, height));
	}

	#endregion

	#region Classes

	private sealed class Indicator
	{
		#region Properties

		public Border Border { get; private set; }

		public FuncControlTemplate<ProgressBar> Template =>
			new((_, scope) =>
			{
				Border = new Border { Name = "PART_Indicator" };
				return Border.RegisterInNameScope(scope);
			});

		#endregion
	}

	#endregion
}