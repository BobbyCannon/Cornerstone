#region References

using System;
using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// Basic in-memory circular logger for Cornerstone.
/// Preallocate a fixed ring of entry shells, overwrites the oldest when full,
/// and exposes snapshots for consumers. Not related to <see cref="Tracker" />.
/// </summary>
[SourceReflection]
[DependencyInjected]
public class Logger
{
	#region Constants

	/// <summary>
	/// Default ring capacity (power of two).
	/// </summary>
	public const int DefaultCapacity = 4096;

	#endregion

	#region Fields

	private readonly LogEntry[] _entries;
	private readonly uint _mask;
	private readonly object _sync;
	private readonly IDateTimeProvider _timeProvider;
	private int _count;
	private ulong _droppedCount;
	private uint _head;
	private ulong _nextSequence;
	private uint _tail;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a logger for dependency injection.
	/// </summary>
	/// <param name="dateTimeProvider"> Optional time provider; defaults to real UTC time. </param>
	[DependencyInjectionConstructor]
	public Logger(IDateTimeProvider dateTimeProvider = null)
		: this(DefaultCapacity, dateTimeProvider)
	{
	}

	/// <summary>
	/// Creates a logger with an explicit capacity.
	/// </summary>
	/// <param name="capacity"> Ring capacity; must be a power of two and at least 4. </param>
	/// <param name="dateTimeProvider"> Optional time provider; defaults to real UTC time. </param>
	/// <param name="minimumLevel"> Initial minimum level; messages below this are ignored. </param>
	public Logger(int capacity, IDateTimeProvider dateTimeProvider = null, LogLevel minimumLevel = LogLevel.Trace)
	{
		if (((capacity & (capacity - 1)) != 0) || (capacity < 4))
		{
			throw new ArgumentException("capacity must be a power of 2 and at least 4.", nameof(capacity));
		}

		_timeProvider = dateTimeProvider ?? DateTimeProvider.RealTime;
		_sync = new object();
		_mask = (uint) capacity - 1;
		_entries = new LogEntry[capacity];
		_head = 0;
		_tail = 0;
		_count = 0;
		_nextSequence = 1;
		_droppedCount = 0;

		for (var i = 0; i < capacity; i++)
		{
			_entries[i] = new LogEntry();
		}

		MinimumLevel = minimumLevel;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Fixed ring capacity.
	/// </summary>
	public int Capacity => (int) (_mask + 1);

	/// <summary>
	/// Number of entries currently held (at most <see cref="Capacity" />).
	/// </summary>
	public int Count
	{
		get
		{
			lock (_sync)
			{
				return _count;
			}
		}
	}

	/// <summary>
	/// Number of entries dropped because the ring was full.
	/// </summary>
	public ulong DroppedCount
	{
		get
		{
			lock (_sync)
			{
				return _droppedCount;
			}
		}
	}

	/// <summary>
	/// Messages below this level are ignored.
	/// Set to <see cref="LogLevel.None" /> to disable all writes.
	/// </summary>
	public LogLevel MinimumLevel { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Clears the ring without changing capacity. Entry shells are retained.
	/// </summary>
	public void Clear()
	{
		lock (_sync)
		{
			for (var i = 0; i < _entries.Length; i++)
			{
				_entries[i].Fill(0, default, LogLevel.Trace, Guid.Empty, string.Empty, null);
			}

			_head = 0;
			_tail = 0;
			_count = 0;
			_droppedCount = 0;
			_nextSequence = 1;
		}
	}

	/// <summary>
	/// Writes a critical message.
	/// </summary>
	public void Critical(string message, Exception exception = null)
	{
		Write(LogLevel.Critical, Guid.Empty, message, exception);
	}

	/// <summary>
	/// Writes a debug message.
	/// </summary>
	public void Debug(string message)
	{
		Write(LogLevel.Debug, Guid.Empty, message);
	}

	/// <summary>
	/// Writes an error message.
	/// </summary>
	public void Error(string message, Exception exception = null)
	{
		Write(LogLevel.Error, Guid.Empty, message, exception);
	}

	/// <summary>
	/// Writes an informational message.
	/// </summary>
	public void Information(string message)
	{
		Write(LogLevel.Information, Guid.Empty, message);
	}

	/// <summary>
	/// Returns true when <paramref name="level" /> would be written given <see cref="MinimumLevel" />.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsEnabled(LogLevel level)
	{
		return (level != LogLevel.None) && (MinimumLevel != LogLevel.None) && (level >= MinimumLevel);
	}

	/// <summary>
	/// Returns a copy of current entries, oldest first.
	/// </summary>
	public LogEntry[] Snapshot()
	{
		lock (_sync)
		{
			if (_count == 0)
			{
				return [];
			}

			var result = new LogEntry[_count];
			var index = _head;

			for (var i = 0; i < _count; i++)
			{
				result[i] = _entries[index].ShallowClone();
				index = (index + 1) & _mask;
			}

			return result;
		}
	}

	/// <summary>
	/// Writes a trace message.
	/// </summary>
	public void Trace(string message)
	{
		Write(LogLevel.Trace, Guid.Empty, message);
	}

	/// <summary>
	/// Writes a warning message.
	/// </summary>
	public void Warning(string message, Exception exception = null)
	{
		Write(LogLevel.Warning, Guid.Empty, message, exception);
	}

	/// <summary>
	/// Writes a message at the given level.
	/// </summary>
	public void Write(LogLevel level, string message)
	{
		Write(level, Guid.Empty, message, null, null);
	}

	/// <summary>
	/// Writes a message correlated to a session.
	/// </summary>
	public void Write(LogLevel level, Guid sessionId, string message)
	{
		Write(level, sessionId, message, null, null);
	}

	/// <summary>
	/// Writes a message with an optional exception.
	/// </summary>
	public void Write(LogLevel level, Guid sessionId, string message, Exception exception)
	{
		Write(level, sessionId, message, exception, null);
	}

	/// <summary>
	/// Writes a message with an explicit UTC timestamp (for tests and Sync).
	/// </summary>
	public void Write(LogLevel level, Guid sessionId, string message, DateTime utcTimestamp)
	{
		Write(level, sessionId, message, null, utcTimestamp);
	}

	/// <summary>
	/// Writes a message with optional exception and explicit UTC timestamp.
	/// </summary>
	public void Write(LogLevel level, Guid sessionId, string message, Exception exception, DateTime? utcTimestamp)
	{
		if (!IsEnabled(level))
		{
			return;
		}

		var timestamp = utcTimestamp ?? _timeProvider.UtcNow;

		lock (_sync)
		{
			if (_count == Capacity)
			{
				_head = (_head + 1) & _mask;
				_count--;
				_droppedCount++;
			}

			var entry = _entries[_tail];
			entry.Fill(_nextSequence++, timestamp, level, sessionId, message, exception);
			_tail = (_tail + 1) & _mask;
			_count++;
		}
	}

	#endregion
}
