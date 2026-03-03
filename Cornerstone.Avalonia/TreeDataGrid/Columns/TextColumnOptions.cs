#region References

using System.Globalization;
using Avalonia.Media;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

/// <summary>
/// Holds less commonly-used options for a <see cref="TextColumn{TModel, TValue}" />.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
public class TextColumnOptions<TModel> : ColumnOptions<TModel>, ITextCellOptions
{
	#region Constructors

	public TextColumnOptions()
	{
		Culture = CultureInfo.CurrentCulture;
		StringFormat = "{0}";
		TextAlignment = TextAlignment.Left;
		TextTrimming = TextTrimming.CharacterEllipsis;
		TextWrapping = TextWrapping.NoWrap;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Culture info used in conjunction with <see cref="StringFormat" />
	/// </summary>
	public CultureInfo Culture { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the column takes part in text searches.
	/// </summary>
	public bool IsTextSearchEnabled { get; set; }

	/// <summary>
	/// Gets or sets the format string for the cells in the column.
	/// </summary>
	public string StringFormat { get; set; }

	/// <summary>
	/// Gets or sets the text alignment mode for the cells in the column.
	/// </summary>
	public TextAlignment TextAlignment { get; set; }

	/// <summary>
	/// Gets or sets the text trimming mode for the cells in the column.
	/// </summary>
	public TextTrimming TextTrimming { get; set; }

	/// <summary>
	/// Gets or sets the text wrapping mode for the cells in the column.
	/// </summary>
	public TextWrapping TextWrapping { get; set; }

	#endregion
}