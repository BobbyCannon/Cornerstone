#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Inputs;

[SourceReflection]
public partial class TabInputsNumberBox : UserControl
{
	#region Constants

	public const string HeaderName = "Number Box";

	#endregion

	#region Constructors

	public TabInputsNumberBox()
	{
		InitializeComponent();
	}

	#endregion
}
