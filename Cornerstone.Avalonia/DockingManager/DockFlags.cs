﻿#region References

using System;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[Flags]
public enum DockFlags
{
	None = 0,
	Left = 1,
	Top = 2,
	Right = 4,
	Bottom = 8,
	All = Left | Top | Right | Bottom
}