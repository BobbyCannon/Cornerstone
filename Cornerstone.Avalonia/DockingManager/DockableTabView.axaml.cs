#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[PseudoClasses(":active")]
public class DockableTabView : TabItem
{
	#region Fields

	public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent;
	public static readonly StyledProperty<bool> IsActiveProperty;
	public static readonly StyledProperty<DockableTabModel> TabModelProperty;

	#endregion

	#region Constructors

	public DockableTabView() : this(new DockableTabModel())
	{
	}

	public DockableTabView(DockableTabModel tabModel)
	{
		TabModel = tabModel;
		UpdatePseudoClasses();
	}

	static DockableTabView()
	{
		ClosedEvent = RoutedEvent.Register<DockableTabView, RoutedEventArgs>("Closed", RoutingStrategies.Direct);
		IsActiveProperty = AvaloniaProperty.Register<DockableTabView, bool>(nameof(IsActive));
		TabModelProperty = AvaloniaProperty.Register<DockableTabView, DockableTabModel>(nameof(TabModel));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets if this is the currently active dockable.
	/// </summary>
	public bool IsActive
	{
		get => GetValue(IsActiveProperty);
		set => SetValue(IsActiveProperty, value);
	}

	public DateTime LastSelectedOn { get; internal set; }

	public DockingTabControl TabControl { get; internal set; }

	public DockableTabModel TabModel
	{
		get => GetValue(TabModelProperty);
		set
		{
			SetValue(TabModelProperty, value);

			Content = value;
			DataContext = value;
			Header = null;
		}
	}

	protected override Type StyleKeyOverride => typeof(DockableTabView);

	#endregion

	#region Methods

	public void Close(bool force)
	{
		if (!force && !TabModel.CanCloseTab())
		{
			return;
		}

		TabModel.OnClosing();

		RaiseEvent(new RoutedEventArgs(ClosedEvent));
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		TabModel.CloseRequested += (_, force) => Close(force);
		base.OnApplyTemplate(e);
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		switch (change.Property.Name)
		{
			case nameof(IsFocused):
			{
				var tabControl = TabControl;
				if (tabControl != null)
				{
					tabControl.IsActive = true;
				}
				break;
			}
			case nameof(IsActive):
			{
				UpdatePseudoClasses();
				break;
			}
			case nameof(IsSelected):
			{
				var tabModel = TabModel;
				if (tabModel != null)
				{
					tabModel.IsSelected = change.GetNewValue<bool>();
				}
				var tabControl = TabControl;
				if (tabControl != null)
				{
					tabControl.IsActive = true;
				}
				UpdatePseudoClasses();
				break;
			}
		}
		base.OnPropertyChanged(change);
	}

	private void UpdatePseudoClasses()
	{
		PseudoClasses.Set(":active", IsActive);
		PseudoClasses.Set(":selected", IsSelected);
	}

	#endregion

	#region Events

	public event EventHandler<RoutedEventArgs> Closed
	{
		add => AddHandler(ClosedEvent, value);
		remove => RemoveHandler(ClosedEvent, value);
	}

	#endregion
}