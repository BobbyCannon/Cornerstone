#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Internal;

/// <summary>
/// Maintains a list of delayed events to raise.
/// </summary>
internal sealed class DelayedEvents
{
	#region Fields

	private readonly Queue<EventCall> _eventCalls = new();

	#endregion

	#region Methods

	public void DelayedRaise(EventHandler handler, object sender, EventArgs e)
	{
		if (handler != null)
		{
			_eventCalls.Enqueue(new EventCall(handler, sender, e));
		}
	}

	public void RaiseEvents()
	{
		while (_eventCalls.Count > 0)
		{
			_eventCalls.Dequeue().Call();
		}
	}

	#endregion

	#region Structures

	private struct EventCall
	{
		#region Fields

		private readonly EventArgs _e;
		private readonly EventHandler _handler;
		private readonly object _sender;

		#endregion

		#region Constructors

		public EventCall(EventHandler handler, object sender, EventArgs e)
		{
			_handler = handler;
			_sender = sender;
			_e = e;
		}

		#endregion

		#region Methods

		public void Call()
		{
			_handler(_sender, _e);
		}

		#endregion
	}

	#endregion
}