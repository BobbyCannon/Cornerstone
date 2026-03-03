#region References

using System;
using Avalonia.Input;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Selection;

/// <summary>
/// Defines the interaction between a <see cref="TreeDataGrid" /> and an
/// <see cref="ITreeDataGridSelection" /> model.
/// </summary>
public interface ITreeDataGridSelectionInteraction
{
	#region Methods

	bool IsCellSelected(int columnIndex, int rowIndex)
	{
		return false;
	}

	bool IsRowSelected(IRow rowModel)
	{
		return false;
	}

	bool IsRowSelected(int rowIndex)
	{
		return false;
	}

	public void OnKeyDown(TreeDataGrid sender, KeyEventArgs e)
	{
	}

	public void OnKeyUp(TreeDataGrid sender, KeyEventArgs e)
	{
	}

	public void OnPointerMoved(TreeDataGrid sender, PointerEventArgs e)
	{
	}

	public void OnPointerPressed(TreeDataGrid sender, PointerPressedEventArgs e)
	{
	}

	public void OnPointerReleased(TreeDataGrid sender, PointerReleasedEventArgs e)
	{
	}

	public void OnPreviewKeyDown(TreeDataGrid sender, KeyEventArgs e)
	{
	}

	public void OnTextInput(TreeDataGrid sender, TextInputEventArgs e)
	{
	}

	#endregion

	#region Events

	public event EventHandler SelectionChanged;

	#endregion
}