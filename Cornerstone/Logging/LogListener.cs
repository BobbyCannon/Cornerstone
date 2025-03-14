﻿#region References

using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// Represents an example on how to listen to the logger.
/// </summary>
/// <remarks>
/// LogListener must be in the same process as the <see cref="Logger"/>.
/// </remarks>
public class LogListener : EventListener, INotifyPropertyChanged
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the log listener.
	/// </summary>
	/// <param name="sessionId"> The session of the log to monitor. </param>
	/// <param name="level"> The level in which to log. </param>
	internal LogListener(Guid sessionId, EventLevel level = EventLevel.Informational)
	{
		SessionId = sessionId;
		Level = level;
		OnlyEventsWithMessages = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The log listener is listening.
	/// </summary>
	public bool IsListening { get; private set; }

	/// <summary>
	/// The level in which to log.
	/// </summary>
	public EventLevel Level { get; set; }

	/// <summary>
	/// Flag to capture only events with messages
	/// </summary>
	/// <remarks>
	/// Defaults to true so only events with a message will be captured.
	/// </remarks>
	public bool OnlyEventsWithMessages { get; set; }

	/// <summary>
	/// Flag to write incoming events to the console.
	/// </summary>
	public bool OutputToConsole { get; set; }

	/// <summary>
	/// The ID of the session.
	/// </summary>
	public Guid SessionId { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Start listening for log events.
	/// </summary>
	public void Start()
	{
		EnableEvents(Logger.Instance, Level);
		IsListening = true;
	}

	/// <summary>
	/// Start listening for log events.
	/// </summary>
	public void Stop()
	{
		DisableEvents(Logger.Instance);
		IsListening = false;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		Stop();
	}

	/// <inheritdoc />
	protected override void OnEventWritten(EventWrittenEventArgs args)
	{
		if ((SessionId != Guid.Empty) && !Equals(args.Payload[0], SessionId))
		{
			return;
		}

		if (OnlyEventsWithMessages && string.IsNullOrEmpty(args.Message))
		{
			return;
		}

		EventWritten?.Invoke(this, args);

		if (OutputToConsole)
		{
			Console.WriteLine(args.GetDetailedMessage());
		}
	}

	/// <summary>
	/// Notified when a property changed.
	/// </summary>
	/// <param name="propertyName"> The name of the property that has changed. </param>
	protected virtual void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(Level):
			{
				if (IsListening)
				{
					Stop();
					Start();
				}
				break;
			}
		}

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	#region Events

	/// <summary>
	/// Occurs when an event is written.
	/// </summary>
	#if NETSTANDARD2_1 || NET6_0_OR_GREATER
	public new event EventHandler<EventWrittenEventArgs> EventWritten;
	#else
	public event EventHandler<EventWrittenEventArgs> EventWritten;
	#endif

	/// <inheritdoc />
	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}