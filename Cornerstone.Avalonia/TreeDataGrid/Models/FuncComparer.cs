#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

internal class FuncComparer<T> : IComparer<T>
{
	#region Fields

	private readonly Comparison<T> _func;

	#endregion

	#region Constructors

	public FuncComparer(Comparison<T> func)
	{
		_func = func;
	}

	#endregion

	#region Methods

	public int Compare(T x, T y)
	{
		return _func(x, y);
	}

	#endregion
}