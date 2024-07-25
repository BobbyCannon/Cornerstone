#region References

using System;
using Android.Webkit;

#endregion

namespace Cornerstone.Avalonia.Android.Handlers;

internal class BlazorWebMessageCallback : WebMessagePort.WebMessageCallback
{
	#region Fields

	private readonly Action<string> _onMessageReceived;

	#endregion

	#region Constructors

	public BlazorWebMessageCallback(Action<string> onMessageReceived)
	{
		_onMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
	}

	#endregion

	#region Methods

	public override void OnMessage(WebMessagePort port, WebMessage message)
	{
		if (message is null)
		{
			throw new ArgumentNullException(nameof(message));
		}

		_onMessageReceived(message.Data);
	}

	#endregion
}