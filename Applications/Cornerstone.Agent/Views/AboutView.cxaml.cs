#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Theme;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
public partial class AboutView : UserControl<AboutViewModel>
{
	#region Constructors

	public AboutView()
	{
		InitializeComponent();
	}

	#endregion
}