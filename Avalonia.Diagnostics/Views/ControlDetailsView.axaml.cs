#region References

using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.TreeDataGrid;

#endregion

namespace Avalonia.Diagnostics.Views;

public partial class ControlDetailsView : CornerstoneUserControl<ControlDetailsViewModel>
{
	#region Fields

	private readonly TreeDataGrid _treeDataGrid;

	#endregion

	#region Constructors

	public ControlDetailsView()
	{
		InitializeComponent();

		_treeDataGrid = this.GetControl<TreeDataGrid>("TreeDataGrid");
	}

	#endregion

	#region Methods

	public void PropertyNamePressed(object sender, PointerPressedEventArgs e)
	{
		var viewModel = (ControlDetailsViewModel) DataContext;

		if (viewModel is null)
		{
			return;
		}

		if (sender is Control { DataContext: SetterViewModel setterVm })
		{
			viewModel.SelectProperty(setterVm.Property);

			if (viewModel.SelectedProperty is not null)
			{
				_treeDataGrid.ScrollIntoView(viewModel.SelectedProperty);
			}
		}
	}

	private void PropertiesGrid_OnDoubleTapped(object sender, TappedEventArgs e)
	{
		if (sender is TreeDataGrid { DataContext: ControlDetailsViewModel controlDetails })
		{
			controlDetails.NavigateToSelectedProperty();
		}
	}

	#endregion
}