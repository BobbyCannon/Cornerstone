#region References

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#endregion

namespace Cornerstone.Presentation.Remote.Wpf;

/// <summary>
/// WPF surface that shows remote frames and forwards pointer input.
/// Designer chrome (scroll, zoom, build) stays in the Visual Studio previewer.
/// </summary>
public class RemoteView : UserControl
{
	#region Fields

	private readonly Image _image;
	private Point _lastPointerPosition;
	private RemoteSession _session;

	#endregion

	#region Constructors

	public RemoteView()
	{
		_image = new Image
		{
			Stretch = Stretch.None,
			SnapsToDevicePixels = true
		};
		_lastPointerPosition = new Point(double.NaN, double.NaN);
		Content = _image;
		Focusable = true;
		ClipToBounds = true;
		SnapsToDevicePixels = true;
		_image.MouseDown += ImageOnMouseDown;
		_image.MouseMove += ImageOnMouseMove;
		_image.MouseUp += ImageOnMouseUp;
		_image.MouseWheel += ImageOnMouseWheel;
		MouseWheel += ImageOnMouseWheel;
		SizeChanged += OnViewSizeChanged;
	}

	#endregion

	#region Properties

	public RemoteSession Session
	{
		get => _session;
		set
		{
			if (ReferenceEquals(_session, value))
			{
				return;
			}

			if (_session != null)
			{
				_session.FrameReceived -= OnSessionFrameReceived;
				_session.Connected -= OnSessionConnected;
			}

			_session = value;
			if (_session != null)
			{
				_session.AttachDispatcher(Dispatcher);
				_session.FrameReceived += OnSessionFrameReceived;
				_session.Connected += OnSessionConnected;
				ApplyBitmap(_session.Bitmap);
				_ = SendViewportAsync();
			}
			else
			{
				_image.Source = null;
			}
		}
	}

	#endregion

	#region Methods

	protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
	{
		base.OnDpiChanged(oldDpi, newDpi);
		ApplyBitmap(_session?.Bitmap);
		_ = SendViewportAsync();
		_ = SendScalingAsync();
	}

	private void ApplyBitmap(BitmapSource bitmap)
	{
		if (!ReferenceEquals(_image.Source, bitmap))
		{
			_image.Source = bitmap;
		}

		if (bitmap == null)
		{
			return;
		}

		var scaling = VisualTreeHelper.GetDpi(this).DpiScaleX;
		if (scaling <= 0)
		{
			scaling = 1;
		}

		var width = bitmap.Width / scaling;
		var height = bitmap.Height / scaling;
		// NaN is the unset Width. Equal values must not be written again; that
		// invalidates layout on every frame and re-enters the hosted editor.
		if (double.IsNaN(_image.Width) || double.IsNaN(_image.Height) ||
			(Math.Abs(_image.Width - width) > 0.5) ||
			(Math.Abs(_image.Height - height) > 0.5))
		{
			_image.Width = width;
			_image.Height = height;
		}
	}

	private double GetInputScaling()
	{
		var result = (_session?.Scaling ?? 1) / VisualTreeHelper.GetDpi(this).DpiScaleX;
		return result > 0 ? result : 1;
	}

	private void ImageOnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (_session == null)
		{
			return;
		}

		var point = e.GetPosition(_image);
		var scaling = GetInputScaling();
		_lastPointerPosition = point;
		_image.CaptureMouse();
		_ = _session.SendPointerPressedAsync(point.X / scaling, point.Y / scaling, e);
	}

	private void ImageOnMouseMove(object sender, MouseEventArgs e)
	{
		if (_session == null)
		{
			return;
		}

		var point = e.GetPosition(_image);
		if (!double.IsNaN(_lastPointerPosition.X) &&
			(Math.Abs(point.X - _lastPointerPosition.X) < 0.5) &&
			(Math.Abs(point.Y - _lastPointerPosition.Y) < 0.5))
		{
			return;
		}

		_lastPointerPosition = point;
		var scaling = GetInputScaling();
		_ = _session.SendPointerMovedAsync(point.X / scaling, point.Y / scaling, e);
	}

	private void ImageOnMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (_session == null)
		{
			return;
		}

		var point = e.GetPosition(_image);
		var scaling = GetInputScaling();
		_ = _session.SendScrollAsync(point.X / scaling, point.Y / scaling, e);
		e.Handled = true;
	}

	private void ImageOnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_session == null)
		{
			return;
		}

		var point = e.GetPosition(_image);
		var scaling = GetInputScaling();
		_lastPointerPosition = point;
		if (_image.IsMouseCaptured)
		{
			_image.ReleaseMouseCapture();
		}

		_ = _session.SendPointerReleasedAsync(point.X / scaling, point.Y / scaling, e);
	}

	private void OnSessionConnected(object sender, EventArgs e)
	{
		_ = SendViewportAsync();
		_ = SendScalingAsync();
	}

	private void OnSessionFrameReceived(object sender, EventArgs e)
	{
		ApplyBitmap(_session?.Bitmap);
	}

	private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_ = SendViewportAsync();
	}

	private Task SendScalingAsync()
	{
		if (_session == null)
		{
			return Task.CompletedTask;
		}

		var dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
		return _session.SetScalingAsync(dpi);
	}

	private Task SendViewportAsync()
	{
		if (_session == null)
		{
			return Task.CompletedTask;
		}

		var dpi = VisualTreeHelper.GetDpi(this);
		var dpiX = dpi.PixelsPerInchX > 0 ? dpi.PixelsPerInchX : 96;
		var dpiY = dpi.PixelsPerInchY > 0 ? dpi.PixelsPerInchY : 96;
		return _session.SendViewportAllocatedAsync(ActualWidth, ActualHeight, dpiX, dpiY);
	}

	#endregion
}