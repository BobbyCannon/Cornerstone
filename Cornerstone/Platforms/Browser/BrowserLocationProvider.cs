#region References

using Cornerstone.Location;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Platforms.Browser;

/// <summary>
/// Implementation for LocationProvider
/// </summary>
[SourceReflection]
public class BrowserLocationProvider : LocationProvider
{
	#region Properties

	public sealed override string ProviderName => "Cornerstone.Browser";

	#endregion
}