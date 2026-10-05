#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public sealed class TableViewColumnHeaderTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DraggingResizerClampsColumnWidthToResizerWidth()
	{
		using var app = Start();

		var column = new TableViewColumn
		{
			Width = new GridLength(50),
			CanUserResize = true,
			ActualWidth = 50
		};
		var header = new TableViewColumnHeader
		{
			Column = column,
			Theme = TableViewColumnHeaderTheme(6)
		};
		var root = new TestRoot { Child = header };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var thumb = header.GetVisualDescendants().OfType<Thumb>().Single();
		thumb.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragDeltaEvent,
			Vector = new Vector(-500, 0)
		});

		CornerstoneTest.AreEqual(new GridLength(6), column.Width);
	}

	[PresentationTestMethod]
	public void DraggingResizerSetsColumnWidthToPixelValue()
	{
		using var app = Start();

		var column = new TableViewColumn
		{
			Width = new GridLength(1, GridUnitType.Star),
			CanUserResize = true,
			ActualWidth = 100
		};
		var header = new TableViewColumnHeader
		{
			Column = column,
			Theme = TableViewColumnHeaderTheme(0)
		};
		var root = new TestRoot { Child = header };
		root.LayoutManager.ExecuteInitialLayoutPass();

		var thumb = header.GetVisualDescendants().OfType<Thumb>().Single();
		thumb.RaiseEvent(new VectorEventArgs
		{
			RoutedEvent = Thumb.DragDeltaEvent,
			Vector = new Vector(15, 0)
		});

		CornerstoneTest.AreEqual(new GridLength(115), column.Width);
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
	}

	private static ControlTheme TableViewColumnHeaderTheme(int resizerWidth)
	{
		return new(typeof(TableViewColumnHeader))
		{
			Setters =
			{
				new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TableViewColumnHeader>((_, scope) =>
					new Thumb
					{
						Name = "PART_Resizer",
						Width = resizerWidth
					}.RegisterInNameScope(scope)))
			}
		};
	}

	#endregion
}