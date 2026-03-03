#region References

using System;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

public abstract class RowEventArgs : EventArgs
{
	#region Properties

	public IRow Row => GetUntypedRow();

	#endregion

	#region Methods

	public static RowEventArgs<T> Create<T>(T row) where T : IRow
	{
		return new RowEventArgs<T>(row);
	}

	protected abstract IRow GetUntypedRow();

	#endregion
}

public class RowEventArgs<TRow> : RowEventArgs
	where TRow : IRow
{
	#region Constructors

	public RowEventArgs(TRow row)
	{
		Row = row;
	}

	#endregion

	#region Properties

	public new TRow Row { get; }

	#endregion

	#region Methods

	protected override IRow GetUntypedRow()
	{
		return Row;
	}

	#endregion
}