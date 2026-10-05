#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class TabStripTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void FirstTabShouldBeSelectedByDefault()
	{
		var target = new TabStrip
		{
			Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
			Items =
			{
				new TabItem
				{
					Name = "first"
				},
				new TabItem
				{
					Name = "second"
				}
			}
		};

		target.ApplyTemplate();

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.Same(target.Items[0], target.SelectedItem);
	}

	[PresentationTestMethod]
	public void RemovingSelectedShouldSelectFirst()
	{
		var target = new TabStrip
		{
			Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
			Items =
			{
				new TabItem
				{
					Name = "first"
				},
				new TabItem
				{
					Name = "second"
				},
				new TabItem
				{
					Name = "3rd"
				}
			}
		};

		target.ApplyTemplate();
		target.SelectedItem = target.Items[1];
		CornerstoneTest.Same(target.Items[1], target.SelectedItem);
		target.Items.RemoveAt(1);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.Same(target.Items[0], target.SelectedItem);
		CornerstoneTest.Same("first", ((TabItem) target.SelectedItem!).Name);
	}

	[PresentationTestMethod]
	public void SettingSelectedItemShouldSetSelection()
	{
		var target = new TabStrip
		{
			Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
			Items =
			{
				new TabItem
				{
					Name = "first"
				},
				new TabItem
				{
					Name = "second"
				}
			}
		};

		target.SelectedItem = target.Items[1];
		target.ApplyTemplate();

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
		CornerstoneTest.Same(target.Items[1], target.SelectedItem);
	}

	private Control CreateTabStripTemplate(TabStrip parent, INameScope scope)
	{
		return new ItemsPresenter
		{
			Name = "itemsPresenter"
		}.RegisterInNameScope(scope);
	}

	#endregion
}