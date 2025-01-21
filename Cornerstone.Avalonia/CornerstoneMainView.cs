#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneMainView<T> : CornerstoneUserControl
{
	#region Constructors

	public CornerstoneMainView(T viewModel, IDispatcher dispatcher) : base(dispatcher)
	{
		UseSafeArea = true;
		ViewModel = viewModel;
		DataContext = ViewModel;
	}

	#endregion

	#region Properties

	public IInsetsManager InsetsManager { get; private set; }

	public Thickness SafeAreaPadding => InsetsManager?.SafeAreaPadding ?? default;

	public bool UseSafeArea { get; set; }

	public T ViewModel { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		CornerstoneApplication.RuntimeInformation.CompleteLoad();

		base.OnLoaded(e);

		var topLevel = TopLevel.GetTopLevel(this);

		if (InsetsManager != null)
		{
			InsetsManager.SafeAreaChanged -= InsetsManagerOnSafeAreaChanged;
		}

		InsetsManager = topLevel?.InsetsManager;

		if (InsetsManager != null)
		{
			InsetsManager.SafeAreaChanged += InsetsManagerOnSafeAreaChanged;
			InsetsManager.DisplayEdgeToEdge = true;
			InsetsManager.IsSystemBarVisible = true;
		}
	}

	private void InsetsManagerOnSafeAreaChanged(object sender, SafeAreaChangedArgs e)
	{
		RaiseSafeAreaChanged();
	}

	private void RaiseSafeAreaChanged()
	{
		OnPropertyChanged(nameof(SafeAreaPadding));
	}

	#endregion
}