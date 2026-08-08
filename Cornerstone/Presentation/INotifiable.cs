#region References

using System.ComponentModel;

#endregion

namespace Cornerstone.Presentation;

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

	#endregion
}