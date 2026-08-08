#region References

using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms;

/// <summary>
/// No-op platform for TFMs without OS-specific implementations (e.g. plain net10.0).
/// </summary>
public class NullPlatform : CornerstoneObject, IPlatform
{
	#region Constructors

	public NullPlatform(DependencyProvider dependencyProvider, RuntimeInformation runtimeInformation)
	{
		DependencyProvider = dependencyProvider;
		RuntimeInformation = runtimeInformation;
	}

	#endregion

	#region Properties

	public DependencyProvider DependencyProvider { get; }

	public RuntimeInformation RuntimeInformation { get; }

	#endregion
}