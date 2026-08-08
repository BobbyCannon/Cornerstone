#region References

using Avalonia.Controls;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Cornerstone.VisualStudio.Documentation;

public partial class MainWindow : Window
{
	#region Constructors

	public MainWindow()
	{
		InitializeComponent();
	}

	public MainWindow(DocumentationCatalog catalog) : this()
	{
		Reader.Catalog = catalog;
	}

	#endregion
}
