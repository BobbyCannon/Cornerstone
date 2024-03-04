#region References

using System;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Represents a time provider
/// </summary>
public class TimeProvider : ITimeProvider
{
	#region Fields

	private bool _locked;
	private Func<DateTime> _provider;
	private readonly Guid _providerId;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize an provider for date time.
	/// </summary>
	public TimeProvider(Func<DateTime> provider)
		: this(Guid.NewGuid(), provider)
	{
	}

	/// <summary>
	/// Initialize an provider for date time.
	/// </summary>
	public TimeProvider(Guid providerId, DateTime dateTime)
		: this(providerId, () => dateTime)
	{
	}

	/// <summary>
	/// Initialize an provider for date time.
	/// </summary>
	public TimeProvider(Guid providerId, Func<DateTime> provider)
	{
		_provider = provider;
		_providerId = providerId;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DateTime Now => _provider.Invoke().ToLocalTime();

	/// <inheritdoc />
	public DateTime UtcNow => _provider.Invoke().ToUniversalTime();

	#endregion

	#region Methods

	/// <inheritdoc />
	public Guid GetProviderId()
	{
		return _providerId;
	}

	/// <summary>
	/// Lock the provider to not allow the time service to changed. Once locked
	/// the provider can no longer be changes or updated.
	/// </summary>
	public void LockProvider()
	{
		_locked = true;
	}

	/// <summary>
	/// Update the time for the provider.
	/// </summary>
	/// <param name="time"> </param>
	public void UpdateDateTime(DateTime time)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower["TimeProviderLocked"]);
		}

		_provider = () => time;
	}

	#endregion
}