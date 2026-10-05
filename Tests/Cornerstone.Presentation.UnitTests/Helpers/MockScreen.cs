#region References

using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

internal class MockScreen : Screen
{
	#region Constructors

	public MockScreen(double scaling, PixelRect bounds, PixelRect workingArea, bool isPrimary)
	{
		Scaling = scaling;
		Bounds = bounds;
		WorkingArea = workingArea;
		IsPrimary = isPrimary;
	}

	#endregion

	#region Methods

	public override bool Equals(Screen other)
	{
		return ReferenceEquals(this, other);
	}

	public override int GetHashCode()
	{
		return RuntimeHelpers.GetHashCode(this);
	}

	#endregion
}