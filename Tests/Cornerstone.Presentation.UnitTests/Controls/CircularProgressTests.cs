#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class CircularProgressTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArcIsEmptyWhenRangeHasNoSpan()
	{
		var target = new CircularProgress
		{
			Minimum = 5,
			Maximum = 5,
			Value = 5
		};

		CornerstoneTest.AreEqual(0, target.Percentage);
		CornerstoneTest.AreEqual(0, target.SweepAngle);
	}

	[PresentationTestMethod]
	public void ArcIsEmptyWhenValueIsAtMinimum()
	{
		var target = new CircularProgress
		{
			Minimum = 10,
			Maximum = 20,
			Value = 10
		};

		CornerstoneTest.AreEqual(0, target.Percentage);
		CornerstoneTest.AreEqual(0, target.SweepAngle);
	}

	[PresentationTestMethod]
	public void ArcIsFullWhenValueIsAtMaximum()
	{
		var target = new CircularProgress { Value = 100 };

		CornerstoneTest.AreEqual(100, target.Percentage);
		CornerstoneTest.AreEqual(360, target.SweepAngle);
	}

	[PresentationTestMethod]
	public void DeclaredPropertiesRoundTrip()
	{
		var target = new CircularProgress();
		var content = new object();
		var stroke = Brushes.Red;

		CornerstoneTest.IsNull(target.Content);
		CornerstoneTest.IsFalse(target.IsIndeterminate);
		CornerstoneTest.IsFalse(target.ShowProgressText);
		CornerstoneTest.AreEqual("{1:0}%", target.ProgressTextFormat);
		CornerstoneTest.IsNull(target.Stroke);
		CornerstoneTest.AreEqual(PenLineCap.Round, target.StrokeLineCap);
		CornerstoneTest.AreEqual(8, target.StrokeThickness);
		CornerstoneTest.AreEqual(0, target.Percentage);
		CornerstoneTest.AreEqual(0, target.SweepAngle);
		CornerstoneTest.IsFalse(target.Classes.Contains(":indeterminate"));

		target.Content = content;
		target.IsIndeterminate = true;
		target.ShowProgressText = true;
		target.ProgressTextFormat = "{0}";
		target.Stroke = stroke;
		target.StrokeLineCap = PenLineCap.Flat;
		target.StrokeThickness = 4;

		CornerstoneTest.IsTrue(ReferenceEquals(content, target.Content));
		CornerstoneTest.IsTrue(target.IsIndeterminate);
		CornerstoneTest.IsTrue(target.ShowProgressText);
		CornerstoneTest.AreEqual("{0}", target.ProgressTextFormat);
		CornerstoneTest.IsTrue(ReferenceEquals(stroke, target.Stroke));
		CornerstoneTest.AreEqual(PenLineCap.Flat, target.StrokeLineCap);
		CornerstoneTest.AreEqual(4, target.StrokeThickness);
		CornerstoneTest.IsTrue(target.Classes.Contains(":indeterminate"));
	}

	[PresentationTestMethod]
	public void IndeterminateClassFollowsIsIndeterminate()
	{
		var target = new CircularProgress();

		target.IsIndeterminate = true;
		CornerstoneTest.IsTrue(target.Classes.Contains(":indeterminate"));

		target.IsIndeterminate = false;
		CornerstoneTest.IsFalse(target.Classes.Contains(":indeterminate"));
	}

	[PresentationTestMethod]
	public void MeasureUsesHeightMinusStrokeAsDiameter()
	{
		var target = new CircularProgress
		{
			StrokeThickness = 8,
			Value = 50
		};

		target.Measure(new Size(200, 100));

		CornerstoneTest.AreEqual(new Size(84, 84), target.DesiredSize);
		CornerstoneTest.AreEqual(50, target.Percentage);
		CornerstoneTest.AreEqual(180, target.SweepAngle);
	}

	[PresentationTestMethod]
	public void ValueMapsAcrossCustomRange()
	{
		var target = new CircularProgress
		{
			Minimum = 10,
			Maximum = 30,
			Value = 20
		};

		CornerstoneTest.AreEqual(50, target.Percentage);
		CornerstoneTest.AreEqual(180, target.SweepAngle);

		target.Value = 25;

		CornerstoneTest.AreEqual(75, target.Percentage);
		CornerstoneTest.AreEqual(270, target.SweepAngle);
	}

	#endregion
}