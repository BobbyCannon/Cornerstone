#region References

using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Documentation;

public partial class DocumentationReaderHostWindow : Window
{
	#region Constructors

	public DocumentationReaderHostWindow()
	{
		InitializeComponent();
	}

	public DocumentationReaderHostWindow(DocumentationCatalog catalog, string title) : this()
	{
		if (!string.IsNullOrWhiteSpace(title))
		{
			Title = title;
		}

		Reader.Catalog = catalog;
	}

	#endregion
}