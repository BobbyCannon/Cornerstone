#region References

using System.ComponentModel;
using System.Windows;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Wpf;
using Sample.Shared;

#endregion

namespace Sample.Client.Wpf.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	#region Constructors

	public MainWindow()
	{
		InitializeComponent();

		var dispatcher = new WpfDispatcher(Dispatcher);
		var runtimeInformation = new RuntimeInformation(dispatcher);

		ViewModel = new SharedViewModel(runtimeInformation, dispatcher);
		DataContext = ViewModel;

		Loaded += OnLoaded;
	}

	#endregion

	#region Properties

	public SharedViewModel ViewModel { get; }

	#endregion

	#region Methods

	protected override void OnClosing(CancelEventArgs e)
	{
		ViewModel.CancellationPending = true;
		ViewModel.WaitUntil(x => !x.IsRunning, 1000, 10);
		base.OnClosing(e);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
	}

	#endregion
}