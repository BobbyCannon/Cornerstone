#region References

using System;
using System.Windows.Input;
using Avalonia;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Cells;

public class TreeDataGridPressHoldButtonCell : TreeDataGridCell
{
	#region Fields

	public static readonly DirectProperty<TreeDataGridPressHoldButtonCell, object> CommandParameterProperty;
	public static readonly DirectProperty<TreeDataGridPressHoldButtonCell, ICommand> CommandProperty;
	public static readonly DirectProperty<TreeDataGridPressHoldButtonCell, TimeSpan> HoldDurationProperty;
	public static readonly DirectProperty<TreeDataGridPressHoldButtonCell, object> ValueProperty;

	private ICommand _command;
	private object _commandParameter;
	private TimeSpan _holdDuration;
	private object _value;

	#endregion

	#region Constructors

	static TreeDataGridPressHoldButtonCell()
	{
		CommandParameterProperty = AvaloniaProperty.RegisterDirect<TreeDataGridPressHoldButtonCell, object>(nameof(CommandParameter), o => o.CommandParameter, (o, v) => o.CommandParameter = v);
		CommandProperty = AvaloniaProperty.RegisterDirect<TreeDataGridPressHoldButtonCell, ICommand>(nameof(Command), o => o.Command, (o, v) => o.Command = v);
		HoldDurationProperty = AvaloniaProperty.RegisterDirect<TreeDataGridPressHoldButtonCell, TimeSpan>(nameof(HoldDuration), o => o.HoldDuration, (o, v) => o.HoldDuration = v);
		ValueProperty = AvaloniaProperty.RegisterDirect<TreeDataGridPressHoldButtonCell, object>(nameof(Value), o => o.Value, (o, v) => o.Value = v);
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

	public TimeSpan HoldDuration
	{
		get => _holdDuration;
		set => SetAndRaise(HoldDurationProperty, ref _holdDuration, value);
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
		if (model is PressHoldButtonCell cell)
		{
			Command = cell.Command;
			CommandParameter = cell.CommandParameter;
			HoldDuration = cell.HoldDuration;
			Value = cell.Value;
			Height = cell.Height ?? 32;
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