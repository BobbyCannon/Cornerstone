#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Maps Cornerstone visual-tree presence onto <see cref="DispatchableViewModel.IsAttached" />.
/// ViewModel and DataContext are handled independently.
/// Attach/Detach are idempotent per owner, so the same instance on both properties is safe.
/// Never sets or clears control properties.
/// </summary>
public static class DispatchableVisualTree
{
	#region Methods

	public static void OnAttachedToVisualTree(object owner, object viewModel, object dataContext)
	{
		(viewModel as DispatchableViewModel)?.Attach(owner);
		(dataContext as DispatchableViewModel)?.Attach(owner);
	}

	public static void OnDataContextChanged(
		object owner,
		object oldDataContext,
		object newDataContext,
		object currentViewModel,
		bool onVisualTree)
	{
		if (!onVisualTree)
		{
			return;
		}

		if (oldDataContext is DispatchableViewModel oldDc
			&& !ReferenceEquals(oldDc, currentViewModel))
		{
			oldDc.Detach(owner);
		}

		(newDataContext as DispatchableViewModel)?.Attach(owner);
	}

	public static void OnDetachedFromVisualTree(object owner, object viewModel, object dataContext)
	{
		(viewModel as DispatchableViewModel)?.Detach(owner);
		(dataContext as DispatchableViewModel)?.Detach(owner);
	}

	public static void OnViewModelChanged(
		object owner,
		object oldViewModel,
		object newViewModel,
		object currentDataContext,
		bool onVisualTree)
	{
		if (!onVisualTree)
		{
			return;
		}

		if (oldViewModel is DispatchableViewModel oldVm
			&& !ReferenceEquals(oldVm, currentDataContext))
		{
			oldVm.Detach(owner);
		}

		(newViewModel as DispatchableViewModel)?.Attach(owner);
	}

	#endregion
}
