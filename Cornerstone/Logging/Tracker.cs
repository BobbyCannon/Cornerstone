#region References

using System;
using System.Diagnostics.Tracing;
using System.Linq;
using Cornerstone.Attributes;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// A tracker to track paths and exceptions. Each tracker instance represents a new sessions.
/// </summary>
public class Tracker : Bindable, IDisposable
{
	#region Constants

	public const string SessionName = "Session";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;

	private readonly TrackerPath _session;

	#endregion

	#region Constructors

	/// <summary>
	/// A tracker to capture, store, and transmit paths to a path repository.
	/// </summary>
	/// <param name="pathRepository"> The final repository used to store the data. </param>
	/// <param name="dateTimeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	[DependencyInjectionConstructor]
	public Tracker(ITrackerRepository pathRepository, IDateTimeProvider dateTimeProvider, IDispatcher dispatcher) : base(dispatcher)
	{
		_dateTimeProvider = dateTimeProvider;
		_session = new TrackerPath(dateTimeProvider, dispatcher)
		{
			Name = SessionName,
			Type = SessionName,
			Data = string.Empty
		};

		PathRepository = pathRepository;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the repository for paths that are tracked.
	/// </summary>
	public ITrackerRepository PathRepository { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds an exception to the tracking session.
	/// </summary>
	/// <param name="exception"> The exception to be added. </param>
	/// <param name="values"> Optional values for this path. </param>
	public void AddException(Exception exception, params TrackerPathValue[] values)
	{
		ValidateTrackerState();
		WriteAndSave(TrackerPath.CreatePathForException(_dateTimeProvider, exception, values));

		if (exception.InnerException != null)
		{
			AddException(exception.InnerException);
		}
	}

	/// <summary>
	/// Adds a path to the tracking session.
	/// </summary>
	/// <param name="name"> The name of the path. </param>
	/// <param name="values"> Optional values for this path. </param>
	public void AddPath(string name, params TrackerPathValue[] values)
	{
		ValidateTrackerState();
		WriteAndSave(new TrackerPath { ParentId = _session.Id, Name = name, Values = values.ToList() });
	}

	/// <summary>
	/// Adds a path with an existing timespan to the tracking session.
	/// </summary>
	/// <param name="name"> The name of the path. </param>
	/// <param name="elapsedTime"> The elapsed time of the path. </param>
	/// <param name="values"> Optional values for this path. </param>
	public void AddPath(string name, TimeSpan elapsedTime, params TrackerPathValue[] values)
	{
		ValidateTrackerState();

		var currentTime = _dateTimeProvider.UtcNow;

		WriteAndSave(new TrackerPath
		{
			ParentId = _session.Id,
			Name = name,
			CompletedOn = currentTime,
			StartedOn = currentTime.Subtract(elapsedTime),
			Values = values.ToList()
		});
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void RunPath(string name, Action<TrackerPath> action)
	{
		using var path = StartPath(name);
		action.Invoke(path);
	}

	/// <summary>
	/// A tracker to capture, store, and transmit paths to a data channel.
	/// </summary>
	/// <param name="repository"> The channel used to store the data remotely. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="values"> The values to associate with this session. </param>
	public static Tracker Start(ITrackerRepository repository, IDateTimeProvider timeProvider, IDispatcher dispatcher, params TrackerPathValue[] values)
	{
		var tracker = new Tracker(repository, timeProvider, dispatcher);
		tracker._session.Values.Add(values);
		tracker.WriteAndSave(tracker._session);
		return tracker;
	}

	/// <summary>
	/// Starts a new path. Once the path is done be sure to call <seealso cref="TrackerPath.Complete" />.
	/// </summary>
	/// <param name="name"> The name of the path. </param>
	/// <param name="values"> Optional values for this path. </param>
	/// <returns> The path for tracking a path. </returns>
	public TrackerPath StartPath(string name, params TrackerPathValue[] values)
	{
		ValidateTrackerState();
		var response = new TrackerPath { ParentId = _session.Id, Name = name, Values = values.ToList(), Type = name };
		response.Completed += ResponseOnCompleted;
		response.Disposed += ResponseOnDisposed;
		return response;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> Should be true if managed resources should be disposed. </param>
	protected virtual void Dispose(bool disposing)
	{
	}

	/// <summary>
	/// Log a message.
	/// </summary>
	/// <param name="message"> The message to log. </param>
	/// <param name="level"> The level of the message. </param>
	protected virtual void OnLog(string message, EventLevel level = EventLevel.Informational)
	{
		Log?.Invoke(message, level);
	}

	private void ResponseOnCompleted(TrackerPath x)
	{
		WriteAndSave(x);
	}

	private void ResponseOnDisposed(TrackerPath path)
	{
		path.Completed -= ResponseOnCompleted;
		path.Disposed -= ResponseOnDisposed;
	}

	/// <summary>
	/// Check to see if the tracker is in a good working state.
	/// </summary>
	private void ValidateTrackerState()
	{
		if (_session != null)
		{
			return;
		}

		const string message = "You must first start the tracker before using it.";
		OnLog(message, EventLevel.Warning);
		throw new InvalidOperationException(message);
	}

	private void WriteAndSave(TrackerPath path)
	{
		PathRepository.Write(path);
	}

	#endregion

	#region Events

	/// <summary>
	/// Event for when the tracker needs to write information.
	/// </summary>
	public event Action<string, EventLevel> Log;

	#endregion
}