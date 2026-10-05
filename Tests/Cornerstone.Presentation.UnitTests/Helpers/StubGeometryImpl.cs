#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public class StubGeometryImpl : IGeometryImpl
{
	#region Fields

	private Func<Point, bool> _fillContains;
	private Func<IPen, Rect> _getRenderBounds;
	private Func<IPen, Point, bool> _strokeContains;

	#endregion

	#region Constructors

	public StubGeometryImpl()
	{
		Bounds = default;
	}

	public StubGeometryImpl(Rect bounds)
	{
		Bounds = bounds;
	}

	#endregion

	#region Properties

	public Rect Bounds { get; set; }

	public double ContourLength { get; set; }

	public IGeometryImpl GeometryImpl => this;

	#endregion

	#region Methods

	public bool FillContains(Point point)
	{
		return _fillContains != null ? _fillContains(point) : Bounds.Contains(point);
	}

	public IntersectionResult GetFillIntersectionResult(IGeometryImpl geometry)
	{
		return IntersectionResult.Empty;
	}

	public Rect GetRenderBounds(IPen pen)
	{
		if (_getRenderBounds != null)
		{
			return _getRenderBounds(pen);
		}

		if (pen == null)
		{
			return Bounds;
		}

		return Bounds.Inflate(pen.Thickness / 2);
	}

	public IGeometryImpl GetWidenedGeometry(IPen pen)
	{
		return this;
	}

	public IGeometryImpl Intersect(IGeometryImpl geometry)
	{
		return new StubGeometryImpl(geometry.Bounds.Intersect(Bounds));
	}

	public void SetFillContains(Func<Point, bool> handler)
	{
		_fillContains = handler;
	}

	public void SetGetRenderBounds(Func<IPen, Rect> handler)
	{
		_getRenderBounds = handler;
	}

	public void SetStrokeContains(Func<IPen, Point, bool> handler)
	{
		_strokeContains = handler;
	}

	public bool StrokeContains(IPen pen, Point point)
	{
		return (_strokeContains != null) && _strokeContains(pen, point);
	}

	public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
	{
		point = default;
		tangent = default;
		return false;
	}

	public bool TryGetPointAtDistance(double distance, out Point point)
	{
		point = default;
		return false;
	}

	public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure,
		[NotNullWhen(true)] out IGeometryImpl segmentGeometry)
	{
		segmentGeometry = null;
		return false;
	}

	public ITransformedGeometryImpl WithTransform(Matrix transform)
	{
		return new StubTransformedGeometryImpl(this, transform);
	}

	#endregion
}

public sealed class StubStreamGeometryImpl : StubGeometryImpl, IStreamGeometryImpl
{
	#region Constructors

	public StubStreamGeometryImpl()
	{
		OpenResult = new StubStreamGeometryContextImpl();
	}

	#endregion

	#region Properties

	public IStreamGeometryContextImpl OpenResult { get; set; }

	#endregion

	#region Methods

	public IStreamGeometryImpl Clone()
	{
		return this;
	}

	public IStreamGeometryContextImpl Open()
	{
		return OpenResult;
	}

	#endregion
}

public sealed class StubTransformedGeometryImpl : StubGeometryImpl, ITransformedGeometryImpl
{
	#region Constructors

	public StubTransformedGeometryImpl(IGeometryImpl sourceGeometry, Matrix transform)
		: base(sourceGeometry.Bounds.TransformToAABB(transform))
	{
		SourceGeometry = sourceGeometry;
		Transform = transform;
	}

	#endregion

	#region Properties

	public IGeometryImpl SourceGeometry { get; }

	public Matrix Transform { get; }

	#endregion
}