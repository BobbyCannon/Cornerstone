#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Controls.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Columns;

public class CheckBoxCell : CornerstoneObject, ICell, IDisposable
{
	#region Fields

	private readonly IObserver<BindingValue<bool?>> _sink;
	private readonly IObservable<BindingValue<bool?>> _source;
	private readonly IDisposable _subscription;
	private bool? _value;

	#endregion

	#region Constructors

	public CheckBoxCell(bool? value)
	{
		_value = value;
		IsReadOnly = true;
	}

	public CheckBoxCell(
		IObservable<BindingValue<bool?>> source,
		IObserver<BindingValue<bool?>> sink,
		bool isReadOnly,
		bool isThreeState)
	{
		_source = source;
		_sink = sink;

		IsReadOnly = isReadOnly;
		IsThreeState = isThreeState;

		_subscription = _source.Subscribe(x =>
		{
			if (x.HasValue)
			{
				Value = x.Value;
			}
		});
	}

	#endregion

	#region Properties

	public bool CanEdit => false;
	public BeginEditGestures EditGestures => BeginEditGestures.None;
	public bool IsReadOnly { get; }
	public bool IsThreeState { get; }
	public bool SingleTapEdit => false;

	public bool? Value
	{
		get => _value;
		[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Checkbox cell publishes a closed BindingValue<bool?>.")]
		set
		{
			if (SetProperty(ref _value, value) && !IsReadOnly)
			{
				_sink?.OnNext(BindingValue<bool?>.FromUntyped(value));
			}
		}
	}

	object ICell.Value => Value;

	#endregion

	#region Methods

	public void Dispose()
	{
		_subscription?.Dispose();
		GC.SuppressFinalize(this);
	}

	#endregion
}