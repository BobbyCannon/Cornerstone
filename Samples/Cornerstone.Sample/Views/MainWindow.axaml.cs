#region References

using System;
using Avalonia.Controls;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Sample.ViewModels;

#endregion

namespace Cornerstone.Sample.Views;

public partial class MainWindow : CornerstoneWindow<MainViewModel>
{
	#region Constructors

	public MainWindow() : this(ViewDependencyProvider.Get<MainViewModel>(), null)
	{
	}

	public MainWindow(MainViewModel viewModel, IDispatcher dispatcher) : base(viewModel, dispatcher)
	{
		// Prevent the designer from null referencing
		if (!Design.IsDesignMode)
		{
			RestoreWindowLocation(ViewModel.ApplicationSettings.MainWindowLocation);
		}

		MainView = new MainView(viewModel, dispatcher);
		InitializeComponent();
	}

	#endregion

	#region Properties

	public MainView MainView { get; }

	#endregion

	#region Methods

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		CornerstoneApplication.RuntimeInformation.Shutdown();
		CornerstoneDispatcher.Instance.IsEnabled = false;

		ViewModel.ApplicationSettings.MainWindowLocation.UpdateWith(GetWindowLocation());
		ViewModel.Uninitialize();

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

	#endregion
}