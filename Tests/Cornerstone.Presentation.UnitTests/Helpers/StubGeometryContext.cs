#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubGeometryContext : IGeometryContext
{
	#region Constructors

	public StubGeometryContext()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	#endregion

	#region Methods

	public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
	{
		Calls.Add(nameof(ArcTo));
	}

	public void BeginFigure(Point startPoint, bool isFilled = true)
	{
		Calls.Add(nameof(BeginFigure));
	}

	public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
	{
		Calls.Add(nameof(CubicBezierTo));
	}

	public void Dispose()
	{
		Calls.Add(nameof(Dispose));
	}

	public void EndFigure(bool isClosed)
	{
		Calls.Add(nameof(EndFigure), isClosed);
	}

	public void LineTo(Point point, bool isStroked = true)
	{
		Calls.Add(nameof(LineTo));
	}

	public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
	{
		Calls.Add(nameof(QuadraticBezierTo));
	}

	public void SetFillRule(FillRule fillRule)
	{
		Calls.Add(nameof(SetFillRule));
	}

	#endregion
}