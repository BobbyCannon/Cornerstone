#region References

using System;
using System.Linq;
using Avalonia;
using Avalonia.Layout;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public class SplitFractions : PresentationList<int>
{
	#region Constructors

	public SplitFractions() : this([])
	{
	}

	public SplitFractions(params int[] fractions)
	{
		Load(fractions);
	}

	#endregion

	#region Properties

	public static SplitFractions Default => new(1);

	#endregion

	#region Methods

	public (int offset, int size)[] CalcFractionLayoutInfos(int totalSize)
	{
		var denominator = this.Sum();
		var layoutInfos = new (int offset, int size)[Count];
		var offset = 0;
		for (var i = 0; i < Count; i++)
		{
			var size = (int) Math.Round((this[i] * totalSize) / (double) denominator);
			layoutInfos[i] = (offset, size);
			offset += size;
		}

		layoutInfos[^1].size += totalSize - offset;

		return layoutInfos;
	}

	public Rect[] CalcFractionRects(Size totalSize, Orientation orientation)
	{
		if (orientation == Orientation.Horizontal)
		{
			return CalcFractionLayoutInfos((int) totalSize.Width)
				.Select(x => new Rect(x.offset, 0, x.size, totalSize.Height))
				.ToArray();
		}
		return CalcFractionLayoutInfos((int) totalSize.Height)
			.Select(x => new Rect(0, x.offset, totalSize.Width, x.size))
			.ToArray();
	}

	public int[] CalcFractionSizes(int totalSize)
	{
		return CalcFractionLayoutInfos(totalSize)
			.Select(x => x.size)
			.ToArray();
	}

	#endregion
}