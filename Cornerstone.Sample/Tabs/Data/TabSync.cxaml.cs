#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Data;

[SourceReflection]
public partial class TabSync : UserControl<TabSyncViewModel>
{
	#region Constants

	public const string HeaderName = "Sync";

	#endregion

	#region Constructors

	public TabSync()
		: this(AppBootstrap.GetInstance<TabSyncViewModel>())
	{
	}

	[DependencyInjectionConstructor]
	public TabSync(TabSyncViewModel viewModel)
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		InitializeComponent();
	}

	#endregion
}