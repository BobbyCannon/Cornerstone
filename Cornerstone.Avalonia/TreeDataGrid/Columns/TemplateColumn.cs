#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

/// <summary>
/// A column in an <see cref="ITreeDataGridSource" /> which displays its values using a data
/// template.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
public class TemplateColumn<TModel> : ColumnBase<TModel>, ITextSearchableColumn<TModel>
{
	#region Fields

	private IDataTemplate _cellEditingTemplate;
	private readonly object _cellEditingTemplateResourceKey;
	private IDataTemplate _cellTemplate;
	private readonly object _cellTemplateResourceKey;
	private readonly Func<Control, IDataTemplate> _getCellTemplate;
	private readonly Func<Control, IDataTemplate> _getEditingCellTemplate;

	#endregion

	#region Constructors

	public TemplateColumn(
		object header,
		IDataTemplate cellTemplate,
		IDataTemplate cellEditingTemplate = null,
		GridLength? width = null,
		TemplateColumnOptions<TModel> options = null)
		: base(header, width, options ?? new())
	{
		_getCellTemplate = GetCellTemplate;
		_cellTemplate = cellTemplate;
		_cellEditingTemplate = cellEditingTemplate;
		_getEditingCellTemplate = cellEditingTemplate is not null ? GetCellEditingTemplate : null;
	}

	public TemplateColumn(
		object header,
		object cellTemplateResourceKey,
		object cellEditingTemplateResourceKey = null,
		GridLength? width = null,
		TemplateColumnOptions<TModel> options = null)
		: base(header, width, options ?? new())
	{
		_cellTemplateResourceKey = cellTemplateResourceKey ??
			throw new ArgumentNullException(nameof(cellTemplateResourceKey));
		_cellEditingTemplateResourceKey = cellEditingTemplateResourceKey;
		_getCellTemplate = GetCellTemplate;
		_getEditingCellTemplate = cellEditingTemplateResourceKey is not null ? GetCellEditingTemplate : null;
	}

	#endregion

	#region Properties

	public new TemplateColumnOptions<TModel> Options => (TemplateColumnOptions<TModel>) base.Options;

	bool ITextSearchableColumn<TModel>.IsTextSearchEnabled => Options.IsTextSearchEnabled;

	#endregion

	#region Methods

	/// <summary>
	/// Creates a cell for this column on the specified row.
	/// </summary>
	/// <param name="row"> The row. </param>
	/// <returns> The cell. </returns>
	public override ICell CreateCell(IRow<TModel> row)
	{
		return new TemplateCell(row.Model, _getCellTemplate, _getEditingCellTemplate, Options);
	}

	/// <summary>
	/// Gets the template to use to display the contents of a cell that is in editing mode.
	/// </summary>
	public IDataTemplate GetCellEditingTemplate(Control anchor)
	{
		if (_cellEditingTemplate is not null)
		{
			return _cellEditingTemplate;
		}

		_cellEditingTemplate = anchor.FindResource(_cellEditingTemplateResourceKey!) as IDataTemplate;

		if (_cellEditingTemplate is null)
		{
			throw new KeyNotFoundException(
				$"No data template resource with the key of '{_cellEditingTemplateResourceKey}' " +
				"could be found for the template column '{Header}'.");
		}

		return _cellEditingTemplate;
	}

	/// <summary>
	/// Gets the template to use to display the contents of a cell that is not in editing mode.
	/// </summary>
	public IDataTemplate GetCellTemplate(Control anchor)
	{
		if (_cellTemplate is not null)
		{
			return _cellTemplate;
		}

		_cellTemplate = anchor.FindResource(_cellTemplateResourceKey!) as IDataTemplate;

		if (_cellTemplate is null)
		{
			throw new KeyNotFoundException(
				$"No data template resource with the key of '{_cellTemplateResourceKey}' " +
				$"could be found for the template column '{Header}'.");
		}

		return _cellTemplate;
	}

	public override Comparison<TModel> GetComparison(ListSortDirection direction)
	{
		return direction switch
		{
			ListSortDirection.Ascending => Options.CompareAscending,
			ListSortDirection.Descending => Options.CompareDescending,
			_ => null
		};
	}

	string ITextSearchableColumn<TModel>.SelectValue(TModel model)
	{
		return Options.TextSearchValueSelector?.Invoke(model);
	}

	#endregion
}