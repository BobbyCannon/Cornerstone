using System.Collections.Generic;
using Cornerstone.Presentation.Input;
using Cornerstone.RemoteLink.Vnc.Client;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.Implementation.MessageTypes.Outgoing;

namespace Cornerstone.RemoteLink.Vnc.Controls;

public partial class VncView
{
	private readonly HashSet<KeySymbol> _pressedKeys = new();

	protected override void OnTextInput(TextInputEventArgs e)
	{
		base.OnTextInput(e);
		if (e.Handled)
		{
			return;
		}

		var connection = Connection;
		if (connection == null)
		{
			return;
		}

		if (string.IsNullOrEmpty(e.Text))
		{
			return;
		}

		foreach (var c in e.Text)
		{
			var keySymbol = KeyMapping.GetSymbolFromChar(c);
			if (!connection.EnqueueMessage(new KeyEventMessage(true, keySymbol)))
			{
				break;
			}

			connection.EnqueueMessage(new KeyEventMessage(false, keySymbol));
		}

		e.Handled = true;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.Handled || (e.Key == Key.None))
		{
			return;
		}

		if (!HandleKeyEvent(true, e.Key, e.KeyModifiers))
		{
			return;
		}

		e.Handled = true;
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		base.OnKeyUp(e);
		if (e.Handled || (e.Key == Key.None))
		{
			return;
		}

		if (!HandleKeyEvent(false, e.Key, e.KeyModifiers))
		{
			return;
		}

		e.Handled = true;
	}

	private bool HandleKeyEvent(bool downFlag, Key key, KeyModifiers keyModifiers)
	{
		var connection = Connection;
		if (connection == null)
		{
			return false;
		}

		var includePrintable = (keyModifiers & KeyModifiers.Control) != 0;
		var keySymbol = KeyMapping.GetSymbolFromKey(key, includePrintable);
		if (keySymbol == KeySymbol.Null)
		{
			return false;
		}

		var queued = connection.EnqueueMessage(new KeyEventMessage(downFlag, keySymbol));
		if (downFlag && queued)
		{
			_pressedKeys.Add(keySymbol);
		}
		else if (!downFlag)
		{
			_pressedKeys.Remove(keySymbol);
		}

		return queued;
	}

	private void ResetKeyPresses()
	{
		var connection = Connection;
		if (connection != null)
		{
			foreach (var keySymbol in _pressedKeys)
			{
				if (!connection.EnqueueMessage(new KeyEventMessage(false, keySymbol)))
				{
					break;
				}
			}
		}

		_pressedKeys.Clear();
	}
}
