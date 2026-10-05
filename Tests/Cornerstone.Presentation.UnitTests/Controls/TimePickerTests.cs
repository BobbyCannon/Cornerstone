#region References

using System;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TimePickerTests : ScopedTestBase
{
	#region Properties

	private static TestServices Services =>
		TestServices.MockThreadingInterface.With(
			fontManagerImpl: new HeadlessFontManagerStub(),
			standardCursorFactory: new StubCursorFactory(),
			textShaperImpl: new HarfBuzzTextShaper(),
			renderInterface: new HeadlessPlatformRenderInterface());

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void SelectedTimeChangedShouldFireWhenSelectedTimeSet()
	{
		using (UnitTestApplication.Start(Services))
		{
			var handled = false;
			var timePicker = new TimePicker();
			timePicker.SelectedTimeChanged += (s, e) => { handled = true; };
			var value = TimeSpan.FromHours(10);
			timePicker.SelectedTime = value;
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			CornerstoneTest.IsTrue(handled);
		}
	}

	[PresentationTestMethod]
	public void SelectedTimeEnableDataValidation()
	{
		using (UnitTestApplication.Start(Services))
		{
			var handled = false;
			var timePicker = new TimePicker();

			timePicker.SelectedTimeChanged += (s, e) =>
			{
				var minTime = new TimeSpan(10, 0, 0);
				var maxTime = new TimeSpan(15, 0, 0);

				if (e.NewTime < minTime)
				{
					throw new DataValidationException($"time is less than {maxTime}");
				}

				if (e.NewTime > maxTime)
				{
					throw new DataValidationException($"time is over {maxTime}");
				}

				handled = true;
			};

			// time is less than
			Assert.Throws<DataValidationException>(() => timePicker.SelectedTime = new TimeSpan(1, 2, 3));

			// time is over
			Assert.Throws<DataValidationException>(() => timePicker.SelectedTime = new TimeSpan(21, 22, 23));

			var exception = new DataValidationException("failed validation");
			var observable =
				new BehaviorSubject<BindingNotification>(new BindingNotification(exception,
					BindingErrorType.DataValidationError));
			timePicker.Bind(TimePicker.SelectedTimeProperty, observable);

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(timePicker));

			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			timePicker.SelectedTime = new TimeSpan(11, 12, 13);
			CornerstoneTest.IsTrue(handled);
		}
	}

	[PresentationTestMethod]
	public void SelectedTimenullShouldUsePlaceholders()
	{
		using (UnitTestApplication.Start(Services))
		{
			var timePicker = new TimePicker
			{
				Template = CreateTemplate()
			};
			timePicker.ApplyTemplate();

			var desc = timePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 1); //Should be layoutroot grid & button

			var button = CornerstoneTest.IsAssignableFrom<Button>(desc.ElementAt(1));
			var container = CornerstoneTest.IsAssignableFrom<Grid>(button.Content);

			var hourTextHost = CornerstoneTest.IsAssignableFrom<Border>(container.Children[0]);
			var hourText = CornerstoneTest.IsAssignableFrom<TextBlock>(hourTextHost.Child);
			var minuteTextHost = CornerstoneTest.IsAssignableFrom<Border>(container.Children[2]);
			var minuteText = CornerstoneTest.IsAssignableFrom<TextBlock>(minuteTextHost.Child);
			var secondTextHost = CornerstoneTest.IsAssignableFrom<Border>(container.Children[4]);
			var secondText = CornerstoneTest.IsAssignableFrom<TextBlock>(secondTextHost.Child);

			var ts = TimeSpan.FromHours(10);
			timePicker.SelectedTime = ts;
			CornerstoneTest.IsNotNull(hourText.Text);
			CornerstoneTest.IsNotNull(minuteText.Text);
			CornerstoneTest.IsNotNull(secondText.Text);

			timePicker.SelectedTime = null;
			CornerstoneTest.IsNull(hourText.Text);
			CornerstoneTest.IsNull(minuteText.Text);
			CornerstoneTest.IsNull(secondText.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow("PART_HourSelector")]
	[DataRow("PART_MinuteSelector")]
	[DataRow("PART_SecondSelector")]
	public void SelectorScrollDownShouldWork(string selectorName)
	{
		TestSelectorScrolling(selectorName, panel => panel.ScrollDown());
	}

	[PresentationTestMethod]
	[DataRow("PART_HourSelector")]
	[DataRow("PART_MinuteSelector")]
	[DataRow("PART_SecondSelector")]
	public void SelectorScrollUpShouldWork(string selectorName)
	{
		TestSelectorScrolling(selectorName, panel => panel.ScrollUp());
	}

	[PresentationTestMethod]
	public void TimePickerPresenterUseSecondsEqualsFalseShouldHaveZeroSeconds()
	{
		using (UnitTestApplication.Start(Services))
		{
			var timePickerPresenter = new TimePickerPresenter
			{
				UseSeconds = false,
				Template = CreatePickerTemplate()
			};
			timePickerPresenter.ApplyTemplate();

			var panel = (Panel) timePickerPresenter.VisualChildren[0];
			var acceptBtn = (Button) panel.VisualChildren[0];

			acceptBtn.PerformClick();

			CornerstoneTest.AreEqual(0, timePickerPresenter.Time.Seconds);
		}
	}

	[PresentationTestMethod]
	public void UseSecondsEqualsFalseShouldHaveZeroSeconds()
	{
		using (UnitTestApplication.Start(Services))
		{
			var timePicker = new TimePicker
			{
				UseSeconds = false,
				Template = CreateTemplate(true)
			};
			timePicker.ApplyTemplate();

			var desc = timePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 2);

			// find button
			CornerstoneTest.IsTrue(desc.ElementAt(1) is Button);
			var btn = (Button) desc.ElementAt(1);

			CornerstoneTest.IsTrue(desc.ElementAt(2) is Popup);
			var popup = (Popup) desc.ElementAt(2);

			var timePickerPresenter = CornerstoneTest.IsAssignableFrom<TimePickerPresenter>(popup.Child);
			var panel = (Panel) timePickerPresenter.VisualChildren[0];
			var acceptBtn = (Button) panel.VisualChildren[0];

			CornerstoneTest.IsFalse(popup.IsOpen);
			btn.PerformClick();
			CornerstoneTest.IsTrue(popup.IsOpen);
			CornerstoneTest.IsFalse(timePickerPresenter.UseSeconds);

			acceptBtn.PerformClick();

			CornerstoneTest.AreEqual(0, timePickerPresenter.Time.Seconds);
			CornerstoneTest.AreEqual(0, timePicker.SelectedTime?.Seconds);
		}
	}

	[PresentationTestMethod]
	public void UseSecondsEqualsFalseShouldHideSeconds()
	{
		using (UnitTestApplication.Start(Services))
		{
			var timePicker = new TimePicker
			{
				UseSeconds = true,
				Template = CreateTemplate()
			};
			timePicker.ApplyTemplate();

			var desc = timePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 1); //Should be layoutroot grid & button

			var button = CornerstoneTest.IsAssignableFrom<Button>(desc.ElementAt(1));
			var container = CornerstoneTest.IsAssignableFrom<Grid>(button.Content);

			var periodTextHost = CornerstoneTest.IsAssignableFrom<Border>(container.Children[4]);
			CornerstoneTest.IsTrue(periodTextHost.IsVisible);

			timePicker.UseSeconds = false;
			CornerstoneTest.IsFalse(periodTextHost.IsVisible);
		}
	}

	[PresentationTestMethod]
	[UseEmptyDesignatorCulture]
	public void Using12HourClockOnCultureWithEmptyPeriodShouldShowPeriod()
	{
		using (UnitTestApplication.Start(Services))
		{
			var timePicker = new TimePicker
			{
				Template = CreateTemplate(), ClockIdentifier = "12HourClock"
			};
			timePicker.ApplyTemplate();

			var desc = timePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 1); //Should be layoutroot grid & button

			var button = CornerstoneTest.IsAssignableFrom<Button>(desc.ElementAt(1));
			var container = CornerstoneTest.IsAssignableFrom<Grid>(button.Content);

			var periodTextHost = CornerstoneTest.IsAssignableFrom<Border>(container.Children[6]);
			var periodText = CornerstoneTest.IsAssignableFrom<TextBlock>(periodTextHost.Child);

			var ts = TimeSpan.FromHours(10);
			timePicker.SelectedTime = ts;
			CornerstoneTest.IsFalse(string.IsNullOrEmpty(periodText.Text));

			timePicker.SelectedTime = null;
			CornerstoneTest.IsFalse(string.IsNullOrEmpty(periodText.Text));
		}
	}

	[PresentationTestMethod]
	public void Using24HourClockShouldHidePeriod()
	{
		using (UnitTestApplication.Start(Services))
		{
			var timePicker = new TimePicker
			{
				ClockIdentifier = "12HourClock",
				Template = CreateTemplate()
			};
			timePicker.ApplyTemplate();

			var desc = timePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 1); //Should be layoutroot grid & button

			var button = CornerstoneTest.IsAssignableFrom<Button>(desc.ElementAt(1));
			var container = CornerstoneTest.IsAssignableFrom<Grid>(button.Content);

			var periodTextHost = CornerstoneTest.IsAssignableFrom<Border>(container.Children[6]);
			CornerstoneTest.IsTrue(periodTextHost.IsVisible);

			timePicker.ClockIdentifier = "24HourClock";
			CornerstoneTest.IsFalse(periodTextHost.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void VerticalContentAlignmentDefaultIsStretch()
	{
		var timePicker = new TimePicker();
		CornerstoneTest.AreEqual(VerticalAlignment.Stretch, timePicker.VerticalContentAlignment);
	}

	[PresentationTestMethod]
	[DataRow(VerticalAlignment.Top)]
	[DataRow(VerticalAlignment.Center)]
	[DataRow(VerticalAlignment.Bottom)]
	[DataRow(VerticalAlignment.Stretch)]
	public void VerticalContentAlignmentRoundTrips(VerticalAlignment value)
	{
		var timePicker = new TimePicker { VerticalContentAlignment = value };
		CornerstoneTest.AreEqual(value, timePicker.VerticalContentAlignment);
	}

	private static IControlTemplate CreatePickerTemplate()
	{
		return new FuncControlTemplate((control, scope) =>
		{
			var acceptButton = new Button
			{
				Name = "PART_AcceptButton"
			}.RegisterInNameScope(scope);

			var hourSelector = new DateTimePickerPanel
			{
				Name = "PART_HourSelector",
				PanelType = DateTimePickerPanelType.Hour,
				ShouldLoop = true
			}.RegisterInNameScope(scope);

			var minuteSelector = new DateTimePickerPanel
			{
				Name = "PART_MinuteSelector",
				PanelType = DateTimePickerPanelType.Minute,
				ShouldLoop = true
			}.RegisterInNameScope(scope);

			var secondHost = new Panel
			{
				Name = "PART_SecondHost"
			}.RegisterInNameScope(scope);

			var secondSelector = new DateTimePickerPanel
			{
				Name = "PART_SecondSelector",
				PanelType = DateTimePickerPanelType.Second,
				ShouldLoop = true
			}.RegisterInNameScope(scope);

			var periodHost = new Panel
			{
				Name = "PART_PeriodHost"
			}.RegisterInNameScope(scope);

			var periodSelector = new DateTimePickerPanel
			{
				Name = "PART_PeriodSelector",
				PanelType = DateTimePickerPanelType.TimePeriod
			}.RegisterInNameScope(scope);

			var pickerContainer = new Grid
			{
				Name = "PART_PickerContainer"
			}.RegisterInNameScope(scope);

			var secondSpacer = new Rectangle
			{
				Name = "PART_SecondSpacer"
			}.RegisterInNameScope(scope);

			var thirdSpacer = new Rectangle
			{
				Name = "PART_ThirdSpacer"
			}.RegisterInNameScope(scope);

			var contentPanel = new Panel();
			contentPanel.Children.AddRange(new Control[] { acceptButton, hourSelector, minuteSelector, secondHost, secondSelector, periodHost, periodSelector, pickerContainer, secondSpacer, thirdSpacer });
			return contentPanel;
		});
	}

	private static IControlTemplate CreateTemplate(bool includePopup = false)
	{
		return new FuncControlTemplate((control, scope) =>
		{
			var layoutRoot = new Grid
			{
				Name = "LayoutRoot"
			}.RegisterInNameScope(scope);

			//Skip contentpresenter
			var flyoutButton = new Button
			{
				Name = "PART_FlyoutButton"
			}.RegisterInNameScope(scope);
			var contentGrid = new Grid
			{
				Name = "PART_FlyoutButtonContentGrid"
			}.RegisterInNameScope(scope);

			var firstPickerHost = new Border
			{
				Name = "PART_FirstPickerHost",
				Child = new TextBlock
				{
					Name = "PART_HourTextBlock"
				}.RegisterInNameScope(scope)
			}.RegisterInNameScope(scope);
			Grid.SetColumn(firstPickerHost, 0);

			var secondPickerHost = new Border
			{
				Name = "PART_SecondPickerHost",
				Child = new TextBlock
				{
					Name = "PART_MinuteTextBlock"
				}.RegisterInNameScope(scope)
			}.RegisterInNameScope(scope);
			Grid.SetColumn(secondPickerHost, 2);

			var thirdPickerHost = new Border
			{
				Name = "PART_ThirdPickerHost",
				Child = new TextBlock
				{
					Name = "PART_SecondTextBlock"
				}.RegisterInNameScope(scope)
			}.RegisterInNameScope(scope);
			Grid.SetColumn(thirdPickerHost, 4);

			var fourthPickerHost = new Border
			{
				Name = "PART_FourthPickerHost",
				Child = new TextBlock
				{
					Name = "PART_PeriodTextBlock"
				}.RegisterInNameScope(scope)
			}.RegisterInNameScope(scope);
			Grid.SetColumn(fourthPickerHost, 6);

			var firstSpacer = new Rectangle
			{
				Name = "PART_FirstColumnDivider"
			}.RegisterInNameScope(scope);
			Grid.SetColumn(firstSpacer, 1);

			var secondSpacer = new Rectangle
			{
				Name = "PART_SecondColumnDivider"
			}.RegisterInNameScope(scope);
			Grid.SetColumn(secondSpacer, 3);

			var thirdSpacer = new Rectangle
			{
				Name = "PART_ThirdColumnDivider"
			}.RegisterInNameScope(scope);
			Grid.SetColumn(thirdSpacer, 5);

			contentGrid.Children.AddRange(new Control[] { firstPickerHost, firstSpacer, secondPickerHost, secondSpacer, thirdPickerHost, thirdSpacer, fourthPickerHost });
			flyoutButton.Content = contentGrid;
			layoutRoot.Children.Add(flyoutButton);

			if (includePopup)
			{
				var popup = new Popup
				{
					Name = "PART_Popup"
				}.RegisterInNameScope(scope);

				var pickerPresenter = new TimePickerPresenter
				{
					Name = "PART_PickerPresenter",
					Template = CreatePickerTemplate()
				}.RegisterInNameScope(scope);
				pickerPresenter.ApplyTemplate();

				popup.Child = pickerPresenter;

				layoutRoot.Children.Add(popup);
			}

			return layoutRoot;
		});
	}

	private static void TestSelectorScrolling(string selectorName, Action<DateTimePickerPanel> scroll)
	{
		using var app = UnitTestApplication.Start(Services);

		var presenter = new TimePickerPresenter { Template = CreatePickerTemplate() };
		presenter.ApplyTemplate();
		presenter.Measure(new Size(1000, 1000));

		var panel = presenter
			.GetVisualDescendants()
			.OfType<DateTimePickerPanel>()
			.FirstOrDefault(panel => panel.Name == selectorName);

		CornerstoneTest.IsNotNull(panel);

		var previousOffset = panel.Offset;
		scroll(panel);
		CornerstoneTest.AreNotEqual(previousOffset, panel.Offset);
	}

	#endregion
}