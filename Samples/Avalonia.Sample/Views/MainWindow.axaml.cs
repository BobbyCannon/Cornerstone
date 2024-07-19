#region References

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Sample.ViewModels;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Views;

public partial class MainWindow : CornerstoneWindow
{
	#region Constructors

	public MainWindow() : this(ViewModelProviderForDesignMode.Get<MainViewModel>())
	{
	}

	public MainWindow(MainViewModel viewModel)
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		MainView = new MainView(ViewModel);

		InitializeComponent();
	}

	#endregion

	#region Properties

	public MainView MainView { get; }

	public MainViewModel ViewModel { get; }

	#endregion

	#region Methods

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		CornerstoneRuntimeInformation.Instance.Shutdown();
		CornerstoneDispatcher.Instance.IsEnabled = false;

		ViewModel.Uninitialize();
		ViewModel.ApplicationSettings.MainWindowLocation.UpdateWith(GetWindowLocation());
		ViewModel.ApplicationSettings.Save();

		base.OnClosing(e);
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		if (Design.IsDesignMode)
		{
			// more changes?
		}

		if (ViewModel.RuntimeInformation.ApplicationIsElevated)
		{
			Title += " (administrator)";
		}

		base.OnInitialized();
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		CornerstoneRuntimeInformation.Instance.CompleteLoad();
		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnOpened(EventArgs e)
	{
		// Prevent the designer from null referencing
		if ((ViewModel?.ApplicationSettings?.MainWindowLocation != null) && !Design.IsDesignMode)
		{
			RestoreWindowLocation(ViewModel.ApplicationSettings.MainWindowLocation);
		}

		base.OnOpened(e);
	}

	#endregion
}