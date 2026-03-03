#region References

using System;
using System.ComponentModel;
using Cornerstone.Avalonia.TreeDataGrid.Experimental.Data;
using Cornerstone.Avalonia.TreeDataGrid.Experimental.Data.Core;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

public class ExpanderCell<TModel> : Notifiable,
	IExpanderCell,
	IDisposable
	where TModel : class
{
	#region Fields

	private readonly CompositeDisposable _subscription;

	#endregion

	#region Constructors

	public ExpanderCell(
		ICell inner,
		IExpanderRow<TModel> row,
		IObservable<bool> showExpander,
		TypedBindingExpression<TModel, bool> isExpanded)
	{
		_subscription = new();
		Content = inner;
		Row = row;
		row.PropertyChanged += RowPropertyChanged;

		_subscription.Add(showExpander.Subscribe(x => Row.UpdateShowExpander(this, x)));

		if (isExpanded is not null)
		{
			_subscription.Add(isExpanded.Subscribe(x =>
			{
				if (x.HasValue)
				{
					IsExpanded = x.Value;
				}
			}));
		}
	}

	#endregion

	#region Properties

	public bool CanEdit => Content.CanEdit;

	public ICell Content { get; }

	public BeginEditGestures EditGestures => Content.EditGestures;

	public bool IsExpanded
	{
		get => Row.IsExpanded;
		set => Row.IsExpanded = value;
	}

	public IExpanderRow<TModel> Row { get; }
	public bool ShowExpander => Row.ShowExpander;
	public object Value => Content.Value;
	object IExpanderCell.Content => Content;
	IRow IExpanderCell.Row => Row;

	#endregion

	#region Methods

	public void Dispose()
	{
		Row.PropertyChanged -= RowPropertyChanged;
		_subscription?.Dispose();
		(Content as IDisposable)?.Dispose();
	}

	private void RowPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if ((e.PropertyName == nameof(Row.IsExpanded))
			|| (e.PropertyName == nameof(Row.ShowExpander)))
		{
			OnPropertyChanged(e.PropertyName);
		}
	}

	#endregion
}