#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Avalonia;

public partial class CornerstoneUserControl<T>
	: CornerstoneUserControl
{
	#region Constructors

	public CornerstoneUserControl() : this(default)
	{
	}

	public CornerstoneUserControl(T viewModel)
	{
		ViewModel = viewModel;
	}

	#endregion

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

public partial class CornerstoneUserControl : UserControl
{
	#region Fields

	private Typeface? _cachedTypeface;

	#endregion

	#region Properties

	[StyledProperty]
	public partial Profiler Profiler { get; set; }

	public Typeface Typeface => _cachedTypeface ??= CornerstoneExtensions.CreateTypeface(this);

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

	#endregion
}