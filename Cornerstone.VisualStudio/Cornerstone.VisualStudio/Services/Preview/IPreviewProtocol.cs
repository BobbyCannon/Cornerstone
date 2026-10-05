#region References

using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

#endregion

namespace Cornerstone.VisualStudio.Services.Preview;

/// <summary>
/// Isolates a remote designer protocol so Avalonia and Cornerstone can diverge.
/// </summary>
internal interface IPreviewProtocol
{
	XamlPreviewPlatform Platform { get; }

	object CreateFrameAck(long sequenceId);

	object CreatePixelFormats();

	object CreatePointerMoved(double x, double y, MouseEventArgs e);

	object CreatePointerPressed(double x, double y, MouseButtonEventArgs e);

	object CreatePointerReleased(double x, double y, MouseButtonEventArgs e);

	object CreateRenderInfo(double dpiX, double dpiY);

	object CreateUpdateXaml(string xaml, string assemblyPath, string themeVariant, string themeColor, string themeDensity);

	void DisposeConnection(object connection);

	IDisposable Listen(IPAddress address, int port, Action<object> onConnected);

	Task SendAsync(object connection, object message);

	void Subscribe(object connection, Action<object, object> onMessage, Action<object, Exception> onException);

	PixelFormat ToWpf(object format);

	bool TryGetFrame(object message, out PreviewFrameData frame);

	bool TryGetXamlResult(object message, out PreviewXamlResult result);

	void Unsubscribe(object connection, Action<object, object> onMessage, Action<object, Exception> onException);
}

internal sealed class PreviewFrameData
{
	public byte[] Data { get; set; }

	public object Format { get; set; }

	public int Height { get; set; }

	public long SequenceId { get; set; }

	public int Stride { get; set; }

	public int Width { get; set; }
}

internal sealed class PreviewXamlResult
{
	public PreviewExceptionDetails Exception { get; set; }

	public string Error { get; set; }
}
