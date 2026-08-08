#region References

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia;

public partial class CornerstoneContentControl<T>
	: CornerstoneContentControl where T : class
{
	#region Properties

	[StyledProperty]
	public partial T ViewModel { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override object GetViewModel()
	{
		return ViewModel;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if ((change.Property == DataContextProperty)
			&& DataContext is T viewModel)
		{
			ViewModel = viewModel;
		}

		if (change.Property == ViewModelProperty)
		{
			DispatchableVisualTree.OnViewModelChanged(
				this, change.OldValue, change.NewValue, DataContext, VisualRoot != null);
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}

[SourceReflection]
public partial class CornerstoneContentControl : ContentControl, IDispatchable
{
	#region Fields

	private Typeface? _cachedTypeface;
	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Properties

	[StyledProperty]
	public partial Profiler Profiler { get; set; }

	public Typeface Typeface => _cachedTypeface ??= CornerstoneExtensions.CreateTypeface(this);

	#endregion

	#region Methods

	public IDispatcher GetDispatcher()
	{
		return CornerstoneApplication.CornerstoneDispatcher;
	}

	public static T GetInstance<T>()
	{
		return AppBootstrap.GetInstance<T>();
	}

	public static object GetInstance(Type type)
	{
		return AppBootstrap.GetInstance(type);
	}

	/// <summary>
	/// Returns the typed <c> ViewModel </c> property when this is a <see cref="CornerstoneContentControl{T}" />;
	/// otherwise null. Used with <see cref="StyledElement.DataContext" /> (independently) for IsAttached.
	/// </summary>
	protected virtual object GetViewModel()
	{
		return null;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		DispatchableVisualTree.OnAttachedToVisualTree(this, GetViewModel(), DataContext);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		DispatchableVisualTree.OnDetachedFromVisualTree(this, GetViewModel(), DataContext);
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == DataContextProperty)
		{
			DispatchableVisualTree.OnDataContextChanged(
				this,
				change.OldValue,
				change.NewValue,
				GetViewModel(),
				VisualRoot != null);
		}

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

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}