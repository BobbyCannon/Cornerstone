#region References

using Cornerstone.Avalonia;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabProgress : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Progress";

	#endregion

	#region Constructors

	public TabProgress() : this(GetInstance<ApplicationSettings>())
	{
	}

	[DependencyInjectionConstructor]
	public TabProgress(ApplicationSettings applicationSettings)
	{
		ApplicationSettings = applicationSettings;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ApplicationSettings ApplicationSettings { get; }

	#endregion
}