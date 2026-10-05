#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Documentation;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class DocumentationReaderWidthTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ToggleFullWidth()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var theme = new CornerstoneTheme();
		CornerstoneTest.IsTrue(theme.TryGetResource(typeof(DocumentationReader), ThemeVariant.Default, out var readerTheme));

		var reader = new DocumentationReader
		{
			Theme = CornerstoneTest.IsType<ControlTheme>(readerTheme),
			Width = 1600,
			Height = 900
		};
		var root = new TestRoot
		{
			ClientSize = new Size(1600, 900),
			Child = reader,
			Styles =
			{
				theme
			}
		};

		root.ApplyStyling();
		reader.ApplyStyling();
		reader.ApplyTemplate();
		root.LayoutManager.ExecuteInitialLayoutPass();

		var column = reader.GetTemplateDescendants().OfType<Border>().Single(x => x.Name == "PART_ReadingColumn");
		var button = reader.GetTemplateDescendants().OfType<ToggleButton>().Single(x => x.Name == "PART_WidthButton");

		CornerstoneTest.IsFalse(reader.IsFullWidth);
		CornerstoneTest.IsFalse(button.IsChecked == true);
		CornerstoneTest.AreEqual("Full width", ToolTip.GetTip(button));
		CornerstoneTest.AreEqual(DocumentationReader.ReadingColumnMaxWidth, column.Bounds.Width);

		button.IsChecked = true;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(reader.IsFullWidth);
		CornerstoneTest.AreEqual("Reading width", ToolTip.GetTip(button));
		CornerstoneTest.IsTrue(column.Bounds.Width > DocumentationReader.ReadingColumnMaxWidth);

		reader.IsFullWidth = false;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsFalse(button.IsChecked == true);
		CornerstoneTest.AreEqual(DocumentationReader.ReadingColumnMaxWidth, column.Bounds.Width);
		CornerstoneTest.AreEqual("Full width", ToolTip.GetTip(button));
	}

	#endregion
}
