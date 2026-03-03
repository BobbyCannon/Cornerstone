#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a notifiable object.
/// </summary>
/// todo: support generics in code generation
/// [SourceReflection]
[Updateable(false)]
public abstract class Notifiable<T> : Notifiable
	where T : class, new()
{
}

/// <summary>
/// Represents a notifiable object.
/// </summary>
[SourceReflection]
[Updateable(false)]
public abstract class Notifiable : INotifiable, IUpdateable, ITrackPropertyChanges
{
	#region Fields

	private readonly HashSet<string> _changedProperties;
	private bool _notificationsEnabled;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a notifiable object.
	/// </summary>
	protected Notifiable()
	{
		_changedProperties = new HashSet<string>(StringComparer.Ordinal);
		_notificationsEnabled = true;
	}

	#endregion

	#region Methods

	public void ApplyChangesTo(object destination)
	{
		destination.UpdateWithOnly(this, _changedProperties.ToArray());
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
		return [.._changedProperties];
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
		if (_changedProperties.Count == 0)
		{
			return false;
		}

		if (settings.IsEmpty())
		{
			return true;
		}

		foreach (var property in _changedProperties)
		{
			if (settings.ShouldProcessProperty(property))
			{
				return true;
			}
		}

		return false;
	}

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

	/// <summary>
	/// Indicates the property is changing on the notifiable object.
	/// </summary>
	/// <param name="propertyName"> The name of the property changing. </param>
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

	public virtual void ResetHasChanges()
	{
		_changedProperties.Clear();
	}

	public virtual bool ShouldUpdate(object update, IncludeExcludeSettings settings)
	{
		return UpdateableExtensions.ShouldUpdate(this, update, settings);
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

	public bool UpdateWith(object update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	public bool UpdateWith(object update, UpdateableAction action)
	{
		// todo: implement
		//var options = Cache.GetSettings(GetRealType(), action);
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	public virtual bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return false;
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

		_changedProperties.Add(propertyName);

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

	protected void UpdateProperty<T>(T current, T update, bool shouldUpdate, Action<T> assignment = null)
	{
		if (!shouldUpdate)
		{
			return;
		}

		if (current is IUpdateable updateable
			&& update is not null)
		{
			updateable.UpdateWith(update);
			return;
		}

		assignment?.Invoke(update);
	}

	protected T UpdateProperty<T>(T current, T update)
	{
		if (current is IUpdateable updateable
			&& update is not null)
		{
			updateable.UpdateWith(update);
			return (T) updateable;
		}

		// todo: support reconcile for collections

		return update;
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	public event PropertyChangingEventHandler PropertyChanging;

	#endregion
}

/// <summary>
/// Represents a notifiable object.
/// </summary>
public interface INotifiable : INotifyPropertyChanged, INotifyPropertyChanging
{
	#region Methods

	/// <summary>
	/// Disable the property change notifications
	/// </summary>
	public void DisablePropertyChangeNotifications();

	/// <summary>
	/// Enable the property change notifications
	/// </summary>
	public void EnablePropertyChangeNotifications();

	/// <summary>
	/// Return true if the change notifications are enabled or otherwise false.
	/// </summary>
	public bool IsPropertyChangeNotificationsEnabled();

	/// <summary>
	/// Notifies the property has changed.
	/// </summary>
	/// <param name="propertyName"> The name of the property to notify. </param>
	/// <remarks>
	/// The property will not show in ITrackPropertyChanges state.
	/// </remarks>
	public void NotifyOfPropertyChanged(string propertyName);

	/// <summary>
	/// Notifies the property is changing.
	/// </summary>
	/// <param name="propertyName"> The name of the property to notify. </param>
	/// <remarks>
	/// The property will not show in ITrackPropertyChanges state.
	/// </remarks>
	public void NotifyOfPropertyChanging(string propertyName);

	#endregion
}