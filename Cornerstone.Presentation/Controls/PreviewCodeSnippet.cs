#region References

using System.Windows.Input;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Naming;

#endregion

namespace Cornerstone.Presentation.Controls;

public sealed class PreviewCodeSnippet : ContentControl
{
	#region Fields

	public static readonly StyledProperty<int> ColumnsProperty;
	public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty;
	public static readonly StyledProperty<ITemplate<Panel>> ItemsPanelProperty;
	public static readonly StyledProperty<ThemeColor> ThemeColorProperty;
	public static readonly StyledProperty<ThemeDensity?> ThemeDensityProperty;
	public static readonly StyledProperty<ThemeVariant> ThemeVariantProperty;
	public static readonly StyledProperty<ICommand> ToggleVariantCommandProperty;

	#endregion

	#region Constructors

	public PreviewCodeSnippet()
	{
		ToggleVariantCommand = new RelayCommand(ToggleVariant);
		VerticalAlignment = VerticalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Top;
	}

	static PreviewCodeSnippet()
	{
		ColumnsProperty = PresentationProperty.Register<PreviewCodeSnippet, int>(nameof(Columns), 1);
		ItemTemplateProperty = PresentationProperty.Register<PreviewCodeSnippet, IDataTemplate>(nameof(ItemTemplate));
		ThemeColorProperty = PresentationProperty.Register<PreviewCodeSnippet, ThemeColor>(nameof(ThemeColor), Cornerstone.Presentation.Theme.Theming.ThemeColor.Blue);
		ThemeDensityProperty = PresentationProperty.Register<PreviewCodeSnippet, ThemeDensity?>(nameof(ThemeDensity), Cornerstone.Presentation.Theme.Theming.ThemeDensity.Normal);
		ToggleVariantCommandProperty = PresentationProperty.Register<PreviewCodeSnippet, ICommand>(nameof(ToggleVariantCommand));
		ItemsPanelProperty = PresentationProperty.Register<ItemsControl, ITemplate<Panel>>(nameof(ItemsPanel));
		ThemeVariantProperty = PresentationProperty.Register<PreviewCodeSnippet, ThemeVariant>(nameof(ThemeVariant), ThemeVariant.Default);
	}

	#endregion

	#region Properties

	public int Columns
	{
		get => GetValue(ColumnsProperty);
		set => SetValue(ColumnsProperty, value);
	}

	public IDataTemplate ItemTemplate
	{
		get => GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}

	public ITemplate<Panel> ItemsPanel
	{
		get => GetValue(ItemsPanelProperty);
		set => SetValue(ItemsPanelProperty, value);
	}

	public ThemeColor? ThemeColor
	{
		get => GetValue(ThemeColorProperty);
		set => SetValue(ThemeColorProperty, value);
	}

	/// <summary>
	/// Compact / Normal / Large. Applied like ThemeColor (app/theme-wide, not subtree-scoped).
	/// </summary>
	public ThemeDensity? ThemeDensity
	{
		get => GetValue(ThemeDensityProperty);
		set => SetValue(ThemeDensityProperty, value);
	}

	public ThemeVariant ThemeVariant
	{
		get => GetValue(ThemeVariantProperty);
		set => SetValue(ThemeVariantProperty, value);
	}

	public ICommand ToggleVariantCommand
	{
		get => GetValue(ToggleVariantCommandProperty);
		set => SetValue(ToggleVariantCommandProperty, value);
	}

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		ItemsPanel ??=
			new FuncTemplate<Panel>(() =>
			{
				var grid = new WrapPanel
				{
					ItemSpacing = 10,
					LineSpacing = 10
				};
				return grid;
			});

		base.OnApplyTemplate(e);

		var grid = e.NameScope.Find<Grid>("PART_MainGrid");

		this.GetObservable(ItemTemplateProperty)
			.Subscribe(_ =>
			{
				if (grid == null)
				{
					return;
				}

				grid.RowDefinitions.Clear();

				if (ItemTemplate == null)
				{
					grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
				}
				else
				{
					grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
				}
			});
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		// Sync combos from the live theme (same as ThemeColor).
		var theme = ApplicationTheme.GetCurrent();
		if (theme != null)
		{
			ThemeColor = theme.ThemeColor;
			ThemeDensity = theme.ThemeDensity;
		}
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		var theme = ApplicationTheme.GetCurrent();
		if (change.Property == ThemeColorProperty)
		{
			if ((theme != null) && (ThemeColor != null))
			{
				theme.ThemeColor = ThemeColor.Value;
			}
		}
		else if (change.Property == ThemeDensityProperty)
		{
			if ((theme != null) && (ThemeDensity != null))
			{
				theme.ThemeDensity = ThemeDensity.Value;
			}
		}

		base.OnPropertyChanged(change);
	}

	private void ToggleVariant(object obj)
	{
		ThemeVariant u;

		if (ThemeVariant == ThemeVariant.Dark)
		{
			u = ThemeVariant.Light;
		}
		else if (ThemeVariant == ThemeVariant.Light)
		{
			u = ThemeVariant.Default;
		}
		else
		{
			u = ThemeVariant.Dark;
		}

		ThemeVariant = u;
	}

	#endregion
}