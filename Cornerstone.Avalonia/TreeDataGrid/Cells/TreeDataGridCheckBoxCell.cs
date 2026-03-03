#region References

using System;
using System.ComponentModel;
using Avalonia;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Cells;

public class TreeDataGridCheckBoxCell : TreeDataGridCell
{
	#region Fields

	public static readonly DirectProperty<TreeDataGridCheckBoxCell, bool> IsReadOnlyProperty;
	public static readonly DirectProperty<TreeDataGridCheckBoxCell, bool> IsThreeStateProperty;
	public static readonly DirectProperty<TreeDataGridCheckBoxCell, bool?> ValueProperty;

	private bool _isReadOnly;
	private bool _isThreeState;
	private bool? _value;

	#endregion

	#region Constructors

	static TreeDataGridCheckBoxCell()
	{
		IsReadOnlyProperty = AvaloniaProperty.RegisterDirect<TreeDataGridCheckBoxCell, bool>(nameof(IsReadOnly), o => o.IsReadOnly, (o, v) => o.IsReadOnly = v);
		IsThreeStateProperty = AvaloniaProperty.RegisterDirect<TreeDataGridCheckBoxCell, bool>(nameof(IsThreeState), o => o.IsThreeState, (o, v) => o.IsThreeState = v);
		ValueProperty = AvaloniaProperty.RegisterDirect<TreeDataGridCheckBoxCell, bool?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);
	}

	#endregion

	#region Properties

	public bool IsReadOnly
	{
		get => _isReadOnly;
		set => SetAndRaise(IsReadOnlyProperty, ref _isReadOnly, value);
	}

	public bool IsThreeState
	{
		get => _isThreeState;
		set => SetAndRaise(IsThreeStateProperty, ref _isThreeState, value);
	}

	public bool? Value
	{
		get => _value;
		set
		{
			if (SetAndRaise(ValueProperty, ref _value, value))
			{
				if (Model is CheckBoxCell cell)
				{
					cell.Value = value;
				}
				RaiseCellValueChanged();
			}
		}
	}

	#endregion

	#region Methods

	public override void Realize(
		TreeDataGridElementFactory factory,
		ITreeDataGridSelectionInteraction selection,
		ICell model,
		int columnIndex,
		int rowIndex)
	{
		if (model is CheckBoxCell cell)
		{
			IsReadOnly = cell.IsReadOnly;
			IsThreeState = cell.IsThreeState;
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

	protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		base.OnModelPropertyChanged(sender, e);

		if ((e.PropertyName == nameof(CheckBoxCell.Value)) && Model is CheckBoxCell checkBoxCell)
		{
			Value = checkBoxCell.Value;
		}
	}

	#endregion
}