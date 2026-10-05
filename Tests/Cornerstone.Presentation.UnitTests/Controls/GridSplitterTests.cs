#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class GridSplitterTests : ScopedTestBase
{
	#region Constructors

	public GridSplitterTests()
	{
		var cursorFactoryImpl = new StubCursorFactory();
		PresentationLocator.CurrentMutable.Bind<ICursorFactory>().ToConstant(cursorFactoryImpl);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void DetectsHorizontalOrientation()
	{
		GridSplitter splitter;

		var grid = new Grid
		{
			RowDefinitions = new RowDefinitions("*,Auto,*"),
			ColumnDefinitions = new ColumnDefinitions("*,*"),
			Children =
			{
				new Border { [Grid.RowProperty] = 0 },
				(splitter = new GridSplitter { [Grid.RowProperty] = 1 }),
				new Border { [Grid.RowProperty] = 2 }
			}
		};

		var root = new TestRoot { Child = grid };
		root.Measure(new Size(100, 300));
		root.Arrange(new Rect(0, 0, 100, 300));
		CornerstoneTest.AreEqual(GridResizeDirection.Rows, splitter.GetEffectiveResizeDirection());
	}

	[PresentationTestMethod]
	public void DetectsVerticalOrientation()
	{
		GridSplitter splitter;

		var grid = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
			RowDefinitions = new RowDefinitions("*,*"),
			Children =
			{
				new Border { [Grid.ColumnProperty] = 0 },
				(splitter = new GridSplitter { [Grid.ColumnProperty] = 1 }),
				new Border { [Grid.ColumnProperty] = 2 }
			}
		};

		var root = new TestRoot { Child = grid };
		root.Measure(new Size(100, 300));
		root.Arrange(new Rect(0, 0, 100, 300));
		CornerstoneTest.AreEqual(GridResizeDirection.Columns, splitter.GetEffectiveResizeDirection());
	}

	[PresentationTestMethod]
	public void DetectsWithBothAuto()
	{
		GridSplitter splitter;

		var grid = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"),
			RowDefinitions = new RowDefinitions("Auto,Auto"),
			Children =
			{
				new Border { [Grid.ColumnProperty] = 0 },
				(splitter = new GridSplitter { [Grid.ColumnProperty] = 1 }),
				new Border { [Grid.ColumnProperty] = 2 }
			}
		};

		var root = new TestRoot { Child = grid };
		root.Measure(new Size(100, 300));
		root.Arrange(new Rect(0, 0, 100, 300));
		CornerstoneTest.AreEqual(GridResizeDirection.Columns, splitter.GetEffectiveResizeDirection());
	}

	[PresentationTestMethod]
	[DataRow(Key.Left, 90, 110)]
	[DataRow(Key.Right, 110, 90)]
	public void HorizontalKeyboardInputCanMoveSplitter(Key key, double expectedWidthFirst, double expectedWidthSecond)
	{
		var control1 = new Border { [Grid.ColumnProperty] = 0 };
		var splitter = new GridSplitter { [Grid.ColumnProperty] = 1, KeyboardIncrement = 10d };
		var control2 = new Border { [Grid.ColumnProperty] = 2 };

		var columnDefinitions = new ColumnDefinitions
		{
			new ColumnDefinition(1, GridUnitType.Star),
			new ColumnDefinition(GridLength.Auto),
			new ColumnDefinition(1, GridUnitType.Star)
		};

		var grid = new Grid { ColumnDefinitions = columnDefinitions, Children = { control1, splitter, control2 } };

		var root = new TestRoot
		{
			Child = grid
		};

		root.Measure(new Size(200, 200));
		root.Arrange(new Rect(0, 0, 200, 200));

		splitter.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key
		});

		CornerstoneTest.AreEqual(columnDefinitions[0].Width, new GridLength(expectedWidthFirst, GridUnitType.Star));
		CornerstoneTest.AreEqual(columnDefinitions[2].Width, new GridLength(expectedWidthSecond, GridUnitType.Star));
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void HorizontalStaysWithinConstraints(bool showsPreview)
	{
		var control1 = new Border { [Grid.RowProperty] = 0 };
		var splitter = new GridSplitter { [Grid.RowProperty] = 1, ShowsPreview = showsPreview };
		var control2 = new Border { [Grid.RowProperty] = 2 };

		var rowDefinitions = new RowDefinitions
		{
			new RowDefinition(1, GridUnitType.Star) { MinHeight = 70, MaxHeight = 110 },
			new RowDefinition(GridLength.Auto),
			new RowDefinition(1, GridUnitType.Star) { MinHeight = 10, MaxHeight = 140 }
		};

		var grid = new Grid { RowDefinitions = rowDefinitions, Children = { control1, splitter, control2 } };

		var root = new TestRoot
		{
			Child = new VisualLayerManager
			{
				Child = grid
			}
		};

		root.Measure(new Size(100, 200));
		root.Arrange(new Rect(0, 0, 100, 200));

		splitter.RaiseEvent(
			new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });

		splitter.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragDeltaEvent,
			Vector = new Vector(0, -100)
		});

		if (showsPreview)
		{
			CornerstoneTest.AreEqual(rowDefinitions[0].Height, new GridLength(1, GridUnitType.Star));
			CornerstoneTest.AreEqual(rowDefinitions[2].Height, new GridLength(1, GridUnitType.Star));
		}
		else
		{
			CornerstoneTest.AreEqual(rowDefinitions[0].Height, new GridLength(70, GridUnitType.Star));
			CornerstoneTest.AreEqual(rowDefinitions[2].Height, new GridLength(130, GridUnitType.Star));
		}

		splitter.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragDeltaEvent,
			Vector = new Vector(0, 100)
		});

		if (showsPreview)
		{
			CornerstoneTest.AreEqual(rowDefinitions[0].Height, new GridLength(1, GridUnitType.Star));
			CornerstoneTest.AreEqual(rowDefinitions[2].Height, new GridLength(1, GridUnitType.Star));
		}
		else
		{
			CornerstoneTest.AreEqual(rowDefinitions[0].Height, new GridLength(110, GridUnitType.Star));
			CornerstoneTest.AreEqual(rowDefinitions[2].Height, new GridLength(90, GridUnitType.Star));
		}

		splitter.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragCompletedEvent
		});

		CornerstoneTest.AreEqual(rowDefinitions[0].Height, new GridLength(110, GridUnitType.Star));
		CornerstoneTest.AreEqual(rowDefinitions[2].Height, new GridLength(90, GridUnitType.Star));
	}

	[PresentationTestMethod]
	public void InFirstPositionDoesntThrowException()
	{
		GridSplitter splitter;
		var grid = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("Auto,*,*"),
			RowDefinitions = new RowDefinitions("*,*"),
			Children =
			{
				(splitter = new GridSplitter { [Grid.ColumnProperty] = 0 }),
				new Border { [Grid.ColumnProperty] = 1 },
				new Border { [Grid.ColumnProperty] = 2 }
			}
		};

		var root = new TestRoot { Child = grid };
		root.Measure(new Size(100, 300));
		root.Arrange(new Rect(0, 0, 100, 300));

		splitter.RaiseEvent(
			new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });

		splitter.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(100, 1000)
		});
	}

	[PresentationTestMethod]
	public void PressingEscapeKeyCancelsResizing()
	{
		var control1 = new Border { [Grid.ColumnProperty] = 0 };
		var splitter = new GridSplitter { [Grid.ColumnProperty] = 1, KeyboardIncrement = 10d };
		var control2 = new Border { [Grid.ColumnProperty] = 2 };

		var columnDefinitions = new ColumnDefinitions
		{
			new ColumnDefinition(1, GridUnitType.Star),
			new ColumnDefinition(GridLength.Auto),
			new ColumnDefinition(1, GridUnitType.Star)
		};

		var grid = new Grid { ColumnDefinitions = columnDefinitions, Children = { control1, splitter, control2 } };

		var root = new TestRoot
		{
			Child = grid
		};

		root.Measure(new Size(200, 200));
		root.Arrange(new Rect(0, 0, 200, 200));

		splitter.RaiseEvent(
			new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });

		splitter.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragDeltaEvent,
			Vector = new Vector(-100, 0)
		});

		CornerstoneTest.AreEqual(columnDefinitions[0].Width, new GridLength(0, GridUnitType.Star));
		CornerstoneTest.AreEqual(columnDefinitions[2].Width, new GridLength(200, GridUnitType.Star));

		splitter.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Escape
		});

		CornerstoneTest.AreEqual(columnDefinitions[0].Width, new GridLength(1, GridUnitType.Star));
		CornerstoneTest.AreEqual(columnDefinitions[2].Width, new GridLength(1, GridUnitType.Star));
	}

	[PresentationTestMethod]
	[DataRow(Key.Up, 90, 110)]
	[DataRow(Key.Down, 110, 90)]
	public void VerticalKeyboardInputCanMoveSplitter(Key key, double expectedHeightFirst, double expectedHeightSecond)
	{
		var control1 = new Border { [Grid.RowProperty] = 0 };
		var splitter = new GridSplitter { [Grid.RowProperty] = 1, KeyboardIncrement = 10d };
		var control2 = new Border { [Grid.RowProperty] = 2 };

		var rowDefinitions = new RowDefinitions
		{
			new RowDefinition(1, GridUnitType.Star),
			new RowDefinition(GridLength.Auto),
			new RowDefinition(1, GridUnitType.Star)
		};

		var grid = new Grid { RowDefinitions = rowDefinitions, Children = { control1, splitter, control2 } };

		var root = new TestRoot
		{
			Child = grid
		};

		root.Measure(new Size(200, 200));
		root.Arrange(new Rect(0, 0, 200, 200));

		splitter.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key
		});

		CornerstoneTest.AreEqual(rowDefinitions[0].Height, new GridLength(expectedHeightFirst, GridUnitType.Star));
		CornerstoneTest.AreEqual(rowDefinitions[2].Height, new GridLength(expectedHeightSecond, GridUnitType.Star));
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void VerticalStaysWithinConstraints(bool showsPreview)
	{
		var control1 = new Border { [Grid.ColumnProperty] = 0 };
		var splitter = new GridSplitter { [Grid.ColumnProperty] = 1, ShowsPreview = showsPreview };
		var control2 = new Border { [Grid.ColumnProperty] = 2 };

		var columnDefinitions = new ColumnDefinitions
		{
			new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 10, MaxWidth = 190 },
			new ColumnDefinition(GridLength.Auto),
			new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 80, MaxWidth = 120 }
		};

		var grid = new Grid { ColumnDefinitions = columnDefinitions, Children = { control1, splitter, control2 } };

		var root = new TestRoot
		{
			Child = new VisualLayerManager
			{
				Child = grid
			}
		};

		root.Measure(new Size(200, 100));
		root.Arrange(new Rect(0, 0, 200, 100));

		splitter.RaiseEvent(
			new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });

		splitter.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragDeltaEvent,
			Vector = new Vector(-100, 0)
		});

		if (showsPreview)
		{
			CornerstoneTest.AreEqual(columnDefinitions[0].Width, new GridLength(1, GridUnitType.Star));
			CornerstoneTest.AreEqual(columnDefinitions[2].Width, new GridLength(1, GridUnitType.Star));
		}
		else
		{
			CornerstoneTest.AreEqual(columnDefinitions[0].Width, new GridLength(80, GridUnitType.Star));
			CornerstoneTest.AreEqual(columnDefinitions[2].Width, new GridLength(120, GridUnitType.Star));
		}

		splitter.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragDeltaEvent,
			Vector = new Vector(100, 0)
		});

		if (showsPreview)
		{
			CornerstoneTest.AreEqual(columnDefinitions[0].Width, new GridLength(1, GridUnitType.Star));
			CornerstoneTest.AreEqual(columnDefinitions[2].Width, new GridLength(1, GridUnitType.Star));
		}
		else
		{
			CornerstoneTest.AreEqual(columnDefinitions[0].Width, new GridLength(120, GridUnitType.Star));
			CornerstoneTest.AreEqual(columnDefinitions[2].Width, new GridLength(80, GridUnitType.Star));
		}

		splitter.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragCompletedEvent
		});

		CornerstoneTest.AreEqual(columnDefinitions[0].Width, new GridLength(120, GridUnitType.Star));
		CornerstoneTest.AreEqual(columnDefinitions[2].Width, new GridLength(80, GridUnitType.Star));
	}

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void WorksInGrid()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = @"<Grid xmlns='https://github.com/BobbyCannon/Cornerstone' ColumnDefinitions='*,10,*'>
    <Border Grid.Column='0'/>
    <GridSplitter Grid.Column='1' ResizeDirection='Columns'/>
    <Border Grid.Column='2'/>
</Grid>";

		var grid = CornerstoneRuntimeXamlLoader.Parse<Grid>(xaml);
		var root = new TestRoot { Child = grid };
		root.Measure(new Size(200, 100));
		root.Arrange(new Rect(0, 0, 200, 100));

		var splitter = CornerstoneTest.IsType<GridSplitter>(grid.Children[1]);

		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });
		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(-20, 0) });
		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragCompletedEvent });

		CornerstoneTest.AreNotEqual(grid.ColumnDefinitions[0].Width, grid.ColumnDefinitions[2].Width);
	}

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void WorksInItemsControlItems()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = @"<ItemsControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                                  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ItemsControl.Resources>
      <ControlTheme x:Key='{x:Type ItemsControl}' TargetType='ItemsControl'>
        <Setter Property='Template'>
          <ControlTemplate>
            <Border Background='{TemplateBinding Background}'
                    BorderBrush='{TemplateBinding BorderBrush}'
                    BorderThickness='{TemplateBinding BorderThickness}'
                    CornerRadius='{TemplateBinding CornerRadius}'
                    Padding='{TemplateBinding Padding}'>
              <ItemsPresenter Name='PART_ItemsPresenter'
                              ItemsPanel='{TemplateBinding ItemsPanel}'/>
            </Border>
          </ControlTemplate>
        </Setter>
      </ControlTheme>
    </ItemsControl.Resources>
    <ItemsControl.Items>
        <Border Grid.Column='0'/>
        <GridSplitter Grid.Column='1' ResizeDirection='Columns'/>
        <Border Grid.Column='2'/>
    </ItemsControl.Items>
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <Grid ColumnDefinitions='*,10,*'/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>";

		var itemsControl = CornerstoneRuntimeXamlLoader.Parse<ItemsControl>(xaml);
		var root = new TestRoot { Child = itemsControl };
		root.Measure(new Size(200, 100));
		root.Arrange(new Rect(0, 0, 200, 100));

		var panel = CornerstoneTest.IsType<Grid>(itemsControl.ItemsPanelRoot);
		var splitter = CornerstoneTest.IsType<GridSplitter>(panel.Children[1]);

		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });
		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(-20, 0) });
		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragCompletedEvent });

		CornerstoneTest.AreNotEqual(panel.ColumnDefinitions[0].Width, panel.ColumnDefinitions[2].Width);
	}

	[PresentationTestMethod]
	[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
	public void WorksInItemsControlItemsSource()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = @"<ItemsControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                                  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                  xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Controls'>
    <ItemsControl.Resources>
      <ControlTheme x:Key='{x:Type ItemsControl}' TargetType='ItemsControl'>
        <Setter Property='Template'>
          <ControlTemplate>
            <Border Background='{TemplateBinding Background}'
                    BorderBrush='{TemplateBinding BorderBrush}'
                    BorderThickness='{TemplateBinding BorderThickness}'
                    CornerRadius='{TemplateBinding CornerRadius}'
                    Padding='{TemplateBinding Padding}'>
              <ItemsPresenter Name='PART_ItemsPresenter'
                              ItemsPanel='{TemplateBinding ItemsPanel}'/>
            </Border>
          </ControlTemplate>
        </Setter>
      </ControlTheme>
    </ItemsControl.Resources>
    <ItemsControl.Styles>
        <Style Selector='ItemsControl > ContentPresenter'>
            <Setter Property='(Grid.Column)' Value='{Binding Column}'/>
        </Style>
    </ItemsControl.Styles>
    <ItemsControl.DataTemplates>
        <DataTemplate DataType='local:TextItem'>
            <Border><TextBlock Text='{Binding Text}'/></Border>
        </DataTemplate>
        <DataTemplate DataType='local:SplitterItem'>
            <GridSplitter ResizeDirection='Columns'/>
        </DataTemplate>
    </ItemsControl.DataTemplates>
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <Grid ColumnDefinitions='*,10,*'/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>";

		var itemsControl = CornerstoneRuntimeXamlLoader.Parse<ItemsControl>(xaml);
		itemsControl.ItemsSource = new List<IGridItem>
		{
			new TextItem { Column = 0, Text = "A" },
			new SplitterItem { Column = 1 },
			new TextItem { Column = 2, Text = "B" }
		};

		var root = new TestRoot { Child = itemsControl };
		root.Measure(new Size(200, 100));
		root.Arrange(new Rect(0, 0, 200, 100));

		var panel = CornerstoneTest.IsType<Grid>(itemsControl.ItemsPanelRoot);
		var cp = CornerstoneTest.IsType<ContentPresenter>(panel.Children[1]);
		cp.UpdateChild();
		var splitter = CornerstoneTest.IsType<GridSplitter>(cp.Child);

		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });
		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(-20, 0) });
		splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragCompletedEvent });

		CornerstoneTest.AreNotEqual(panel.ColumnDefinitions[0].Width, panel.ColumnDefinitions[2].Width);
	}

	#endregion
}

public interface IGridItem
{
	#region Properties

	int Column { get; set; }

	#endregion
}

[TestClass]
public class TextItem : IGridItem
{
	#region Properties

	public int Column { get; set; }
	public string Text { get; set; }

	#endregion
}

[TestClass]
public class SplitterItem : IGridItem
{
	#region Properties

	public int Column { get; set; }

	#endregion
}