#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using TimeProvider = Cornerstone.Runtime.TimeProvider;

#endregion

namespace Cornerstone;

/// <summary>
/// Represents the service to provide time. Allows control for when the system is being tested.
/// </summary>
public static class TimeService
{
	#region Fields

	private static bool _locked;
	private static readonly SpeedyList<ITimeProvider> _providers;

	#endregion

	#region Constructors

	static TimeService()
	{
		_providers = [];

		RealTime = new TimeProvider(Guid.Parse("48E21BDA-9E7A-4767-8E3B-B218203C9A71"), () => DateTime.UtcNow);

		Reset();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents the time service last provider.
	/// </summary>
	public static ITimeProvider CurrentTime => _providers.LastOrDefault() ?? RealTime;

	/// <summary>
	/// Represents the systems real time (DateTime.Now / DateTime.UtcNow).
	/// </summary>
	public static ITimeProvider RealTime { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Lock the service to not allow the time service to change.
	/// </summary>
	public static void LockService()
	{
		_locked = true;
	}

	/// <summary>
	/// Remove the current provider from the stack
	/// </summary>
	public static void PopProvider()
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.TimeServiceLocked]);
		}

		var provider = _providers.LastOrDefault();
		_providers.Remove(provider);
	}

	/// <summary>
	/// Add a new DateTime provider onto the stack.
	/// </summary>
	public static TimeProvider PushProvider(Func<DateTime> provider)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.TimeServiceLocked]);
		}

		var response = new TimeProvider(provider);
		_providers.Add(response);
		return response;
	}

	/// <summary>
	/// Add a new DateTime provider onto the stack.
	/// </summary>
	public static void PushProvider(ITimeProvider provider)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.TimeServiceLocked]);
		}

		_providers.Add(provider);
	}

	/// <summary>
	/// Remove the provider from the stack
	/// </summary>
	public static void RemoveProvider(Guid providerId)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.TimeServiceLocked]);
		}

		_providers.RemoveWhere(x => x.GetProviderId() == providerId);
	}

	/// <summary>
	/// Remove the provider from the stack
	/// </summary>
	public static void RemoveProvider(ITimeProvider provider)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.TimeServiceLocked]);
		}

		_providers.Remove(provider);
	}

	/// <summary>
	/// Resets the providers to the default values.
	/// </summary>
	public static void Reset(ITimeProvider provider = null)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.TimeServiceLocked]);
		}

		_providers.Clear();

		if (provider != null)
		{
			_providers.Add(provider);
		}
	}

	#endregion
}

/// <summary>
/// Represents the service to provide time. Allows control for when the system is being tested.
/// </summary>
public interface ITimeProvider : IProvider
{
	#region Properties

	/// <summary>
	/// Gets the DateTime in local time.
	/// </summary>
	public DateTime Now { get; }

	/// <summary>
	/// Gets the DateTime in UTC.
	/// </summary>
	public DateTime UtcNow { get; }

	#endregion
}