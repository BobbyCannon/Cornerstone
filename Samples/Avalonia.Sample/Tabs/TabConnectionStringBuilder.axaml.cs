#region References

using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Storage;

#endregion

namespace Avalonia.Sample.Tabs;

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