#region References

using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

internal sealed class StubDrawingContextImpl : IDrawingContextImpl, IDrawingContextImplWithEffects
{
	#region Constructors

	public StubDrawingContextImpl()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public Matrix Transform { get; set; }

	#endregion

	#region Methods

	public void Clear(Color color)
	{
		RecordVoid(nameof(Clear), color);
	}

	public IDrawingContextLayerImpl CreateLayer(PixelSize size)
	{
		Calls.Add(nameof(CreateLayer), size);
		return null;
	}

	public void Dispose()
	{
		RecordVoid(nameof(Dispose));
	}

	public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
	{
		RecordVoid(nameof(DrawBitmap), source, opacity, sourceRect, destRect);
	}

	public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
	{
		RecordVoid(nameof(DrawBitmap), source, opacityMask, opacityMaskRect, destRect);
	}

	public void DrawEllipse(IBrush brush, IPen pen, Rect rect)
	{
		RecordVoid(nameof(DrawEllipse), brush, pen, rect);
	}

	public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
	{
		RecordVoid(nameof(DrawGeometry), brush, pen, geometry);
	}

	public void DrawGlyphRun(IBrush foreground, IGlyphRunImpl glyphRun)
	{
		RecordVoid(nameof(DrawGlyphRun), foreground, glyphRun);
	}

	public void DrawLine(IPen pen, Point p1, Point p2)
	{
		RecordVoid(nameof(DrawLine), pen, p1, p2);
	}

	public void DrawRectangle(IBrush brush, IPen pen, RoundedRect rect, BoxShadows boxShadow = default)
	{
		RecordVoid(nameof(DrawRectangle), brush, pen, rect, boxShadow);
	}

	public void DrawRegion(IBrush brush, IPen pen, IPlatformRenderInterfaceRegion region)
	{
		RecordVoid(nameof(DrawRegion), brush, pen, region);
	}

	public object GetFeature(Type t)
	{
		Calls.Add(nameof(GetFeature), t);
		return null;
	}

	public void PopClip()
	{
		RecordVoid(nameof(PopClip));
	}

	public void PopEffect()
	{
		RecordVoid(nameof(PopEffect));
	}

	public void PopGeometryClip()
	{
		RecordVoid(nameof(PopGeometryClip));
	}

	public void PopLayer()
	{
		RecordVoid(nameof(PopLayer));
	}

	public void PopOpacity()
	{
		RecordVoid(nameof(PopOpacity));
	}

	public void PopOpacityMask()
	{
		RecordVoid(nameof(PopOpacityMask));
	}

	public void PopRenderOptions()
	{
		RecordVoid(nameof(PopRenderOptions));
	}

	public void PopTextOptions()
	{
		RecordVoid(nameof(PopTextOptions));
	}

	public void PushClip(Rect clip)
	{
		RecordVoid(nameof(PushClip), clip);
	}

	public void PushClip(IPlatformRenderInterfaceRegion region)
	{
		RecordVoid(nameof(PushClip), region);
	}

	public void PushClip(RoundedRect clip)
	{
		RecordVoid(nameof(PushClip), clip);
	}

	public void PushEffect(Rect? clipRect, IEffect effect)
	{
		RecordVoid(nameof(PushEffect), clipRect, effect);
	}

	public void PushGeometryClip(IGeometryImpl clip)
	{
		RecordVoid(nameof(PushGeometryClip), clip);
	}

	public void PushLayer(Rect bounds)
	{
		RecordVoid(nameof(PushLayer), bounds);
	}

	public void PushOpacity(double opacity, Rect? rect)
	{
		RecordVoid(nameof(PushOpacity), opacity, rect);
	}

	public void PushOpacityMask(IBrush mask, Rect bounds)
	{
		RecordVoid(nameof(PushOpacityMask), mask, bounds);
	}

	public void PushRenderOptions(RenderOptions renderOptions)
	{
		RecordVoid(nameof(PushRenderOptions), renderOptions);
	}

	public void PushTextOptions(TextOptions textOptions)
	{
		RecordVoid(nameof(PushTextOptions), textOptions);
	}

	private void RecordVoid(string name, params object[] args)
	{
		Calls.Add(name, args);
	}

	#endregion
}