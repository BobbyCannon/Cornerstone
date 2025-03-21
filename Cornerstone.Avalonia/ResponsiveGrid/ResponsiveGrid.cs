#region References

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.ResponsiveGrid;

[DoNotNotify]
public partial class ResponsiveGrid : Grid
{
	#region Constructors

	public ResponsiveGrid()
	{
		MaxDivision = 12;
		Thresholds = new SizeThresholds();
	}

	#endregion

	#region Methods

	protected override Size ArrangeOverride(Size finalSize)
	{
		var columnWidth = finalSize.Width / MaxDivision;
		var group = Children.GroupBy(GetActualRow);

		double temp = 0;
		foreach (var rows in group)
		{
			double max = 0;

			var columnHeight = rows.Max(o => o.DesiredSize.Height);
			foreach (var element in rows)
			{
				var column = GetActualColumn(element);
				//var row = GetActualRow(element);
				var columnSpan = GetSpan(element, finalSize.Width);
				var rect = new Rect(columnWidth * column, temp, columnWidth * columnSpan, columnHeight);
				element.Arrange(rect);
				max = Math.Max(element.DesiredSize.Height, max);
			}

			temp += max;
		}

		return finalSize;
	}

	protected int GetOffset(Control element, double width)
	{
		int span;

		var getXS = new Func<Control, int>(o =>
		{
			var x = GetXS_Offset(o);
			return x != 0 ? x : 0;
		});
		var getSM = new Func<Control, int>(o =>
		{
			var x = GetSM_Offset(o);
			return x != 0 ? x : getXS(o);
		});
		var getMD = new Func<Control, int>(o =>
		{
			var x = GetMD_Offset(o);
			return x != 0 ? x : getSM(o);
		});
		var getLG = new Func<Control, int>(o =>
		{
			var x = GetLG_Offset(o);
			return x != 0 ? x : getMD(o);
		});

		if (width < Thresholds.XSmallToSmall)
		{
			span = getXS(element);
		}
		else if (width < Thresholds.SmallToMedium)
		{
			span = getSM(element);
		}
		else if (width < Thresholds.MediumToLarge)
		{
			span = getMD(element);
		}
		else
		{
			span = getLG(element);
		}

		return Math.Min(Math.Max(0, span), MaxDivision);
	}

	protected int GetPull(Control element, double width)
	{
		int span;

		var getXS = new Func<Control, int>(o =>
		{
			var x = GetXS_Pull(o);
			return x != 0 ? x : 0;
		});
		var getSM = new Func<Control, int>(o =>
		{
			var x = GetSM_Pull(o);
			return x != 0 ? x : getXS(o);
		});
		var getMD = new Func<Control, int>(o =>
		{
			var x = GetMD_Pull(o);
			return x != 0 ? x : getSM(o);
		});
		var getLG = new Func<Control, int>(o =>
		{
			var x = GetLG_Pull(o);
			return x != 0 ? x : getMD(o);
		});

		if (width < Thresholds.XSmallToSmall)
		{
			span = getXS(element);
		}
		else if (width < Thresholds.SmallToMedium)
		{
			span = getSM(element);
		}
		else if (width < Thresholds.MediumToLarge)
		{
			span = getMD(element);
		}
		else
		{
			span = getLG(element);
		}

		return Math.Min(Math.Max(0, span), MaxDivision);
	}

	protected int GetPush(Control element, double width)
	{
		int span;

		var getXS = new Func<Control, int>(o =>
		{
			var x = GetXS_Push(o);
			return x != 0 ? x : 0;
		});
		var getSM = new Func<Control, int>(o =>
		{
			var x = GetSM_Push(o);
			return x != 0 ? x : getXS(o);
		});
		var getMD = new Func<Control, int>(o =>
		{
			var x = GetMD_Push(o);
			return x != 0 ? x : getSM(o);
		});
		var getLG = new Func<Control, int>(o =>
		{
			var x = GetLG_Push(o);
			return x != 0 ? x : getMD(o);
		});

		if (width < Thresholds.XSmallToSmall)
		{
			span = getXS(element);
		}
		else if (width < Thresholds.SmallToMedium)
		{
			span = getSM(element);
		}
		else if (width < Thresholds.MediumToLarge)
		{
			span = getMD(element);
		}
		else
		{
			span = getLG(element);
		}

		return Math.Min(Math.Max(0, span), MaxDivision);
	}

	protected int GetSpan(Control element, double width)
	{
		int span;

		var getXS = new Func<Control, int>(o =>
		{
			var x = GetXS(o);
			return x != 0 ? x : MaxDivision;
		});
		var getSM = new Func<Control, int>(o =>
		{
			var x = GetSM(o);
			return x != 0 ? x : getXS(o);
		});
		var getMD = new Func<Control, int>(o =>
		{
			var x = GetMD(o);
			return x != 0 ? x : getSM(o);
		});
		var getLG = new Func<Control, int>(o =>
		{
			var x = GetLG(o);
			return x != 0 ? x : getMD(o);
		});

		if (width < Thresholds.XSmallToSmall)
		{
			span = getXS(element);
		}
		else if (width < Thresholds.SmallToMedium)
		{
			span = getSM(element);
		}
		else if (width < Thresholds.MediumToLarge)
		{
			span = getMD(element);
		}
		else
		{
			span = getLG(element);
		}

		return Math.Min(Math.Max(0, span), MaxDivision);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var count = 0;
		var currentRow = 0;
		var availableWidth = double.IsPositiveInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / MaxDivision;

		foreach (var child in Children)
		{
			if (child is not { IsVisible: true })
			{
				continue;
			}

			var span = GetSpan(child, availableSize.Width);
			var offset = GetOffset(child, availableSize.Width);
			var push = GetPush(child, availableSize.Width);
			var pull = GetPull(child, availableSize.Width);

			if ((count + span + offset) > MaxDivision)
			{
				currentRow++;
				count = 0;
			}

			SetActualColumn(child, (count + offset + push) - pull);
			SetActualRow(child, currentRow);

			count += span + offset;

			var size = new Size(availableWidth * span, double.PositiveInfinity);
			child.Measure(size);
		}

		var group = Children.GroupBy(GetActualRow).ToList();
		var totalSize = new Size();
		
		if (group.Count != 0)
		{
			totalSize = new Size(
				group.Max(rows => rows.Sum(o => o.DesiredSize.Width)),
				group.Sum(rows => rows.Max(o => o.DesiredSize.Height))
			);
		}

		return totalSize;
	}

	#endregion
}