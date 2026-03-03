#region References

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Media;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

public class TextCell<T> : Notifiable, ITextCell, IDisposable, IEditableObject
{
	#region Fields

	private string _editText;
	private bool _isEditing;
	private readonly ITextCellOptions _options;
	private readonly IObserver<BindingValue<T>> _sink;
	private readonly IDisposable _subscription;
	private T _value;

	#endregion

	#region Constructors

	public TextCell(T value)
	{
		_value = value;
		IsReadOnly = true;
	}

	public TextCell(
		IObservable<BindingValue<T>> source,
		IObserver<BindingValue<T>> sink,
		bool isReadOnly,
		ITextCellOptions options = null)
	{
		IsReadOnly = isReadOnly;
		_sink = sink;
		_options = options;

		_subscription = source.Subscribe(x =>
		{
			if (x.HasValue)
			{
				Value = x.Value;
			}
		});
	}

	#endregion

	#region Properties

	public bool CanEdit => !IsReadOnly;
	public BeginEditGestures EditGestures => _options?.BeginEditGestures ?? BeginEditGestures.Default;
	public bool IsReadOnly { get; }

	public string Text
	{
		get
		{
			if (_isEditing)
			{
				return _editText;
			}
			if (_options?.StringFormat is { } format)
			{
				return string.Format(_options.Culture ?? CultureInfo.CurrentCulture, format, _value);
			}
			return _value?.ToString();
		}
		set
		{
			if (_isEditing)
			{
				_editText = value;
			}
			else
			{
				try
				{
					Value = (T) System.Convert.ChangeType(value, typeof(T));
				}
				catch
				{
					// TODO: Data validation errors.
				}
			}
		}
	}

	public TextAlignment TextAlignment => _options?.TextAlignment ?? TextAlignment.Left;
	public TextTrimming TextTrimming => _options?.TextTrimming ?? TextTrimming.None;
	public TextWrapping TextWrapping => _options?.TextWrapping ?? TextWrapping.NoWrap;

	public T Value
	{
		get => _value;
		set
		{
			if (SetProperty(ref _value, value) && !IsReadOnly && !_isEditing)
			{
				_sink!.OnNext(value!);
			}
		}
	}

	object ICell.Value => Value;

	#endregion

	#region Methods

	public void BeginEdit()
	{
		if (!_isEditing && !IsReadOnly)
		{
			_isEditing = true;
			_editText = Text;
		}
	}

	public void CancelEdit()
	{
		if (_isEditing)
		{
			_isEditing = false;
			_editText = null;
		}
	}

	public void Dispose()
	{
		_subscription?.Dispose();
		GC.SuppressFinalize(this);
	}

	public void EndEdit()
	{
		if (_isEditing)
		{
			var text = _editText;
			_isEditing = false;
			_editText = null;
			Text = text;
		}
	}

	#endregion
}