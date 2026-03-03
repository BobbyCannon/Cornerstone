namespace Cornerstone.Avalonia.TreeDataGrid.Models;

public interface ITextSearchableColumn<TModel>
{
	#region Properties

	public bool IsTextSearchEnabled { get; }

	#endregion

	#region Methods

	internal string SelectValue(TModel model);

	#endregion
}