#region References

using System;
using Avalonia.Interactivity;

#endregion

namespace Avalonia.Diagnostics.Models;

public class EventChainLink
{
	#region Constructors

	public EventChainLink(object handler, bool handled, RoutingStrategies route)
	{
		Handler = handler ?? throw new ArgumentNullException(nameof(handler));
		Handled = handled;
		Route = route;
	}

	#endregion

	#region Properties

	public bool BeginsNewRoute { get; set; }

	public bool Handled { get; set; }

	public object Handler { get; }

	public string HandlerName
	{
		get
		{
			if (Handler is INamed named && !string.IsNullOrEmpty(named.Name))
			{
				return named.Name + " (" + Handler.GetType().Name + ")";
			}

			return Handler.GetType().Name;
		}
	}

	public RoutingStrategies Route { get; }

	#endregion
}