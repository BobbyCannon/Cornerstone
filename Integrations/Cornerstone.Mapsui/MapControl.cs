#region References

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering.SceneGraph;
using Cornerstone.Presentation.Threading;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Manipulations;
using Mapsui.Rendering;
using Mapsui.UI;
using Mapsui.Utilities;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;

#endregion

namespace Cornerstone.Mapsui;

public partial class MapControl : UserControl, IMapControl
{
	#region Fields

	private MapsuiCustomDrawOperation _drawOperation;
	private readonly ManipulationTracker _manipulationTracker;
	private double _mouseWheelPos;
	private readonly ConcurrentDictionary<long, ScreenPosition> _positions;
	private bool _shiftPressed;

	#endregion

	#region Constructors

	public MapControl()
	{
		_drawOperation = null;
		_manipulationTracker = new ManipulationTracker();
		_mouseWheelPos = 0.0;
		_positions = new ConcurrentDictionary<long, ScreenPosition>();
		_shiftPressed = false;

		Initialized += MapControlInitialized;
		PointerPressed += MapControlPointerPressed;
		PointerReleased += MapControlPointerReleased;
		PointerMoved += MapControlPointerMoved;
		PointerExited += MapControlPointerExited;
		PointerCaptureLost += MapControlPointerCaptureLost;
		PointerWheelChanged += MapControlPointerWheelChanged;
		KeyDown += (_, e) => _shiftPressed = IsShiftPressed(e.KeyModifiers);
		KeyUp += (_, e) => _shiftPressed = IsShiftPressed(e.KeyModifiers);
		ClipToBounds = true;
		SharedConstructor();
	}

	#endregion

	#region Properties

	public static readonly DirectProperty<MapControl, Map> MapProperty =
		PresentationProperty.RegisterDirect<MapControl, Map>(nameof(Map), o => o.Map, (o, v) => o.Map = v);

	public double ContinuousMouseWheelZoomStepSize
	{
		get => Map.Navigator.MouseWheelAnimation.ContinuousMouseWheelZoomStepSize;
		set => Map.Navigator.MouseWheelAnimation.ContinuousMouseWheelZoomStepSize = value;
	}

	public bool UseContinuousMouseWheelZoom
	{
		get => Map.Navigator.MouseWheelAnimation.UseContinuousMouseWheelZoom;
		set => Map.Navigator.MouseWheelAnimation.UseContinuousMouseWheelZoom = value;
	}

	#endregion

	#region Methods

	public void ClearTouchState()
	{
		_positions.Clear();
	}

	public virtual void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public float? GetPixelDensity()
	{
		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel == null)
		{
			return null;
		}

		return (float)topLevel.RenderScaling;
	}

	public void InvalidateCanvas()
	{
		RunOnUiThread(InvalidateVisual);
	}

	public void OpenInBrowser(string url)
	{
		Catch.TaskRun(() =>
		{
			using var process = Process.Start(new ProcessStartInfo
			{
				FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
				Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"-e {url}" : "",
				CreateNoWindow = true,
				UseShellExecute = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
			});
		});
	}

	public override void Render(DrawingContext context)
	{
		if (_drawOperation == null)
		{
			_drawOperation = new MapsuiCustomDrawOperation(
				new Rect(0, 0, Bounds.Width, Bounds.Height),
				_renderController,
				ctx => Map.RenderService.GpuContext = ctx);
		}

		_drawOperation.Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
		context.Custom(_drawOperation);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_drawOperation?.Dispose();
			Map?.Dispose();
		}

		SharedDispose(disposing);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property.Name == nameof(Bounds))
		{
			SharedOnSizeChanged(Bounds.Width, Bounds.Height);
		}
	}

	private static bool IsShiftPressed(KeyModifiers keyModifiers)
	{
		return (keyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift;
	}

	private static void RunOnUiThread(Action action)
	{
		Catch.TaskRun(() => Dispatcher.UIThread.InvokeAsync(action));
	}

	private bool GetShiftPressed()
	{
		return _shiftPressed;
	}

	private bool IsHovering(PointerEventArgs e)
	{
		return e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
	}

	private void MapControlInitialized(object sender, EventArgs e)
	{
		SharedOnSizeChanged(Bounds.Width, Bounds.Height);
	}

	private void MapControlPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
	{
		ClearTouchState();
	}

	private void MapControlPointerExited(object sender, PointerEventArgs e)
	{
		ClearTouchState();
	}

	private void MapControlPointerMoved(object sender, PointerEventArgs e)
	{
		var isHovering = IsHovering(e);
		var position = e.GetPosition(this).ToScreenPosition();
		if (isHovering)
		{
			OnPointerMoved([position], isHovering);
			return;
		}

		_positions[e.Pointer.Id] = position;
		if (OnPointerMoved(_positions.Values.ToArray(), isHovering))
		{
			return;
		}

		_manipulationTracker.Manipulate(_positions.Values.ToArray(), Map.Navigator.Manipulate);
	}

	private void MapControlPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (IsHovering(e))
		{
			return;
		}

		var position = e.GetPosition(this).ToScreenPosition();
		_positions[e.Pointer.Id] = position;
		if (_positions.Count == 1)
		{
			_manipulationTracker.Restart(_positions.Values.ToArray());
		}

		if (OnPointerPressed(_positions.Values.ToArray()))
		{
			return;
		}

		e.Pointer.Capture(this);
	}

	private void MapControlPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		_positions.TryRemove(e.Pointer.Id, out _);
		var position = e.GetPosition(this).ToScreenPosition();
		OnPointerReleased([position]);
		e.Pointer.Capture(null);
	}

	private void MapControlPointerWheelChanged(object sender, PointerWheelEventArgs e)
	{
		var pos = e.GetPosition(this).ToScreenPosition();
		if (Map.Navigator.MouseWheelAnimation.UseContinuousMouseWheelZoom)
		{
			var stepSize = Map.Navigator.MouseWheelAnimation.ContinuousMouseWheelZoomStepSize;
			var scaleFactor = Math.Pow(2, e.Delta.Y * -stepSize);
			Map.Navigator.MouseWheelZoomContinuous(scaleFactor, pos);
			return;
		}

		_mouseWheelPos += e.Delta.Y;
		if (Math.Abs(_mouseWheelPos) < 1.0)
		{
			return;
		}

		var delta = Math.Sign(_mouseWheelPos);
		_mouseWheelPos -= delta;
		Map.Navigator.MouseWheelZoom(delta, pos);
	}

	#endregion

	#region Classes

	private sealed class MapsuiCustomDrawOperation : ICustomDrawOperation
	{
		#region Fields

		private readonly RenderController _renderController;
		private readonly Action<object> _setGpuContext;

		#endregion

		#region Constructors

		public MapsuiCustomDrawOperation(Rect bounds, RenderController renderController, Action<object> setGpuContext)
		{
			Bounds = bounds;
			_renderController = renderController;
			_setGpuContext = setGpuContext;
		}

		#endregion

		#region Properties

		public Rect Bounds { get; set; }

		#endregion

		#region Methods

		public void Dispose()
		{
		}

		public bool Equals(ICustomDrawOperation other)
		{
			return false;
		}

		public bool HitTest(Point p)
		{
			return true;
		}

		public void Render(ImmediateDrawingContext context)
		{
			var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
			if (leaseFeature == null)
			{
				return;
			}

			using var lease = leaseFeature.Lease();
			_setGpuContext(lease.GrContext);
			var canvas = lease.SkCanvas;
			canvas.Save();
			_renderController?.Render(canvas);
			canvas.Restore();
		}

		#endregion
	}

	#endregion
}