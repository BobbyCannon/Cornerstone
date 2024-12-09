#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneMainView<T> : CornerstoneUserControl
{
	#region Constructors

	public CornerstoneMainView(T viewModel, IDispatcher dispatcher) : base(dispatcher)
	{
		ViewModel = viewModel;
		DataContext = ViewModel;
	}

	#endregion

	#region Properties

	public T ViewModel { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		CornerstoneApplication.RuntimeInformation.CompleteLoad();

		base.OnLoaded(e);

		var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;

		if (insetsManager != null)
		{
			insetsManager.DisplayEdgeToEdge = false;
			insetsManager.IsSystemBarVisible = false;
		}
	}

	#endregion
}