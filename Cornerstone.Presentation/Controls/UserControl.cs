#region References

using System;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Automation.Peers;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.DesignTime;

#endregion

namespace Cornerstone.Presentation.Controls;

public class UserControl<T> : UserControl
	where T : class
{
	#region Fields

	public static readonly StyledProperty<T> ViewModelProperty;

	#endregion

	#region Constructors

	public UserControl()
	{
		if (!Design.IsDesignMode)
		{
			return;
		}

		// ReSharper disable once VirtualMemberCallInConstructor
		var designData = CreateDesignData();
		if (designData != null)
		{
			DataContext ??= designData;
		}

		DesignLifecycleHook?.Invoke(DataContext);
	}

	static UserControl()
	{
		ViewModelProperty = PresentationProperty.Register<UserControl<T>, T>(nameof(ViewModel));
	}

	#endregion

	#region Properties

	public T ViewModel
	{
		get => GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	protected override Type StyleKeyOverride => typeof(UserControl);

	#endregion

	#region Methods

	/// <summary>
	/// Overrides should only build sample view models (as they do today) and not
	/// assume the derived constructor has finished or even started.
	/// </summary>
	/// <returns> The design </returns>
	protected virtual T CreateDesignData()
	{
		return DesignDataFactory?.Invoke(typeof(T)) as T;
	}

	protected override object GetViewModel()
	{
		return ViewModel;
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if ((change.Property == DataContextProperty)
			&& DataContext is T viewModel)
		{
			ViewModel = viewModel;
		}

		if (change.Property == ViewModelProperty)
		{
			ViewModelChangedHook?.Invoke(this, change.OldValue, change.NewValue, DataContext, VisualRoot != null);
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}

/// <summary>
/// Provides the base class for defining a new control that encapsulates related existing controls and provides its own logic.
/// </summary>
[SourceReflection]
public class UserControl : ContentControl
{
	#region Methods

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new UserControlAutomationPeer(this);
	}

	#endregion
}