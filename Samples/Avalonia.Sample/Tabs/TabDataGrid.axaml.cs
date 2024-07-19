#region References

using System.Collections.Generic;
using Avalonia.Controls;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Sample.Shared.Sync;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabDataGrid : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "DataGrid";

	#endregion

	#region Constructors

	public TabDataGrid()
	{
		var list = new SpeedyList<AccountSync>
		{
			new() { Name = "John Doe", EmailAddress = "john@domain.com" },
			new() { Name = "Elizabeth Thomas", EmailAddress = "et@user.co" },
			new() { Name = "Zack Ward", EmailAddress = "ward.zack@corp.net" }
		};
		DataGridPeopleSource = list;
		LineVisibilities = EnumExtensions.GetAllEnumDetails<DataGridGridLinesVisibility>().Values;

		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public SpeedyList<AccountSync> DataGridPeopleSource { get; }

	public IEnumerable<EnumExtensions.EnumDetails> LineVisibilities { get; }

	#endregion
}