#region References

using Cornerstone.Location;
using Foundation;

#endregion

namespace Cornerstone.Platforms.iOS;

[Preserve(AllMembers = true)]
public class IOSLocationProvider : LocationProvider
{
	#region Properties

	public sealed override string ProviderName => "Cornerstone.iOS";

	#endregion
}