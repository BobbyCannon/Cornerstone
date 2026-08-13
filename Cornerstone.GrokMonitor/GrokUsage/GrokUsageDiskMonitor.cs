#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

/// <summary>
/// Watches Grok home logs and sessions on disk and coalesces change notifications
/// through <see cref="Throttle" /> (leading edge + single trailing edge).
/// Does not load usage or mutate domain state; the host supplies the refresh callback.
/// </summary>
public sealed class GrokUsageDiskMonitor : IDisposable
{
	#region Constants

	/// <summary>
	/// Default coalesce window for disk-driven refresh callbacks.
	/// </summary>
	public static readonly TimeSpan DefaultThrottleInterval = TimeSpan.FromSeconds(1);

	#endregion

	#region Fields

	private readonly Action<Guid> _onHomeChanged;
	private readonly TimeSpan _throttleInterval;
	private readonly Dictionary<Guid, HomeWatch> _homes = new();
	private readonly object _lock = new();
	private bool _disposed;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a disk monitor with <see cref="DefaultThrottleInterval" />.
	/// </summary>
	/// <param name="onHomeChanged"> Invoked on the throttle edge when a home's usage files change. May run on a thread-pool thread. </param>
	public GrokUsageDiskMonitor(Action<Guid> onHomeChanged)
		: this(onHomeChanged, DefaultThrottleInterval)
	{
	}

	/// <summary>
	/// Creates a disk monitor.
	/// </summary>
	/// <param name="onHomeChanged"> Invoked on the throttle edge when a home's usage files change. May run on a thread-pool thread. </param>
	/// <param name="throttleInterval"> Coalesce window for disk-driven callbacks. </param>
	public GrokUsageDiskMonitor(Action<Guid> onHomeChanged, TimeSpan throttleInterval)
	{
		_onHomeChanged = onHomeChanged ?? throw new ArgumentNullException(nameof(onHomeChanged));
		if (throttleInterval < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(throttleInterval));
		}

		_throttleInterval = throttleInterval;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Number of homes currently tracked (for tests and diagnostics).
	/// </summary>
	public int TrackedHomeCount
	{
		get
		{
			lock (_lock)
			{
				return _homes.Count;
			}
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Stops all watchers and throttles.
	/// </summary>
	public void Dispose()
	{
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			foreach (var watch in _homes.Values)
			{
				watch.Dispose();
			}

			_homes.Clear();
		}
	}

	/// <summary>
	/// Queues a throttled change for a home (tests and re-arm after partial attach).
	/// </summary>
	public void NotifyChanged(Guid homeId)
	{
		Throttle throttle = null;
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			if (_homes.TryGetValue(homeId, out var watch))
			{
				throttle = watch.Throttle;
			}
		}

		throttle?.Trigger();
	}

	/// <summary>
	/// Aligns watchers with the current set of homes. Adds, updates, or removes as needed.
	/// Missing logs/sessions directories are skipped until a later sync when they exist.
	/// </summary>
	public void SyncHomes(IEnumerable<(Guid Id, string Path)> homes)
	{
		if (homes == null)
		{
			return;
		}

		var desired = new Dictionary<Guid, string>();
		foreach (var (id, path) in homes)
		{
			if ((id == Guid.Empty) || string.IsNullOrWhiteSpace(path))
			{
				continue;
			}

			string full;
			try
			{
				full = Path.GetFullPath(path);
			}
			catch
			{
				continue;
			}

			desired[id] = full;
		}

		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			foreach (var existingId in _homes.Keys.ToList())
			{
				if (!desired.ContainsKey(existingId))
				{
					_homes[existingId].Dispose();
					_homes.Remove(existingId);
				}
			}

			foreach (var pair in desired)
			{
				if (_homes.TryGetValue(pair.Key, out var watch))
				{
					if (!string.Equals(watch.HomePath, pair.Value, StringComparison.OrdinalIgnoreCase))
					{
						watch.Dispose();
						_homes[pair.Key] = CreateHomeWatch(pair.Key, pair.Value);
					}
					else
					{
						// Re-attach if logs/sessions appeared since last sync.
						watch.EnsureWatchers();
					}
				}
				else
				{
					_homes[pair.Key] = CreateHomeWatch(pair.Key, pair.Value);
				}
			}
		}
	}

	private HomeWatch CreateHomeWatch(Guid homeId, string homePath)
	{
		// Capture id for the throttle callback (may run after dictionary updates).
		var id = homeId;
		var watch = new HomeWatch(
			homePath,
			_throttleInterval,
			() =>
			{
				if (_disposed)
				{
					return;
				}

				_onHomeChanged(id);
			});
		watch.EnsureWatchers();
		return watch;
	}

	#endregion

	#region Classes

	private sealed class HomeWatch : IDisposable
	{
		#region Fields

		private FileSystemWatcher _logsWatcher;
		private FileSystemWatcher _sessionsWatcher;
		private bool _disposed;

		#endregion

		#region Constructors

		public HomeWatch(string homePath, TimeSpan throttleInterval, Action throttledNotify)
		{
			HomePath = homePath;
			Throttle = new Throttle(throttledNotify, throttleInterval);
		}

		#endregion

		#region Properties

		public string HomePath { get; }

		public Throttle Throttle { get; }

		#endregion

		#region Methods

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			DisposeWatcher(ref _logsWatcher);
			DisposeWatcher(ref _sessionsWatcher);
			Throttle.Dispose();
		}

		/// <summary>
		/// Creates or refreshes file watchers when logs/sessions directories exist.
		/// </summary>
		public void EnsureWatchers()
		{
			if (_disposed)
			{
				return;
			}

			var logsDir = Path.Combine(HomePath, "logs");
			if (Directory.Exists(logsDir))
			{
				if (_logsWatcher == null)
				{
					_logsWatcher = TryCreateWatcher(logsDir, "unified.jsonl", includeSubdirectories: false);
				}
			}
			else
			{
				DisposeWatcher(ref _logsWatcher);
			}

			var sessionsDir = Path.Combine(HomePath, "sessions");
			if (Directory.Exists(sessionsDir))
			{
				if (_sessionsWatcher == null)
				{
					_sessionsWatcher = TryCreateWatcher(sessionsDir, filter: null, includeSubdirectories: true);
				}
			}
			else
			{
				DisposeWatcher(ref _sessionsWatcher);
			}
		}

		private static void DisposeWatcher(ref FileSystemWatcher watcher)
		{
			if (watcher == null)
			{
				return;
			}

			try
			{
				watcher.EnableRaisingEvents = false;
			}
			catch
			{
				// Ignore shutdown races.
			}

			watcher.Dispose();
			watcher = null;
		}

		private void OnFsEvent(object sender, FileSystemEventArgs e)
		{
			if (_disposed)
			{
				return;
			}

			// Ignore noise under sessions (locks, rewind points) only when clearly not usage inputs.
			// Prefer coalescing via throttle over aggressive path filtering.
			Throttle.Trigger();
		}

		private void OnFsRenamed(object sender, RenamedEventArgs e)
		{
			OnFsEvent(sender, e);
		}

		private FileSystemWatcher TryCreateWatcher(string directory, string filter, bool includeSubdirectories)
		{
			try
			{
				var watcher = string.IsNullOrEmpty(filter)
					? new FileSystemWatcher(directory)
					: new FileSystemWatcher(directory, filter);

				watcher.NotifyFilter = NotifyFilters.LastWrite
					| NotifyFilters.Size
					| NotifyFilters.FileName
					| NotifyFilters.DirectoryName;
				watcher.IncludeSubdirectories = includeSubdirectories;
				watcher.Changed += OnFsEvent;
				watcher.Created += OnFsEvent;
				watcher.Deleted += OnFsEvent;
				watcher.Renamed += OnFsRenamed;
				watcher.EnableRaisingEvents = true;
				return watcher;
			}
			catch
			{
				// Path disappeared or access denied; next SyncHomes may recover.
				return null;
			}
		}

		#endregion
	}

	#endregion
}
