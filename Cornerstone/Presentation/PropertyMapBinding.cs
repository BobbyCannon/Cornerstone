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
	private readonly DispatchableViewModel _view;
	private readonly ITrackPropertyChanges _viewChanges;

	#endregion

	#region Constructors

	public PropertyMapBinding(object model, ITrackPropertyChanges modelChanges, DispatchableViewModel view)
	{
		_model = model ?? throw new ArgumentNullException(nameof(model));
		_modelChanges = modelChanges ?? throw new ArgumentNullException(nameof(modelChanges));
		_view = view ?? throw new ArgumentNullException(nameof(view));
		_viewChanges = view;
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
			if (IsPropertyChanged(_modelChanges, entry.ModelPropertyName))
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
			if (IsPropertyChanged(_modelChanges, entry.ModelPropertyName))
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

	public IPropertyMap MapOneWay<TModelValue, TViewValue>(
		string modelPropertyName,
		string viewPropertyName,
		Func<TModelValue, TViewValue> toView)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(modelPropertyName);
		ArgumentException.ThrowIfNullOrWhiteSpace(viewPropertyName);
		ArgumentNullException.ThrowIfNull(toView);

		_entries.Add(new MapEntry
		{
			ModelPropertyName = modelPropertyName,
			ViewPropertyName = viewPropertyName,
			TwoWay = false,
			ApplyInbound = () => ApplyInboundConverted(modelPropertyName, viewPropertyName, toView),
			ApplyOutbound = null
		});

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

		_entries.Add(new MapEntry
		{
			ModelPropertyName = modelPropertyName,
			ViewPropertyName = viewPropertyName,
			TwoWay = true,
			ApplyInbound = () => ApplyInboundIdentity(modelPropertyName, viewPropertyName),
			ApplyOutbound = () => ApplyOutboundIdentity(modelPropertyName, viewPropertyName)
		});

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

		_entries.Add(new MapEntry
		{
			ModelPropertyName = modelPropertyName,
			ViewPropertyName = viewPropertyName,
			TwoWay = true,
			ApplyInbound = () => ApplyInboundConverted(modelPropertyName, viewPropertyName, toView),
			ApplyOutbound = () => ApplyOutboundConverted(modelPropertyName, viewPropertyName, toModel)
		});

		return this;
	}

	private void ApplyInboundConverted<TModelValue, TViewValue>(
		string modelPropertyName,
		string viewPropertyName,
		Func<TModelValue, TViewValue> toView)
	{
		if (!TryGetValue<TModelValue>(_model, modelPropertyName, out var modelValue))
		{
			return;
		}

		var viewValue = toView(modelValue);
		if (TryGetValue<TViewValue>(_view, viewPropertyName, out var current)
			&& EqualityComparer<TViewValue>.Default.Equals(current, viewValue))
		{
			_modelChanges.ResetHasChanged(modelPropertyName);
			return;
		}

		SetViewValue(viewPropertyName, viewValue);
		_modelChanges.ResetHasChanged(modelPropertyName);
	}

	private void ApplyInboundIdentity(string modelPropertyName, string viewPropertyName)
	{
		if (!SourceReflector.TryGetMemberValue(_model, modelPropertyName, out var modelValue))
		{
			return;
		}

		if (SourceReflector.TryGetMemberValue(_view, viewPropertyName, out var current)
			&& Equals(current, modelValue))
		{
			_modelChanges.ResetHasChanged(modelPropertyName);
			return;
		}

		SetViewValue(viewPropertyName, modelValue);
		_modelChanges.ResetHasChanged(modelPropertyName);
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

	private static bool IsPropertyChanged(ITrackPropertyChanges source, string propertyName)
	{
		// Only the mapped property counts — do not treat unrelated dirty bits as pending for this map.
		return source.HasChanges(new[] { propertyName }.ToOnlyIncludingSettings());
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
		public string ModelPropertyName;
		public bool TwoWay;
		public string ViewPropertyName;

		#endregion
	}

	#endregion
}
