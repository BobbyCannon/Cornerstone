#region References

using System.Runtime.InteropServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Interactivity;

#endregion

namespace PInvoke;

public partial class MainWindow : Window
{
	#region Constructors

	public MainWindow()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	[DllImport("libhello")]
	private static extern int Add(int a, int b);

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		Add(1, 2);
	}

	#endregion
}