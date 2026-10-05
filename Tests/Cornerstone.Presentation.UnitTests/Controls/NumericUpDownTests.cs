#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class NumericUpDownTests : ScopedTestBase
{
	#region Properties

	private static TestServices Services => TestServices.StyledWindow;

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void FormatStringIsAppliedImmediately()
	{
		RunTest((control, textbox) =>
		{
			const decimal value = 10.11m;

			// Establish and verify initial conditions.
			control.FormatString = "F0";
			control.Value = value;
			CornerstoneTest.AreEqual(value.ToString("F0"), control.Text);

			// Check that FormatString is applied.
			control.FormatString = "F2";
			CornerstoneTest.AreEqual(value.ToString("F2"), control.Text);
		});
	}

	public static IEnumerable<object[]> IncrementDecrementTestData()
	{
		// if min and max are not defined and value was null, 0 should be ne new value after spin
		yield return [decimal.MinValue, decimal.MaxValue, null, SpinDirection.Decrease, 0m];
		yield return [decimal.MinValue, decimal.MaxValue, null, SpinDirection.Increase, 0m];

		// if no value was defined, but Min or Max are defined, use these as the new value
		yield return [-400m, -200m, null, SpinDirection.Decrease, -200m];
		yield return [200m, 400m, null, SpinDirection.Increase, 200m];

		// Value should be clamped to Min / Max after spinning
		yield return [200m, 400m, 5m, SpinDirection.Increase, 200m];
		yield return [200m, 400m, 200m, SpinDirection.Decrease, 200m];
	}

	[PresentationTestMethod]
	[TestData(nameof(IncrementDecrementTestData))]
	public void IncrementDecrementTests(decimal min, decimal max, decimal? value, SpinDirection direction,
		decimal? expected)
	{
		var control = CreateControl();
		if (min > decimal.MinValue)
		{
			control.Minimum = min;
		}
		if (max < decimal.MaxValue)
		{
			control.Maximum = max;
		}
		control.Value = value;

		var spinner = GetSpinner(control);

		spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, direction));

		CornerstoneTest.AreEqual(control.Value, expected);
	}

	[PresentationTestMethod]
	public void NumberFormatIsAppliedImmediately()
	{
		RunTest((control, textbox) =>
		{
			const decimal value = 10.11m;
			var initialNumberFormat = new NumberFormatInfo { NumberDecimalSeparator = "." };
			var newNumberFormat = new NumberFormatInfo { NumberDecimalSeparator = ";" };

			// Establish and verify initial conditions.
			control.NumberFormat = initialNumberFormat;
			control.Value = value;
			CornerstoneTest.AreEqual(value.ToString(initialNumberFormat), control.Text);

			// Check that NumberFormat is applied.
			control.NumberFormat = newNumberFormat;
			CornerstoneTest.AreEqual(value.ToString(newNumberFormat), control.Text);
		});
	}

	[PresentationTestMethod]
	public void PlaceholderForegroundCanBeSet()
	{
		using (UnitTestApplication.Start(Services))
		{
			var control = CreateControl();
			control.PlaceholderText = "Enter value";
			control.PlaceholderForeground = Brushes.Red;

			CornerstoneTest.AreEqual(Brushes.Red, control.PlaceholderForeground);
		}
	}

	[PresentationTestMethod]
	public void TabIndexShouldBeSynchronizedWithInnerTextBox()
	{
		RunTest((control, textbox) =>
		{
			// Set TabIndex on NumericUpDown
			control.TabIndex = 5;

			// The inner TextBox should inherit the same TabIndex
			CornerstoneTest.AreEqual(5, textbox.TabIndex);

			// Change TabIndex and verify it gets synchronized
			control.TabIndex = 10;
			CornerstoneTest.AreEqual(10, textbox.TabIndex);
		});
	}

	[PresentationTestMethod]
	public void TextConverterIsAppliedImmediately()
	{
		RunTest((control, textbox) =>
		{
			const decimal value = 10.11m;
			var initialConverter = new TestNumericUpDownValueConverter("C2");
			var newConverter = new TestNumericUpDownValueConverter("P2");

			// Establish and verify initial conditions.
			control.Value = value;
			control.TextConverter = initialConverter;
			var oldText = control.Text ?? string.Empty;
			CornerstoneTest.AreEqual("¤10.11", oldText);

			// Check that NumberFormat is applied.
			control.TextConverter = newConverter;
			var newText = control.Text ?? string.Empty;
			CornerstoneTest.AreEqual("1,011.00 %", newText);
		});
	}

	[PresentationTestMethod]
	public void TextValidation()
	{
		RunTest((control, textbox) =>
		{
			var exception = new InvalidCastException("failed validation");
			var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
			control.Bind(NumericUpDown.TextProperty, textObservable);
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(control));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(control));
		});
	}

	[PresentationTestMethod]
	public void ValueValidation()
	{
		RunTest((control, textbox) =>
		{
			var exception = new InvalidCastException("failed validation");
			var valueObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
			control.Bind(NumericUpDown.ValueProperty, valueObservable);
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(control));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(control));
		});
	}

	private NumericUpDown CreateControl()
	{
		var control = new NumericUpDown
		{
			Template = CreateTemplate()
		};

		control.ApplyTemplate();
		return control;
	}

	private static IControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate<NumericUpDown>((control, scope) =>
		{
			var textBox =
				new TextBox
				{
					Name = "PART_TextBox"
				}.RegisterInNameScope(scope);
			return new ButtonSpinner
			{
				Name = "PART_Spinner",
				Content = textBox
			}.RegisterInNameScope(scope);
		});
	}

	private static ButtonSpinner GetSpinner(NumericUpDown control)
	{
		return control.GetTemplateDescendants()
			.OfType<ButtonSpinner>()
			.First();
	}

	private static TextBox GetTextBox(NumericUpDown control)
	{
		return control.GetTemplateDescendants()
			.OfType<ButtonSpinner>()
			.Select(b => b.Content)
			.OfType<TextBox>()
			.First();
	}

	private void RunTest(Action<NumericUpDown, TextBox> test)
	{
		using (UnitTestApplication.Start(Services))
		{
			var control = CreateControl();
			var textBox = GetTextBox(control);
			var window = new Window { Content = control };
			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			Dispatcher.UIThread.RunJobs();
			test.Invoke(control, textBox);
		}
	}

	#endregion

	#region Classes

	private class TestNumericUpDownValueConverter(string format) : IValueConverter
	{
		#region Methods

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var input = value?.ToString() ?? string.Empty;
			if (string.IsNullOrEmpty(input))
			{
				return 0m;
			}
			var numberPattern = new Regex("[0-9.,]+");
			var match = numberPattern.Matches(input).FirstOrDefault();
			if (match == null)
			{
				return 0m;
			}

			return decimal.Parse(match.Value, CultureInfo.InvariantCulture);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not decimal inputNumber)
			{
				return null;
			}
			return inputNumber.ToString(format, CultureInfo.InvariantCulture);
		}

		#endregion
	}

	#endregion
}