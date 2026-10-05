#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Data;

[SourceReflection]
public partial class TabSyncMesh : UserControl<TabSyncMeshViewModel>
{
	#region Constants

	public const string HeaderName = "Mesh";

	#endregion

	#region Constructors

	public TabSyncMesh()
		: this(AppBootstrap.GetInstance<TabSyncMeshViewModel>())
	{
	}

	[DependencyInjectionConstructor]
	public TabSyncMesh(TabSyncMeshViewModel viewModel)
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		InitializeComponent();
	}

	#endregion
}
