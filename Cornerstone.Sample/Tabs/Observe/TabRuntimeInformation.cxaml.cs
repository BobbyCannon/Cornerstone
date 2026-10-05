#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Observe;

[SourceReflection]
public partial class TabRuntimeInformation : UserControl
{
	#region Constants

	public const string HeaderName = "Runtime Information";

	#endregion

	#region Constructors

	public TabRuntimeInformation() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabRuntimeInformation(IRuntimeInformation runtimeInformation)
	{
		RuntimeInformation = runtimeInformation;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool HasPlatformOverrides => PlatformOverrides.Count > 0;

	public IReadOnlyDictionary<string, object> PlatformOverrides => (RuntimeInformation as RuntimeInformation)?.PlatformOverrides ?? new Dictionary<string, object>();

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}