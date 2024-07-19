#region References

using Avalonia.Controls;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.DataGrid;

public static class DataGridConverters
{
	#region Fields

	public static readonly FuncValueConverter<DataGridGridLinesVisibility, bool> ShowVerticalLines = new(x => x.HasFlag(DataGridGridLinesVisibility.Vertical));

	#endregion
}