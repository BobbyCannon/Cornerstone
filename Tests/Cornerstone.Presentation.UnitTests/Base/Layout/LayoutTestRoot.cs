#region References

using System;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

internal class LayoutTestRoot : TestRoot
{
	#region Properties

	public bool Arranged { get; set; }
	public Func<Layoutable, Size, Size> DoArrangeOverride { get; set; }
	public Func<Layoutable, Size, Size> DoMeasureOverride { get; set; }
	public bool Measured { get; set; }

	#endregion

	#region Methods

	protected override Size ArrangeOverride(Size finalSize)
	{
		Arranged = true;
		return DoArrangeOverride != null ? DoArrangeOverride(this, finalSize) : base.ArrangeOverride(finalSize);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		Measured = true;
		return DoMeasureOverride != null ? DoMeasureOverride(this, availableSize) : base.MeasureOverride(availableSize);
	}

	#endregion
}