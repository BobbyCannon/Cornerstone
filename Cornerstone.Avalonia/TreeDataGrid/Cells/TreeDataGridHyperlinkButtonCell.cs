#region References

using System;
using System.Windows.Input;
using Avalonia;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Cells;

public class TreeDataGridHyperlinkButtonCell : TreeDataGridCell
{
	#region Fields

	public static readonly DirectProperty<TreeDataGridHyperlinkButtonCell, object> CommandParameterProperty;
	public static readonly DirectProperty<TreeDataGridHyperlinkButtonCell, ICommand> CommandProperty;
	public static readonly DirectProperty<TreeDataGridHyperlinkButtonCell, object> ValueProperty;

	private ICommand _command;
	private object _commandParameter;
	private object _value;

	#endregion

	#region Constructors

	static TreeDataGridHyperlinkButtonCell()
	{
		CommandParameterProperty = AvaloniaProperty.RegisterDirect<TreeDataGridHyperlinkButtonCell, object>(nameof(CommandParameter), o => o.CommandParameter, (o, v) => o.CommandParameter = v);
		CommandProperty = AvaloniaProperty.RegisterDirect<TreeDataGridHyperlinkButtonCell, ICommand>(nameof(Command), o => o.Command, (o, v) => o.Command = v);
		ValueProperty = AvaloniaProperty.RegisterDirect<TreeDataGridHyperlinkButtonCell, object>(nameof(Value), o => o.Value, (o, v) => o.Value = v);
	}

	#endregion

	#region Properties

	public ICommand Command
	{
		get => _command;
		set => SetAndRaise(CommandProperty, ref _command, value);
	}

	public object CommandParameter
	{
		get => _commandParameter;
		set => SetAndRaise(CommandParameterProperty, ref _commandParameter, value);
	}

	public object Value
	{
		get => _value;
		set => SetAndRaise(ValueProperty, ref _value, value);
	}

	#endregion

	#region Methods

	public override void Realize(TreeDataGridElementFactory factory, ITreeDataGridSelectionInteraction selection, ICell model, int columnIndex, int rowIndex)
	{
		if (model is HyperlinkButtonCell cell)
		{
			Command = cell.Command;
			CommandParameter = cell.CommandParameter;
			Value = cell.Value;
		}
		else
		{
			throw new InvalidOperationException("Invalid cell model.");
		}

		base.Realize(factory, selection, model, columnIndex, rowIndex);
		SubscribeToModelChanges();
	}

	public override void Unrealize()
	{
		UnsubscribeFromModelChanges();
		base.Unrealize();
	}

	#endregion
}