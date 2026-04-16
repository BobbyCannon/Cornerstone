#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

internal class DockingOverlayWindow : Window
{
	#region Fields

	private readonly Dictionary<Control, DockAreaInfo> _areas;
	private readonly DockingManager _dockingManager;
	private Control _hoveredControl;
	private DockUILayoutInfo _hoveredControlDockUI;
	private DropTarget _hoveredDropTarget;
	private DockUILayoutInfo _outerEdgesDockUI;

	#endregion

	#region Constructors

	public DockingOverlayWindow(DockingManager dockingManager)
	{
		_areas = [];
		_hoveredDropTarget = DropTarget.None;
		_dockingManager = dockingManager;

		Background = null;
	}

	#endregion

	#region Methods

	public void Dragging(PointerEventArgs e)
	{
		OnPointerMoved(e);
	}

	public Result GetDroppedResult()
	{
		return new(_hoveredControl, _hoveredDropTarget);
	}

	public override void Hide()
	{
		if (_hoveredControl != null)
		{
			AreaExited?.Invoke(this, new AreaExitedEventArgs(_hoveredControl));
		}

		base.Hide();
	}

	public override void Render(DrawingContext ctx)
	{
		var draggedTabInfoNullable = _dockingManager.DraggedTabInfo;
		if (draggedTabInfoNullable == null)
		{
			return;
		}

		var draggedTabInfo = draggedTabInfoNullable.Value;

		ctx.FillRectangle(_dockingManager.DockIndicatorBackground, Bounds);

		if ((_hoveredControl != null) && _areas.TryGetValue(_hoveredControl, out var areaInfo))
		{
			ctx.DrawRectangle(_dockingManager.DockIndicatorStrokePen, areaInfo.Bounds);

			var dockUI = _hoveredControlDockUI;
			var hoveredTarget = _hoveredDropTarget;
			var cornerRadius = dockUI.CornerRadius;

			// Always draw split indicators (they are always allowed)
			DrawDockControl(dockUI.LeftSplitRect, (0, .5), (0, 1), hoveredTarget.IsSplitDock(Dock.Left), cornerRadius, ctx);
			DrawDockControl(dockUI.RightSplitRect, (.5, 1), (0, 1), hoveredTarget.IsSplitDock(Dock.Right), cornerRadius, ctx);
			DrawDockControl(dockUI.TopSplitRect, (0, 1), (0, .5), hoveredTarget.IsSplitDock(Dock.Top), cornerRadius, ctx);
			DrawDockControl(dockUI.BottomSplitRect, (0, 1), (.5, 1), hoveredTarget.IsSplitDock(Dock.Bottom), cornerRadius, ctx);

			// Center only if the control accepts the tab
			var canFill = CanFillWith(_hoveredControl, draggedTabInfo);
			if (canFill && (dockUI.CenterRect != default))
			{
				DrawDockControl(dockUI.CenterRect, (0, 1), (0, 1), hoveredTarget.IsFill(), cornerRadius, ctx);
			}

			// Preview fill / split area
			if (_hoveredDropTarget.IsSplitDock(out var dock))
			{
				var rect = DockingManager.CalculateDockRect(draggedTabInfo, areaInfo.Bounds, dock);
				ctx.FillRectangle(_dockingManager.DockIndicatorFieldFill, rect);
			}
			else if (_hoveredDropTarget.IsFill() && canFill)
			{
				ctx.FillRectangle(_dockingManager.DockIndicatorFieldFill, areaInfo.Bounds);
			}
		}

		// Tab bar insert indicator (only if the tab is accepted)
		if (_hoveredDropTarget.IsTabBar(out var tabIndex))
		{
			if (_hoveredControl is not TabControl tabControl)
			{
				Debug.Fail("Invalid dropTarget for control");
				return;
			}

			// Only show if the target tab control accepts this tab model
			if (tabControl is DockingTabControl dtc &&
				!dtc.CanAcceptTabModel(draggedTabInfo.TabModel))
			{
				// Skip drawing tab insert if not accepted
				return;
			}

			var hoveredTabItem = (TabItem) tabControl.Items[tabIndex]!;
			var rect = new Rect(_dockingManager.GetBoundsOf(hoveredTabItem).TopLeft, draggedTabInfo.TabItemSize);
			ctx.FillRectangle(_dockingManager.DockIndicatorFieldHoveredFill, rect);
		}
	}

	public void UpdateAreas()
	{
		_areas.Clear();
		_dockingManager.VisitDockingTreeNodes<Control>(control =>
		{
			var dockAreaInfo = new DockAreaInfo
			{
				Bounds = _dockingManager.GetBoundsOf(control)
			};

			_areas[control] = dockAreaInfo;
		});

		_outerEdgesDockUI = CalcDockUILayout(_dockingManager, true);
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		base.OnClosing(e);

		if (_hoveredControl != null)
		{
			AreaExited?.Invoke(this, new AreaExitedEventArgs(_hoveredControl));
		}

		_hoveredControl = null;
		_hoveredDropTarget = DropTarget.None;
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		var pos = e.GetPosition(_dockingManager);
		var (newHoveredControl, _) = _areas.FirstOrDefault(x => x.Value.Bounds.Contains(pos));

		if (newHoveredControl != null)
		{
			if (newHoveredControl != _hoveredControl)
			{
				_hoveredControlDockUI = CalcDockUILayout(newHoveredControl);
			}

			_hoveredDropTarget = EvaluateDropTarget(pos, newHoveredControl, _hoveredControlDockUI);
		}
		else
		{
			_hoveredDropTarget = DropTarget.None;
		}

		var outerEdgesDropTarget = EvaluateDropTarget(pos, null, _outerEdgesDockUI);
		if (!outerEdgesDropTarget.IsNone())
		{
			_hoveredDropTarget = outerEdgesDropTarget;
			newHoveredControl = null;
		}

		InvalidateVisual();

		if (newHoveredControl == _hoveredControl)
		{
			return;
		}

		if (_hoveredControl != null)
		{
			AreaExited?.Invoke(this, new AreaExitedEventArgs(_hoveredControl));
		}

		if (newHoveredControl != null)
		{
			AreaEntered?.Invoke(this, new AreaEnteredEventArgs(newHoveredControl));
		}

		_hoveredControl = newHoveredControl;

		base.OnPointerMoved(e);
	}

	private DockUILayoutInfo CalcDockUILayout(Control control, bool isForOuterEdges = false)
	{
		Span<Rect> rects = stackalloc Rect[9];
		DockUILayoutInfo info;
		Rect bounds;

		if (isForOuterEdges)
		{
			Debug.Assert(control == _dockingManager);
			bounds = control.Bounds.WithX(0).WithY(0);
			Calculate3X3DockUIRectsWithMargin(bounds, rects, out var cornerRadius);
			info = new DockUILayoutInfo { CornerRadius = cornerRadius };
		}
		else
		{
			var areaInfo = _areas[control];
			bounds = areaInfo.Bounds;

			Calculate3X3DockUIRectsWithMargin(bounds, rects, out var cornerRadius);

			info = new DockUILayoutInfo { CornerRadius = cornerRadius };

			if (_dockingManager.CanSplit(control))
			{
				info.LeftSplitRect = DockUIRect(rects, -1, 0);
				info.RightSplitRect = DockUIRect(rects, 1, 0);
				info.TopSplitRect = DockUIRect(rects, 0, -1);
				info.BottomSplitRect = DockUIRect(rects, 0, 1);
			}

			if (_dockingManager.CanFill(control))
			{
				info.CenterRect = DockUIRect(rects, 0, 0);
			}
		}

		return info;
	}

	private void Calculate3X3DockUIRectsWithMargin(Rect bounds, Span<Rect> rects, out float cornerRadius)
	{
		var fieldSize = _dockingManager.DockIndicatorFieldSize;
		var fieldSpacing = _dockingManager.DockIndicatorFieldSpacing;
		var totalSize =
			(_dockingManager.DockIndicatorFieldSize * 5) +
			(_dockingManager.DockIndicatorFieldSpacing * 2);

		var scaling = Math.Min(bounds.Width, bounds.Height) / totalSize;
		scaling = Math.Min(scaling, 1);

		var indicatorSizeScaled = new Size(fieldSize, fieldSize) * scaling;
		var spacingScaled = fieldSpacing * scaling;

		var distance = indicatorSizeScaled.Width + spacingScaled;

		for (var x = 0; x < 3; x++)
		{
			for (var y = 0; y < 3; y++)
			{
				rects[x + (y * 3)] = bounds
					.CenterRect(new Rect(indicatorSizeScaled))
					.Translate(new Vector(distance * (x - 1), distance * (y - 1)));
			}
		}

		cornerRadius = (float) (_dockingManager.DockIndicatorFieldCornerRadius * scaling);
	}

	/// <summary>
	/// Determines whether the hovered control can accept the dragged tab for a **center/fill** operation.
	/// </summary>
	private bool CanFillWith(Control control, DockingManager.TabInfo draggedTabInfo)
	{
		return control is not DockingTabControl dtc
			? _dockingManager.CanFill(control)
			: dtc.CanAcceptTabModel(draggedTabInfo.TabModel);
	}

	private static Rect DockUIRect(ReadOnlySpan<Rect> rects, int x, int y)
	{
		var idx = Math.Clamp(x, -1, 1) + 1 + ((Math.Clamp(y, -1, 1) + 1) * 3);
		var rect = rects[idx];
		var offset = rect.Center - rects[4].Center;

		// handle x and y values outside the rects
		rect = rect.Translate(-offset);
		return rect.Translate(new Vector(offset.X * Math.Abs(x), offset.Y * Math.Abs(y)));
	}

	private void DrawDockControl(Rect rect, (double l, double r) lrPercent, (double t, double b) tbPercent, bool isHovered, float cornerRadius, DrawingContext ctx, bool isNeighborDock = false)
	{
		var l = LinearInterpolation(rect.Left, rect.Right, lrPercent.l);
		var r = LinearInterpolation(rect.Left, rect.Right, lrPercent.r);
		var t = LinearInterpolation(rect.Top, rect.Bottom, tbPercent.t);
		var b = LinearInterpolation(rect.Top, rect.Bottom, tbPercent.b);
		var fillRect = new Rect(new Point(l, t), new Point(r, b));

		ctx.FillRectangle(isHovered ? _dockingManager.DockIndicatorFieldHoveredFill : _dockingManager.DockIndicatorFieldFill, fillRect, cornerRadius);
		ctx.DrawRectangle(_dockingManager.DockIndicatorStrokePen, isNeighborDock ? fillRect : rect, cornerRadius);
		return;

		static double LinearInterpolation(double a, double b, double t)
		{
			return ((1 - t) * a) + (t * b);
		}
	}

	private DropTarget EvaluateDropTarget(Point pos, Control targetControl, in DockUILayoutInfo layoutInfo)
	{
		if (layoutInfo.CenterRect.Contains(pos))
		{
			return DropTarget.Fill;
		}
		if (layoutInfo.LeftSplitRect.Contains(pos))
		{
			return DropTarget.SplitDock(Dock.Left);
		}
		if (layoutInfo.RightSplitRect.Contains(pos))
		{
			return DropTarget.SplitDock(Dock.Right);
		}
		if (layoutInfo.TopSplitRect.Contains(pos))
		{
			return DropTarget.SplitDock(Dock.Top);
		}
		if (layoutInfo.BottomSplitRect.Contains(pos))
		{
			return DropTarget.SplitDock(Dock.Bottom);
		}

		if (targetControl is TabControl tabControl)
		{
			var hoveredTabItem = tabControl.Items
				.OfType<TabItem>()
				.LastOrDefault(x => _dockingManager.GetBoundsOf(x).Contains(pos));

			if (hoveredTabItem != null)
			{
				return DropTarget.TabBar(tabControl.Items.IndexOf(hoveredTabItem));
			}
		}

		return DropTarget.None;
	}

	#endregion

	#region Events

	public event EventHandler<AreaEnteredEventArgs> AreaEntered;
	public event EventHandler<AreaExitedEventArgs> AreaExited;

	#endregion

	#region Structures

	private struct DockAreaInfo
	{
		#region Fields

		public Rect Bounds;

		#endregion
	}

	private struct DockUILayoutInfo
	{
		#region Fields

		public Rect BottomSplitRect;
		public Rect CenterRect;
		public float CornerRadius;
		public Rect LeftSplitRect;
		public Rect RightSplitRect;
		public Rect TopSplitRect;

		#endregion
	}

	public readonly struct Result(Control control, DropTarget dropTarget)
	{
		#region Methods

		public bool IsFillControl([NotNullWhen(true)] out Control target)
		{
			target = control;
			return (target != null) && dropTarget.IsFill();
		}

		public bool IsInsertTab([NotNullWhen(true)] out TabControl target, out int index)
		{
			target = control as TabControl;
			var isCorrectTarget = dropTarget.IsTabBar(out index);
			return (target != null) && isCorrectTarget;
		}

		public bool IsSplitControl([NotNullWhen(true)] out Control target, out Dock dock)
		{
			target = control;
			var isCorrectTarget = dropTarget.IsSplitDock(out dock);
			return (target != null) && isCorrectTarget;
		}

		public bool IsTarget()
		{
			return control != null;
		}

		#endregion
	}

	#endregion

	#region Records

	public record AreaEnteredEventArgs(Control Control)
	{
		#region Properties

		public Control Control { get; init; } = Control;

		#endregion
	}

	public record AreaExitedEventArgs(Control Control)
	{
		#region Properties

		public Control Control { get; init; } = Control;

		#endregion
	}

	#endregion
}