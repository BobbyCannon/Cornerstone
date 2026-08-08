#region References

using System;
using Avalonia;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Layout preferences for a <see cref="LayoutGrid"/>: orientation, split mode, and first-pane
/// share for both horizontal and vertical layouts (so toggling direction keeps each mode's size).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class LayoutGridSettings : CornerstoneObject
{
	#region Constants

	/// <summary>
	/// Default first-pane share (half) for either orientation.
	/// </summary>
	public const double DefaultFirstShare = 0.5;

	/// <summary>
	/// Minimum first-pane star share (avoids collapsing a pane).
	/// </summary>
	public const double MinFirstShare = 0.05;

	/// <summary>
	/// Maximum first-pane star share.
	/// </summary>
	public const double MaxFirstShare = 0.95;

	#endregion

	#region Constructors

	public LayoutGridSettings() : this(true, true)
	{
	}

	public LayoutGridSettings(bool isHorizontal, bool splitGrid)
	{
		IsHorizontal = isHorizontal;
		SplitGrid = splitGrid;
		HorizontalFirstShare = DefaultFirstShare;
		VerticalFirstShare = DefaultFirstShare;
	}

	#endregion

	#region Properties

	/// <summary>
	/// First content pane share when <see cref="IsHorizontal"/> is true (column 0 / width).
	/// Range roughly 0–1 (star fraction); clamped when applied.
	/// </summary>
	public partial double HorizontalFirstShare { get; set; }

	public partial bool IsHorizontal { get; set; }

	public partial bool SplitGrid { get; set; }

	/// <summary>
	/// First content pane share when <see cref="IsHorizontal"/> is false (row 0 / height).
	/// Range roughly 0–1 (star fraction); clamped when applied.
	/// </summary>
	public partial double VerticalFirstShare { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Apply both orientation shares and flags onto a live grid.
	/// </summary>
	public void ApplyTo(LayoutGrid grid)
	{
		if (grid == null)
		{
			return;
		}

		grid.IsHorizontal = IsHorizontal;
		grid.SplitGrid = SplitGrid;
		grid.RestoreSize(ClampShare(VerticalFirstShare), ClampShare(HorizontalFirstShare));
	}

	/// <summary>
	/// Clamp a star share into a usable range (defaults invalid values).
	/// </summary>
	public static double ClampShare(double share)
	{
		if (double.IsNaN(share) || double.IsInfinity(share) || (share <= 0) || (share >= 1))
		{
			return DefaultFirstShare;
		}

		if (share < MinFirstShare)
		{
			return MinFirstShare;
		}

		if (share > MaxFirstShare)
		{
			return MaxFirstShare;
		}

		return share;
	}

	/// <summary>
	/// Update only the share for the grid's current orientation from measured first-pane size.
	/// </summary>
	public void UpdateActiveShareFromFirstPane(LayoutGrid grid, Size firstPaneSize)
	{
		if ((grid == null) || (grid.Bounds.Width <= 0) || (grid.Bounds.Height <= 0))
		{
			return;
		}

		if (grid.IsHorizontal)
		{
			var share = firstPaneSize.Width / grid.Bounds.Width;
			if ((share > 0) && (share < 1))
			{
				HorizontalFirstShare = ClampShare(share);
			}
		}
		else
		{
			var share = firstPaneSize.Height / grid.Bounds.Height;
			if ((share > 0) && (share < 1))
			{
				VerticalFirstShare = ClampShare(share);
			}
		}
	}

	/// <summary>
	/// Read current star shares from the grid definitions into this settings object (both axes).
	/// </summary>
	public void UpdateFromGridDefinitions(LayoutGrid grid)
	{
		if (grid == null)
		{
			return;
		}

		HorizontalFirstShare = ClampShare(grid.GetFirstColumnShare());
		VerticalFirstShare = ClampShare(grid.GetFirstRowShare());
	}

	#endregion
}