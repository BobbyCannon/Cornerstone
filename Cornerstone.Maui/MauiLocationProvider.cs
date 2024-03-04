#region References

using Cornerstone.Data;
using Cornerstone.Location;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#if (ANDROID)
using Cornerstone.Maui.Platforms.Android;

#elif (IOS)
using Cornerstone.Maui.Platforms.iOS;

#elif (MACCATALYST)
using Cornerstone.Maui.Platforms.MacCatalyst;

#elif (WINDOWS)
using Cornerstone.Maui.Platforms.Windows;
#endif

#endregion

namespace Cornerstone.Maui;

/// <summary>
/// Implementation for LocationProvider.
/// </summary>
public class MauiLocationProvider
	: MauiLocationProvider<Location.Location, IHorizontalLocation, IVerticalLocation, LocationProviderSettings>, ILocationProvider
{
	#region Constructors

	/// <inheritdoc />
	public MauiLocationProvider(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion
}

/// <summary>
/// Implementation for LocationProvider.
/// </summary>
public class MauiLocationProvider<T, TH, TV, T2>
	: LocationProviderImplementation<T, TH, TV, T2>
	where T : class, ILocation<TH, TV>, ICloneable<T>, IUpdateable, new()
	where TH : class, IHorizontalLocation, IUpdateable
	where TV : class, IVerticalLocation, IUpdateable
	where T2 : LocationProviderSettings, new()
{
	#region Constructors

	/// <inheritdoc />
	public MauiLocationProvider(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion
}