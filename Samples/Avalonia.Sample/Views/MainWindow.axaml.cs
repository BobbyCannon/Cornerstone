#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Views;

public partial class MainWindow : CornerstoneWindow
{
	#region Constructors

	public MainWindow()
	{
		InitializeComponent();

		Dispatcher.UIThread.Invoke(() => WindowState = WindowState.Maximized);
		
	}

	#endregion

	#region Methods

	private void ButtonToggleMaximize(object sender, RoutedEventArgs e)
	{
		WindowState = WindowState == WindowState.Maximized
			? WindowState.Normal
			: WindowState.Maximized;
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		switch (change.Property.Name)
		{
			case nameof(WindowState):
			{
				MainView.AddHistory($"Window State: {WindowState}");
				break;
			}
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}