#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public class SplitPanel : Panel
{
	#region Fields

	private (int index, Point lastPointerPosition)? _draggedSplitLine;
	private SplitFractions _fractions;
	private static readonly Cursor _horizontalResizeCursor;
	private Orientation _orientation;
	private static readonly Cursor _verticalResizeCursor;

	#endregion

	#region Constructors

	public SplitPanel()
	{
		_fractions = SplitFractions.Default;
		_orientation = Orientation.Horizontal;

		SplitLines = [];
	}

	static SplitPanel()
	{
		_verticalResizeCursor = new(StandardCursorType.SizeNorthSouth);
		_horizontalResizeCursor = new(StandardCursorType.SizeWestEast);
	}

	#endregion

	#region Properties

	public SplitFractions Fractions
	{
		get => _fractions;
		set
		{
			var oldCount = _fractions.Count;

			_fractions = (value == null) || (value.Count == 0)
				? SplitFractions.Default
				: value;

			if (oldCount != _fractions.Count)
			{
				VisualChildren.RemoveAll(SplitLines);
				SplitLines.Clear();

				for (var i = 0; i < (_fractions.Count - 1); i++)
				{
					var line = new SplitPanelLine
					{
						Cursor = Orientation == Orientation.Horizontal
							? _horizontalResizeCursor
							: _verticalResizeCursor
					};

					SplitLines.Add(line);
					VisualChildren.Add(line);
				}
			}

			InvalidateMeasure();
		}
	}

	public Orientation Orientation
	{
		get => _orientation;
		set
		{
			if (_orientation == value)
			{
				return;
			}

			_orientation = value;

			foreach (var line in SplitLines)
			{
				line.Cursor = value == Orientation.Horizontal
					? _horizontalResizeCursor
					: _verticalResizeCursor;
			}

			InvalidateArrange();
		}
	}

	public int SlotCount => Fractions.Count;

	protected List<SplitPanelLine> SplitLines { get; }

	#endregion

	#region Methods

	public Control GetControlAtSlot(Index slot)
	{
		var idx = slot.GetOffset(Fractions.Count);

		if ((idx < 0) || (idx >= Fractions.Count))
		{
			throw new ArgumentOutOfRangeException(nameof(slot), idx, null);
		}

		return idx < Children.Count ? Children[idx] : null;
	}

	public void GetSlotSize(int slot, out int size, out Size size2D)
	{
		var sizes = Fractions.CalcFractionSizes(
			(int) ExtractForOrientation(Bounds.Size));

		size = sizes[slot];

		size2D = Orientation == Orientation.Horizontal
			? new Size(size, Bounds.Height)
			: new Size(Bounds.Width, size);
	}

	public void RemoveSlot(int slot)
	{
		var fractions = Enumerable.Range(0, Fractions.Count)
			.Select(i => Fractions[i])
			.ToList();

		fractions.RemoveAt(slot);

		Fractions = new SplitFractions([.. fractions]);

		if (Children.Count > slot)
		{
			Children.RemoveAt(slot);
		}
	}

	public bool TrySplitSlot(int slot, (Dock dock, int fraction, Control item) insert, int remainingSlotFraction)
	{
		switch (insert.dock)
		{
			case Dock.Left or Dock.Right when Orientation is Orientation.Vertical:
			case Dock.Top or Dock.Bottom when Orientation is Orientation.Horizontal:
			{
				return false;
			}
		}

		List<int> slotSizes =
		[
			.. _fractions.CalcFractionSizes((int) ExtractForOrientation(Bounds.Size))
		];

		var total = insert.fraction + remainingSlotFraction;
		var insertSlotSize = (slotSizes[slot] * insert.fraction) / total;
		slotSizes[slot] -= insertSlotSize;

		if (insert.dock is Dock.Right or Dock.Bottom)
		{
			slot++;
		}

		slotSizes.Insert(slot, insertSlotSize);
		Children.Insert(slot, insert.item);
		Fractions = new SplitFractions([.. slotSizes]);
		return true;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var rects = _fractions.CalcFractionRects(finalSize, Orientation);
		var slotCount = Fractions.Count;

		for (var i = 0; i < (slotCount - 1); i++)
		{
			if (Orientation == Orientation.Horizontal)
			{
				SplitLines[i].StartPoint = rects[i].TopRight;
				SplitLines[i].EndPoint = rects[i].BottomRight;
			}
			else
			{
				SplitLines[i].StartPoint = rects[i].BottomLeft;
				SplitLines[i].EndPoint = rects[i].BottomRight;
			}

			SplitLines[i].InvalidateVisual();
		}

		for (var i = 0; i < Math.Min(Children.Count, slotCount); i++)
		{
			Children[i].Arrange(rects[i]);
		}

		return finalSize;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var rects = _fractions.CalcFractionRects(availableSize, Orientation);
		var slotCount = Fractions.Count;

		double desiredWidth = 0;
		double desiredHeight = 0;
		for (var i = 0; i < Math.Min(Children.Count, slotCount); i++)
		{
			Children[i].Measure(rects[i].Size);
			if (Orientation == Orientation.Horizontal)
			{
				desiredWidth += Children[i].DesiredSize.Width;
				desiredHeight = Math.Max(desiredHeight, Children[i].DesiredSize.Height);
			}
			else
			{
				desiredWidth = Math.Max(desiredWidth, Children[i].DesiredSize.Width);
				desiredHeight += Children[i].DesiredSize.Height;
			}
		}

		return new Size(desiredWidth, desiredHeight);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		if (_draggedSplitLine == null)
		{
			return;
		}

		var isHorizontal = Orientation == Orientation.Horizontal;

		var pointerPos = e.GetPosition(this);
		var (splitIndex, lastPointerPosition) = _draggedSplitLine.Value;
		var pointerDelta = new Point(
			Math.Round(pointerPos.X - lastPointerPosition.X),
			Math.Round(pointerPos.Y - lastPointerPosition.Y)
		);

		var fractionSizes = _fractions.CalcFractionSizes(
			(int) ExtractForOrientation(Bounds.Size));

		var delta = (int) (isHorizontal ? pointerDelta.X : pointerDelta.Y);

		var minFractionSizeSlotBefore = 20;
		var minFractionSizeSlotAfter = 20;

		if (splitIndex < Children.Count)
		{
			minFractionSizeSlotBefore = Math.Max(
				minFractionSizeSlotBefore,
				(int) GetMinSize(Children[splitIndex])
			);
		}
		if ((splitIndex + 1) < Children.Count)
		{
			minFractionSizeSlotAfter = Math.Max(
				minFractionSizeSlotAfter,
				(int) GetMinSize(Children[splitIndex + 1])
			);
		}

		if ((fractionSizes[splitIndex] + delta) < minFractionSizeSlotBefore)
		{
			delta = -(fractionSizes[splitIndex] - minFractionSizeSlotBefore);
		}
		if ((fractionSizes[splitIndex + 1] - delta) < minFractionSizeSlotAfter)
		{
			delta = fractionSizes[splitIndex + 1] - minFractionSizeSlotAfter;
		}

		if ((fractionSizes[splitIndex] + delta) < minFractionSizeSlotBefore)
		{
			// we are trapped, abort
			return;
		}

		fractionSizes[splitIndex] += delta;
		fractionSizes[splitIndex + 1] -= delta;

		pointerDelta = isHorizontal
			? pointerDelta.WithX(delta)
			: pointerDelta.WithY(delta);

		_draggedSplitLine = (splitIndex, lastPointerPosition + pointerDelta);

		var newFractions = new SplitFractions(fractionSizes);
		Fractions = newFractions;
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

		Debug.WriteLine($"Clicked on separator between slot {splitIndex} and {splitIndex + 1}");

		_draggedSplitLine = (splitIndex, e.GetPosition(this));
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		_draggedSplitLine = null;
	}

	private double ExtractForOrientation(Size size)
	{
		return Orientation == Orientation.Horizontal ? size.Width : size.Height;
	}

	private double GetMinSize(Control control)
	{
		return Orientation == Orientation.Horizontal ? control.MinWidth : control.MinHeight;
	}

	#endregion
}