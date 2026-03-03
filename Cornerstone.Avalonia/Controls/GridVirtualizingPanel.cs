#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class GridVirtualizingPanel : VirtualizingPanel
{
	#region Fields

	public static readonly StyledProperty<double> ItemSizeProperty;
	public static readonly AttachedProperty<int> RecycleKeyProperty;
	public static readonly StyledProperty<double> SpacingProperty;

	private int _column;
	private int _end;
	private int _getEnd;
	private bool _getReset;
	private int _getStart;
	private double _height;
	private int _start;

	#endregion

	#region Constructors

	public GridVirtualizingPanel()
	{
		EffectiveViewportChanged += OnEffectiveViewportChanged;
	}

	static GridVirtualizingPanel()
	{
		ItemSizeProperty = AvaloniaProperty.Register<GridVirtualizingPanel, double>(nameof(ItemSize), 100);
		RecycleKeyProperty = AvaloniaProperty.RegisterAttached<GridVirtualizingPanel, Control, int>("RecycleKey");
		SpacingProperty = AvaloniaProperty.Register<GridVirtualizingPanel, double>(nameof(Spacing));
	}

	#endregion

	#region Properties

	public double ItemSize
	{
		get => GetValue(ItemSizeProperty);
		set => SetValue(ItemSizeProperty, value);
	}

	public double Spacing
	{
		get => GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	protected Rect Viewport { get; private set; }

	#endregion

	#region Methods

	public static int GetRecycleKey(Control obj)
	{
		return obj.GetValue(RecycleKeyProperty);
	}

	public static void SetRecycleKey(Control obj, int value)
	{
		obj.SetValue(RecycleKeyProperty, value);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var spacing = Spacing;
		var itemSize = ItemSize;
		var column = (int) Math.Max(1, (finalSize.Width + spacing) / ((itemSize > 0 ? itemSize : 120d) + spacing));
		var columnSize = Math.Max(0, (finalSize.Width - (spacing * (column - 1))) / column);

		var currentRow = _start / column;
		var currentTop = currentRow * (_height + spacing);

		ForeachItems(GetChildren(_start, _end), column, (_, items) =>
		{
			for (var i = 0; i < items.Count; i++)
			{
				var element = items[i];
				element.Arrange(new(i * (columnSize + spacing), currentTop, columnSize, _height));
			}

			currentTop += _height + spacing;
		});

		return finalSize;
	}

	protected override Control ContainerFromIndex(int index)
	{
		foreach (var control in Children)
		{
			if (GetRecycleKey(control) == index)
			{
				return control;
			}
		}

		return null;
	}

	protected static void ForeachItems(List<Control> children, int column, Action<int, List<Control>> action)
	{
		var items = new List<Control>();

		var row = -1;

		foreach (var child in children)
		{
			items.Add(child);

			if (items.Count < column)
			{
				continue;
			}

			action(++row, items);
			items.Clear();
		}

		action(++row, items);
		items.Clear();
	}

	protected List<Control> GetChildren(int start, int end)
	{
		if (_getReset || (_getStart != start) || (_getEnd != end))
		{
			var clearAll = _getReset;
			_getReset = false;
			_getStart = Math.Max(0, start);
			_getEnd = Math.Min(end, Items.Count - 1);

			var cache = new Dictionary<int, Control>();
			foreach (var control in Children.ToArray())
			{
				var key = GetRecycleKey(control);
				if (clearAll || (key < _getStart) || (key > _getEnd))
				{
					RemoveInternalChild(control);
				}
				else
				{
					cache[key] = control;
				}
			}

			if (Items.Count > 0)
			{
				for (var i = _getStart; i <= _getEnd; i++)
				{
					if (cache.ContainsKey(i))
					{
						continue;
					}

					CreateElement(Items[i], i, i);
				}
			}
		}

		var controls = Children.ToList();
		controls.Sort((x, y) => GetRecycleKey(x).CompareTo(GetRecycleKey(y)));
		return controls;
	}

	protected override IInputElement GetControl(NavigationDirection direction, IInputElement from, bool wrap)
	{
		if (from is not Control control)
		{
			return null;
		}

		var num = IndexFromContainer(control);
		var index = num;

		switch (direction)
		{
			case NavigationDirection.Up:
				index -= _column;
				break;

			case NavigationDirection.Down:
				index += _column;
				break;

			case NavigationDirection.Left:
				index--;
				break;

			case NavigationDirection.Right:
				index++;
				break;

			case NavigationDirection.Next:
				index++;
				break;

			case NavigationDirection.Previous:
				index--;
				break;
		}

		return ScrollIntoView(index);
	}

	protected override IEnumerable<Control> GetRealizedContainers()
	{
		return GetChildren(_getStart, _getEnd);
	}

	protected override int IndexFromContainer(Control container)
	{
		return GetRecycleKey(container);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var spacing = Spacing;
		var itemSize = ItemSize;
		var column = (int) Math.Max(1, (availableSize.Width + spacing) / ((itemSize > 0 ? itemSize : 120d) + spacing));
		var columnSize = Math.Max(0, (availableSize.Width - (spacing * (column - 1))) / column);
		var layoutSize = new Size(columnSize, double.PositiveInfinity);

		_column = column;

		if (_height == 0)
		{
			foreach (var element in GetChildren(0, 1))
			{
				element.Measure(layoutSize);
				_height = Math.Max(_height, element.DesiredSize.Height);
			}
		}

		var currentRow = Math.Max(0, (int) (Viewport.Y / (_height + spacing)));
		var displayRows = Math.Max(0, (int) (Viewport.Height / (_height + spacing)) + ((Viewport.Height % (_height + spacing)) > 0 ? 1 : 0));

		_start = currentRow * column;
		_end = (_start + (displayRows * column)) - 1;

		foreach (var element in GetChildren(_start, _end))
		{
			element.Measure(layoutSize);
			_height = Math.Max(_height, element.DesiredSize.Height);
		}

		var rows = (Items.Count / column) + ((Items.Count % column) > 0 ? 1 : 0);

		return availableSize.WithHeight((rows * (_height + spacing)) - spacing);
	}

	protected override void OnItemsChanged(IReadOnlyList<object> items, NotifyCollectionChangedEventArgs e)
	{
		base.OnItemsChanged(items, e);

		_getReset = true;
		InvalidateMeasure();
	}

	protected override Control ScrollIntoView(int index)
	{
		var control = ContainerFromIndex(index);
		if (control is not null)
		{
			control.BringIntoView();
		}

		return control;
	}

	private void CreateElement(object item, int index, int recycleKey)
	{
		var containerGenerator = ItemContainerGenerator;
		var container = containerGenerator!.CreateContainer(item, index, recycleKey);
		SetRecycleKey(container, recycleKey);
		containerGenerator.PrepareItemContainer(container, item, index);
		AddInternalChild(container);
		containerGenerator.ItemContainerPrepared(container, item, index);
	}

	private void OnEffectiveViewportChanged(object sender, EffectiveViewportChangedEventArgs e)
	{
		Viewport = e.EffectiveViewport;
		InvalidateMeasure();
	}

	#endregion
}