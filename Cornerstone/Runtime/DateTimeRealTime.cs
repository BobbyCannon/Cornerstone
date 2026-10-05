#region References

using System;
using System.Diagnostics;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Production UTC for DateTimeProvider.RealTime. Stopwatch interpolation that recaptures as soon as QPC falls behind the wall (sleep) and when it leads the wall by more than the recapture window.
/// </summary>
internal sealed class DateTimeRealTime
{
    #region Fields

    public static readonly TimeSpan DefaultRecaptureWindow;
    private readonly Func<long> _getTimestamp;
    private readonly Func<DateTime> _getWall;
    private readonly object _lock;
    private long _qpcAnchor;
    private readonly long _recaptureTicks;
    private DateTime _wallAnchor;

    #endregion

    #region Constructors

    public DateTimeRealTime()
        : this(() => DateTime.UtcNow, Stopwatch.GetTimestamp, DefaultRecaptureWindow)
    {
    }

    public DateTimeRealTime(Func<DateTime> getWall, Func<long> getTimestamp, TimeSpan recaptureWindow)
    {
        _getWall = getWall;
        _getTimestamp = getTimestamp;
        _lock = new object();
        _recaptureTicks = recaptureWindow.Ticks;
        var qpc = _getTimestamp();
        Recapture(_getWall().ToUniversalTime(), qpc);
    }

    static DateTimeRealTime()
    {
        DefaultRecaptureWindow = TimeSpan.FromMilliseconds(100);
    }

    #endregion

    #region Methods

    public DateTime GetUtcNow()
    {
        lock (_lock)
        {
            var qpc = _getTimestamp();
            var wall = _getWall().ToUniversalTime();
            var interpolated = _wallAnchor + Stopwatch.GetElapsedTime(_qpcAnchor, qpc);
            var deltaTicks = interpolated.Ticks - wall.Ticks;

            if ((deltaTicks < 0) || (deltaTicks > _recaptureTicks))
            {
                Recapture(wall, qpc);
                return DateTime.SpecifyKind(wall, DateTimeKind.Utc);
            }

            return DateTime.SpecifyKind(interpolated, DateTimeKind.Utc);
        }
    }

    private void Recapture(DateTime wallUtc, long qpc)
    {
        _wallAnchor = DateTime.SpecifyKind(wallUtc, DateTimeKind.Utc);
        _qpcAnchor = qpc;
    }

    #endregion
}