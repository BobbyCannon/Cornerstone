#region References

using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabDatabases : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Databases";

	#endregion

	#region Constructors

	public TabDatabases()
	{
		DatabaseTypes = SelectionOption.GetEnumOptions(DatabaseType.Unknown);
		NumberOfItems = 1000;
		SelectedDatabaseType = DatabaseType.Memory;

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public string ConnectionString { get; set; }

	public SpeedyList<SelectionOption<DatabaseType>> DatabaseTypes { get; }

	public int NumberOfItems { get; set; }

	public DatabaseType SelectedDatabaseType { get; set; }

	#endregion
}