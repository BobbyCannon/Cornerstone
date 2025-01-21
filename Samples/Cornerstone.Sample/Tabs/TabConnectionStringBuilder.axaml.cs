#region References

using Cornerstone.Avalonia;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabConnectionStringBuilder : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Connection String Builder";

	#endregion

	#region Constructors

	public TabConnectionStringBuilder()
	{
		DataContext = this;
		InitializeComponent();
	}

	#endregion
}