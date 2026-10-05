#region References

using System;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Time provider. RealTime is the OS wall clock. Tests inject a frozen or stepped provider.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    #region Fields

    private bool _locked;
    private Func<DateTime> _provider;
    private readonly Guid _providerId;

    #endregion

    #region Constructors

    /// <summary>
    /// Initialize a provider for date time.
    /// </summary>
    public DateTimeProvider(Func<DateTime> provider)
        : this(Guid.NewGuid(), provider)
    {
    }

    /// <summary>
    /// Initialize a provider for date time.
    /// </summary>
    public DateTimeProvider(Guid providerId, DateTime dateTime)
        : this(providerId, () => dateTime)
    {
    }

    /// <summary>
    /// Initialize a provider for date time.
    /// </summary>
    public DateTimeProvider(Guid providerId, Func<DateTime> provider)
    {
        _locked = false;
        _provider = provider;
        _providerId = providerId;
    }

    static DateTimeProvider()
    {
        var clock = new DateTimeRealTime();
        var realTime = new DateTimeProvider(Guid.Parse("48E21BDA-9E7A-4767-8E3B-B218203C9A71"), clock.GetUtcNow);
        realTime.LockProvider();
        RealTime = realTime;
    }

    #endregion

    #region Properties

    public DateTime Now => _provider.Invoke().ToLocalTime();

    /// <summary>
    /// Production clock. High-resolution UTC that follows the OS after sleep and clock changes.
    /// </summary>
    public static IDateTimeProvider RealTime { get; }

    public DateTime UtcNow => _provider.Invoke().ToUniversalTime();

    #endregion

    #region Methods

    public Guid GetProviderId()
    {
        return _providerId;
    }

    /// <summary>
    /// Lock the provider so the time service cannot be replaced. Once locked
    /// the provider can no longer be changed or updated.
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

/// <summary>
/// Represents the service to provide time. Allows control for when the system is being tested.
/// </summary>
public interface IDateTimeProvider
{
    #region Properties

    /// <summary>
    /// Gets the DateTime in the current time zone.
    /// </summary>
    /// <value> The current UTC date and time for the sync client. </value>
    DateTime Now { get; }

    /// <summary>
    /// Gets the DateTime in UTC.
    /// </summary>
    /// <value> The current UTC date and time for the sync client. </value>
    DateTime UtcNow { get; }

    #endregion
}