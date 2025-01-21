#region References

using Cornerstone.Avalonia;
using Cornerstone.Sample.ViewModels;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabPropertyGrid : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Property Grid";

	#endregion

	#region Constructors

	public TabPropertyGrid()
	{
		ApplicationSettings = GetInstance<ApplicationSettings>();
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ApplicationSettings ApplicationSettings { get; }

	#endregion
}