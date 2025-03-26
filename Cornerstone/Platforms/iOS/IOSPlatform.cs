namespace Cornerstone.Platforms.iOS;

public static class IOSPlatform
{
	#region Properties

	public static DependencyProvider DependencyProvider { get; private set; }

	#endregion

	#region Methods

	public static void Initialize(DependencyProvider dependencyProvider)
	{
		DependencyProvider = dependencyProvider;
	}

	#endregion
}