#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone.State;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabControlsProgress : UserControl
{
	#region Constants

	public const string HeaderName = "Progress";

	#endregion

	#region Constructors

	public TabControlsProgress() : this(AppBootstrap.GetInstance<AppSettings>())
	{
	}

	[DependencyInjectionConstructor]
	public TabControlsProgress(AppSettings appSettings)
	{
		AppSettings = appSettings;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public AppSettings AppSettings { get; }

	#endregion
}