#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Internal;
#if !NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a notifiable object.
/// </summary>
public abstract class Notifiable<T> : Notifiable, ICloneable<T>, IUpdateable<T>
	where T : class, new()
{
	#region Methods

	/// <inheritdoc />
	public virtual T DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return (T) this.DeepCloneUsingUpdateWith(typeof(T), maxDepth, settings);
	}

	/// <inheritdoc />
	public T ShallowClone(IncludeExcludeSettings settings = null)
	{
		return DeepClone(0, settings);
	}

	/// <inheritdoc />
	public virtual bool ShouldUpdate(T update, IncludeExcludeSettings settings)
	{
		return UpdateableExtensions.ShouldUpdate(this, update, settings);
	}

	/// <inheritdoc />
	public bool TryUpdateWith(T update)
	{
		return TryUpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <inheritdoc />
	public bool TryUpdateWith(T update, IncludeExcludeSettings settings)
	{
		return ShouldUpdate(update, settings)
			&& UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public bool UpdateWith(T update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <inheritdoc />
	public bool UpdateWith(T update, UpdateableAction action)
	{
		var options = Cache.GetOptions(GetRealType(), action);
		return UpdateWith(update, options);
	}

	/// <inheritdoc />
	public abstract bool UpdateWith(T update, IncludeExcludeSettings settings);

	/// <inheritdoc />
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
public abstract class Notifiable : INotifiable, IUpdateable, ICloneable, IUpdateableOptionsProvider
{
	#region Fields

	private readonly ConcurrentDictionary<string, DateTime> _changedProperties;
	private bool _notificationsEnabled;
	private Type _realType;
	private readonly ReadOnlySet<string> _writableProperties;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a notifiable object.
	/// </summary>
	protected Notifiable()
	{
		_changedProperties = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
		_writableProperties = Cache.GetSettableProperties(this).Select(x => x.Name).ToReadOnlySet();
		_notificationsEnabled = true;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void ApplyChangesTo(object destination)
	{
		destination.UpdateWithOnly(this, _changedProperties.Keys.ToArray());
	}

	/// <inheritdoc />
	public virtual object DeepCloneObject(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return this.DeepCloneUsingUpdateWith(GetRealType(), maxDepth, settings);
	}

	/// <inheritdoc />
	public virtual void DisablePropertyChangeNotifications()
	{
		_notificationsEnabled = false;
	}

	/// <inheritdoc />
	public virtual void EnablePropertyChangeNotifications()
	{
		_notificationsEnabled = true;
	}

	/// <inheritdoc />
	public ReadOnlySet<string> GetChangedProperties()
	{
		return new ReadOnlySet<string>(_changedProperties.Keys);
	}

	/// <inheritdoc />
	public virtual HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
	{
		return GetRealType().GetDefaultIncludedProperties(action);
	}

	/// <summary>
	/// Cached version of the "real" type, meaning not EF proxy but rather root type
	/// </summary>
	public Type GetRealType()
	{
		return _realType ??= this.GetRealTypeUsingReflection();
	}

	/// <inheritdoc />
	public IncludeExcludeSettings GetUpdateableOptions(UpdateableAction action)
	{
		return Cache.GetOptions(GetRealType(), action);
	}

	/// <inheritdoc />
	public bool HasChanges()
	{
		return HasChanges(IncludeExcludeSettings.Empty);
	}

	/// <inheritdoc />
	public virtual bool HasChanges(IncludeExcludeSettings settings)
	{
		return _changedProperties.Any(x => settings.ShouldProcessProperty(x.Key));
	}

	/// <inheritdoc />
	public virtual bool IsPropertyChangeNotificationsEnabled()
	{
		return _notificationsEnabled;
	}

	/// <summary>
	/// Indicates the property has changed on the notifiable object.
	/// </summary>
	/// <param name="propertyName"> The name of the property has changed. </param>
	public void NotifyOfPropertyChanged(string propertyName)
	{
		// Ensure we have not paused property notifications
		if ((propertyName == null) || !IsPropertyChangeNotificationsEnabled())
		{
			// Property change notifications have been paused or property null so bounce
			return;
		}

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <inheritdoc />
	public void NotifyOfPropertyChanging(string propertyName)
	{
		// Ensure we have not paused property notifications
		if ((propertyName == null) || !IsPropertyChangeNotificationsEnabled())
		{
			// Property change notifications have been paused or property null so bounce
			return;
		}

		PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
	}

	/// <inheritdoc />
	public virtual void ResetHasChanges()
	{
		_changedProperties.Clear();
	}

	/// <inheritdoc />
	public object ShallowCloneObject(IncludeExcludeSettings settings = null)
	{
		return DeepCloneObject(0, settings);
	}

	/// <inheritdoc />
	public virtual bool ShouldUpdate(object update, IncludeExcludeSettings settings)
	{
		return UpdateableExtensions.ShouldUpdate(this, update, settings);
	}

	/// <inheritdoc />
	public bool TryUpdateWith(object update)
	{
		return TryUpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <inheritdoc />
	public bool TryUpdateWith(object update, IncludeExcludeSettings settings)
	{
		return ShouldUpdate(update, settings)
			&& UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public bool UpdateWith(object update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <inheritdoc />
	public bool UpdateWith(object update, UpdateableAction action)
	{
		var options = Cache.GetOptions(GetRealType(), action);
		return UpdateWith(update, options);
	}

	/// <inheritdoc />
	public virtual bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return this.UpdateWithUsingReflection(update, settings);
	}

	/// <summary>
	/// Indicates the property has changed on the notifiable object.
	/// </summary>
	/// <param name="propertyName"> The name of the property has changed. </param>
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		// Ensure we have not paused property notifications
		if ((propertyName == null) || !IsPropertyChangeNotificationsEnabled())
		{
			// Property change notifications have been paused or property null so bounce
			return;
		}

		if (_writableProperties?.Contains(propertyName) == true)
		{
			_changedProperties.AddOrUpdate(propertyName, _ => DateTime.UtcNow, (_, _) => DateTime.UtcNow);
		}

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Indicates the property is changing on the notifiable object.
	/// </summary>
	/// <param name="propertyName"> The name of the property is changing. </param>
	protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
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
	/// Change the property then notify that it changed.
	/// </summary>
	/// <param name="update"> The update to change the property. </param>
	/// <param name="propertyName"> The name of the property to notify. </param>
	protected void SetProperty(Action update, [CallerMemberName] string propertyName = "")
	{
		if (!string.IsNullOrWhiteSpace(propertyName))
		{
			OnPropertyChanging(propertyName);
		}

		update();

		if (!string.IsNullOrWhiteSpace(propertyName))
		{
			OnPropertyChanged(propertyName);
		}
	}

	/// <summary>
	/// Change the property then notify that it changed.
	/// </summary>
	/// <param name="field"> The field that represents the property. </param>
	/// <param name="value"> The value to change the property. </param>
	/// <param name="propertyName"> The name of the property to notify. </param>
	/// <param name="validate"> Optional flag to trigger validation.  </param>
	#if (NETSTANDARD2_0)
	protected bool SetProperty<T>(ref T field, T value, bool validate = false, [CallerMemberName] string propertyName = null)
	#else
	protected bool SetProperty<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, bool validate = false, [CallerMemberName] string propertyName = null)
	#endif
	{
		if (Equals(field, value))
		{
			return false;
		}

		if (!string.IsNullOrWhiteSpace(propertyName))
		{
			OnPropertyChanging(propertyName);
		}

		field = value;

		if (!string.IsNullOrWhiteSpace(propertyName))
		{
			OnPropertyChanged(propertyName);
		}

		return true;
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event PropertyChangedEventHandler PropertyChanged;

	/// <inheritdoc />
	public event PropertyChangingEventHandler PropertyChanging;

	#endregion
}

/// <summary>
/// Represents a notifiable object.
/// </summary>
public interface INotifiable : INotifyPropertyChanged, INotifyPropertyChanging, ITrackPropertyChanges
{
	#region Methods

	/// <summary>
	/// Disable the property change notifications
	/// </summary>
	void DisablePropertyChangeNotifications();

	/// <summary>
	/// Enable the property change notifications
	/// </summary>
	void EnablePropertyChangeNotifications();

	/// <summary>
	/// Return true if the change notifications are enabled or otherwise false.
	/// </summary>
	bool IsPropertyChangeNotificationsEnabled();

	/// <summary>
	/// Notifies the property has changed.
	/// </summary>
	/// <param name="propertyName"> The name of the property to notify. </param>
	/// <remarks>
	/// The property will not show in ITrackPropertyChanges state.
	/// </remarks>
	void NotifyOfPropertyChanged(string propertyName);

	/// <summary>
	/// Notifies the property is changing.
	/// </summary>
	/// <param name="propertyName"> The name of the property to notify. </param>
	/// <remarks>
	/// The property will not show in ITrackPropertyChanges state.
	/// </remarks>
	void NotifyOfPropertyChanging(string propertyName);

	#endregion
}