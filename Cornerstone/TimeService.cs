#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone;

/// <summary>
/// Represents the service to provide time. Allows control for when the system is being tested.
/// </summary>
public static class TimeService
{
	#region Fields

	private static bool _locked;
	private static readonly SpeedyList<IDateTimeProvider> _providers;

	#endregion

	#region Constructors

	static TimeService()
	{
		_providers = [];

		var realTime = new DateTimeProvider(Guid.Parse("48E21BDA-9E7A-4767-8E3B-B218203C9A71"), () => DateTime.UtcNow);
		realTime.LockProvider();

		RealTime = realTime;
		Reset();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents the time service last provider.
	/// </summary>
	public static IDateTimeProvider CurrentTime => _providers.LastOrDefault() ?? RealTime;

	/// <summary>
	/// Represents the systems real time (DateTime.Now / DateTime.UtcNow).
	/// </summary>
	public static IDateTimeProvider RealTime { get; }

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
	public static IDateTimeProvider PushProvider(Func<DateTime> provider)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.TimeServiceLocked]);
		}

		var response = new DateTimeProvider(provider);
		_providers.Add(response);
		return response;
	}

	/// <summary>
	/// Add a new DateTime provider onto the stack.
	/// </summary>
	public static void PushProvider(IDateTimeProvider provider)
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
	public static void RemoveProvider(IDateTimeProvider provider)
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
	public static void Reset(IDateTimeProvider provider = null)
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