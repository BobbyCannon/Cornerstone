#region References

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

public class TemplateCell : ICell, IEditableObject
{
	#region Fields

	private readonly ITemplateCellOptions _options;

	#endregion

	#region Constructors

	public TemplateCell(
		object value,
		Func<Control, IDataTemplate> getCellTemplate,
		Func<Control, IDataTemplate> getCellEditingTemplate,
		ITemplateCellOptions options)
	{
		GetCellTemplate = getCellTemplate;
		GetCellEditingTemplate = getCellEditingTemplate;
		Value = value;
		_options = options;
	}

	#endregion

	#region Properties

	public bool CanEdit => GetCellEditingTemplate is not null;
	public BeginEditGestures EditGestures => _options?.BeginEditGestures ?? BeginEditGestures.Default;
	public Func<Control, IDataTemplate> GetCellEditingTemplate { get; }
	public Func<Control, IDataTemplate> GetCellTemplate { get; }
	public object Value { get; }

	#endregion

	#region Methods

	void IEditableObject.BeginEdit()
	{
		(Value as IEditableObject)?.BeginEdit();
	}

	void IEditableObject.CancelEdit()
	{
		(Value as IEditableObject)?.CancelEdit();
	}

	void IEditableObject.EndEdit()
	{
		(Value as IEditableObject)?.EndEdit();
	}

	#endregion
}