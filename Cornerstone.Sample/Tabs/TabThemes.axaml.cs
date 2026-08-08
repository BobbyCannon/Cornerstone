#region References

using Cornerstone.Avalonia;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone.State;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Showcase Color / Mode / Density — bound to <see cref="AppSettings" /> for persistence.
/// </summary>
[SourceReflection]
public partial class TabThemes : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Themes";

	#endregion

	#region Constructors

	public TabThemes() : this(AppBootstrap.GetInstance<AppSettings>())
	{
	}

	[DependencyInjectionConstructor]
	public TabThemes(AppSettings settings)
	{
		Settings = settings;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public AppSettings Settings { get; }

	#endregion
}