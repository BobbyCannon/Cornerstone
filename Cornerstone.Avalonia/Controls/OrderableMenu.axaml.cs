#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.Controls;

[DoNotNotify]
public class OrderableMenu : Menu
{
	#region Constants

	private const double DragThreshold = 5.0;

	#endregion

	#region Fields

	public static readonly StyledProperty<DragDropState> DragDropStateProperty;

	private IMenuItemData _potentialDragItem;

	#endregion

	#region Constructors

	public OrderableMenu()
	{
		DragDropState = new DragDropState();
	}

	static OrderableMenu()
	{
		DragDropStateProperty = AvaloniaProperty.Register<OrderableMenu, DragDropState>(nameof(DragDropState));
	}

	#endregion

	#region Properties

	public DragDropState DragDropState
	{
		get => GetValue(DragDropStateProperty);
		set => SetValue(DragDropStateProperty, value);
	}

	#endregion

	#region Methods

	protected override void OnInitialized()
	{
		AddDragDropHandlers();

		base.OnInitialized();
	}

	private void AddDragDropHandlers()
	{
		AddHandler(DragDrop.DropEvent, Drop);
		AddHandler(DragDrop.DragLeaveEvent, DragLeave);
		AddHandler(DragDrop.DragOverEvent, DragOver);

		AddHandler(PointerMovedEvent, MenuPointerMoved);
		AddHandler(PointerPressedEvent, MenuPointerPressed);
		AddHandler(PointerReleasedEvent, MenuPointerReleased);
	}

	private void DragLeave(object sender, DragEventArgs e)
	{
		ResetBorder(e);
	}

	private void DragOver(object sender, DragEventArgs e)
	{
		if (!e.Data.Contains("MenuItem"))
		{
			e.DragEffects = DragDropEffects.None;
			ResetBorder(e);
			return;
		}

		if (!TryGetMenuitemData(e, out var draggedData, out var targetData, out var targetControl)
			|| (targetData == draggedData))
		{
			if ((targetData == draggedData) || !Equals(sender, this))
			{
				e.DragEffects = DragDropEffects.None;
				ResetBorder(e);
				return;
			}

			targetControl = this;
		}

		e.DragEffects = DragDropEffects.Move;

		var dropPoint = e.GetPosition(targetControl);
		var targetHeight = targetControl.Bounds.Height;
		var targetWidth = targetControl.Bounds.Width;

		DragDropState.TargetIsHorizontal = targetControl is OrderableMenu || GetParent(targetControl) is OrderableMenu;
		DragDropState.TargetIsParent = (targetControl == this) || (targetData?.IsParent == true);
		DragDropState.TargetInsertBefore = !DragDropState.TargetIsParent
			&& (DragDropState.TargetIsHorizontal
				? dropPoint.X < (targetWidth / 2)
				: dropPoint.Y < (targetHeight / 2)
			);

		if (targetControl is MenuItem mi)
		{
			mi.BorderBrush = Brushes.Gray;
			mi.BorderThickness = DragDropState.TargetIsParent
				? new Thickness(1)
				: DragDropState.TargetIsHorizontal
					? DragDropState.TargetInsertBefore
						? new Thickness(1, 0, 0, 0)
						: new Thickness(0, 0, 1, 0)
					: DragDropState.TargetInsertBefore
						? new Thickness(0, 1, 0, 0)
						: new Thickness(0, 0, 0, 1);
		}
	}

	private void Drop(object sender, DragEventArgs e)
	{
		if (!DragDropState.IsDragging || !e.Data.Contains("MenuItem"))
		{
			e.DragEffects = DragDropEffects.None;
			ResetBorder(e);
			return;
		}

		if (!TryGetMenuitemData(e, out var draggedData, out var targetData, out var targetControl)
			|| (targetData == draggedData))
		{
			if ((targetData == draggedData) || !Equals(sender, this))
			{
				e.DragEffects = DragDropEffects.None;
				ResetBorder(e);
				return;
			}

			targetControl = this;
		}

		ResetBorder(e);

		var dropPoint = e.GetPosition(targetControl);
		var targetHeight = targetControl.Bounds.Height;
		var targetWidth = targetControl.Bounds.Width;

		DragDropState.TargetIsHorizontal = targetControl is OrderableMenu || GetParent(targetControl) is OrderableMenu;
		DragDropState.TargetIsParent = (targetControl == this) || (targetData?.IsParent == true);
		DragDropState.TargetInsertBefore = !DragDropState.TargetIsParent
			&& (DragDropState.TargetIsHorizontal
				? dropPoint.X < (targetWidth / 2)
				: dropPoint.Y < (targetHeight / 2)
			);

		// Find and remove the item from its current location
		var sourceCollection = FindParentCollection(draggedData, ItemsSource);
		sourceCollection?.Remove(draggedData);

		if (DragDropState.TargetIsParent)
		{
			var targetCollection = targetData?.GetMenuItems() ?? (ISpeedyList) ItemsSource;
			targetCollection?.Add(draggedData);
			UpdatePositions(targetCollection);
		}
		else
		{
			// Find the target collection (could be MenuItemsSource or a Children collection)
			if ((FindParentCollection(targetData, ItemsSource) ?? ItemsSource)
				is not ISpeedyList targetCollection)
			{
				return;
			}

			var targetIndex = targetCollection.IndexOf(targetData);
			if (targetIndex >= 0)
			{
				var newPosition = DragDropState.TargetInsertBefore ? targetIndex : targetIndex + 1;
				targetCollection.Insert(newPosition, draggedData);
			}
			else
			{
				targetCollection.Add(draggedData);
			}

			UpdatePositions(targetCollection);
		}
	}

	private static ISpeedyList FindParentCollection(IMenuItemData item, IEnumerable source)
	{
		if (source is not ISpeedyList rootCollection)
		{
			return null;
		}

		// Use a stack to iteratively traverse the hierarchy
		var stack = new Stack<(ISpeedyList collection, int depth)>();
		stack.Push((rootCollection, 0));

		while (stack.Count > 0)
		{
			var (currentCollection, _) = stack.Pop();

			if (currentCollection.Contains(item))
			{
				return currentCollection;
			}

			foreach (var parent in currentCollection)
			{
				if (parent is not IMenuItemData parentMenuData)
				{
					continue;
				}

				if (parentMenuData.GetMenuItems() is { } children)
				{
					stack.Push((children, 0));
				}
			}
		}

		return null;
	}

	private Control GetParent(object source)
	{
		if (source is not Control control)
		{
			return null;
		}

		// Traverse up the visual tree
		while (control != null)
		{
			if (control.Parent
				is MenuItem
				or OrderableMenu)
			{
				return (Control) control.Parent;
			}

			// Continue to next parent
			control = control.Parent as Control;
		}

		return null;
	}

	private void MenuPointerMoved(object sender, PointerEventArgs e)
	{
		var p = e.GetCurrentPoint(this);

		if ((_potentialDragItem != null) && !p.Properties.IsLeftButtonPressed)
		{
			_potentialDragItem = null;
			return;
		}

		if ((_potentialDragItem == null) || DragDropState.IsDragging)
		{
			return;
		}

		var currentPosition = e.GetPosition(this);
		var dx = currentPosition.X - DragDropState.PressPosition.X;
		var dy = currentPosition.Y - DragDropState.PressPosition.Y;

		DragDropState.DragDistance = Math.Sqrt((dx * dx) + (dy * dy));

		if (!(DragDropState.DragDistance > DragThreshold))
		{
			return;
		}

		DragDropState.IsDragging = true;

		var dragData = new DataObject();
		dragData.Set("MenuItem", _potentialDragItem);

		_potentialDragItem = null;

		DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move)
			.ContinueWith(_ =>
			{
				CornerstoneApplication.Dispatcher.Dispatch(() =>
				{
					DragDropState.IsDragging = false;
				});
			});

		// Release pointer after drag
		if (e.Source is Control control)
		{
			control.ReleasePointerCapture(e.Pointer);
		}
	}

	private void MenuPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (DragDropState.IsDragging
			|| !TryGetMenuItemData(e.Source, out var item)
			|| e.Source is not Control)
		{
			return;
		}

		_potentialDragItem = item;

		DragDropState.PressPosition = e.GetPosition(this);
		DragDropState.IsDragging = false;
	}

	private void MenuPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		if (!DragDropState.IsDragging && (_potentialDragItem != null))
		{
			_potentialDragItem = null;
		}

		DragDropState.IsDragging = false;
	}

	private void ResetBorder(DragEventArgs e)
	{
		var targetControl = (e.Source as Control).FindParent<MenuItem>();
		if (targetControl != null)
		{
			targetControl.BorderThickness = new Thickness(0);
			targetControl.BorderBrush = Brushes.Transparent;
		}
	}

	private bool TryGetMenuitemData(
		DragEventArgs e,
		out IMenuItemData draggedData,
		out IMenuItemData targetData,
		out Control targetControl)
	{
		draggedData = (IMenuItemData) e.Data.Get("MenuItem");
		targetControl = (e.Source as Control).FindParent<MenuItem>();

		if (!TryGetMenuItemData(targetControl, out targetData))
		{
			return false;
		}

		return true;
	}

	private static bool TryGetMenuItemData(object value, out IMenuItemData data)
	{
		if (value is not Control control)
		{
			data = null;
			return false;
		}

		var item = control.DataContext as IMenuItemData
			?? control.Parent?.DataContext as IMenuItemData;
		if (item == null)
		{
			data = null;
			return false;
		}

		data = item;
		return true;
	}

	private static void UpdatePositions(ISpeedyList collection)
	{
		for (var i = 0; i < collection.Count; i++)
		{
			if (collection[i] is IMenuItemData item)
			{
				item.Order = i;
			}
		}
	}

	#endregion
}

public class DragDropState : Bindable
{
	#region Properties

	public double DragDistance { get; set; }

	public bool IsDragging { get; set; }

	public Point PressPosition { get; set; }

	public bool TargetInsertBefore { get; set; }

	public bool TargetIsHorizontal { get; set; }

	public bool TargetIsParent { get; set; }

	#endregion
}