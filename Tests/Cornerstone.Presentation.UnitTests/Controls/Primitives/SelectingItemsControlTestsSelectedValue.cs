#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class SelectingItemsControlTestsSelectedValue : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingItemsShouldClearSelectedValue()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = TestClass.GetItems();
			var sic = new SelectingItemsControl
			{
				ItemsSource = items,
				Template = Template(),
				SelectedValueBinding = new Binding("Name"),
				SelectedValue = "Item2"
			};

			Prepare(sic);

			sic.ItemsSource = new List<TestClass>
			{
				new("NewItem", string.Empty)
			};

			CornerstoneTest.AreEqual(null, sic.SelectedValue);
		}
	}

	[PresentationTestMethod]
	public void ChangingSelectedValueBindingUpdatesSelectedValue()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = TestClass.GetItems();
			var sic = new SelectingItemsControl
			{
				ItemsSource = items,
				SelectedValueBinding = new Binding("Name"),
				Template = Template()
			};

			sic.SelectedValue = "Item2";

			sic.SelectedValueBinding = new Binding("AltProperty");

			// Ensure SelectedItem didn't change
			CornerstoneTest.AreEqual(items[2], sic.SelectedItem);

			CornerstoneTest.AreEqual("Alt2", sic.SelectedValue);
		}
	}

	[PresentationTestMethod]
	public void ClearingSelectedValueShouldRaiseSelectionChangedEvent()
	{
		var items = TestClass.GetItems();
		var sic = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template(),
			SelectedValueBinding = new Binding("Name"),
			SelectedValue = "Item2"
		};

		var called = false;
		sic.SelectionChanged += (s, e) =>
		{
			CornerstoneTest.Same(items[2], e.RemovedItems.Cast<object>().Single());
			CornerstoneTest.Empty(e.AddedItems);
			called = true;
		};

		sic.SelectedValue = null;
		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void HandlesNullSelectedItemWhenSelectedValueBindingAssigned()
	{
		// Issue #11220
		var items = new object[] { null };
		var sic = new SelectingItemsControl
		{
			ItemsSource = items,
			SelectedIndex = 1,
			SelectedValueBinding = new Binding("Name"),
			Template = Template()
		};

		CornerstoneTest.IsNull(sic.SelectedValue);
	}

	[PresentationTestMethod]
	public void SelectedValueWithNullSelectedValueBindingIsItem()
	{
		var items = TestClass.GetItems();
		var sic = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template()
		};

		sic.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1], sic.SelectedValue);
	}

	[PresentationTestMethod]
	public void SettingSelectedIndexSetsSelectedValue()
	{
		var items = TestClass.GetItems();
		var sic = new SelectingItemsControl
		{
			ItemsSource = items,
			SelectedValueBinding = new Binding("Name"),
			Template = Template()
		};

		sic.SelectedIndex = 1;

		CornerstoneTest.AreEqual(items[1].Name, sic.SelectedValue);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemSetsSelectedValue()
	{
		var items = TestClass.GetItems();
		var sic = new SelectingItemsControl
		{
			ItemsSource = items,
			SelectedValueBinding = new Binding("Name"),
			Template = Template()
		};

		sic.SelectedItem = items[1];

		CornerstoneTest.AreEqual(items[1].Name, sic.SelectedValue);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemsSetsSelectedValue()
	{
		var items = TestClass.GetItems();
		var sic = new ListBox
		{
			ItemsSource = items,
			SelectedValueBinding = new Binding("Name"),
			Template = Template()
		};

		sic.SelectedItems = new List<TestClass>
		{
			items[2],
			items[4],
			items[5]
		};

		// When interacting, SelectedItem is the first item in the SelectedItems collection
		// But when set here, it's the last
		CornerstoneTest.AreEqual(items[5].Name, sic.SelectedValue);
	}

	[PresentationTestMethod]
	public void SettingSelectedValueBeforeInitializeShouldRetainSelection()
	{
		var items = TestClass.GetItems();
		var sic = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template(),
			SelectedValueBinding = new Binding("Name"),
			SelectedValue = "Item2"
		};

		sic.BeginInit();
		sic.EndInit();

		CornerstoneTest.AreEqual(items[2].Name, sic.SelectedValue);
	}

	[PresentationTestMethod]
	public void SettingSelectedValueDuringInitializeShouldTakePriorityOverPreviousValue()
	{
		var items = TestClass.GetItems();
		var sic = new SelectingItemsControl
		{
			ItemsSource = items,
			Template = Template(),
			SelectedValueBinding = new Binding("Name"),
			SelectedValue = "Item2"
		};

		sic.BeginInit();
		sic.SelectedValue = "Item1";
		sic.EndInit();

		CornerstoneTest.AreEqual(items[1].Name, sic.SelectedValue);
	}

	[PresentationTestMethod]
	public void SettingSelectedValueSetsSelectedIndex()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = TestClass.GetItems();
			var sic = new SelectingItemsControl
			{
				ItemsSource = items,
				SelectedValueBinding = new Binding("Name"),
				Template = Template()
			};

			Prepare(sic);

			sic.SelectedValue = items[2].Name;

			CornerstoneTest.AreEqual(2, sic.SelectedIndex);
		}
	}

	[PresentationTestMethod]
	public void SettingSelectedValueSetsSelectedItem()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = TestClass.GetItems();
			var sic = new SelectingItemsControl
			{
				ItemsSource = items,
				SelectedValueBinding = new Binding("Name"),
				Template = Template()
			};

			Prepare(sic);

			sic.SelectedValue = "Item2";

			CornerstoneTest.AreEqual(items[2], sic.SelectedItem);
		}
	}

	[PresentationTestMethod]
	public void SettingSelectedValueShouldRaiseSelectionChangedEvent()
	{
		// Unlike SelectedIndex/SelectedItem tests, we need the ItemsControl to
		// initialize so that SelectedValue can actually be looked up
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var items = TestClass.GetItems();
			var sic = new SelectingItemsControl
			{
				ItemsSource = items,
				Template = Template(),
				SelectedValueBinding = new Binding("Name")
			};

			Prepare(sic);

			var called = false;
			sic.SelectionChanged += (s, e) =>
			{
				CornerstoneTest.Same(items[2], e.AddedItems.Cast<object>().Single());
				CornerstoneTest.Empty(e.RemovedItems);
				called = true;
			};

			sic.SelectedValue = "Item2";
			CornerstoneTest.IsTrue(called);
		}
	}

	[PresentationTestMethod]
	public void SettingSelectedValueToNonExistentItemWithoutItemsSourceShouldKeepSelectionUntilItemsSourceIsSet()
	{
		var target = new SelectingItemsControl
		{
			Template = Template(),
			SelectedValueBinding = new Binding("Name")
		};

		target.ApplyTemplate();
		target.SelectedValue = "Item2";

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.Same("Item2", target.SelectedValue);

		target.ItemsSource = Array.Empty<TestClass>();

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.IsNull(target.SelectedValue);
	}

	[PresentationTestMethod]
	public void SettingSelectedValueWithoutItemsSourceShouldKeepSelectionIfItemExistsWhenItemsSourceIsSet()
	{
		var target = new SelectingItemsControl
		{
			Template = Template(),
			SelectedValueBinding = new Binding("Name")
		};

		target.ApplyTemplate();
		target.SelectedValue = "Item2";

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.IsNull(target.SelectedItem);
		CornerstoneTest.Same("Item2", target.SelectedValue);

		var items = TestClass.GetItems();
		target.ItemsSource = items;

		CornerstoneTest.AreEqual(2, target.SelectedIndex);
		CornerstoneTest.Same(items[2], target.SelectedItem);
		CornerstoneTest.AreEqual("Item2", target.SelectedValue);
	}

	private static void Prepare(SelectingItemsControl target)
	{
		var root = new TestRoot
		{
			Child = target,
			Width = 100,
			Height = 100,
			Styles =
			{
				new Style(x => x.Is<SelectingItemsControl>())
				{
					Setters =
					{
						new Setter(ListBox.TemplateProperty, Template())
					}
				}
			}
		};

		root.LayoutManager.ExecuteInitialLayoutPass();
	}

	private static FuncControlTemplate Template()
	{
		return new FuncControlTemplate<SelectingItemsControl>((control, scope) =>
			new ItemsPresenter
			{
				Name = "itemsPresenter",
				[~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty]
			}.RegisterInNameScope(scope));
	}

	#endregion
}

internal class TestClass
{
	#region Constructors

	public TestClass(string name, string alt)
	{
		Name = name;
		AltProperty = alt;
	}

	#endregion

	#region Properties

	public string AltProperty { get; set; }

	public string Name { get; set; }

	#endregion

	#region Methods

	public static List<TestClass> GetItems()
	{
		return new List<TestClass>
		{
			new(null, null),
			new("Item1", "Alt1"),
			new("Item2", "Alt2"),
			new("Item3", "Alt3"),
			new("Item4", "Alt4"),
			new("Item5", "Alt5")
		};
	}

	#endregion
}