#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabControlsTabs : UserControl
{
	#region Constants

	public const string HeaderName = "Tabs";

	#endregion

	#region Constructors

	public TabControlsTabs()
	{
		InitializeComponent();
	}

	#endregion
}