#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Dispatch binding that maps selected model properties onto a ViewModel (and optionally back).
/// Driven by <see cref="ITrackPropertyChanges" /> bits on both sides; applied on the dispatcher tick.
/// </summary>
internal sealed class PropertyMapBinding : IDispatchBinding, IPropertyMap
{
	#region Fields

	private bool _applyingInbound;
	private readonly List<MapEntry> _entries;
	private readonly object _model;
	private readonly ITrackPropertyChanges _modelChanges;
	private bool _seeded;
	private readonly object _view;
	private readonly ITrackPropertyChanges _viewChanges;

	#endregion

	#region Constructors

	public PropertyMapBinding(object model, ITrackPropertyChanges modelChanges, DispatchableViewModel view)
		: this(model, modelChanges, view, view)
	{
	}

	public PropertyMapBinding(
		object model,
		ITrackPropertyChanges modelChanges,
		DispatchableViewModel owner,
		object destination)
	{
		_model = model ?? throw new ArgumentNullException(nameof(model));
		_modelChanges = modelChanges ?? throw new ArgumentNullException(nameof(modelChanges));
		_view = destination ?? throw new ArgumentNullException(nameof(destination));
		_viewChanges = destination as ITrackPropertyChanges ?? owner
			?? throw new ArgumentNullException(nameof(owner));
		_entries = [];
	}

	#endregion

	#region Methods

	public void ApplyPendingChanges()
	{
		if (_entries.Count == 0)
		{
			_seeded = true;
			return;
		}

		// First tick: pull current model values even if no change bits were set (settings load, etc.).
		if (!_seeded)
		{
			foreach (var entry in _entries)
			{
				entry.ApplyInbound();
			}

			_seeded = true;
		}

		// User edits win in the same tick.
		foreach (var entry in _entries)
		{
			if (entry.TwoWay && IsPropertyChanged(_viewChanges, entry.ViewPropertyName))
			{
				entry.ApplyOutbound();
			}
		}

		foreach (var entry in _entries)
		{
			// Copy when this map has not projected the current model value, even if
			// another map already consumed the shared change bit.
			if (IsPropertyChanged(_modelChanges, entry.ModelPropertyName)
				|| InboundDiffers(entry))
			{
				entry.ApplyInbound();
			}
		}
	}

	public bool HasPendingChanges()
	{
		if (!_seeded)
		{
			return _entries.Count > 0;
		}

		foreach (var entry in _entries)
		{
			if (IsPropertyChanged(_modelChanges, entry.ModelPropertyName)
				|| InboundDiffers(entry))
			{
				return true;
			}

			if (entry.TwoWay && IsPropertyChanged(_viewChanges, entry.ViewPropertyName))
			{
				return true;
			}
		}

		return false;
	}

	public IPropertyMap MapOneWay(string propertyName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

		var entry = new MapEntry
		{
			Model = _model,
			ModelPropertyName = propertyName,
			ViewPropertyName = propertyName,
			TwoWay = false
		};
		entry.ApplyInbound = () => ApplyInboundIdentity(entry);
		entry.InboundDiffers = () => InboundIdentityDiffers(entry);
		_entries.Add(entry);

		return this;
	}

	public IPropertyMap MapOneWay<TModelValue, TViewValue>(
		string modelPropertyName,
		string viewPropertyName,
		Func<TModelValue, TViewValue> toView)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(modelPropertyName);
		ArgumentException.ThrowIfNullOrWhiteSpace(viewPropertyName);
		ArgumentNullException.ThrowIfNull(toView);

		var entry = new MapEntry
		{
			Model = _model,
			ModelPropertyName = modelPropertyName,
			ViewPropertyName = viewPropertyName,
			TwoWay = false
		};
		entry.ApplyInbound = () => ApplyInboundConverted(entry, toView);
		entry.InboundDiffers = () => InboundConvertedDiffers(entry, toView);
		_entries.Add(entry);

		return this;
	}

	public IPropertyMap MapTwoWay(string propertyName)
	{
		return MapTwoWay(propertyName, propertyName);
	}

	public IPropertyMap MapTwoWay(string modelPropertyName, string viewPropertyName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(modelPropertyName);
		ArgumentException.ThrowIfNullOrWhiteSpace(viewPropertyName);

		var entry = new MapEntry
		{
			Model = _model,
			ModelPropertyName = modelPropertyName,
			ViewPropertyName = viewPropertyName,
			TwoWay = true
		};
		entry.ApplyInbound = () => ApplyInboundIdentity(entry);
		entry.ApplyOutbound = () => ApplyOutboundIdentity(modelPropertyName, viewPropertyName);
		entry.InboundDiffers = () => InboundIdentityDiffers(entry);
		_entries.Add(entry);

		return this;
	}

	public IPropertyMap MapTwoWay<TModelValue, TViewValue>(
		string modelPropertyName,
		string viewPropertyName,
		Func<TModelValue, TViewValue> toView,
		Func<TViewValue, TModelValue> toModel)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(modelPropertyName);
		ArgumentException.ThrowIfNullOrWhiteSpace(viewPropertyName);
		ArgumentNullException.ThrowIfNull(toView);
		ArgumentNullException.ThrowIfNull(toModel);

		var entry = new MapEntry
		{
			Model = _model,
			ModelPropertyName = modelPropertyName,
			ViewPropertyName = viewPropertyName,
			TwoWay = true
		};
		entry.ApplyInbound = () => ApplyInboundConverted(entry, toView);
		entry.ApplyOutbound = () => ApplyOutboundConverted(modelPropertyName, viewPropertyName, toModel);
		entry.InboundDiffers = () => InboundConvertedDiffers(entry, toView);
		_entries.Add(entry);

		return this;
	}

	private void ApplyInboundConverted<TModelValue, TViewValue>(
		MapEntry entry,
		Func<TModelValue, TViewValue> toView)
	{
		if (!TryGetValue<TModelValue>(_model, entry.ModelPropertyName, out var modelValue))
		{
			return;
		}

		var viewValue = toView(modelValue);
		RememberProjected(entry, viewValue);
		if (TryGetValue<TViewValue>(_view, entry.ViewPropertyName, out var current)
			&& EqualityComparer<TViewValue>.Default.Equals(current, viewValue))
		{
			_modelChanges.ResetHasChanged(entry.ModelPropertyName);
			return;
		}

		SetViewValue(entry.ViewPropertyName, viewValue);
		_modelChanges.ResetHasChanged(entry.ModelPropertyName);
	}

	private void ApplyInboundIdentity(MapEntry entry)
	{
		if (!SourceReflector.TryGetMemberValue(_model, entry.ModelPropertyName, out var modelValue))
		{
			return;
		}

		RememberProjected(entry, modelValue);
		if (SourceReflector.TryGetMemberValue(_view, entry.ViewPropertyName, out var current)
			&& Equals(current, modelValue))
		{
			_modelChanges.ResetHasChanged(entry.ModelPropertyName);
			return;
		}

		SetViewValue(entry.ViewPropertyName, modelValue);
		_modelChanges.ResetHasChanged(entry.ModelPropertyName);
	}

	private void ApplyOutboundConverted<TModelValue, TViewValue>(
		string modelPropertyName,
		string viewPropertyName,
		Func<TViewValue, TModelValue> toModel)
	{
		if (_applyingInbound)
		{
			return;
		}

		if (!TryGetValue<TViewValue>(_view, viewPropertyName, out var viewValue))
		{
			return;
		}

		var modelValue = toModel(viewValue);
		if (TryGetValue<TModelValue>(_model, modelPropertyName, out var current)
			&& EqualityComparer<TModelValue>.Default.Equals(current, modelValue))
		{
			_viewChanges.ResetHasChanged(viewPropertyName);
			return;
		}

		SourceReflector.TrySetMemberValue(_model, modelPropertyName, modelValue);
		_viewChanges.ResetHasChanged(viewPropertyName);
	}

	private void ApplyOutboundIdentity(string modelPropertyName, string viewPropertyName)
	{
		if (_applyingInbound)
		{
			return;
		}

		if (!SourceReflector.TryGetMemberValue(_view, viewPropertyName, out var viewValue))
		{
			return;
		}

		if (SourceReflector.TryGetMemberValue(_model, modelPropertyName, out var current)
			&& Equals(current, viewValue))
		{
			_viewChanges.ResetHasChanged(viewPropertyName);
			return;
		}

		SourceReflector.TrySetMemberValue(_model, modelPropertyName, viewValue);
		_viewChanges.ResetHasChanged(viewPropertyName);
	}

	private static bool InboundConvertedDiffers<TModelValue, TViewValue>(
		MapEntry entry,
		Func<TModelValue, TViewValue> toView)
	{
		if (!TryGetValue<TModelValue>(entry.Model, entry.ModelPropertyName, out var modelValue))
		{
			return false;
		}

		var projected = toView(modelValue);
		if (!entry.HasProjected)
		{
			return true;
		}

		return !EqualityComparer<object>.Default.Equals(entry.LastProjected, projected);
	}

	private bool InboundDiffers(MapEntry entry)
	{
		return (entry.InboundDiffers != null) && entry.InboundDiffers();
	}

	private static bool InboundIdentityDiffers(MapEntry entry)
	{
		if (!SourceReflector.TryGetMemberValue(entry.Model, entry.ModelPropertyName, out var modelValue))
		{
			return false;
		}

		if (!entry.HasProjected)
		{
			return true;
		}

		return !Equals(entry.LastProjected, modelValue);
	}

	private static bool IsPropertyChanged(ITrackPropertyChanges source, string propertyName)
	{
		// Only the mapped property counts — do not treat unrelated dirty bits as pending for this map.
		return source.HasChanges(new[] { propertyName }.ToOnlyIncludingSettings());
	}

	private static void RememberProjected(MapEntry entry, object value)
	{
		entry.LastProjected = value;
		entry.HasProjected = true;
	}

	private void SetViewValue(string viewPropertyName, object value)
	{
		_applyingInbound = true;
		try
		{
			SourceReflector.TrySetMemberValue(_view, viewPropertyName, value);

			// Setter marks the view dirty; clear so we do not immediately write back.
			_viewChanges.ResetHasChanged(viewPropertyName);
		}
		finally
		{
			_applyingInbound = false;
		}
	}

	private static bool TryGetValue<T>(object target, string propertyName, out T value)
	{
		if (!SourceReflector.TryGetMemberValue(target, propertyName, out var raw))
		{
			value = default;
			return false;
		}

		if (raw is null)
		{
			value = default;
			return true;
		}

		if (raw is T typed)
		{
			value = typed;
			return true;
		}

		// Nullable / boxed value types
		value = (T) raw;
		return true;
	}

	#endregion

	#region Classes

	private sealed class MapEntry
	{
		#region Fields

		public Action ApplyInbound;
		public Action ApplyOutbound;
		public bool HasProjected;
		public Func<bool> InboundDiffers;
		public object LastProjected;
		public object Model;
		public string ModelPropertyName;
		public bool TwoWay;
		public string ViewPropertyName;

		#endregion
	}

	#endregion
}