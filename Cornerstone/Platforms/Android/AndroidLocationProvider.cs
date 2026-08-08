#region References

using Cornerstone.Location;

#endregion

namespace Cornerstone.Platforms.Android;

/// <summary>
/// Implementation for LocationProvider
/// </summary>
public class AndroidLocationProvider : LocationProvider
{
	#region Properties

	public sealed override string ProviderName => "Cornerstone.Android";

	#endregion
}