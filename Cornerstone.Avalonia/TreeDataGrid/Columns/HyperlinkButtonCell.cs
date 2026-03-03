#region References

using System.Windows.Input;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

public class HyperlinkButtonCell : Notifiable, ICell
{
	#region Constructors

	public HyperlinkButtonCell(ICommand command, object commandParameter, object value)
	{
		Command = command;
		CommandParameter = commandParameter;
		Value = value;
	}

	#endregion

	#region Properties

	public bool CanEdit => false;
	public ICommand Command { get; set; }
	public object CommandParameter { get; }
	public BeginEditGestures EditGestures => BeginEditGestures.None;
	public object Value { get; }

	#endregion
}