#region References

using System;
using System.Windows.Input;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

public class PressHoldButtonCell : Notifiable, ICell
{
	#region Constructors

	public PressHoldButtonCell(ICommand command, object commandParameter, object value, TimeSpan holdDuration, double? height)
	{
		Command = command;
		CommandParameter = commandParameter;
		Value = value;
		HoldDuration = holdDuration;
		Height = height;
	}

	#endregion

	#region Properties

	public bool CanEdit => false;
	public ICommand Command { get; set; }
	public object CommandParameter { get; }
	public BeginEditGestures EditGestures => BeginEditGestures.None;
	public double? Height { get; }
	public TimeSpan HoldDuration { get; }
	public object Value { get; }

	#endregion
}