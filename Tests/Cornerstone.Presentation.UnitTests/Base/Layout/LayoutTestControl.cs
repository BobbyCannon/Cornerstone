#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

internal class LayoutTestControl : Decorator
{
	#region Properties

	public bool Arranged { get; set; }
	public bool CallBaseArrange { get; set; }
	public bool CallBaseMeasure { get; set; }
	public Func<Layoutable, Size, Size> DoArrangeOverride { get; set; }
	public Func<Layoutable, Size, Size> DoMeasureOverride { get; set; }
	public bool Measured { get; set; }

	#endregion

	#region Methods

	protected override Size ArrangeOverride(Size finalSize)
	{
		Arranged = true;

		if (DoArrangeOverride is not null)
		{
			var overrideResult = DoArrangeOverride(this, finalSize);
			return CallBaseArrange ? base.ArrangeOverride(overrideResult) : overrideResult;
		}
		return base.ArrangeOverride(finalSize);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		Measured = true;

		if (DoMeasureOverride is not null)
		{
			var overrideResult = DoMeasureOverride(this, availableSize);
			return CallBaseMeasure ? base.MeasureOverride(overrideResult) : overrideResult;
		}
		return base.MeasureOverride(availableSize);
	}

	#endregion
}