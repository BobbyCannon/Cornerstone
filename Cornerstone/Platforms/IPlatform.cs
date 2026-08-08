#region References

using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms;

/// <summary>
/// Platform host: TFM-specific DI registration and device overrides, driven by lifecycle phases.
/// </summary>
public interface IPlatform : ILifecycle
{
	#region Properties

	/// <summary>
	/// The dependency provider used for platform service registration.
	/// </summary>
	DependencyProvider DependencyProvider { get; }

	/// <summary>
	/// Runtime information that receives platform overrides.
	/// </summary>
	RuntimeInformation RuntimeInformation { get; }

	#endregion
}