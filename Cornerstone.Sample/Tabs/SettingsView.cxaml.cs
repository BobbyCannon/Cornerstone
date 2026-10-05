#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class SettingsView : UserControl
{
	#region Constructors

	public SettingsView() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	[DependencyInjectionConstructor]
	public SettingsView(AppViewModel viewModel)
	{
		DataContext = viewModel;
		InitializeComponent();
	}

	#endregion
}
