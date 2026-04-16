#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using System;
using System.ComponentModel;

#endregion

namespace Cornerstone.Avalonia;

public partial class CornerstoneUserControl<T>
	: CornerstoneUserControl where T : class
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
public partial class CornerstoneUserControl : UserControl, IDispatchable
{
	#region Fields

	private Typeface? _cachedTypeface;
	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Properties

	public Profiler Profiler { get; set; }

	public Typeface Typeface => _cachedTypeface ??= CornerstoneExtensions.CreateTypeface(this);

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

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == FontFamilyProperty)
			|| (change.Property == FontSizeProperty)
			|| (change.Property == FontStretchProperty)
			|| (change.Property == ForegroundProperty))
		{
			_cachedTypeface = null;
			InvalidateVisual();
		}
	}

	protected virtual void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}