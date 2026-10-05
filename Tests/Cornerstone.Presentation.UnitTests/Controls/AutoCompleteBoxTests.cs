#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class AutoCompleteBoxTests : ScopedTestBase
{
	#region Properties

	private static TestServices FocusServices =>
		TestServices.MockThreadingInterface.With(
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler(),
			inputManager: new InputManager(),
			standardCursorFactory: new StubCursorFactory(),
			textShaperImpl: new HarfBuzzTextShaper(),
			fontManagerImpl: new HeadlessFontManagerStub());

	private static TestServices Services => TestServices.StyledWindow;

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AttemptingToOpenWithoutItemsDoesNotPreventFutureOpeningWithItems()
	{
		RunTest((control, textbox) =>
		{
			// Allow the drop down to open without anything entered.
			control.MinimumPrefixLength = 0;

			// Clear the items.
			var source = control.ItemsSource;
			control.ItemsSource = null;
			control.IsDropDownOpen = true;

			// DropDown was not actually opened because there are no items.
			CornerstoneTest.IsFalse(control.IsDropDownOpen);

			// Set the items and try to open the drop down again.
			control.ItemsSource = source;
			control.IsDropDownOpen = true;

			// DropDown can now be opened.
			CornerstoneTest.IsTrue(control.IsDropDownOpen);
		});
	}

	[PresentationTestMethod]
	public void BoundTextWillUpdateAlways()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var viewModel = new AutoCompleteBoxViewModel();

			var control = CreateControl();

			// Setup the binding
			control[!AutoCompleteBox.TextProperty] = CompiledBinding.Create<AutoCompleteBoxViewModel, string>
				(vm => vm.TextValue, viewModel, mode: BindingMode.TwoWay);

			// Ensure the bound text matches "foo"
			CornerstoneTest.AreEqual("foo", control.Text);

			// Change the view model value several times and ensure the bound text is updated
			for (var i = 0; i < 10; i++)
			{
				viewModel.UpdateTextValueTwice();
				Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
				CornerstoneTest.AreEqual("bar", control.Text);
			}
		}
	}

	[PresentationTestMethod]
	public void BoundTextWillUpdateFromBarToBarViaFoo()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var viewModel = new AutoCompleteBoxViewModel();
			viewModel.TextValue = "bar";

			var control = CreateControl();
			control.ApplyTemplate();

			// Setup the binding
			control[!AutoCompleteBox.TextProperty] = CompiledBinding.Create<AutoCompleteBoxViewModel, string>
				(vm => vm.TextValue, viewModel, mode: BindingMode.TwoWay);

			CornerstoneTest.AreEqual("bar", control.Text);

			var textChangedCount = 0;
			control.TextChanged += (s, e) => textChangedCount++;

			// Change the view model value "bar" -> "foo" -> "bar"
			viewModel.UpdateTextValueTwice();

			// Programmatic TextProperty updates should synchronously raise TextChanged, and
			// OnTextBoxTextChanged is suppressed for the corresponding TextBox.Text updates.
			CornerstoneTest.AreEqual("bar", control.Text);
			CornerstoneTest.AreEqual(2, textChangedCount);

			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual("bar", control.Text);
			CornerstoneTest.AreEqual(2, textChangedCount);
		}
	}

	[PresentationTestMethod]
	public void CanCancelDropDownClosing()
	{
		RunTest((control, textbox) =>
		{
			control.DropDownClosing += (s, e) => e.Cancel = true;

			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsTrue(control.IsDropDownOpen);

			control.IsDropDownOpen = false;
			CornerstoneTest.IsTrue(control.IsDropDownOpen);
		});
	}

	[PresentationTestMethod]
	public void CanCancelDropDownOpening()
	{
		RunTest((control, textbox) =>
		{
			control.DropDownOpening += (s, e) => e.Cancel = true;

			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsFalse(control.IsDropDownOpen);
		});
	}

	[PresentationTestMethod]
	public void CanCancelPopulation()
	{
		RunTest((control, textbox) =>
		{
			var populating = false;
			var populated = false;
			control.FilterMode = AutoCompleteFilterMode.None;
			control.Populating += (s, e) =>
			{
				e.Cancel = true;
				populating = true;
			};
			control.Populated += (s, e) => populated = true;

			textbox.Text = "accounti";
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(populating);
			CornerstoneTest.IsFalse(populated);
		});
	}

	[PresentationTestMethod]
	public void CaretIndexChanges()
	{
		var text = "Sample text";
		var expectedText = "Saple text";
		RunTest((control, textbox) =>
		{
			control.Text = text;
			control.Measure(Size.Infinity);
			Dispatcher.UIThread.RunJobs();

			textbox.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Right
			});
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.AreEqual(1, control.CaretIndex);
			CornerstoneTest.AreEqual(textbox.CaretIndex, control.CaretIndex);

			control.CaretIndex = 3;

			CornerstoneTest.AreEqual(3, control.CaretIndex);
			CornerstoneTest.AreEqual(textbox.CaretIndex, control.CaretIndex);

			textbox.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Back
			});
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.AreEqual(2, control.CaretIndex);
			CornerstoneTest.AreEqual(textbox.CaretIndex, control.CaretIndex);
			CornerstoneTest.IsTrue((control.Text == expectedText) && (textbox.Text == expectedText));
		});
	}

	[PresentationTestMethod]
	public void CustomFilterModeWithoutItemFilterSettingThrowsException()
	{
		RunTest((control, textbox) =>
		{
			control.FilterMode = AutoCompleteFilterMode.Custom;
			Assert.Throws<Exception>(() => { control.Text = "a"; });
		});
	}

	[PresentationTestMethod]
	public void CustomItemSelector()
	{
		RunTest((control, textbox) =>
		{
			CornerstoneTest.IsNotNull(control.ItemsSource);

			var selectedItem = control.ItemsSource.Cast<object>().First();
			var input = "42";

			control.ItemSelector = (text, item) => text + item;
			CornerstoneTest.AreEqual(control.ItemSelector("4", 2), "42");

			control.Text = input;
			control.SelectedItem = selectedItem;
			CornerstoneTest.AreEqual(control.Text, control.ItemSelector(input, selectedItem));
		});
	}

	[PresentationTestMethod]
	public void CustomPopulationSupported()
	{
		RunTest((control, textbox) =>
		{
			var custom = "Custom!";
			var search = "accounti";
			var populated = false;
			var populatedOk = false;
			control.FilterMode = AutoCompleteFilterMode.None;
			control.Populating += (s, e) =>
			{
				control.ItemsSource = new[] { custom };
				CornerstoneTest.AreEqual(search, e.Parameter);
			};
			control.Populated += (s, e) =>
			{
				populated = true;
				var collection = e.Data as ReadOnlyCollection<object>;
				populatedOk = (collection != null) && (collection.Count == 1);
			};

			textbox.Text = search;
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(populated);
			CornerstoneTest.IsTrue(populatedOk);
		});
	}

	[PresentationTestMethod]
	public void CustomTextSelector()
	{
		RunTest((control, textbox) =>
		{
			CornerstoneTest.IsNotNull(control.ItemsSource);

			var selectedItem = control.ItemsSource.Cast<object>().First();
			var input = "42";

			control.TextSelector = (text, item) => text + item;
			CornerstoneTest.AreEqual(control.TextSelector("4", "2"), "42");

			control.Text = input;
			control.SelectedItem = selectedItem;
			CornerstoneTest.AreEqual(control.Text, control.TextSelector(input, selectedItem.ToString()));
		});
	}

	[PresentationTestMethod]
	public void ExplicitDropdownOpenRequestMinimumPrefixLength0()
	{
		RunTest((control, textbox) =>
		{
			control.Text = "";
			control.MinimumPrefixLength = 0;
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsFalse(control.IsDropDownOpen);

			control.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Down
			});

			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(control.IsDropDownOpen);
		});
	}

	[PresentationTestMethod]
	public void FiresDropDownEvents()
	{
		RunTest((control, textbox) =>
		{
			var openEvent = false;
			var closeEvent = false;
			control.DropDownOpened += (s, e) => openEvent = true;
			control.DropDownClosed += (s, e) => closeEvent = true;
			control.ItemsSource = CreateSimpleStringArray();

			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsTrue(control.SearchText == "a");
			CornerstoneTest.IsTrue(control.IsDropDownOpen);
			CornerstoneTest.IsTrue(openEvent);

			textbox.Text = string.Empty;
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsTrue(control.SearchText == string.Empty);
			CornerstoneTest.IsFalse(control.IsDropDownOpen);
			CornerstoneTest.IsTrue(closeEvent);
		});
	}

	[PresentationTestMethod]
	public void ItemSearch()
	{
		RunTest((control, textbox) =>
		{
			control.FilterMode = AutoCompleteFilterMode.Custom;
			control.ItemFilter = (_, item) => item is string;

			// Just set to null briefly to exercise that code path
			var filter = control.ItemFilter;
			CornerstoneTest.IsNotNull(filter);
			control.ItemFilter = null;
			CornerstoneTest.IsNull(control.ItemFilter);
			control.ItemFilter = filter;
			CornerstoneTest.IsNotNull(control.ItemFilter);

			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "acc";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "cook";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "accept";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "cook";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);
		});
	}

	[PresentationTestMethod]
	public void LosingFocusClosesDropDown()
	{
		using var app = UnitTestApplication.Start(FocusServices);

		var target1 = CreateControl();
		target1.ItemsSource = CreateSimpleStringArray();
		var textBox1 = GetTextBox(target1);

		var target2 = CreateControl();

		target1.ApplyTemplate();
		target2.ApplyTemplate();

		_ = new TestRoot
		{
			Child = new StackPanel
			{
				Children = { target1, target2 }
			}
		};

		target1.Focus();
		textBox1.Text = "a";
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		CornerstoneTest.IsTrue(target1.IsDropDownOpen);

		target2.Focus();
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.IsFalse(target1.IsFocused);
		CornerstoneTest.IsFalse(target1.IsDropDownOpen);
	}

	[PresentationTestMethod]
	public void MinimumPrefixLengthWorks()
	{
		RunTest((control, textbox) =>
		{
			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsTrue(control.IsDropDownOpen);

			textbox.Text = string.Empty;
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsFalse(control.IsDropDownOpen);

			control.MinimumPrefixLength = 3;

			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsFalse(control.IsDropDownOpen);

			textbox.Text = "acc";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsTrue(control.IsDropDownOpen);
		});
	}

	[PresentationTestMethod]
	public void OpeningContextMenuDoesnotLoseSelection()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var target1 = CreateControl();
			target1.ContextMenu = new TestContextMenu();
			var textBox1 = GetTextBox(target1);
			textBox1.Text = "1234";

			var target2 = CreateControl();
			var textBox2 = GetTextBox(target2);
			textBox2.Text = "5678";

			var sp = new StackPanel();
			sp.Children.Add(target1);
			sp.Children.Add(target2);

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			var root = new TestRoot { Child = sp };

			textBox1.SelectionStart = 0;
			textBox1.SelectionEnd = 3;

			target1.Focus();
			CornerstoneTest.IsFalse(target2.IsFocused);
			CornerstoneTest.IsTrue(target1.IsFocused);

			target2.Focus();

			CornerstoneTest.AreEqual("123", textBox1.SelectedText);
		}
	}

	[PresentationTestMethod]
	public void OrdinalSearchFilters()
	{
		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinal)("am", "name"));
		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinal)("AME", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinal)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("na", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("AME", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.ContainsOrdinalCaseSensitive)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinal)("na", "na"));
		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinal)("na", "NA"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinal)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("na", "na"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("na", "NA"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.EqualsOrdinalCaseSensitive)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinal)("na", "name"));
		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinal)("NAM", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinal)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("na", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("NAM", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive)("hello", "name"));
	}

	[PresentationTestMethod]
	public void PlaceholderForegroundCanBeSet()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var control = CreateControl();
			control.PlaceholderText = "Search...";
			control.PlaceholderForeground = Brushes.Green;

			CornerstoneTest.AreEqual(Brushes.Green, control.PlaceholderForeground);
		}
	}

	[PresentationTestMethod]
	public void SearchFilters()
	{
		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.Contains)("am", "name"));
		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.Contains)("AME", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.Contains)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("na", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("AME", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.ContainsCaseSensitive)("hello", "name"));

		CornerstoneTest.IsNull(GetFilter(AutoCompleteFilterMode.Custom));
		CornerstoneTest.IsNull(GetFilter(AutoCompleteFilterMode.None));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.Equals)("na", "na"));
		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.Equals)("na", "NA"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.Equals)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("na", "na"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("na", "NA"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.EqualsCaseSensitive)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.StartsWith)("na", "name"));
		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.StartsWith)("NAM", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.StartsWith)("hello", "name"));

		CornerstoneTest.IsTrue(GetNotNullFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("na", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("NAM", "name"));
		CornerstoneTest.IsFalse(GetNotNullFilter(AutoCompleteFilterMode.StartsWithCaseSensitive)("hello", "name"));
	}

	[PresentationTestMethod]
	public void SelectedItemValidation()
	{
		RunTest((control, textbox) =>
		{
			var exception = new InvalidCastException("failed validation");
			var itemObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
			control.Bind(AutoCompleteBox.SelectedItemProperty, itemObservable);
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(control));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(control));
		});
	}

	[PresentationTestMethod]
	public void StringSearch()
	{
		RunTest((control, textbox) =>
		{
			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "acc";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "cook";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "accept";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);

			textbox.Text = "cook";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual(textbox.Text, control.Text);
		});
	}

	[PresentationTestMethod]
	public void TextChangedEventFires()
	{
		RunTest((control, textbox) =>
		{
			var textChanged = false;
			control.TextChanged += (s, e) => textChanged = true;

			textbox.Text = "a";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsTrue(textChanged);

			textChanged = false;
			control.Text = "conversati";
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsTrue(textChanged);

			textChanged = false;
			control.Text = null;
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.IsTrue(textChanged);
		});
	}

	[PresentationTestMethod]
	public void TextCompletion()
	{
		RunTest((control, textbox) =>
		{
			control.IsTextCompletionEnabled = true;
			textbox.Text = "accounti";
			textbox.SelectionStart = textbox.SelectionEnd = textbox.Text.Length;
			Dispatcher.UIThread.RunJobs();
			CornerstoneTest.AreEqual("accounti", control.SearchText);
			CornerstoneTest.AreEqual("accounting", textbox.Text);
		});
	}

	[PresentationTestMethod]
	public void TextCompletionSelectsText()
	{
		RunTest((control, textbox) =>
		{
			control.IsTextCompletionEnabled = true;

			textbox.Text = "ac";
			textbox.SelectionEnd = textbox.SelectionStart = 2;
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(control.IsDropDownOpen);
			CornerstoneTest.IsTrue(Math.Abs(textbox.SelectionEnd - textbox.SelectionStart) > 2);
		});
	}

	[PresentationTestMethod]
	public void TextCompletionViaTextProperty()
	{
		RunTest((control, textbox) =>
		{
			control.IsTextCompletionEnabled = true;

			CornerstoneTest.AreEqual(string.Empty, control.Text);
			control.Text = "close";
			CornerstoneTest.IsNotNull(control.SelectedItem);
		});
	}

	[PresentationTestMethod]
	public void TextValidation()
	{
		RunTest((control, textbox) =>
		{
			var exception = new InvalidCastException("failed validation");
			var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
			control.Bind(AutoCompleteBox.TextProperty, textObservable);
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(control));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(control));
		});
	}

	[PresentationTestMethod]
	public void TextValidationTextBoxErrorsBinding()
	{
		RunTest((control, textbox) =>
		{
			// simulate the TemplateBinding that would be used within the AutoCompleteBox control theme for the inner PART_TextBox
			//      DataValidationErrors.Errors="{TemplateBinding (DataValidationErrors.Errors)}"
			textbox.Bind(DataValidationErrors.ErrorsProperty, control.GetBindingObservable(DataValidationErrors.ErrorsProperty));

			var exception = new InvalidCastException("failed validation");
			var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
			control.Bind(AutoCompleteBox.TextProperty, textObservable);
			Dispatcher.UIThread.RunJobs();

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(control));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(control));

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(textbox));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(textbox));
		});
	}

	/*private static TestServices Services => TestServices.MockThreadingInterface.With(
		standardCursorFactory: new StubCursorFactory(),
		windowingPlatform: new MockWindowingPlatform());*/

	private AutoCompleteBox CreateControl()
	{
		var autoCompleteBox =
			new AutoCompleteBox
			{
				Template = CreateTemplate()
			};

		autoCompleteBox.ApplyTemplate();
		return autoCompleteBox;
	}

	/// <summary>
	/// Creates a large list of strings for AutoCompleteBox testing.
	/// </summary>
	/// <returns> Returns a new List of string values. </returns>
	private static IList<string> CreateSimpleStringArray()
	{
		return new List<string>
		{
			"a",
			"abide",
			"able",
			"about",
			"above",
			"absence",
			"absurd",
			"accept",
			"acceptance",
			"accepted",
			"accepting",
			"access",
			"accessed",
			"accessible",
			"accident",
			"accidentally",
			"accordance",
			"account",
			"accounting",
			"accounts",
			"accusation",
			"accustomed",
			"ache",
			"across",
			"act",
			"active",
			"actual",
			"actually",
			"ada",
			"added",
			"adding",
			"addition",
			"additional",
			"additions",
			"address",
			"addressed",
			"addresses",
			"addressing",
			"adjourn",
			"adoption",
			"advance",
			"advantage",
			"adventures",
			"advice",
			"advisable",
			"advise",
			"affair",
			"affectionately",
			"afford",
			"afore",
			"afraid",
			"after",
			"afterwards",
			"again",
			"against",
			"age",
			"aged",
			"agent",
			"ago",
			"agony",
			"agree",
			"agreed",
			"agreement",
			"ah",
			"ahem",
			"air",
			"airs",
			"ak",
			"alarm",
			"alarmed",
			"alas",
			"alice",
			"alive",
			"all",
			"allow",
			"almost",
			"alone",
			"along",
			"aloud",
			"already",
			"also",
			"alteration",
			"altered",
			"alternate",
			"alternately",
			"altogether",
			"always",
			"am",
			"ambition",
			"among",
			"an",
			"ancient",
			"and",
			"anger",
			"angrily",
			"angry",
			"animal",
			"animals",
			"ann",
			"annoy",
			"annoyed",
			"another",
			"answer",
			"answered",
			"answers",
			"antipathies",
			"anxious",
			"anxiously",
			"any",
			"anyone",
			"anything",
			"anywhere",
			"appealed",
			"appear",
			"appearance",
			"appeared",
			"appearing",
			"appears",
			"applause",
			"apple",
			"apples",
			"applicable",
			"apply",
			"approach",
			"arch",
			"archbishop",
			"arches",
			"archive",
			"are",
			"argue",
			"argued",
			"argument",
			"arguments",
			"arise",
			"arithmetic",
			"arm",
			"arms",
			"around",
			"arranged",
			"array",
			"arrived",
			"arrow",
			"arrum",
			"as",
			"ascii",
			"ashamed",
			"ask",
			"askance",
			"asked",
			"asking",
			"asleep",
			"assembled",
			"assistance",
			"associated",
			"at",
			"ate",
			"atheling",
			"atom",
			"attached",
			"attempt",
			"attempted",
			"attempts",
			"attended",
			"attending",
			"attends",
			"audibly",
			"australia",
			"author",
			"authority",
			"available",
			"avoid",
			"away",
			"awfully",
			"axes",
			"axis",
			"b",
			"baby",
			"back",
			"backs",
			"bad",
			"bag",
			"baked",
			"balanced",
			"bank",
			"banks",
			"banquet",
			"bark",
			"barking",
			"barley",
			"barrowful",
			"based",
			"bat",
			"bathing",
			"bats",
			"bawled",
			"be",
			"beak",
			"bear",
			"beast",
			"beasts",
			"beat",
			"beating",
			"beau",
			"beauti",
			"beautiful",
			"beautifully",
			"beautify",
			"became",
			"because",
			"become",
			"becoming",
			"bed",
			"beds",
			"bee",
			"been",
			"before",
			"beg",
			"began",
			"begged",
			"begin",
			"beginning",
			"begins",
			"begun",
			"behead",
			"beheaded",
			"beheading",
			"behind",
			"being",
			"believe",
			"believed",
			"bells",
			"belong",
			"belongs",
			"beloved",
			"below",
			"belt",
			"bend",
			"bent",
			"besides",
			"best",
			"better",
			"between",
			"bill",
			"binary",
			"bird",
			"birds",
			"birthday",
			"bit",
			"bite",
			"bitter",
			"blacking",
			"blades",
			"blame",
			"blasts",
			"bleeds",
			"blew",
			"blow",
			"blown",
			"blows",
			"body",
			"boldly",
			"bone",
			"bones",
			"book",
			"books",
			"boon",
			"boots",
			"bore",
			"both",
			"bother",
			"bottle",
			"bottom",
			"bough",
			"bound",
			"bowed",
			"bowing",
			"box",
			"boxed",
			"boy",
			"brain",
			"branch",
			"branches",
			"brandy",
			"brass",
			"brave",
			"breach",
			"bread",
			"break",
			"breath",
			"breathe",
			"breeze",
			"bright",
			"brightened",
			"bring",
			"bringing",
			"bristling",
			"broke",
			"broken",
			"brother",
			"brought",
			"brown",
			"brush",
			"brushing",
			"burn",
			"burning",
			"burnt",
			"burst",
			"bursting",
			"busily",
			"business",
			"business@pglaf",
			"busy",
			"but",
			"butter",
			"buttercup",
			"buttered",
			"butterfly",
			"buttons",
			"by",
			"bye",
			"c",
			"cackled",
			"cake",
			"cakes",
			"calculate",
			"calculated",
			"call",
			"called",
			"calling",
			"calmly",
			"came",
			"camomile",
			"can",
			"canary",
			"candle",
			"cannot",
			"canterbury",
			"canvas",
			"capering",
			"capital",
			"card",
			"cardboard",
			"cards",
			"care",
			"carefully",
			"cares",
			"carried",
			"carrier",
			"carroll",
			"carry",
			"carrying",
			"cart",
			"cartwheels",
			"case",
			"cat",
			"catch",
			"catching",
			"caterpillar",
			"cats",
			"cattle",
			"caucus",
			"caught",
			"cauldron",
			"cause",
			"caused",
			"cautiously",
			"cease",
			"ceiling",
			"centre",
			"certain",
			"certainly",
			"chain",
			"chains",
			"chair",
			"chance",
			"chanced",
			"change",
			"changed",
			"changes",
			"changing",
			"chapter",
			"character",
			"charge",
			"charges",
			"charitable",
			"charities",
			"chatte",
			"cheap",
			"cheated",
			"check",
			"checked",
			"checks",
			"cheeks",
			"cheered",
			"cheerfully",
			"cherry",
			"cheshire",
			"chief",
			"child",
			"childhood",
			"children",
			"chimney",
			"chimneys",
			"chin",
			"choice",
			"choke",
			"choked",
			"choking",
			"choose",
			"choosing",
			"chop",
			"chorus",
			"chose",
			"christmas",
			"chrysalis",
			"chuckled",
			"circle",
			"circumstances",
			"city",
			"civil",
			"claim",
			"clamour",
			"clapping",
			"clasped",
			"classics",
			"claws",
			"clean",
			"clear",
			"cleared",
			"clearer",
			"clearly",
			"clever",
			"climb",
			"clinging",
			"clock",
			"close",
			"closed",
			"closely",
			"closer",
			"clubs",
			"coast",
			"coaxing",
			"codes",
			"coils",
			"cold",
			"collar",
			"collected",
			"collection",
			"come",
			"comes",
			"comfits",
			"comfort",
			"comfortable",
			"comfortably",
			"coming",
			"commercial",
			"committed",
			"common",
			"commotion",
			"company",
			"compilation",
			"complained",
			"complaining",
			"completely",
			"compliance",
			"comply",
			"complying",
			"compressed",
			"computer",
			"computers",
			"concept",
			"concerning",
			"concert",
			"concluded",
			"conclusion",
			"condemn",
			"conduct",
			"confirmation",
			"confirmed",
			"confused",
			"confusing",
			"confusion",
			"conger",
			"conqueror",
			"conquest",
			"consented",
			"consequential",
			"consider",
			"considerable",
			"considered",
			"considering",
			"constant",
			"consultation",
			"contact",
			"contain",
			"containing",
			"contempt",
			"contemptuous",
			"contemptuously",
			"content",
			"continued",
			"contract",
			"contradicted",
			"contributions",
			"conversation",
			"conversations",
			"convert",
			"cook",
			"cool",
			"copied",
			"copies",
			"copy",
			"copying",
			"copyright",
			"corner",
			"corners",
			"corporation",
			"corrupt",
			"cost",
			"costs",
			"could",
			"couldn",
			"counting",
			"countries",
			"country",
			"couple",
			"couples",
			"courage",
			"course",
			"court",
			"courtiers",
			"coward",
			"crab",
			"crash",
			"crashed",
			"crawled",
			"crawling",
			"crazy",
			"created",
			"creating",
			"creation",
			"creature",
			"creatures",
			"credit",
			"creep",
			"crept",
			"cried",
			"cries",
			"crimson",
			"critical",
			"crocodile",
			"croquet",
			"croqueted",
			"croqueting",
			"cross",
			"crossed",
			"crossly",
			"crouched",
			"crowd",
			"crowded",
			"crown",
			"crumbs",
			"crust",
			"cry",
			"crying",
			"cucumber",
			"cunning",
			"cup",
			"cupboards",
			"cur",
			"curiosity",
			"curious",
			"curiouser",
			"curled",
			"curls",
			"curly",
			"currants",
			"current",
			"curtain",
			"curtsey",
			"curtseying",
			"curving",
			"cushion",
			"custard",
			"custody",
			"cut",
			"cutting"
		};
	}

	private IControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate<AutoCompleteBox>((control, scope) =>
		{
			var textBox =
				new TextBox
				{
					Name = "PART_TextBox",
					[!!TextBox.CaretIndexProperty] = control[!!AutoCompleteBox.CaretIndexProperty]
				}.RegisterInNameScope(scope);
			var listbox =
				new ListBox
				{
					Name = "PART_SelectingItemsControl"
				}.RegisterInNameScope(scope);
			var popup =
				new Popup
				{
					Name = "PART_Popup",
					PlacementTarget = control
				}.RegisterInNameScope(scope);

			var panel = new Panel();
			panel.Children.Add(textBox);
			panel.Children.Add(popup);
			panel.Children.Add(listbox);

			return panel;
		});
	}

	/// <summary>
	/// Retrieves a defined predicate filter through a new AutoCompleteBox
	/// control instance.
	/// </summary>
	/// <param name="mode"> The FilterMode of interest. </param>
	/// <returns> Returns the predicate instance. </returns>
	private static AutoCompleteFilterPredicate<string> GetFilter(AutoCompleteFilterMode mode)
	{
		return new AutoCompleteBox { FilterMode = mode }
			.TextFilter;
	}

	private static AutoCompleteFilterPredicate<string> GetNotNullFilter(AutoCompleteFilterMode mode)
	{
		var filter = GetFilter(mode);
		CornerstoneTest.IsNotNull(filter);
		return filter;
	}

	private TextBox GetTextBox(AutoCompleteBox control)
	{
		return control.GetTemplateDescendants()
			.OfType<TextBox>()
			.First();
	}

	private void RunTest(Action<AutoCompleteBox, TextBox> test)
	{
		using (UnitTestApplication.Start(Services))
		{
			var control = CreateControl();
			control.ItemsSource = CreateSimpleStringArray();
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

	private class TestContextMenu : ContextMenu
	{
		#region Constructors

		public TestContextMenu()
		{
			IsOpen = true;
		}

		#endregion
	}

	#endregion
}

[TestClass]
public class AutoCompleteBoxViewModel : INotifyPropertyChanged
{
	#region Constructors

	public AutoCompleteBoxViewModel()
	{
		TextValue = "foo";
	}

	#endregion

	#region Properties

	public string TextValue
	{
		get;
		set => SetField(ref field, value);
	}

	#endregion

	#region Methods

	public void UpdateTextValueTwice()
	{
		TextValue = "foo";
		TextValue = "bar";
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}