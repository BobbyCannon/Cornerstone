#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ComboBoxTests : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _helper = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ClickingOnControlPseudoClass()
	{
		var target = new ComboBox
		{
			ItemsSource = new[] { "Foo", "Bar" }
		};

		_helper.Down(target);
		CornerstoneTest.IsTrue(target.Classes.Contains(ComboBox.pcPressed));
		_helper.Up(target);
		CornerstoneTest.IsTrue(!target.Classes.Contains(ComboBox.pcPressed));
		CornerstoneTest.IsTrue(target.Classes.Contains(ComboBox.pcDropdownOpen));

		_helper.Down(target);
		CornerstoneTest.IsTrue(!target.Classes.Contains(ComboBox.pcPressed));
		_helper.Up(target);
		CornerstoneTest.IsTrue(!target.Classes.Contains(ComboBox.pcPressed));

		CornerstoneTest.IsFalse(target.IsDropDownOpen);
		CornerstoneTest.IsTrue(!target.Classes.Contains(ComboBox.pcDropdownOpen));
	}

	[PresentationTestMethod]
	public void ClickingOnControlTogglesIsDropDownOpen()
	{
		var target = new ComboBox
		{
			ItemsSource = new[] { "Foo", "Bar" }
		};

		_helper.Down(target);
		_helper.Up(target);
		CornerstoneTest.IsTrue(target.IsDropDownOpen);
		CornerstoneTest.IsTrue(target.Classes.Contains(ComboBox.pcDropdownOpen));

		_helper.Down(target);
		_helper.Up(target);

		CornerstoneTest.IsFalse(target.IsDropDownOpen);
		CornerstoneTest.IsTrue(!target.Classes.Contains(ComboBox.pcDropdownOpen));
	}

	[PresentationTestMethod]
	public void CloseWindowOnAltF4WhenComboBoxIsFocus()
	{
		var inputManagerMock = new StubInputManager();
		var services = TestServices.StyledWindow.With(inputManager: inputManagerMock);

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();

			window.KeyDown += (s, e) =>
			{
				if (!e.Handled
					&& e.KeyModifiers.HasAllFlags(KeyModifiers.Alt)
					&& (e.Key == Key.F4))
				{
					e.Handled = true;
					window.Close();
				}
			};

			var count = 0;

			var target = new ComboBox
			{
				Items = { new Canvas() },
				SelectedIndex = 0,
				Template = GetTemplate()
			};

			window.Content = target;

			window.Closing +=
				(sender, e) => { count++; };

			window.Show();

			target.Focus();

			_helper.Down(target);
			_helper.Up(target);
			CornerstoneTest.IsTrue(target.IsDropDownOpen);

			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				KeyModifiers = KeyModifiers.Alt,
				Key = Key.F4
			});

			CornerstoneTest.AreEqual(1, count);
		}
	}

	[PresentationTestMethod]
	public void DetachingClosedComboBoxKeepsCurrentFocus()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target = new ComboBox
			{
				Items = { new Canvas() },
				SelectedIndex = 0,
				Template = GetTemplate()
			};

			var other = new Control { Focusable = true };

			StackPanel panel;

			var root = new TestRoot { Child = panel = new StackPanel { Children = { target, other } } };

			target.ApplyTemplate();
			target.Presenter!.ApplyTemplate();

			other.Focus();

			CornerstoneTest.IsTrue(other.IsFocused);

			panel.Children.Remove(target);

			CornerstoneTest.IsTrue(other.IsFocused);
		}
	}

	[PresentationTestMethod]
	public void DisplayMemberBindingIsAppliedToControlItemInDropDown()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var item = new Canvas { Tag = "foo" };
		var target = new ComboBox
		{
			Items = { item },
			DisplayMemberBinding = CompiledBinding.Create((Canvas c) => c.Tag),
			SelectedIndex = 0
		};

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		target.IsDropDownOpen = true;
		window.LayoutManager.ExecuteLayoutPass();

		var container = CornerstoneTest.IsType<ComboBoxItem>(target.ContainerFromIndex(0));
		var textBlock = CornerstoneTest.IsType<TextBlock>(container.Presenter?.Child);
		CornerstoneTest.AreEqual("foo", textBlock.Text);
	}

	[PresentationTestMethod]
	public void DisplayMemberBindingIsAppliedToControlItemInSelectionBox()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var item = new Canvas { Tag = "foo" };

		var target = new ComboBox
		{
			Items = { item },
			DisplayMemberBinding = CompiledBinding.Create((Canvas c) => c.Tag),
			SelectedIndex = 0
		};

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		var selectionBoxControl = target.FindDescendantOfType<ContentControl>(
			false, c => c.ContentTemplate == target.SelectionBoxItemTemplate);
		CornerstoneTest.IsNotNull(selectionBoxControl);
		var textBlock = CornerstoneTest.IsType<TextBlock>(selectionBoxControl.Presenter?.Child);
		CornerstoneTest.AreEqual("foo", textBlock.Text);
	}

	[PresentationTestMethod]
	public void DisplayMemberBindingIsAppliedToControlItemInSelectionBoxWhenChanged()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var item = new Canvas { Tag = "foo" };
		var target = new ComboBox { Items = { item }, SelectedIndex = 0 };

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		AssertSelectionBoxItemIsVisualBrushOf(target, item);

		target.DisplayMemberBinding = CompiledBinding.Create((Canvas c) => c.Tag);
		window.LayoutManager.ExecuteLayoutPass();

		var textBlock = CornerstoneTest.IsType<TextBlock>(GetSelectionBoxControl(target).Presenter?.Child);
		CornerstoneTest.AreEqual("foo", textBlock.Text);

		target.DisplayMemberBinding = null;
		window.LayoutManager.ExecuteLayoutPass();

		AssertSelectionBoxItemIsVisualBrushOf(target, item);
	}

	[PresentationTestMethod]
	public void DisplayMemberBindingIsNotAppliedToSelectionBoxItemWithoutSelection()
	{
		var target = new ComboBox
		{
			DisplayMemberBinding = new Binding(),
			ItemsSource = new[] { "foo", "bar" }
		};

		target.SelectedItem = null;
		CornerstoneTest.IsNull(target.SelectionBoxItem);

		target.SelectedItem = "foo";
		CornerstoneTest.IsNotNull(target.SelectionBoxItem);

		target.SelectedItem = null;
		CornerstoneTest.IsNull(target.SelectionBoxItem);
	}

	[PresentationTestMethod]
	public void FlowDirectionOfRectangleContentShouldBeLeftToRight()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new ComboBox
		{
			FlowDirection = FlowDirection.RightToLeft,
			Items =
			{
				new ComboBoxItem
				{
					Content = new Control()
				}
			},
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();
		target.SelectedIndex = 0;

		var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
		CornerstoneTest.IsNotNull(rectangle);
		CornerstoneTest.AreEqual(FlowDirection.LeftToRight, rectangle.FlowDirection);
	}

	[PresentationTestMethod]
	public void FlowDirectionOfRectangleContentUpdatedAfterInvalidateMirrorTransform()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var parentContent = new Decorator
		{
			Child = new Control()
		};
		var target = new ComboBox
		{
			Items =
			{
				new ComboBoxItem
				{
					Content = parentContent.Child
				}
			},
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();
		target.SelectedIndex = 0;

		var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
		CornerstoneTest.IsNotNull(rectangle);
		CornerstoneTest.AreEqual(FlowDirection.LeftToRight, rectangle.FlowDirection);

		parentContent.FlowDirection = FlowDirection.RightToLeft;
		target.FlowDirection = FlowDirection.RightToLeft;

		CornerstoneTest.AreEqual(FlowDirection.RightToLeft, rectangle.FlowDirection);
	}

	[PresentationTestMethod]
	public void FlowDirectionOfRectangleContentUpdatedAfterOpenPopup()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parentContent = new Decorator
			{
				Child = new Control()
			};
			var target = new ComboBox
			{
				FlowDirection = FlowDirection.RightToLeft,
				Items =
				{
					new ComboBoxItem
					{
						Content = parentContent.Child,
						Template = null // ugly hack, so we can "attach" same child to the two different trees
					}
				},
				Template = GetTemplate()
			};

			var root = new TestRoot(target);
			target.ApplyTemplate();
			target.SelectedIndex = 0;

			var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
			CornerstoneTest.IsNotNull(rectangle);
			CornerstoneTest.AreEqual(FlowDirection.LeftToRight, rectangle.FlowDirection);

			parentContent.FlowDirection = FlowDirection.RightToLeft;

			var popup = target.GetVisualDescendants().OfType<Popup>().First();
			popup.PlacementTarget = new Window();
			popup.Open();

			CornerstoneTest.AreEqual(FlowDirection.RightToLeft, rectangle.FlowDirection);
		}
	}

	[PresentationTestMethod]
	public void FocusesNextItemOnKeyDown()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target = new ComboBox
			{
				Items =
				{
					new ComboBoxItem { Content = "bla" },
					new ComboBoxItem { Content = "dd", IsEnabled = false },
					new ComboBoxItem { Content = "sdf" }
				},
				Template = GetTemplate()
			};
			var root = new TestRoot(target);
			target.ApplyTemplate();
			target.Presenter!.ApplyTemplate();
			target.Focus();
			CornerstoneTest.AreEqual(target.SelectedIndex, -1);
			CornerstoneTest.IsTrue(target.IsFocused);
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Down
			});
			CornerstoneTest.AreEqual(target.SelectedIndex, 0);
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Down
			});
			CornerstoneTest.AreEqual(target.SelectedIndex, 2);
		}
	}

	[PresentationTestMethod]
	public void ItemTemplateIsAppliedToControlItemInDropDown()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var item = new Canvas();
		var target = new ComboBox
		{
			Items = { item },
			ItemTemplate = new FuncDataTemplate<object>((x, _) => new TextBlock { Tag = x }),
			SelectedIndex = 0
		};

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		target.IsDropDownOpen = true;
		window.LayoutManager.ExecuteLayoutPass();

		var container = CornerstoneTest.IsType<ComboBoxItem>(target.ContainerFromIndex(0));
		var textBlock = CornerstoneTest.IsType<TextBlock>(container.Presenter?.Child);
		CornerstoneTest.Same(item, textBlock.Tag);
	}

	[PresentationTestMethod]
	public void ItemTemplateIsAppliedToControlItemInSelectionBox()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var item = new Canvas();
		var target = new ComboBox
		{
			Items = { item },
			ItemTemplate = new FuncDataTemplate<object>((x, _) => new TextBlock { Tag = x }),
			SelectedIndex = 0
		};

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		var selectionBoxControl = target.FindDescendantOfType<ContentControl>(
			false, c => c.ContentTemplate == target.SelectionBoxItemTemplate);
		CornerstoneTest.IsNotNull(selectionBoxControl);
		var textBlock = CornerstoneTest.IsType<TextBlock>(selectionBoxControl.Presenter?.Child);
		CornerstoneTest.Same(item, textBlock.Tag);
	}

	[PresentationTestMethod]
	public void ItemTemplateIsAppliedToControlItemInSelectionBoxWhenChanged()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var item = new Canvas();
		var target = new ComboBox { Items = { item }, SelectedIndex = 0 };

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		AssertSelectionBoxItemIsVisualBrushOf(target, item);

		target.ItemTemplate = new FuncDataTemplate<object>((x, _) => new TextBlock { Tag = x });
		window.LayoutManager.ExecuteLayoutPass();

		var textBlock = CornerstoneTest.IsType<TextBlock>(GetSelectionBoxControl(target).Presenter?.Child);
		CornerstoneTest.Same(item, textBlock.Tag);

		target.ItemTemplate = null;
		window.LayoutManager.ExecuteLayoutPass();

		AssertSelectionBoxItemIsVisualBrushOf(target, item);
	}

	[PresentationTestMethod]
	public void ReopeningDropDownFocusesSelectedItemAfterScrolledToTop()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow.With(
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler()));

		var target = new ComboBox { Template = GetTemplate() };

		for (var i = 0; i < 100; ++i)
		{
			var item = new ComboBoxItem { Content = $"Item {i}" };
			target.Items.Add(item);
		}

		var selectedItem = target.Items[60] as ComboBoxItem;
		CornerstoneTest.IsTrue(selectedItem != null);

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();
		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		var scrollViewer = target.GetVisualDescendants().OfType<ScrollViewer>().First();
		CornerstoneTest.IsTrue(scrollViewer != null);

		target.SelectedItem = selectedItem;
		target.Focus();
		target.IsDropDownOpen = true;
		window.LayoutManager.ExecuteLayoutPass();

		scrollViewer.ScrollToHome();
		target.IsDropDownOpen = false;
		window.LayoutManager.ExecuteLayoutPass();

		target.IsDropDownOpen = true;
		window.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(selectedItem.IsFocused && selectedItem.IsVisible);
	}

	[PresentationTestMethod]
	public void SelectedItemValidation()
	{
		using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
		{
			var target = new ComboBox
			{
				Template = GetTemplate()
			};

			target.ApplyTemplate();
			target.Presenter!.ApplyTemplate();

			var exception = new InvalidCastException("failed validation");
			var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
			target.Bind(ComboBox.SelectedItemProperty, textObservable);

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(target));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(target));
		}
	}

	[PresentationTestMethod]
	public void SelectionBoxItemIsRectangleWithVisualBrushWhenSelectionIsControl()
	{
		var target = new ComboBox
		{
			Items = { new Canvas() },
			SelectedIndex = 0
		};
		var root = new TestRoot(target);

		var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
		CornerstoneTest.IsNotNull(rectangle);

		var brush = rectangle.Fill as VisualBrush;
		CornerstoneTest.IsNotNull(brush);
		CornerstoneTest.Same(target.Items[0], brush.Visual);
	}

	[PresentationTestMethod]
	public void SelectionBoxItemRectangleIsRemovedFromLogicalTree()
	{
		var target = new ComboBox
		{
			Items = { new Canvas() },
			SelectedIndex = 0,
			Template = GetTemplate()
		};

		var root = new TestRoot { Child = target };
		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
		CornerstoneTest.IsNotNull(rectangle);
		CornerstoneTest.IsTrue(((ILogical) target).IsAttachedToLogicalTree);
		CornerstoneTest.IsTrue(((ILogical) rectangle).IsAttachedToLogicalTree);

		rectangle.DetachedFromLogicalTree += (s, e) => { };

		root.Child = null;

		CornerstoneTest.IsFalse(((ILogical) target).IsAttachedToLogicalTree);
		CornerstoneTest.IsFalse(((ILogical) rectangle).IsAttachedToLogicalTree);
	}

	[PresentationTestMethod]
	public void SelectionBoxItemTemplateInheritsFromItemTemplateWhenItemTemplateChanged()
	{
		IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
		IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
		IDataTemplate itemTemplate2 = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "?" });
		var target = new ComboBox { ItemsSource = new[] { "Foo" }, ItemTemplate = itemTemplate };

		CornerstoneTest.AreEqual(itemTemplate, target.SelectionBoxItemTemplate);

		target.ItemTemplate = itemTemplate2;
		target.SelectionBoxItemTemplate = null;

		CornerstoneTest.AreEqual(itemTemplate2, target.SelectionBoxItemTemplate);
	}

	[PresentationTestMethod]
	public void SelectionBoxItemTemplateInheritsFromItemTemplateWhenNotSet()
	{
		IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
		var target = new ComboBox
		{
			ItemsSource = new[] { "Foo" },
			ItemTemplate = itemTemplate
		};

		CornerstoneTest.AreEqual(itemTemplate, target.SelectionBoxItemTemplate);
	}

	[PresentationTestMethod]
	public void SelectionBoxItemTemplateIsAppliedToControlItemInSelectionBox()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var item = new Canvas();
		var target = new ComboBox
		{
			Items = { item },
			SelectionBoxItemTemplate = new FuncDataTemplate<object>((x, _) => new TextBlock { Tag = x }),
			SelectedIndex = 0
		};

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		var selectionBoxControl = target.FindDescendantOfType<ContentControl>(
			false, c => c.ContentTemplate == target.SelectionBoxItemTemplate);
		CornerstoneTest.IsNotNull(selectionBoxControl);
		var textBlock = CornerstoneTest.IsType<TextBlock>(selectionBoxControl.Presenter?.Child);
		CornerstoneTest.Same(item, textBlock.Tag);
	}

	[PresentationTestMethod]
	public void SelectionBoxItemTemplateIsAppliedToControlItemInSelectionBoxWhenChanged()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var item = new Canvas();
		var target = new ComboBox { Items = { item }, SelectedIndex = 0 };

		var window = new Window { Content = target };
		window.Show();
		window.LayoutManager.ExecuteInitialLayoutPass();

		AssertSelectionBoxItemIsVisualBrushOf(target, item);

		target.SelectionBoxItemTemplate = new FuncDataTemplate<object>((x, _) => new TextBlock { Tag = x });
		window.LayoutManager.ExecuteLayoutPass();

		var textBlock = CornerstoneTest.IsType<TextBlock>(GetSelectionBoxControl(target).Presenter?.Child);
		CornerstoneTest.Same(item, textBlock.Tag);

		target.SelectionBoxItemTemplate = null;
		window.LayoutManager.ExecuteLayoutPass();

		AssertSelectionBoxItemIsVisualBrushOf(target, item);
	}

	[PresentationTestMethod]
	public void SelectionBoxItemTemplateOverridesItemTemplate()
	{
		IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
		IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
		var target = new ComboBox
		{
			ItemsSource = new[] { "Foo" },
			SelectionBoxItemTemplate = selectionBoxItemTemplate,
			ItemTemplate = itemTemplate
		};

		CornerstoneTest.AreEqual(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);
	}

	[PresentationTestMethod]
	public void SelectionBoxItemTemplateOverridesItemTemplateAfterItemTemplateChanged()
	{
		IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
		IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
		IDataTemplate itemTemplate2 = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "?" });
		var target = new ComboBox
		{
			ItemsSource = new[] { "Foo" },
			SelectionBoxItemTemplate = selectionBoxItemTemplate,
			ItemTemplate = itemTemplate
		};

		CornerstoneTest.AreEqual(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);

		target.ItemTemplate = itemTemplate2;

		CornerstoneTest.AreEqual(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);
	}

	[PresentationTestMethod]
	[DataRow(-1, 2, "c", "A item", "B item", "C item")]
	[DataRow(0, 1, "b", "A item", "B item", "C item")]
	[DataRow(2, 2, "x", "A item", "B item", "C item")]
	[DataRow(0, 34, "y", "0 item", "1 item", "2 item", "3 item", "4 item", "5 item", "6 item", "7 item", "8 item", "9 item", "A item", "B item", "C item", "D item", "E item", "F item", "G item", "H item", "I item", "J item", "K item", "L item", "M item", "N item", "O item", "P item", "Q item", "R item", "S item", "T item", "U item", "V item", "W item", "X item", "Y item", "Z item")]
	public void TextSearchShouldHaveExpectedSelectedIndex(
		int initialSelectedIndex,
		int expectedSelectedIndex,
		string searchTerm,
		params string[] contents)
	{
		TestTextSearch(
			initialSelectedIndex,
			expectedSelectedIndex,
			searchTerm,
			_ => { },
			contents.Select(content => new ComboBoxItem { Content = content }));
	}

	[PresentationTestMethod]
	[DataRow(-1, 1, "c", new[] { "A item", "B item", "C item" }, new[] { "B search", "C search", "A search" })]
	[DataRow(0, 2, "baz", new[] { "A item", "B item", "C item" }, new[] { "foo", "bar", "baz" })]
	public void TextSearchWithDisplayMemberBindingShouldHaveExpectedSelectedIndex(
		int initialSelectedIndex,
		int expectedSelectedIndex,
		string searchTerm,
		string[] values,
		string[] displays)
	{
		CornerstoneTest.AreEqual(values.Length, displays.Length);

		TestTextSearch(
			initialSelectedIndex,
			expectedSelectedIndex,
			searchTerm,
			comboBox => comboBox.DisplayMemberBinding = new Binding(nameof(Item.Display)),
			values.Select((value, index) => new Item(value, displays[index])));
	}

	[PresentationTestMethod]
	[DataRow(-1, 1, "c", new[] { "A item", "B item", "C item" }, new[] { "B search", "C search", "A search" })]
	[DataRow(0, 2, "baz", new[] { "A item", "B item", "C item" }, new[] { "foo", "bar", "baz" })]
	public void TextSearchWithTextSearchBindingShouldHaveExpectedSelectedIndex(
		int initialSelectedIndex,
		int expectedSelectedIndex,
		string searchTerm,
		string[] values,
		string[] displays)
	{
		CornerstoneTest.AreEqual(values.Length, displays.Length);

		TestTextSearch(
			initialSelectedIndex,
			expectedSelectedIndex,
			searchTerm,
			comboBox => TextSearch.SetTextBinding(comboBox, new Binding(nameof(Item.Display))),
			values.Select((value, index) => new Item(value, displays[index])));
	}

	[PresentationTestMethod]
	[DataRow(-1, 1, "c", new[] { "A item", "B item", "C item" }, new[] { "B search", "C search", "A search" })]
	[DataRow(0, 2, "baz", new[] { "A item", "B item", "C item" }, new[] { "foo", "bar", "baz" })]
	public void TextSearchWithTextSearchTextShouldHaveExpectedSelectedIndex(
		int initialSelectedIndex,
		int expectedSelectedIndex,
		string searchTerm,
		string[] contents,
		string[] searchTexts)
	{
		CornerstoneTest.AreEqual(contents.Length, searchTexts.Length);

		TestTextSearch(
			initialSelectedIndex,
			expectedSelectedIndex,
			searchTerm,
			_ => { },
			contents.Select((item, index) =>
			{
				var comboBoxItem = new ComboBoxItem { Content = item };
				TextSearch.SetText(comboBoxItem, searchTexts[index]);
				return comboBoxItem;
			}));
	}

	[PresentationTestMethod]
	public void TextValidation()
	{
		using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
		{
			var target = new ComboBox
			{
				Template = GetTemplate()
			};

			target.ApplyTemplate();
			target.Presenter!.ApplyTemplate();

			var exception = new InvalidCastException("failed validation");
			var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
			target.Bind(ComboBox.TextProperty, textObservable);

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(target));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(target));
		}
	}

	[PresentationTestMethod]
	public void WhenEditableAndItemSelectedViaTextThenFocusSwapsViaTabSwappingBackShouldFocusTextBox()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);

		var items = new[]
		{
			new Item("Value 1", "Display 1"),
			new Item("Value 2", "Display 2")
		};
		var target = new ComboBox
		{
			DisplayMemberBinding = new Binding("Display"),
			IsEditable = true,
			IsTabStop = false,
			ItemsSource = items,
			Template = GetTemplate()
		};
		TextSearch.SetTextBinding(target, new Binding("Value"));
		KeyboardNavigation.SetTabNavigation(target, KeyboardNavigationMode.Local);

		var previousControl = new ComboBox
		{
			ItemsSource = new[] { "Baz" }
		};

		var container = new StackPanel
		{
			Children =
			{
				previousControl,
				target
			}
		};
		var root = new TestRoot(container);
		var keyboardNavHandler = new KeyboardNavigationHandler();
		keyboardNavHandler.SetOwner(root);

		target.ApplyTemplate();
		target.Presenter!.ApplyTemplate();

		var containerPanel = target.GetTemplateDescendants().OfType<Panel>().FirstOrDefault(x => x.Name == "container");
		var editableTextBox = containerPanel?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(x => x.Name == "PART_EditableTextBox");
		var popup = containerPanel?.GetVisualDescendants().OfType<Popup>().FirstOrDefault(x => x.Name == "PART_Popup");
		var popupScrollViewer = popup?.Child as ScrollViewer;
		var scrollViewerItemsPresenter = popupScrollViewer?.Content as ItemsPresenter;
		var popupVirtualizingStackPanel = scrollViewerItemsPresenter?.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();

		CornerstoneTest.IsNotNull(editableTextBox);
		CornerstoneTest.IsNotNull(scrollViewerItemsPresenter);
		CornerstoneTest.IsNotNull(popupVirtualizingStackPanel);

		//force the popup to render the ComboBoxItem(s) as they are what get set as "focused" if this test fails
		popupVirtualizingStackPanel.Measure(Size.Infinity);

		target.Focus();
		CornerstoneTest.IsTrue(editableTextBox.IsFocused);

		target.Text = "Value 1";
		CornerstoneTest.Same(target.SelectedItem, items[0]);
		var item1 = scrollViewerItemsPresenter.ContainerFromIndex(0);
		CornerstoneTest.IsType<ComboBoxItem>(item1);

		RaiseTabKeyPress(target, true);

		CornerstoneTest.IsFalse(target.IsFocused);
		CornerstoneTest.IsTrue(previousControl.IsFocused);

		RaiseTabKeyPress(previousControl);

		var focused = root.FocusManager.GetFocusedElement();
		CornerstoneTest.Same(editableTextBox, focused);
	}

	[PresentationTestMethod]
	public void WhenEditableInputTextMatchesAnItemItIsSelected()
	{
		var target = new ComboBox
		{
			DisplayMemberBinding = new Binding(),
			IsEditable = true,
			ItemsSource = new[] { "foo", "bar" }
		};

		target.SelectedItem = null;
		CornerstoneTest.IsNull(target.SelectedItem);

		target.Text = "foo";
		CornerstoneTest.IsNotNull(target.SelectedItem);
		CornerstoneTest.AreEqual(target.SelectedItem, "foo");
	}

	[PresentationTestMethod]
	public void WhenEditableTextSearchTextBindingIsPrioritisedOverDisplayMember()
	{
		var items = new[]
		{
			new Item("Value 1", "Display 1"),
			new Item("Value 2", "Display 2")
		};
		var target = new ComboBox
		{
			DisplayMemberBinding = new Binding("Display"),
			IsEditable = true,
			ItemsSource = items
		};
		TextSearch.SetTextBinding(target, new Binding("Value"));

		target.SelectedItem = null;
		CornerstoneTest.IsNull(target.SelectedItem);

		target.Text = "Value 1";
		CornerstoneTest.IsNotNull(target.SelectedItem);
		CornerstoneTest.AreEqual(target.SelectedItem, items[0]);
	}

	[PresentationTestMethod]
	public void WhenItemsSourceChangesItSelectsAnItemByText()
	{
		var items = new[]
		{
			new Item("Value 1", "Display 1"),
			new Item("Value 2", "Display 2")
		};
		var items2 = new[]
		{
			new Item("Value 1", "Display 3"),
			new Item("Value 2", "Display 4")
		};
		var target = new ComboBox
		{
			DisplayMemberBinding = new Binding("Display"),
			IsEditable = true,
			ItemsSource = items
		};
		TextSearch.SetTextBinding(target, new Binding("Value"));

		target.SelectedItem = null;
		CornerstoneTest.IsNull(target.SelectedItem);

		target.Text = "Value 1";
		CornerstoneTest.IsNotNull(target.SelectedItem);
		CornerstoneTest.AreEqual(target.SelectedItem, items[0]);

		target.ItemsSource = items2;
		CornerstoneTest.IsNotNull(target.SelectedItem);
		CornerstoneTest.AreEqual(target.SelectedItem, items2[0]);
		CornerstoneTest.AreEqual(target.Text, "Value 1");
	}

	[PresentationTestMethod]
	public void WhenTabbingOutWithDropdownOpenItCloses()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);

		var target = new ComboBox
		{
			ItemsSource = new[] { "Foo", "Bar" }
		};
		var nextControl = new ComboBox
		{
			ItemsSource = new[] { "Baz" }
		};

		var container = new StackPanel
		{
			Children =
			{
				target,
				nextControl
			}
		};
		var root = new TestRoot(container);
		var keyboardNavHandler = new KeyboardNavigationHandler();
		keyboardNavHandler.SetOwner(root);

		target.Focus();
		_helper.Down(target);
		_helper.Up(target);
		CornerstoneTest.IsTrue(target.IsFocused);
		CornerstoneTest.IsTrue(target.IsDropDownOpen);

		RaiseTabKeyPress(target);

		CornerstoneTest.IsFalse(target.IsFocused);
		CornerstoneTest.IsTrue(nextControl.IsFocused);
		CornerstoneTest.IsFalse(target.IsDropDownOpen);
	}

	[PresentationTestMethod]
	public void WrapSelectionShouldWork()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target = new ComboBox
			{
				Items =
				{
					new ComboBoxItem { Content = "bla" },
					new ComboBoxItem { Content = "dd" },
					new ComboBoxItem { Content = "sdf", IsEnabled = false }
				},
				Template = GetTemplate(),
				WrapSelection = true
			};
			var root = new TestRoot(target);
			target.ApplyTemplate();
			target.Presenter!.ApplyTemplate();
			target.Focus();
			CornerstoneTest.AreEqual(target.SelectedIndex, -1);
			CornerstoneTest.IsTrue(target.IsFocused);
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Up
			});
			CornerstoneTest.AreEqual(target.SelectedIndex, 1);
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Down
			});
			CornerstoneTest.AreEqual(target.SelectedIndex, 0);
		}
	}

	private static void AssertSelectionBoxItemIsVisualBrushOf(ComboBox target, Control item)
	{
		var rectangle = CornerstoneTest.IsType<Rectangle>(target.SelectionBoxItem);
		var visualBrush = CornerstoneTest.IsType<VisualBrush>(rectangle.Fill);
		CornerstoneTest.Same(item, visualBrush.Visual);
	}

	private static ContentControl GetSelectionBoxControl(ComboBox target)
	{
		var selectionBoxControl = target.FindDescendantOfType<ContentControl>(
			false, c => c.ContentTemplate == target.SelectionBoxItemTemplate);
		CornerstoneTest.IsNotNull(selectionBoxControl);
		return selectionBoxControl;
	}

	private static FuncControlTemplate GetTemplate()
	{
		return new FuncControlTemplate<ComboBox>((parent, scope) =>
		{
			return new Panel
			{
				Name = "container",
				Children =
				{
					new ContentControl
					{
						[!ContentControl.ContentProperty] = parent[!ComboBox.SelectionBoxItemProperty]
					},
					new Popup
					{
						Name = "PART_Popup",
						[!!Popup.IsOpenProperty] = parent[!!ComboBox.IsDropDownOpenProperty],
						Child = new ScrollViewer
						{
							Name = "PART_ScrollViewer",
							Content = new ItemsPresenter
							{
								Name = "PART_ItemsPresenter",
								ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel())
							}.RegisterInNameScope(scope)
						}.RegisterInNameScope(scope)
					}.RegisterInNameScope(scope),
					new TextBox
					{
						Name = "PART_EditableTextBox"
					}.RegisterInNameScope(scope)
				}
			};
		});
	}

	private void RaiseTabKeyPress(Control target, bool withShift = false)
	{
		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Tab,
			KeyModifiers = withShift ? KeyModifiers.Shift : KeyModifiers.None
		});

		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyUpEvent,
			Key = Key.Tab,
			KeyModifiers = withShift ? KeyModifiers.Shift : KeyModifiers.None
		});
	}

	private static void TestTextSearch(
		int initialSelectedIndex,
		int expectedSelectedIndex,
		string searchTerm,
		Action<ComboBox> configureComboBox,
		IEnumerable<object> itemsSource)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new ComboBox
			{
				Template = GetTemplate(),
				ItemsSource = itemsSource.ToArray()
			};

			configureComboBox(target);

			TestRoot root = new(target)
			{
				ClientSize = new(500, 500)
			};

			root.LayoutManager.ExecuteInitialLayoutPass();
			target.SelectedIndex = initialSelectedIndex;

			var args = new TextInputEventArgs
			{
				Text = searchTerm,
				RoutedEvent = InputElement.TextInputEvent
			};

			target.RaiseEvent(args);

			CornerstoneTest.AreEqual(expectedSelectedIndex, target.SelectedIndex);
		}
	}

	#endregion

	#region Records

	private sealed record Item(string Value, string Display);

	#endregion
}