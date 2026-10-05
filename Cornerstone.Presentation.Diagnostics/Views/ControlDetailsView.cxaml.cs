#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Diagnostics.ViewModels;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Controls.TreeDataGrid;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Diagnostics.Views;

public partial class ControlDetailsView : UserControl<ControlDetailsViewModel>
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