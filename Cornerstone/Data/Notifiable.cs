#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a notifiable object.
/// </summary>
public abstract class Notifiable : INotifiable, IUpdateable, IUpdateableOptionsProvider
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
	public UpdateableOptions GetUpdateableOptions(UpdateableAction action)
	{
		return Cache.GetOptions(GetRealType(), action);
	}

	/// <inheritdoc />
	public bool HasChanges()
	{
		return HasChanges(Array.Empty<string>());
	}

	/// <inheritdoc />
	public virtual bool HasChanges(params string[] exclusions)
	{
		return _changedProperties.Any(x => !exclusions.Contains(x.Key));
	}

	/// <inheritdoc />
	public virtual bool IsPropertyChangeNotificationsEnabled()
	{
		return _notificationsEnabled;
	}

	/// <inheritdoc />
	public virtual void ResetHasChanges()
	{
		_changedProperties.Clear();
	}

	/// <inheritdoc />
	public virtual bool ShouldUpdate(object update, UpdateableOptions options)
	{
		return UpdateableExtensions.ShouldUpdate(this, update, options);
	}

	/// <summary>
	/// Indicates the property has changed on the notifiable object.
	/// </summary>
	/// <param name="propertyName"> The name of the property has changed. </param>
	public void TriggerPropertyChangedNotification(string propertyName)
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
	public bool TryUpdateWith(object update)
	{
		return TryUpdateWith(update, UpdateableOptions.Empty);
	}

	/// <inheritdoc />
	public bool TryUpdateWith(object update, UpdateableOptions options)
	{
		return ShouldUpdate(update, options)
			&& UpdateWith(update, options);
	}

	/// <inheritdoc />
	public bool UpdateWith(object update)
	{
		return UpdateWith(update, UpdateableOptions.Empty);
	}

	/// <inheritdoc />
	public bool UpdateWith(object update, UpdateableAction action)
	{
		var options = Cache.GetOptions(GetRealType(), action);
		return UpdateWith(update, options);
	}

	/// <inheritdoc />
	public virtual bool UpdateWith(object update, UpdateableOptions options)
	{
		return this.UpdateWithUsingReflection(update, options);
	}

	/// <summary>
	/// Indicates the property has changed on the notifiable object.
	/// </summary>
	/// <param name="propertyName"> The name of the property has changed. </param>
	// ReSharper disable once UnusedMemberHierarchy.Global
	protected virtual void OnPropertyChanged(string propertyName)
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

	#endregion

	#region Events

	/// <inheritdoc />
	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}

/// <summary>
/// Represents a notifiable object.
/// </summary>
public interface INotifiable : INotifyPropertyChanged, ITrackPropertyChanges
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
	/// Notifies the property has changed but will not affect ITrackPropertyChanges state.
	/// </summary>
	/// <param name="propertyName"> The name of the property to notify. </param>
	void TriggerPropertyChangedNotification(string propertyName);

	#endregion
}