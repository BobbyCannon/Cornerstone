#region References

using System;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

internal struct DropTarget : IEquatable<DropTarget>
{
	#region Fields

	private Dock _dock;
	private Kind _kind;
	private int _tabIndex;

	#endregion

	#region Properties

	public static DropTarget Fill => new() { _kind = Kind.Fill };

	public static DropTarget None => new();

	#endregion

	#region Methods

	public readonly bool Equals(DropTarget other)
	{
		return (_dock == other._dock)
			&& (_kind == other._kind)
			&& (_tabIndex == other._tabIndex);
	}

	public readonly override bool Equals(object obj)
	{
		return obj is DropTarget target && Equals(target);
	}

	public readonly override int GetHashCode()
	{
		return 0;
	}

	public readonly bool IsFill()
	{
		return _kind == Kind.Fill;
	}

	public readonly bool IsNone()
	{
		return _kind == Kind.None;
	}

	public readonly bool IsSplitDock(Dock dock)
	{
		return IsSplitDock(out var value) && (value == dock);
	}

	public readonly bool IsSplitDock(out Dock dock)
	{
		dock = _dock;
		return _kind == Kind.SplitDock;
	}

	public readonly bool IsTabBar(int tabIndex)
	{
		return IsTabBar(out var value) && (value == tabIndex);
	}

	public readonly bool IsTabBar(out int tabIndex)
	{
		tabIndex = _tabIndex;
		return _kind == Kind.TabBar;
	}

	public static bool operator ==(DropTarget left, DropTarget right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DropTarget left, DropTarget right)
	{
		return !left.Equals(right);
	}

	public static DropTarget SplitDock(Dock dock)
	{
		return new() { _kind = Kind.SplitDock, _dock = dock };
	}

	public static DropTarget TabBar(int tabIndex)
	{
		return new() { _kind = Kind.TabBar, _tabIndex = tabIndex };
	}

	#endregion

	#region Enumerations

	private enum Kind
	{
		None,
		Fill,
		SplitDock,
		TabBar
	}

	#endregion
}