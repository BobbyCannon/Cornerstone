#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

[DoNotNotify]
[PseudoClasses(":active")]
public class DockableTabItem : TabItem
{
	#region Fields

	public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent;
	public static readonly StyledProperty<bool> IsActiveProperty;
	public static readonly StyledProperty<DockableTabModel> TabModelProperty;

	#endregion

	#region Constructors

	public DockableTabItem() : this(new DockableTabModel())
	{
	}

	public DockableTabItem(DockableTabModel tabModel)
	{
		TabModel = tabModel;
		UpdatePseudoClasses(IsActive);
	}

	static DockableTabItem()
	{
		ClosedEvent = RoutedEvent.Register<DockableTabItem, RoutedEventArgs>("Closed", RoutingStrategies.Direct);
		IsActiveProperty = AvaloniaProperty.Register<DockableTabItem, bool>(nameof(IsActive));
		TabModelProperty = AvaloniaProperty.Register<DockableTabItem, DockableTabModel>(nameof(TabModel));
	}

	#endregion

	#region Properties

	public Button CloseButton { get; private set; }

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

	protected override Type StyleKeyOverride => typeof(DockableTabItem);

	#endregion

	#region Methods

	public void Close()
	{
		if (!TabModel.CanCloseTab())
		{
			return;
		}

		RaiseEvent(new RoutedEventArgs(ClosedEvent));
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (CloseButton != null)
		{
			CloseButton.Click -= CloseButtonOnClick;
		}

		CloseButton = e.NameScope.Find<Button>("PART_CloseButton");

		if (CloseButton != null)
		{
			CloseButton.Click += CloseButtonOnClick;
		}

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
				UpdatePseudoClasses(change.GetNewValue<bool>());
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
				break;
			}
		}
		base.OnPropertyChanged(change);
	}

	private void CloseButtonOnClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void UpdatePseudoClasses(bool isActive)
	{
		PseudoClasses.Set(":active", isActive);
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