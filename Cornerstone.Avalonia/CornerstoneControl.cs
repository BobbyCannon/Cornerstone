#region References

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Avalonia;

public partial class CornerstoneControl<T>
	: CornerstoneControl where T : class
{
	#region Properties

	[StyledProperty]
	public partial T ViewModel { get; set; }

	#endregion

	#region Methods

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ViewModelProperty)
		{
			DataContext = ViewModel;
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}

public partial class CornerstoneControl : Control
{
	#region Fields

	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Properties

	[StyledProperty]
	public partial Profiler Profiler { get; set; }

	#endregion

	#region Methods

	public static T GetInstance<T>()
	{
		return CornerstoneApplication.DependencyProvider.GetInstance<T>();
	}

	public static object GetInstance(Type type)
	{
		return CornerstoneApplication.DependencyProvider.GetInstance(type);
	}

	protected void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}