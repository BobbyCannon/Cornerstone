#region References

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;

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
		if ((change.Property == DataContextProperty)
			&& DataContext is T viewModel)
		{
			ViewModel = viewModel;
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}

[SourceReflection]
public partial class CornerstoneControl : Control, IDispatchable
{
	#region Fields

	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Properties

	[StyledProperty]
	public partial Profiler Profiler { get; set; }

	#endregion

	#region Methods

	public IDispatcher GetDispatcher()
	{
		return CornerstoneApplication.CornerstoneDispatcher;
	}

	public static T GetInstance<T>()
	{
		return CornerstoneApplication.DependencyProvider.GetInstance<T>();
	}

	public static object GetInstance(Type type)
	{
		return CornerstoneApplication.DependencyProvider.GetInstance(type);
	}

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}