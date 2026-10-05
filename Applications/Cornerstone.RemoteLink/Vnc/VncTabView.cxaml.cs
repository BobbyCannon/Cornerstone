#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Theme;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.RemoteLink.Vnc;

[SourceReflection]
public partial class VncTabView : UserControl<VncTabViewModel>
{
	#region Constructors

	[DependencyInjectionConstructor]
	public VncTabView()
	{
		InitializeComponent();
	}

	#endregion
}
