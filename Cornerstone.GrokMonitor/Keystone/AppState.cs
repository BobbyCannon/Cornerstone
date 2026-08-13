#region References

using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone.State;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.Keystone;

/// <summary>
/// Root app state for Grok Monitor. Domain data lives under feature slices (GrokUsage).
/// </summary>
[SourceReflection]
[DependencyInjected]
public partial class AppState : KeystoneState
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppState(AppSettings settings, GrokUsageState grokUsage)
	{
		Settings = Track(settings);
		GrokUsage = grokUsage;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Local Grok CLI usage homes (Personal / Work). Processors update; the usage tab projects.
	/// </summary>
	public GrokUsageState GrokUsage { get; }

	/// <summary>
	/// Persisted shell settings (theme, etc.). Loaded via lifecycle Track.
	/// </summary>
	public AppSettings Settings { get; }

	#endregion
}