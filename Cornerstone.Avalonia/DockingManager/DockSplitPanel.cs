#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[DoNotNotify]
public class DockSplitPanel : DockPanel
{
	#region Fields

	private (int index, Dock dock, Point lastPointerPosition)? _draggedSplitLine;
	private static readonly Cursor _horizontalResizeCursor;
	private static readonly Cursor _verticalResizeCursor;

	#endregion

	#region Constructors

	public DockSplitPanel()
	{
		SplitLines = new();
	}

	static DockSplitPanel()
	{
		_verticalResizeCursor = new(StandardCursorType.SizeNorthSouth);
		_horizontalResizeCursor = new(StandardCursorType.SizeWestEast);
	}

	#endregion

	#region Properties

	private List<SplitPanelLine> SplitLines { get; }

	#endregion

	#region Methods

	protected override void ArrangeCore(Rect finalRect)
	{
		base.ArrangeCore(finalRect);

		for (var i = 0; i < (Children.Count - 1); i++)
		{
			var child = Children[i];
			var bounds = child.Bounds;
			var splitLine = SplitLines[i];

			switch (GetDock(child))
			{
				case Dock.Left:
					splitLine.StartPoint = bounds.TopRight;
					splitLine.EndPoint = bounds.BottomRight;
					splitLine.Cursor = _horizontalResizeCursor;
					break;
				case Dock.Right:
					splitLine.StartPoint = bounds.TopLeft;
					splitLine.EndPoint = bounds.BottomLeft;
					splitLine.Cursor = _horizontalResizeCursor;
					break;
				case Dock.Top:
					splitLine.StartPoint = bounds.BottomLeft;
					splitLine.EndPoint = bounds.BottomRight;
					splitLine.Cursor = _verticalResizeCursor;
					break;
				case Dock.Bottom:
					splitLine.StartPoint = bounds.TopLeft;
					splitLine.EndPoint = bounds.TopRight;
					splitLine.Cursor = _verticalResizeCursor;
					break;
			}

			splitLine.InvalidateVisual();
		}
	}

	protected override void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		base.ChildrenChanged(sender, e);

		VisualChildren.RemoveAll(SplitLines);
		SplitLines.Clear();

		for (var i = 0; i < (Children.Count - 1); i++)
		{
			var line = new SplitPanelLine();
			SplitLines.Add(line);
			VisualChildren.Add(line);
		}
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		if (_draggedSplitLine == null)
		{
			return;
		}

		var pos = e.GetPosition(this);
		var (index, dock, lastPointerPosition) = _draggedSplitLine.Value;
		var delta = pos - lastPointerPosition;

		var child = Children[index];
		var visual = child.Bounds.Size;
		var previousSetSize = new Size(child.Width, child.Height);

		child.Width = double.NaN;
		child.Height = double.NaN;
		child.Measure(visual);
		var desired = child.DesiredSize;

		var newWidth = previousSetSize.Width;
		var newHeight = previousSetSize.Height;

		const double margin = 10;
		double maxSize = default;

		switch (dock)
		{
			case Dock.Left:
				maxSize = Bounds.Width - child.Bounds.Left - margin;
				newWidth = Math.Clamp(visual.Width + delta.X, desired.Width, maxSize);
				break;
			case Dock.Right:
				maxSize = child.Bounds.Right - margin;
				newWidth = Math.Clamp(visual.Width - delta.X, desired.Width, maxSize);
				break;
			case Dock.Top:
				maxSize = Bounds.Height - child.Bounds.Top - margin;
				newHeight = Math.Clamp(visual.Height + delta.Y, desired.Height, maxSize);
				break;
			case Dock.Bottom:
				maxSize = child.Bounds.Bottom - margin;
				newHeight = Math.Clamp(visual.Height - delta.Y, desired.Height, maxSize);
				break;
		}

		child.Width = newWidth;
		child.Height = newHeight;

		switch (dock)
		{
			case Dock.Left or Dock.Right when (newWidth == desired.Width) || (newWidth == maxSize):
			case Dock.Top or Dock.Bottom when (newHeight == desired.Height) || (newHeight == maxSize):
			{
				//nothing changed visually
				return;
			}
			default:
			{
				_draggedSplitLine = (index, dock, pos);
				break;
			}
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		if (e.Source is not SplitPanelLine line)
		{
			return;
		}

		var splitIndex = SplitLines.IndexOf(line);
		if (splitIndex == -1)
		{
			return;
		}

		_draggedSplitLine = (splitIndex, GetDock(Children[splitIndex]), e.GetPosition(this));
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);

		_draggedSplitLine = null;
	}

	#endregion
}