#region References

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneContentControl : ContentControl, IDispatchable, INotifyPropertyChanging
{
	#region Fields

	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Methods

	/// <inheritdoc />
	public IDispatcher GetDispatcher()
	{
		return CornerstoneDispatcher.Instance;
	}

	public static T GetInstance<T>()
	{
		return CornerstoneApplication.GetInstance<T>();
	}

	public void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected virtual void OnPropertyChanging(string propertyName)
	{
		PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
	}

	/// <summary>
	/// Change the property then notify that it changed.
	/// </summary>
	/// <param name="field"> The field that represents the property. </param>
	/// <param name="value"> The value to change the property. </param>
	/// <param name="propertyName"> The name of the property to notify. </param>
	/// <param name="validate"> Optional flag to trigger validation. </param>
	protected bool SetProperty<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, bool validate = false, [CallerMemberName] string propertyName = null)
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

	public event PropertyChangingEventHandler PropertyChanging;

	#endregion
}