#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.DesignTime;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// App view. Declares its own <see cref="ViewModel" /> (does not use
/// <c> UserControl&lt;T&gt; </c>), so it overrides <see cref="GetViewModel" /> to return that property.
/// DataContext IsAttached is handled by the non-generic <see cref="UserControl" /> base.
/// </summary>
public partial class CornerstoneAppView<T>
	: UserControl where T : class
{
	#region Constructors

	public CornerstoneAppView()
	{
		if (!Design.IsDesignMode)
		{
			return;
		}

		var designData = CreateDesignData();
		if (designData != null)
		{
			DataContext ??= designData;
		}
		if (DataContext is ViewModel viewModel
			&& !viewModel.IsLifecycleInitialized())
		{
			viewModel.InitializeLifecycle();
			viewModel.LoadLifecycle();
			viewModel.StartLifecycle();
		}
	}

	public CornerstoneAppView(T viewModel) : this()
	{
		DataContext = viewModel;
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial T ViewModel { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Design-time sample for this app view. Default resolves <typeparamref name="T"/> from DI.
	/// Override when the real type is session-scoped or not DI-constructible.
	/// </summary>
	protected virtual T CreateDesignData()
	{
		return AppBootstrap.GetInstance<T>();
	}

	/// <inheritdoc />
	protected override object GetViewModel()
	{
		return ViewModel;
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if ((change.Property == DataContextProperty)
			&& DataContext is T viewModel)
		{
			ViewModel = viewModel;
		}

		if (change.Property == ViewModelProperty)
		{
			DispatchableVisualTree.OnViewModelChanged(
				this, change.OldValue, change.NewValue, DataContext, VisualRoot != null);
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}
