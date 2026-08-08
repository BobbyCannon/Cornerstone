#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia;

/// <summary>
/// Maps Avalonia visual-tree presence onto <see cref="DispatchableViewModel.IsAttached" />.
/// ViewModel and DataContext are handled independently.
/// <see cref="DispatchableViewModel.Attach" /> / <see cref="DispatchableViewModel.Detach" />
/// are idempotent per owner, so the same instance on both properties is safe.
/// Never sets or clears control properties.
/// </summary>
internal static class DispatchableVisualTree
{
	#region Methods

	/// <summary>
	/// Control entered the visual tree.
	/// </summary>
	public static void OnAttachedToVisualTree(object owner, object viewModel, object dataContext)
	{
		(viewModel as DispatchableViewModel)?.Attach(owner);
		(dataContext as DispatchableViewModel)?.Attach(owner);
	}

	/// <summary>
	/// DataContext changed while on the visual tree.
	/// Does not detach a value that is still the current ViewModel.
	/// </summary>
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

		// Keep ownership if ViewModel still references the old instance.
		if (oldDataContext is DispatchableViewModel oldDc
			&& !ReferenceEquals(oldDc, currentViewModel))
		{
			oldDc.Detach(owner);
		}

		(newDataContext as DispatchableViewModel)?.Attach(owner);
	}

	/// <summary>
	/// Control left the visual tree.
	/// </summary>
	public static void OnDetachedFromVisualTree(object owner, object viewModel, object dataContext)
	{
		(viewModel as DispatchableViewModel)?.Detach(owner);
		(dataContext as DispatchableViewModel)?.Detach(owner);
	}

	/// <summary>
	/// ViewModel changed while on the visual tree.
	/// Does not detach a value that is still the current DataContext.
	/// </summary>
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

		// Keep ownership if DataContext still references the old instance.
		if (oldViewModel is DispatchableViewModel oldVm
			&& !ReferenceEquals(oldVm, currentDataContext))
		{
			oldVm.Detach(owner);
		}

		(newViewModel as DispatchableViewModel)?.Attach(owner);
	}

	#endregion
}