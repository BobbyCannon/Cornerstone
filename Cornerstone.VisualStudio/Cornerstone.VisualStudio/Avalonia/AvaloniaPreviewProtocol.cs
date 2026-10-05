#region References

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using global::Avalonia.Remote.Protocol;
using global::Avalonia.Remote.Protocol.Designer;
using global::Avalonia.Remote.Protocol.Input;
using global::Avalonia.Remote.Protocol.Viewport;
using AvMouseButton = global::Avalonia.Remote.Protocol.Input.MouseButton;
using WpfMouseButton = System.Windows.Input.MouseButton;
using WpfPixelFormat = System.Windows.Media.PixelFormat;
using Cornerstone.VisualStudio.Services;
using Cornerstone.VisualStudio.Services.Preview;

#endregion

namespace Cornerstone.VisualStudio.Avalonia;

internal sealed class AvaloniaPreviewProtocol : IPreviewProtocol
{
	public static readonly AvaloniaPreviewProtocol Instance = new();

	public XamlPreviewPlatform Platform => XamlPreviewPlatform.Avalonia;

	public object CreateFrameAck(long sequenceId)
	{
		return new FrameReceivedMessage { SequenceId = sequenceId };
	}

	public object CreatePixelFormats()
	{
		return new ClientSupportedPixelFormatsMessage
		{
			Formats =
			[
				global::Avalonia.Remote.Protocol.Viewport.PixelFormat.Bgra8888,
				global::Avalonia.Remote.Protocol.Viewport.PixelFormat.Rgba8888
			]
		};
	}

	public object CreatePointerMoved(double x, double y, MouseEventArgs e)
	{
		return new PointerMovedEventMessage
		{
			X = x,
			Y = y,
			Modifiers = GetModifiers(e)
		};
	}

	public object CreatePointerPressed(double x, double y, MouseButtonEventArgs e)
	{
		return new PointerPressedEventMessage
		{
			X = x,
			Y = y,
			Button = GetButton(e.ChangedButton),
			Modifiers = GetModifiers(e)
		};
	}

	public object CreatePointerReleased(double x, double y, MouseButtonEventArgs e)
	{
		return new PointerReleasedEventMessage
		{
			X = x,
			Y = y,
			Button = GetButton(e.ChangedButton),
			Modifiers = GetModifiers(e)
		};
	}

	public object CreateRenderInfo(double dpiX, double dpiY)
	{
		return new ClientRenderInfoMessage { DpiX = dpiX, DpiY = dpiY };
	}

	public object CreateUpdateXaml(string xaml, string assemblyPath, string themeVariant, string themeColor, string themeDensity)
	{
		return new UpdateXamlMessage { Xaml = xaml, AssemblyPath = assemblyPath };
	}

	public void DisposeConnection(object connection)
	{
		(connection as IDisposable)?.Dispose();
	}

	public IDisposable Listen(IPAddress address, int port, Action<object> onConnected)
	{
		return new BsonTcpTransport().Listen(address, port, t => onConnected(t));
	}

	public Task SendAsync(object connection, object message)
	{
		if (connection is IAvaloniaRemoteTransportConnection transport)
		{
			return transport.Send(message);
		}

		return Task.CompletedTask;
	}

	public void Subscribe(object connection, Action<object, object> onMessage, Action<object, Exception> onException)
	{
		if (connection is IAvaloniaRemoteTransportConnection transport)
		{
			transport.OnMessage += (c, m) => onMessage(c, m);
			transport.OnException += (c, e) => onException(c, e);
		}
	}

	public WpfPixelFormat ToWpf(object format)
	{
		if (format is global::Avalonia.Remote.Protocol.Viewport.PixelFormat pixel)
		{
			switch (pixel)
			{
				case global::Avalonia.Remote.Protocol.Viewport.PixelFormat.Bgra8888:
					return PixelFormats.Bgra32;
				case global::Avalonia.Remote.Protocol.Viewport.PixelFormat.Rgb565:
					return PixelFormats.Bgr565;
				case global::Avalonia.Remote.Protocol.Viewport.PixelFormat.Rgba8888:
					return PixelFormats.Pbgra32;
			}
		}

		throw new NotSupportedException("Unsupported pixel format.");
	}

	public bool TryGetFrame(object message, out PreviewFrameData frame)
	{
		if (message is FrameMessage f)
		{
			frame = new PreviewFrameData
			{
				SequenceId = f.SequenceId,
				Data = f.Data,
				Format = f.Format,
				Width = f.Width,
				Height = f.Height,
				Stride = f.Stride
			};
			return true;
		}

		frame = null;
		return false;
	}

	public bool TryGetXamlResult(object message, out PreviewXamlResult result)
	{
		if (message is UpdateXamlResultMessage update)
		{
			result = new PreviewXamlResult
			{
				Error = update.Error,
				Exception = Map(update.Exception)
			};
			return true;
		}

		result = null;
		return false;
	}

	public void Unsubscribe(object connection, Action<object, object> onMessage, Action<object, Exception> onException)
	{
		// Avalonia events are not stored as the same delegates; dispose the connection instead.
	}

	private static AvMouseButton GetButton(WpfMouseButton button)
	{
		switch (button)
		{
			case WpfMouseButton.Left:
				return AvMouseButton.Left;
			case WpfMouseButton.Middle:
				return AvMouseButton.Middle;
			case WpfMouseButton.Right:
				return AvMouseButton.Right;
			default:
				return AvMouseButton.None;
		}
	}

	private static InputModifiers[] GetModifiers(MouseEventArgs e)
	{
		var result = new List<InputModifiers>();
		if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
		{
			result.Add(InputModifiers.Alt);
		}
		if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
		{
			result.Add(InputModifiers.Control);
		}
		if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
		{
			result.Add(InputModifiers.Shift);
		}
		if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0)
		{
			result.Add(InputModifiers.Windows);
		}
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			result.Add(InputModifiers.LeftMouseButton);
		}
		if (e.RightButton == MouseButtonState.Pressed)
		{
			result.Add(InputModifiers.RightMouseButton);
		}
		if (e.MiddleButton == MouseButtonState.Pressed)
		{
			result.Add(InputModifiers.MiddleMouseButton);
		}

		return result.ToArray();
	}

	private static PreviewExceptionDetails Map(ExceptionDetails error)
	{
		if (error == null)
		{
			return null;
		}

		return new PreviewExceptionDetails
		{
			ExceptionType = error.ExceptionType,
			Message = error.Message,
			LineNumber = error.LineNumber,
			LinePosition = error.LinePosition
		};
	}
}
