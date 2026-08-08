#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Keystone.Lifecycle;

#endregion

namespace Cornerstone.Location;

public class LocationProviderStub : LocationProvider
{
	#region Properties

	public override string ProviderName => "Stub";

	#endregion
}

public abstract class LocationProvider : IInitializableLifecycle, ILocationProvider
{
	#region Fields

	private bool _isLifecycleInitialized;

	#endregion

	#region Constructors

	protected LocationProvider()
	{
		CurrentValue = new Location();
	}

	#endregion

	#region Properties

	public Location CurrentValue { get; }

	public abstract string ProviderName { get; }

	public string Status { get; protected set; }

	#endregion

	#region Methods

	public virtual Task<Location> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null)
	{
		return Task.FromResult(CurrentValue);
	}

	public virtual void InitializeLifecycle()
	{
		_isLifecycleInitialized = true;
	}

	public bool IsLifecycleInitialized()
	{
		return _isLifecycleInitialized;
	}

	public virtual void UninitializeLifecycle()
	{
		_isLifecycleInitialized = false;
	}

	#endregion
}

/// <summary>
/// Represents a location provider using all Cornerstone data models.
/// </summary>
public interface ILocationProvider
{
	#region Properties

	public string ProviderName { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets position async with specified parameters.
	/// </summary>
	/// <param name="timeout"> Timeout to wait, Default Infinite </param>
	/// <param name="cancelToken"> Cancellation token </param>
	/// <returns> The current location for this provider. </returns>
	Task<Location> GetCurrentLocationAsync(TimeSpan? timeout = null, CancellationToken? cancelToken = null);

	#endregion
}