#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Keystone;

[SourceReflection]
public partial class TabKeystone : UserControl
{
	#region Constants

	public const string HeaderName = "Keystone";

	#endregion

	#region Constructors

	public TabKeystone()
	{
		InitializeComponent();
	}

	#endregion
}
