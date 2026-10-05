using System;
using System.Collections.Generic;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Threading;
using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;
using Cornerstone.RemoteLink.Vnc.Client;
using Cornerstone.RemoteLink.Vnc.Client.Output;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.RemoteLink.Vnc.Controls;

/// <summary>
/// Displays a remote screen using the RFB protocol.
/// </summary>
public partial class VncView : RfbRenderTarget, IOutputHandler
{
	/// <summary>
	/// Defines the Connection property.
	/// </summary>
	public static readonly DirectProperty<VncView, RfbConnection> ConnectionProperty =
		PresentationProperty.RegisterDirect<VncView, RfbConnection>(nameof(Connection), o => o.Connection, (o, v) => o.Connection = v);

	private RfbConnection _connection;
	private readonly List<IDisposable> _connectionDetachDisposable = new();

	static VncView()
	{
		FocusableProperty.OverrideDefaultValue(typeof(VncView), true);
		ClipToBoundsProperty.OverrideDefaultValue(typeof(VncView), true);
	}

	/// <summary>
	/// Gets or sets the connection shown in this view.
	/// </summary>
	public RfbConnection Connection
	{
		get => _connection;
		set
		{
			if (_connection != null)
			{
				if (ReferenceEquals(_connection.OutputHandler, this))
				{
					_connection.OutputHandler = null;
				}
			}

			DisposeConnectionDetach();
			AttachFramebuffer(null);
			ResetKeyPresses();

			if (value != null)
			{
				if (value.RenderTarget is VncFramebuffer framebuffer)
				{
					AttachFramebuffer(framebuffer);
				}
				else
				{
					value.RenderTarget = this;
				}

				value.OutputHandler = this;
				Dispatcher.UIThread.Post(SendInitialSizeUpdate);
			}

			SetAndRaise(ConnectionProperty, ref _connection, value);
		}
	}

	public VncView()
	{
		InitSizing();
		LostFocus += (_, _) => ResetKeyPresses();
	}

	/// <inheritdoc />
	public virtual void RingBell()
	{
		Console.Beep();
	}

	/// <inheritdoc />
	public virtual void HandleServerClipboardUpdate(string text)
	{
		Dispatcher.UIThread.Post(async () =>
		{
			var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
			if (clipboard != null)
			{
				await clipboard.SetTextAsync(text).ConfigureAwait(true);
			}
		});
	}

	private void DisposeConnectionDetach()
	{
		foreach (var disposable in _connectionDetachDisposable)
		{
			disposable.Dispose();
		}

		_connectionDetachDisposable.Clear();
	}

	private void AddConnectionDetach(IDisposable disposable)
	{
		_connectionDetachDisposable.Add(disposable);
	}
}
