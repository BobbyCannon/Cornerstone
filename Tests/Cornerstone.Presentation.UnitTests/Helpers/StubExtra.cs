#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Utilities;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubCursorFactory : ICursorFactory
{
	#region Methods

	public ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot)
	{
		return new StubCursorImpl();
	}

	public ICursorImpl GetCursor(StandardCursorType cursorType)
	{
		return new StubCursorImpl();
	}

	#endregion

	#region Classes

	private sealed class StubCursorImpl : ICursorImpl
	{
		#region Methods

		public void Dispose()
		{
		}

		#endregion
	}

	#endregion
}

public class StubBitmapImpl : IBitmapImpl
{
	#region Constructors

	public StubBitmapImpl()
	{
		Dpi = new Vector(96, 96);
		PixelSize = new PixelSize(1, 1);
	}

	#endregion

	#region Properties

	public Vector Dpi { get; set; }

	public PixelSize PixelSize { get; set; }

	public int Version { get; set; }

	#endregion

	#region Methods

	public void Dispose()
	{
	}

	public void Save(Stream stream, BitmapEncoderOptions options)
	{
	}

	#endregion
}

public sealed class StubGlyphRunImpl : IGlyphRunImpl
{
	#region Constructors

	public StubGlyphRunImpl()
	{
		Bounds = default;
	}

	#endregion

	#region Properties

	public Point BaselineOrigin { get; set; }

	public Rect Bounds { get; set; }

	public double FontRenderingEmSize { get; set; }

	#endregion

	#region Methods

	public void Dispose()
	{
	}

	public IReadOnlyList<float> GetIntersections(float lowerLimit, float upperLimit)
	{
		return Array.Empty<float>();
	}

	#endregion
}

public sealed class StubBitmap : IBitmap
{
	#region Constructors

	public StubBitmap(int width, int height)
	{
		Size = new Size(width, height);
		PixelSize = new PixelSize(width, height);
		Dpi = new Vector(96, 96);
	}

	#endregion

	#region Properties

	public Vector Dpi { get; set; }

	public PixelSize PixelSize { get; set; }

	public Size Size { get; set; }

	internal IRef<IBitmapImpl> PlatformImpl { get; set; }

	IRef<IBitmapImpl> IBitmap.PlatformImpl => PlatformImpl;

	#endregion

	#region Methods

	public void Dispose()
	{
	}

	public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
	{
	}

	public void Save(Stream stream, BitmapEncoderOptions options)
	{
	}

	#endregion
}

public sealed class StubImageBrushSource : IImageBrushSource
{
	#region Properties

	IRef<IBitmapImpl> IImageBrushSource.Bitmap => null;

	#endregion

	#region Methods

	public IBitmapImpl GetBitmap()
	{
		return null;
	}

	#endregion
}

public sealed class StubGlobalStyles : IGlobalStyles
{
	#region Constructors

	public StubGlobalStyles()
	{
		Styles = new Styles();
	}

	#endregion

	#region Properties

	public bool IsStylesInitialized => true;

	public Styles Styles { get; }

	public IStyleHost StylingParent { get; set; }

	#endregion

	#region Methods

	public void StylesAdded(IReadOnlyList<IStyle> styles)
	{
	}

	public void StylesRemoved(IReadOnlyList<IStyle> styles)
	{
	}

	#endregion

	#region Events

	public event Action<IReadOnlyList<IStyle>> GlobalStylesAdded
	{
		add { }
		remove { }
	}

	public event Action<IReadOnlyList<IStyle>> GlobalStylesRemoved
	{
		add { }
		remove { }
	}

	#endregion
}

public sealed class StubLogicalScrollable : Control, ILogicalScrollable
{
	#region Constructors

	public StubLogicalScrollable()
	{
		PageScrollSize = new Size(45, 67);
		ScrollSize = new Size(16, 16);
		Extent = default;
		Viewport = default;
	}

	#endregion

	#region Properties

	public bool CanHorizontallyScroll { get; set; }

	public bool CanVerticallyScroll { get; set; }

	public Size Extent { get; set; }

	public bool IsLogicalScrollEnabled { get; set; }

	public Vector Offset { get; set; }

	public Size PageScrollSize { get; set; }

	public Size ScrollSize { get; set; }

	public Size Viewport { get; set; }

	#endregion

	#region Methods

	public bool BringIntoView(Control target, Rect targetRect)
	{
		return false;
	}

	public Control GetControlInDirection(NavigationDirection direction, Control from)
	{
		return null;
	}

	public void RaiseScrollInvalidated(EventArgs e)
	{
	}

	#endregion

	#region Events

	public event EventHandler ScrollInvalidated
	{
		add { }
		remove { }
	}

	#endregion
}

public sealed class StubInputManager : IInputManager
{
	#region Constructors

	public StubInputManager()
	{
		Calls = new StubCallLog();
		PreProcess = new Subject<RawInputEventArgs>();
		Process = new Subject<RawInputEventArgs>();
		PostProcess = new Subject<RawInputEventArgs>();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public IObservable<RawInputEventArgs> PostProcess { get; }

	public IObservable<RawInputEventArgs> PreProcess { get; }

	public IObservable<RawInputEventArgs> Process { get; }

	#endregion

	#region Methods

	public void ProcessInput(RawInputEventArgs e)
	{
		Calls.Add(nameof(ProcessInput), e);
	}

	#endregion
}

public sealed class StubDrawingContextLayerImpl : StubBitmapImpl, IDrawingContextLayerImpl
{
	#region Properties

	public bool CanBlit => false;

	public bool IsCorrupted => false;

	#endregion

	#region Methods

	public void Blit(IDrawingContextImpl context)
	{
	}

	public IDrawingContextImpl CreateDrawingContext()
	{
		return new StubDrawingContextImpl();
	}

	#endregion
}

public sealed class StubStreamGeometryContextImpl : IStreamGeometryContextImpl
{
	#region Methods

	public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
	{
	}

	public void BeginFigure(Point startPoint, bool isFilled = true)
	{
	}

	public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
	{
	}

	public void Dispose()
	{
	}

	public void EndFigure(bool isClosed = true)
	{
	}

	public void LineTo(Point point, bool isStroked = true)
	{
	}

	public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
	{
	}

	public void SetFillRule(FillRule fillRule)
	{
	}

	#endregion
}