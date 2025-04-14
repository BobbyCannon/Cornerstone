#region References

using System;
using Avalonia.Controls;
using Cornerstone.Location;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample;

internal static class ViewDependencyProvider
{
	#region Constructors

	static ViewDependencyProvider()
	{
		if (!Design.IsDesignMode)
		{
			throw new Exception("Do not use except in design mode.");
		}

		DependencyProvider = new DependencyProvider("Sample Design Time");
		RuntimeInformation = RuntimeInformationData.GetSample();

		LocationProvider = new LocationProvider();
		LocationProvider.CurrentValue.HorizontalLocation.Latitude = 33.751599;
		LocationProvider.CurrentValue.HorizontalLocation.Longitude = -84.390641;

		DependencyProvider.AddSingleton(LocationProvider);

		App.RegisterServices(DependencyProvider);

		DependencyProvider.SetupCornerstoneServices(
			runtimeInformation: RuntimeInformation
		);
	}

	#endregion

	#region Properties

	public static DependencyProvider DependencyProvider { get; }

	public static ILocationProvider LocationProvider { get; }

	public static IRuntimeInformation RuntimeInformation { get; set; }

	#endregion

	#region Methods

	public static T Get<T>()
	{
		return DependencyProvider.GetInstance<T>();
	}

	#endregion
}