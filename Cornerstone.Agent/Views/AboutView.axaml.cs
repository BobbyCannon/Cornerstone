#region References

using Cornerstone.Avalonia;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
public partial class AboutView : CornerstoneUserControl<AboutViewModel>
{
	#region Constructors

	public AboutView()
	{
		InitializeComponent();
	}

	#endregion
}