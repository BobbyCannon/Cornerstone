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
using Cornerstone.Runtime;

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
		return AppBootstrap.GetInstance<T>();
	}

	public static object GetInstance(Type type)
	{
		return AppBootstrap.GetInstance(type);
	}

	/// <summary>
	/// Returns the typed ViewModel property when this is a <see cref="CornerstoneControl{T}" />;
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
	}

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}