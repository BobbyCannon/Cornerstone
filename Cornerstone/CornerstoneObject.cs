#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Cornerstone.Data;
using Cornerstone.Internal;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using CollectionExtensions = Cornerstone.Extensions.CollectionExtensions;
using ICloneable = Cornerstone.Data.ICloneable;

#endregion

namespace Cornerstone;

/// <summary>
/// Represents a notifiable object.
/// </summary>
/// todo: support generics in code generation
/// [SourceReflection]
public abstract class CornerstoneObject<T> : CornerstoneObject, ICloneable<T>, IUpdateable<T>
	where T : class, new()
{
	#region Methods

	public virtual T DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return (T) this.DeepCloneUsingUpdateWith(typeof(T), maxDepth, settings);
	}

	public T ShallowClone(IncludeExcludeSettings settings = null)
	{
		return DeepClone(0, settings);
	}

	public abstract bool UpdateWith(T update, IncludeExcludeSettings settings);

	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			T value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}

/// <summary>
/// Represents a notifiable object.
/// </summary>
[SourceReflection]
public abstract partial class CornerstoneObject : ILifecycle, ICloneable, INotifiable, IUpdateable, ITrackPropertyChanges
{
	#region Fields

	/// <summary>
	/// Supports up to 64 properties
	/// </summary>
	private ulong _changedBits;

	private bool _isInitialized;
	private bool _isLoaded;
	private bool _isStarted;
	private bool _notificationsEnabled;
	private readonly SourceTypeInfo _sourceType;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a notifiable object.
	/// </summary>
	protected CornerstoneObject()
	{
		_sourceType = SourceReflector.GetSourceType(this);
		_notificationsEnabled = true;
	}

	#endregion

	#region Methods

	public void ApplyChangesTo(object destination)
	{
		// atomically take & clear
		var bits = Interlocked.Exchange(ref _changedBits, 0UL);
		if (bits == 0)
		{
			return;
		}

		// Now apply using the captured bits
		destination.UpdateWithOnly(this, GetPropertyNamesFromBits(bits));
	}

	public virtual bool CanProcessLifecycle()
	{
		return false;
	}

	public virtual object DeepCloneObject(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return this.DeepCloneUsingUpdateWith(GetType(), maxDepth, settings);
	}

	public virtual void DisablePropertyChangeNotifications()
	{
		_notificationsEnabled = false;
	}

	public virtual void EnablePropertyChangeNotifications()
	{
		_notificationsEnabled = true;
	}

	public virtual IEnumerable<string> GetChangedProperties()
	{
		if ((_changedBits == 0) || (_sourceType == null))
		{
			yield break;
		}

		var bits = _changedBits;
		while (bits != 0)
		{
			var bit = BitOperations.TrailingZeroCount(bits);
			var name = _sourceType.GetPropertyNameByBit(bit);
			if (name != null)
			{
				yield return name;
			}

			// clear lowest set bit
			bits &= bits - 1;
		}
	}

	public virtual HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
	{
		return [];
	}

	public bool HasChanges()
	{
		return HasChanges(IncludeExcludeSettings.Empty);
	}

	public virtual bool HasChanges(IncludeExcludeSettings settings)
	{
		if (_changedBits == 0)
		{
			return false;
		}

		if (settings.IsEmpty())
		{
			return true;
		}

		// Now we must inspect the actual bits.
		// Avoid the iterator allocation / yield overhead of GetChangedProperties()
		var bits = _changedBits;
		while (bits != 0)
		{
			var bit = BitOperations.TrailingZeroCount(bits);
			var name = _sourceType.GetPropertyNameByBit(bit);

			if ((name != null) && settings.ShouldProcessProperty(name))
			{
				return true;
			}

			bits &= bits - 1; // clear lowest set bit
		}

		return false;
	}

	public virtual void InitializeLifecycle()
	{
		_isInitialized = true;
	}

	public virtual bool IsLifecycleInitialized()
	{
		return _isInitialized;
	}

	public virtual bool IsLifecycleLoaded()
	{
		return _isLoaded;
	}

	public virtual bool IsLifecycleStarted()
	{
		return _isStarted;
	}

	public virtual bool IsPropertyChangeNotificationsEnabled()
	{
		return _notificationsEnabled;
	}

	public virtual void LoadLifecycle()
	{
		_isLoaded = true;
	}

	public virtual void ProcessLifecycle()
	{
	}

	public virtual void ResetHasChanged(string propertyName)
	{
		if (string.IsNullOrWhiteSpace(propertyName) || (_sourceType == null))
		{
			return;
		}

		var bit = _sourceType.GetPropertyBit(propertyName);
		if (bit < 0)
		{
			return;
		}

		// Atomically clear only the specified bit
		Interlocked.And(ref _changedBits, ~(1UL << bit));
	}

	public virtual void ResetHasChanges()
	{
		// Single atomic write – safe even if a background thread is reading _changedBits
		Interlocked.Exchange(ref _changedBits, 0UL);
	}

	public object ShallowCloneObject(IncludeExcludeSettings settings = null)
	{
		return DeepCloneObject(0, settings);
	}

	public virtual bool ShouldUpdate(object update, IncludeExcludeSettings settings)
	{
		return UpdateableExtensions.ShouldUpdate(this, update, settings);
	}

	public virtual void StartLifecycle()
	{
		_isStarted = true;
	}

	public virtual void StopLifecycle()
	{
		_isStarted = false;
	}

	public bool TryUpdateWith(object update)
	{
		return TryUpdateWith(update, IncludeExcludeSettings.Empty);
	}

	public bool TryUpdateWith(object update, IncludeExcludeSettings settings)
	{
		return ShouldUpdate(update, settings)
			&& UpdateWith(update, settings);
	}

	public virtual void UninitializeLifecycle()
	{
		_isInitialized = false;
	}

	public virtual void UnloadLifecycle()
	{
		_isLoaded = false;
	}

	public virtual bool UpdatePropertyWith(string propertyName, object value)
	{
		return false;
	}

	public bool UpdateWith(object update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	public bool UpdateWith(object update, UpdateableAction action)
	{
		var options = Cache.GetSettings(GetType(), action);
		return UpdateWith(update, options);
	}

	public virtual bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return false;
	}

	/// <summary>
	/// Notifies that a computed/dependent property has changed.
	/// Use this instead of OnPropertyChanged for properties that are calculated.
	/// </summary>
	protected void NotifyComputedPropertyChanged(string propertyName)
	{
		OnPropertyChanged<object>(propertyName, null, null);
	}

	/// <summary>
	/// Notifies that a computed/dependent property has changed.
	/// Use this instead of OnPropertyChanged for properties that are calculated.
	/// </summary>
	protected void NotifyComputedPropertyChanged<T>(string propertyName, T newValue)
	{
		OnPropertyChanged(propertyName, default, newValue);
	}

	/// <summary>
	/// Notifies that a computed/dependent property has changed.
	/// Use this instead of OnPropertyChanged for properties that are calculated.
	/// </summary>
	protected void NotifyComputedPropertyChanged<T>(string propertyName, T oldValue, T newValue)
	{
		if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
		{
			OnPropertyChanged(propertyName, oldValue, newValue);
		}
	}

	/// <summary>
	/// Indicates the property has changed on the notifiable object.
	/// </summary>
	/// <param name="propertyName"> The name of the property has changed. </param>
	/// <param name="oldValue"> The old value of the property. </param>
	/// <param name="newValue"> The new value of the property. </param>
	protected virtual void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		// Ensure we have not paused property notifications
		if ((propertyName == null) || !IsPropertyChangeNotificationsEnabled())
		{
			// Property change notifications have been paused or property null so bounce
			return;
		}

		TrackPropertyChanged(propertyName);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Indicates the property is changing on the notifiable object.
	/// </summary>
	/// <param name="propertyName"> The name of the property is changing. </param>
	/// <param name="oldValue"> The old value of the property. </param>
	/// <param name="newValue"> The new value of the property. </param>
	protected virtual void OnPropertyChanging<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		// Ensure we have not paused property notifications
		if ((propertyName == null) || !IsPropertyChangeNotificationsEnabled())
		{
			// Property change notifications have been paused or property null so bounce
			return;
		}

		PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
	}

	/// <summary>
	/// Set the track flags for the properties.
	/// </summary>
	protected virtual void SetChangedProperties(params IEnumerable<string> properties)
	{
		foreach (var property in properties)
		{
			TrackPropertyChanged(property);
		}
	}

	/// <summary>
	/// Change the property then notify that it changed.
	/// </summary>
	/// <param name="field"> The field that represents the property. </param>
	/// <param name="value"> The value to change the property. </param>
	/// <param name="propertyName"> The name of the property to notify. </param>
	protected bool SetProperty<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		return SetProperty(ref field, value, false, propertyName);
	}

	/// <summary>
	/// Change the property then notify that it changed.
	/// </summary>
	/// <param name="field"> The field that represents the property. </param>
	/// <param name="value"> The value to change the property. </param>
	/// <param name="propertyName"> The name of the property to notify. </param>
	/// <param name="validate"> Optional flag to trigger validation. </param>
	protected bool SetProperty<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, bool validate, [CallerMemberName] string propertyName = null)
	{
		if (Equals(field, value))
		{
			return false;
		}

		var oldValue = field;

		if (!string.IsNullOrWhiteSpace(propertyName))
		{
			OnPropertyChanging(propertyName, oldValue, value);
		}

		field = value;

		if (!string.IsNullOrWhiteSpace(propertyName))
		{
			OnPropertyChanged(propertyName, oldValue, value);
		}

		return true;
	}

	protected bool TryUpdateProperty<T>(T current, T update, bool shouldUpdate, Action<T> assignment = null)
	{
		if (!shouldUpdate)
		{
			return true;
		}

		if (current is IUpdateable updateable
			&& update is not null)
		{
			return updateable.UpdateWith(update);
		}

		assignment?.Invoke(update);
		return true;
	}

	protected bool TryUpdateProperty<T, T2>(T current, T update, bool shouldUpdate, Action<T> assignment = null)
	{
		if (!shouldUpdate)
		{
			return false;
		}

		switch (current)
		{
			case IPresentationList<T2> presentationList
				when update is IPresentationList<T2> presentationUpdateList:
			{
				CollectionExtensions.ReconcileListAndItems(presentationList, presentationUpdateList);
				return true;
			}
			case IList<T2> list
				when update is IList<T2> updateList:
			{
				CollectionExtensions.ReconcileListAndItems(list, updateList);
				return true;
			}
			default:
			{
				TryUpdateProperty(current, update, true, assignment);
				return true;
			}
		}
	}

	/// <summary>
	/// Converts a change-bit mask into the corresponding property names.
	/// </summary>
	private IEnumerable<string> GetPropertyNamesFromBits(ulong bits)
	{
		if ((bits == 0) || (_sourceType == null))
		{
			yield break;
		}

		while (bits != 0)
		{
			var bit = BitOperations.TrailingZeroCount(bits);
			var name = _sourceType.GetPropertyNameByBit(bit);

			if (name != null)
			{
				yield return name;
			}

			bits &= bits - 1;
		}
	}

	private void TrackPropertyChanged(string propertyName)
	{
		var bit = _sourceType?.GetPropertyBit(propertyName) ?? -1;
		if (bit >= 0)
		{
			Interlocked.Or(ref _changedBits, 1UL << bit);
		}
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	public event PropertyChangingEventHandler PropertyChanging;

	#endregion
}