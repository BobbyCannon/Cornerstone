#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubHitTester : IHitTester
{
	#region Fields

	private Func<Point, Visual, Func<Visual, bool>, Visual> _hitTestFirst;
	private Func<Point, Visual, Func<Visual, bool>, IEnumerable<Visual>> _hitTestPoint;

	#endregion

	#region Constructors

	public StubHitTester()
	{
		_hitTestPoint = (_, _, _) => Array.Empty<Visual>();
		_hitTestFirst = (_, _, _) => null;
	}

	#endregion

	#region Methods

	public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter)
	{
		return _hitTestPoint(p, root, filter);
	}

	public IEnumerable<GeometryHitTestResult> HitTest(Geometry geometry, Visual root, Func<Visual, bool> filter)
	{
		return Array.Empty<GeometryHitTestResult>();
	}

	public Visual HitTestFirst(Point p, Visual root, Func<Visual, bool> filter)
	{
		return _hitTestFirst(p, root, filter);
	}

	public GeometryHitTestResult HitTestFirst(Geometry geometry, Visual root, Func<Visual, bool> filter)
	{
		return null;
	}

	public void SetHit(Visual hit)
	{
		if (hit == null)
		{
			_hitTestPoint = (_, _, _) => Array.Empty<Visual>();
			_hitTestFirst = (_, _, _) => null;
			return;
		}

		_hitTestPoint = (_, _, _) => new[] { hit };
		_hitTestFirst = (_, _, _) => hit;
	}

	public void SetHitTestFirst(Func<Point, Visual, Func<Visual, bool>, Visual> handler)
	{
		_hitTestFirst = handler ?? ((_, _, _) => null);
	}

	public void SetHitTestPoint(Func<Point, Visual, Func<Visual, bool>, IEnumerable<Visual>> handler)
	{
		_hitTestPoint = handler ?? ((_, _, _) => Array.Empty<Visual>());
	}

	#endregion
}