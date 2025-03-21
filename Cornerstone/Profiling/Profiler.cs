#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Text;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Profiling;

public class Profiler : Bindable<Profiler>, IDetailedTimer
{
	#region Fields

	private DetailedTimer _currentTimer;
	private readonly ReaderWriterLockTiny _lock;
	private readonly IDateTimeProvider _timeProvider;
	private readonly Dictionary<string, DetailedTimer> _timerLookup;
	private readonly IWeakEventManager _weakEventManager;

	#endregion

	#region Constructors

	public Profiler(string name)
		: this(name, DateTimeProvider.RealTime, null)
	{
	}

	public Profiler(string name, IDispatcher dispatcher)
		: this(name, DateTimeProvider.RealTime, dispatcher)
	{
	}

	public Profiler(string name, IDateTimeProvider timeProvider, IDispatcher dispatcher)
		: base(dispatcher)
	{
		_timeProvider = timeProvider;
		_lock = new ReaderWriterLockTiny();
		_timerLookup = new Dictionary<string, DetailedTimer>(StringComparer.OrdinalIgnoreCase);

		AppendToExistingTimers = true;
		Name = name ?? "Profiler";
		Timers = new SpeedyList<DetailedTimer>(dispatcher);

		_weakEventManager = new WeakEventManager();
		_weakEventManager.AddCollectionChanged(Timers, this, TimersOnCollectionChanged);
	}

	~Profiler()
	{
		_weakEventManager.Remove(Timers);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Append time to existing timers. Defaults to true.
	/// </summary>
	public bool AppendToExistingTimers { get; set; }

	/// <inheritdoc />
	public TimeSpan Elapsed => TimeSpan.FromTicks(Timers.Sum(x => x.Elapsed.Ticks));

	/// <summary>
	/// Indicates the timer is running or not.
	/// </summary>
	public bool IsRunning => _currentTimer?.IsRunning ?? false;

	/// <inheritdoc />
	public string Name { get; }

	/// <inheritdoc />
	public decimal Percent => 100m;

	/// <inheritdoc />
	public SpeedyList<DetailedTimer> Timers { get; }

	#endregion

	#region Methods

	public void Reset()
	{
		Timers.Clear();

		_currentTimer = null;
		_timerLookup.Clear();
		_weakEventManager.Reset();
	}

	public void Start(string name)
	{
		try
		{
			_lock.EnterWriteLock();

			if (AppendToExistingTimers)
			{
				if (_currentTimer?.TryGetChild(name, out var timer) == true)
				{
					timer.Start();
					_currentTimer = timer;
					return;
				}

				if (_timerLookup.TryGetValue(name, out timer))
				{
					_currentTimer?.Stop();
					timer.Start();
					_currentTimer = timer;
					return;
				}
			}

			if (_currentTimer != null)
			{
				var childTimer = _currentTimer.StartTimer(name);
				_currentTimer = childTimer;
				return;
			}

			var newTimer = new DetailedTimer(name, this, _timeProvider, GetDispatcher());
			newTimer.Start();

			_currentTimer = newTimer;

			Timers.Add(newTimer);
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	public void Stop()
	{
		try
		{
			_lock.EnterWriteLock();

			var currentTimer = _currentTimer;
			if (currentTimer == null)
			{
				return;
			}

			currentTimer.Stop();

			_currentTimer = currentTimer.Parent as DetailedTimer;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	public void Time(string name, Action action)
	{
		try
		{
			Start(name);
			action();
		}
		finally
		{
			Stop();
		}
	}

	public T Time<T>(string name, Func<T> action)
	{
		try
		{
			Start(name);
			return action();
		}
		finally
		{
			Stop();
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		try
		{
			_lock.EnterReadLock();
			var builder = new TextBuilder();
			Timers.ForEach(x => x.Stop());
			Timers.ForEach(x => x.RefreshPercent());
			DetailedTimer.ToDetailedString(this, builder);
			return builder.ToString();
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	/// <summary>
	/// Update the Profiler with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the entity. </param>
	public override bool UpdateWith(Profiler update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			AppendToExistingTimers = update.AppendToExistingTimers;
			Timers.Reconcile(update.Timers);
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(AppendToExistingTimers)), x => x.AppendToExistingTimers = update.AppendToExistingTimers);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Timers)), x => x.Timers.Reconcile(update.Timers));
		}

		return true;
	}

	private void TimersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (!AppendToExistingTimers)
		{
			return;
		}

		if (e.OldItems != null)
		{
			foreach (DetailedTimer item in e.OldItems)
			{
				_timerLookup.Remove(item.Name);
			}
		}

		if (e.NewItems != null)
		{
			foreach (DetailedTimer item in e.NewItems)
			{
				_timerLookup.Add(item.Name, item);
			}
		}
	}

	#endregion
}