#region References

using System.Collections.Generic;
using Cornerstone.Avalonia;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabRuntimeInformation : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Runtime Information";

	#endregion

	#region Constructors

	public TabRuntimeInformation() : this(CornerstoneApplication.GetInstance<IRuntimeInformation>())
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

	public IReadOnlyDictionary<string, object> PlatformOverrides => (RuntimeInformation as RuntimeInformation)?.PlatformOverrides ?? new Dictionary<string, object>();

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}