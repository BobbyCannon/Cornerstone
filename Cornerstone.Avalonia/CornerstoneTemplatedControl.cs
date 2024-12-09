#region References

using System.ComponentModel;
using Avalonia.Controls.Primitives;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia;

[DoNotNotify]
public class CornerstoneTemplatedControl : TemplatedControl, IDispatchable
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

	public static T GetService<T>()
	{
		return CornerstoneApplication.GetService<T>();
	}

	public void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}