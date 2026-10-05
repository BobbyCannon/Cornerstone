#region References

using System;
using System.Collections;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public sealed class TableViewTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddingColumnAddsCellToRealizedRowsAndHeaders()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var cellsPresenter = GetCellsPresenter(row);
		CornerstoneTest.AreEqual(2, cellsPresenter.Children.Count);

		var headersPresenter = GetColumnHeadersPresenter(target);
		CornerstoneTest.AreEqual(2, headersPresenter.Children.Count);

		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		CornerstoneTest.AreEqual(3, cellsPresenter.Children.Count);
		CornerstoneTest.AreEqual(3, headersPresenter.Children.Count);

		// The newly added cell must also be part of the row's logical tree.
		var logicalChildren = row.GetLogicalChildren().ToArray();
		CornerstoneTest.AreEqual(cellsPresenter.Children, logicalChildren);
	}

	[PresentationTestMethod]
	public void CanUserResizeColumnsDefaultsToTrue()
	{
		CornerstoneTest.IsTrue(new TableView().CanUserResizeColumns);
	}

	[PresentationTestMethod]
	public void CellColumnPropertyIsSetToOwningColumn()
	{
		using var app = Start();

		var column0 = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
		var column1 = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(column0);
		target.Columns.Add(column1);

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var firstCell = (TableViewCell) GetCellsPresenter(row).Children[0];
		var secondCell = (TableViewCell) GetCellsPresenter(row).Children[1];

		CornerstoneTest.Same(column0, firstCell.Column);
		CornerstoneTest.Same(column1, secondCell.Column);
	}

	[PresentationTestMethod]
	public void CellContentDefaultsToRowItemWhenNoTemplateOrBinding()
	{
		using var app = Start();

		var item = new Person("Alice");
		var target = CreateTarget(new[] { item });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var firstCell = (TableViewCell) GetCellsPresenter(row).Children[0];

		CornerstoneTest.Same(item, firstCell.Content);
	}

	[PresentationTestMethod]
	public void CellUsesColumnBinding()
	{
		using var app = Start();

		var target = CreateTarget(new[] { new Person("Alice"), new Person("Bob") });
		target.Columns.Add(new TableViewColumn
		{
			Width = new GridLength(1, GridUnitType.Star),
			Binding = new ReflectionBinding(nameof(Person.Name))
		});
		target.Columns.Add(new TableViewColumn
		{
			Width = new GridLength(1, GridUnitType.Star)
		});

		Prepare(target);

		var rows = target.GetRealizedContainers().Cast<TableViewRow>().ToArray();
		var firstCell = (TableViewCell) GetCellsPresenter(rows[0]).Children[0];
		var secondCell = (TableViewCell) GetCellsPresenter(rows[1]).Children[0];

		CornerstoneTest.AreEqual("Alice", firstCell.Content);
		CornerstoneTest.AreEqual("Bob", secondCell.Content);
	}

	[PresentationTestMethod]
	public void CellUsesColumnCellTemplate()
	{
		using var app = Start();

		var template = new FuncDataTemplate<object>((_, _) => new TextBlock());
		var item = new Person("Alice");
		var target = CreateTarget(new[] { item });
		target.Columns.Add(new TableViewColumn
		{
			Width = new GridLength(1, GridUnitType.Star),
			CellTemplate = template
		});
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var firstCell = (TableViewCell) GetCellsPresenter(row).Children[0];

		CornerstoneTest.Same(template, firstCell.ContentTemplate);
		CornerstoneTest.Same(item, firstCell.Content);
	}

	[PresentationTestMethod]
	public void CellUsesColumnCellTheme()
	{
		using var app = Start();

		var cellTheme = new ControlTheme(typeof(TableViewCell));
		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(new TableViewColumn
		{
			Width = new GridLength(1, GridUnitType.Star),
			CellTheme = cellTheme
		});
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var firstCell = (TableViewCell) GetCellsPresenter(row).Children[0];
		var secondCell = (TableViewCell) GetCellsPresenter(row).Children[1];

		CornerstoneTest.Same(cellTheme, firstCell.Theme);
		CornerstoneTest.IsNull(secondCell.Theme);
	}

	[PresentationTestMethod]
	public void CellUsesColumnHorizontalContentAlignment()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(new TableViewColumn
		{
			Width = new GridLength(1, GridUnitType.Star),
			HorizontalContentAlignment = HorizontalAlignment.Right
		});
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var firstCell = (TableViewCell) GetCellsPresenter(row).Children[0];
		var secondCell = (TableViewCell) GetCellsPresenter(row).Children[1];

		CornerstoneTest.AreEqual(HorizontalAlignment.Right, firstCell.HorizontalContentAlignment);
		CornerstoneTest.AreEqual(HorizontalAlignment.Left, secondCell.HorizontalContentAlignment);
	}

	[PresentationTestMethod]
	public void ChangingColumnPropertiesUpdatesExistingHeadersAndCells()
	{
		using var app = Start();

		var cellThemeA = new ControlTheme(typeof(TableViewCell));
		var cellThemeB = new ControlTheme(typeof(TableViewCell));
		var headerThemeA = new ControlTheme(typeof(TableViewColumnHeader));
		var headerThemeB = new ControlTheme(typeof(TableViewColumnHeader));
		var headerTemplateA = new FuncDataTemplate<object>((_, _) => new TextBlock());
		var headerTemplateB = new FuncDataTemplate<object>((_, _) => new TextBlock());

		var item = new Person("Alice", "Ally");
		var column = new TableViewColumn
		{
			Width = new GridLength(1, GridUnitType.Star),
			CellTheme = cellThemeA,
			HorizontalContentAlignment = HorizontalAlignment.Left,
			Binding = new ReflectionBinding(nameof(Person.Name)),
			Header = "H1",
			HeaderTheme = headerThemeA,
			HeaderTemplate = headerTemplateA
		};
		var target = CreateTarget(new[] { item });
		target.Columns.Add(column);

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var cell = (TableViewCell) GetCellsPresenter(row).Children[0];
		var header = (TableViewColumnHeader) GetColumnHeadersPresenter(target).Children[0];

		CornerstoneTest.Same(cellThemeA, cell.Theme);
		CornerstoneTest.AreEqual(HorizontalAlignment.Left, cell.HorizontalContentAlignment);
		CornerstoneTest.IsNull(cell.ContentTemplate);
		CornerstoneTest.AreEqual("Alice", cell.Content);

		CornerstoneTest.Same(headerThemeA, header.Theme);
		CornerstoneTest.AreEqual(HorizontalAlignment.Left, header.HorizontalContentAlignment);
		CornerstoneTest.Same(headerTemplateA, header.ContentTemplate);
		CornerstoneTest.AreEqual("H1", header.Content);

		// Mutating the column after the cell and header were built should update both.
		// Switch the Binding first to confirm it's reflected in the cell content.
		column.CellTheme = cellThemeB;
		column.HorizontalContentAlignment = HorizontalAlignment.Right;
		column.Binding = new ReflectionBinding(nameof(Person.Nickname));
		column.Header = "H2";
		column.HeaderTheme = headerThemeB;
		column.HeaderTemplate = headerTemplateB;

		CornerstoneTest.Same(cellThemeB, cell.Theme);
		CornerstoneTest.AreEqual(HorizontalAlignment.Right, cell.HorizontalContentAlignment);
		CornerstoneTest.IsNull(cell.ContentTemplate);
		CornerstoneTest.AreEqual("Ally", cell.Content);

		CornerstoneTest.Same(headerThemeB, header.Theme);
		CornerstoneTest.AreEqual(HorizontalAlignment.Right, header.HorizontalContentAlignment);
		CornerstoneTest.Same(headerTemplateB, header.ContentTemplate);
		CornerstoneTest.AreEqual("H2", header.Content);

		// CellTemplate takes priority over Binding: the row item flows through the template.
		var cellTemplateB = new FuncDataTemplate<object>((_, _) => new TextBlock());
		column.CellTemplate = cellTemplateB;

		CornerstoneTest.Same(cellTemplateB, cell.ContentTemplate);
		CornerstoneTest.Same(item, cell.Content);
	}

	[PresentationTestMethod]
	public void ChangingColumnWidthDoesNotRecreateCells()
	{
		using var app = Start();

		var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(column);
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target, 200);

		var presenter = GetCellsPresenter((TableViewRow) target.GetRealizedContainers().Single());
		var cellsBefore = presenter.Children.ToArray();

		column.Width = new GridLength(2, GridUnitType.Star);
		Layout(target);

		var cellsAfter = presenter.Children.ToArray();
		CornerstoneTest.AreEqual(cellsBefore, cellsAfter);
	}

	[PresentationTestMethod]
	public void ChangingColumnWidthInvalidatesRowPresenterMeasure()
	{
		using var app = Start();

		var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(column);
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target, 200);

		var presenter = GetCellsPresenter((TableViewRow) target.GetRealizedContainers().Single());
		CornerstoneTest.IsTrue(presenter.IsMeasureValid);

		column.Width = new GridLength(2, GridUnitType.Star);

		CornerstoneTest.IsFalse(presenter.IsMeasureValid);
	}

	[PresentationTestMethod]
	public void ChangingColumnWidthUpdatesActualWidthAfterLayout()
	{
		using var app = Start();

		var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(column);
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target, 200);

		CornerstoneTest.AreEqual(100, target.Columns[0].ActualWidth);
		CornerstoneTest.AreEqual(100, target.Columns[1].ActualWidth);

		column.Width = new GridLength(3, GridUnitType.Star);
		Layout(target);

		CornerstoneTest.AreEqual(150, target.Columns[0].ActualWidth);
		CornerstoneTest.AreEqual(50, target.Columns[1].ActualWidth);
	}

	[PresentationTestMethod]
	[DataRow(true, null, true)] // inherits the table's value
	[DataRow(true, true, true)] // column opts in
	[DataRow(true, false, false)] // column opts out
	[DataRow(false, null, false)] // inherits the table's value
	[DataRow(false, true, true)] // column opts in
	[DataRow(false, false, false)] // column opts out
	public void ColumnCanUserEffectivelyResizeCombinesTableViewAndColumnSettings(
		bool canResizeColumns,
		bool? canResize,
		bool expected)
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		target.CanUserResizeColumns = canResizeColumns;
		var column = new TableViewColumn { CanUserResize = canResize };
		target.Columns.Add(column);

		Prepare(target);

		CornerstoneTest.AreEqual(expected, column.CanUserEffectivelyResize);
	}

	[PresentationTestMethod]
	public void ColumnCanUserEffectivelyResizeUpdatesWhenColumnCanResizeChanges()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		target.CanUserResizeColumns = false;
		var column = new TableViewColumn();
		target.Columns.Add(column);

		Prepare(target);

		CornerstoneTest.IsFalse(column.CanUserEffectivelyResize);

		column.CanUserResize = true;
		CornerstoneTest.IsTrue(column.CanUserEffectivelyResize);

		column.CanUserResize = null;
		CornerstoneTest.IsFalse(column.CanUserEffectivelyResize);
	}

	[PresentationTestMethod]
	public void ColumnCanUserEffectivelyResizeUpdatesWhenTableViewCanResizeColumnsChanges()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		var column = new TableViewColumn();
		target.Columns.Add(column);

		Prepare(target);

		CornerstoneTest.IsTrue(column.CanUserEffectivelyResize);

		target.CanUserResizeColumns = false;
		CornerstoneTest.IsFalse(column.CanUserEffectivelyResize);

		target.CanUserResizeColumns = true;
		CornerstoneTest.IsTrue(column.CanUserEffectivelyResize);
	}

	[PresentationTestMethod]
	public void ColumnCannotBelongToTwoTableViews()
	{
		using var app = Start();

		var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };

		var first = CreateTarget(new[] { "Foo" });
		first.Columns.Add(column);
		Prepare(first);

		CornerstoneTest.Same(first, column.TableView);

		var second = CreateTarget(new[] { "Bar" });
		second.Columns.Add(column);

		Assert.Throws<InvalidOperationException>(() => Prepare(second));
	}

	[PresentationTestMethod]
	public void ColumnLogicalParentIsSetWhenAddedAndUnsetWhenRemoved()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		var column = new TableViewColumn();
		target.Columns.Add(column);

		Prepare(target);

		CornerstoneTest.Same(target, column.GetLogicalParent());

		target.Columns.Remove(column);

		CornerstoneTest.IsNull(column.GetLogicalParent());
	}

	[PresentationTestMethod]
	public void ColumnReceivesStylesFromTableView()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		var style = new Style(x => x.OfType<TableViewColumn>().Class("right-align"))
		{
			Setters = { new Setter(TableViewColumn.HorizontalContentAlignmentProperty, HorizontalAlignment.Right) }
		};
		target.Styles.Add(style);

		var column = new TableViewColumn
		{
			Classes = { "right-align" }
		};
		target.Columns.Add(column);

		Prepare(target);

		CornerstoneTest.AreEqual(HorizontalAlignment.Right, column.HorizontalContentAlignment);
	}

	[PresentationTestMethod]
	public void ColumnTableViewIsSetWhenAddedAndUnsetWhenRemoved()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
		target.Columns.Add(column);

		Prepare(target);

		CornerstoneTest.Same(target, column.TableView);

		target.Columns.Remove(column);

		CornerstoneTest.IsNull(column.TableView);
	}

	[PresentationTestMethod]
	public void ContainerForEachItemIsTableViewRow()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo", "Bar", "Baz" });

		Prepare(target);

		var containers = target.GetRealizedContainers().ToList();
		CornerstoneTest.AreEqual(3, containers.Count);
		CornerstoneTest.All(containers, c => CornerstoneTest.IsType<TableViewRow>(c));
	}

	[PresentationTestMethod]
	public void ReTemplatingRowDetachesOldCellsAndRebuildsNewCells()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(new TableViewColumn());
		target.Columns.Add(new TableViewColumn());

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var oldCells = GetCellsPresenter(row).Children.ToArray();
		CornerstoneTest.AreEqual(2, oldCells.Length);
		CornerstoneTest.AreEqual(oldCells, row.GetLogicalChildren());

		row.Template = RowTemplate();
		row.ApplyTemplate();

		var newCells = GetCellsPresenter(row).Children.ToArray();
		CornerstoneTest.AreEqual(2, newCells.Length);

		CornerstoneTest.All(oldCells, cell => CornerstoneTest.IsNull(cell.Parent));
		CornerstoneTest.AreEqual(newCells, row.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void RemovingColumnRemovesCellFromRealizedRowsAndHeaders()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var cellsPresenter = GetCellsPresenter(row);
		CornerstoneTest.AreEqual(3, cellsPresenter.Children.Count);

		var headersPresenter = GetColumnHeadersPresenter(target);
		CornerstoneTest.AreEqual(3, headersPresenter.Children.Count);

		var removedCell = (TableViewCell) cellsPresenter.Children[2];

		target.Columns.RemoveAt(2);

		CornerstoneTest.AreEqual(2, cellsPresenter.Children.Count);
		CornerstoneTest.AreEqual(2, headersPresenter.Children.Count);

		// The removed cell must also be detached from the row's logical tree, while the remaining cells stay in it.
		var logicalChildren = row.GetLogicalChildren().ToArray();
		CornerstoneTest.DoesNotContain(logicalChildren, removedCell);
		CornerstoneTest.AreEqual(cellsPresenter.Children, logicalChildren);
	}

	[PresentationTestMethod]
	public void ReplacingColumnsCollectionUpdatesRealizedRowsAndHeaders()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
		target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var cellsPresenter = GetCellsPresenter(row);
		CornerstoneTest.AreEqual(2, cellsPresenter.Children.Count);

		var headersPresenter = GetColumnHeadersPresenter(target);
		CornerstoneTest.AreEqual(2, headersPresenter.Children.Count);

		var oldCells = cellsPresenter.Children.ToArray();

		target.Columns =
		[
			new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) },
			new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) },
			new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) }
		];

		CornerstoneTest.AreEqual(3, cellsPresenter.Children.Count);
		CornerstoneTest.AreEqual(3, headersPresenter.Children.Count);

		// The new cells must be in the row's logical tree, and the old ones gone.
		var logicalChildren = row.GetLogicalChildren().ToArray();
		CornerstoneTest.AreEqual(cellsPresenter.Children, logicalChildren);
		CornerstoneTest.All(oldCells, cell => CornerstoneTest.DoesNotContain(logicalChildren, cell));
	}

	[PresentationTestMethod]
	public void RowHasOneCellPerColumn()
	{
		using var app = Start();

		var target = CreateTarget(new[] { "Foo" });
		target.Columns.Add(new TableViewColumn());
		target.Columns.Add(new TableViewColumn());
		target.Columns.Add(new TableViewColumn());

		Prepare(target);

		var row = (TableViewRow) target.GetRealizedContainers().Single();
		var cells = GetCellsPresenter(row).Children;
		CornerstoneTest.AreEqual(3, cells.Count);
		CornerstoneTest.All(cells, c => CornerstoneTest.IsType<TableViewCell>(c));

		// Cells must also be part of the row's logical tree.
		var logicalChildren = row.GetLogicalChildren().ToArray();
		CornerstoneTest.AreEqual(cells, logicalChildren);
	}

	private static TableView CreateTarget(IEnumerable items)
	{
		return new()
		{
			Template = TableViewTemplate(),
			ItemContainerTheme = TableViewRowTheme(),
			ItemsSource = items
		};
	}

	private static TableViewCellsPresenter GetCellsPresenter(TableViewRow row)
	{
		var presenter = row.GetVisualDescendants()
			.OfType<TableViewCellsPresenter>()
			.FirstOrDefault();
		CornerstoneTest.IsNotNull(presenter);
		return presenter;
	}

	private static TableViewColumnHeadersPresenter GetColumnHeadersPresenter(TableView target)
	{
		var presenter = target.GetVisualDescendants()
			.OfType<TableViewColumnHeadersPresenter>()
			.FirstOrDefault();
		CornerstoneTest.IsNotNull(presenter);
		return presenter;
	}

	private static void Layout(Control control)
	{
		control.GetLayoutManager()?.ExecuteLayoutPass();
	}

	private static void Prepare(TableView target, double width = 300, double height = 200)
	{
		target.Width = width;
		target.Height = height;
		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
	}

	private static FuncControlTemplate<TableViewRow> RowTemplate()
	{
		return new((_, scope) =>
			new TableViewCellsPresenter
			{
				Name = "PART_CellsPresenter"
			}.RegisterInNameScope(scope));
	}

	private static FuncControlTemplate ScrollViewerTemplate()
	{
		return new FuncControlTemplate<ScrollViewer>((_, scope) =>
			new Panel
			{
				Children =
				{
					new ScrollContentPresenter
					{
						Name = "PART_ContentPresenter"
					}.RegisterInNameScope(scope)
				}
			});
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
	}

	private static ControlTheme TableViewRowTheme()
	{
		return new(typeof(TableViewRow))
		{
			Setters =
			{
				new Setter(TemplatedControl.TemplateProperty, RowTemplate())
			}
		};
	}

	private static FuncControlTemplate TableViewTemplate()
	{
		return new FuncControlTemplate<TableView>((parent, scope) =>
			new DockPanel
			{
				Children =
				{
					new TableViewColumnHeadersPresenter
					{
						[DockPanel.DockProperty] = Dock.Top
					},
					new ScrollViewer
					{
						Name = "PART_ScrollViewer",
						Template = ScrollViewerTemplate(),
						Content = new ItemsPresenter
						{
							Name = "PART_ItemsPresenter",
							[~ItemsPresenter.ItemsPanelProperty] =
								parent.GetObservable(ItemsControl.ItemsPanelProperty).ToBinding()
						}.RegisterInNameScope(scope)
					}.RegisterInNameScope(scope)
				}
			});
	}

	#endregion

	#region Classes

	private class Person
	{
		#region Constructors

		public Person(string name, string nickname = null)
		{
			Name = name;
			Nickname = nickname;
		}

		#endregion

		#region Properties

		public string Name { get; }
		public string Nickname { get; }

		#endregion
	}

	#endregion
}