#region References

using System;
using Avalonia.Controls;
using Avalonia.Sample.ViewModels;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Sample.Views;

public partial class MainWindow : CornerstoneWindow
{
	#region Constructors

	public MainWindow() : this(DesignModeDependencyProvider.Get<MainViewModel>(), null)
	{
	}

	public MainWindow(MainViewModel viewModel, IDispatcher dispatcher) : base(dispatcher)
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		MainView = new MainView(ViewModel, dispatcher);

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
		CornerstoneApplication.RuntimeInformation.Shutdown();
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