#region References

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.Controls;

public sealed class PreviewCodeSnippet : CornerstoneContentControl
{
	#region Fields

	public static readonly StyledProperty<int> ColumnsProperty;
	public static readonly StyledProperty<ITemplate<Panel>> ItemsPanelProperty;
	public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty;
	public static readonly StyledProperty<ThemeColor> ThemeColorProperty;
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
		ColumnsProperty = AvaloniaProperty.Register<PreviewCodeSnippet, int>(nameof(Columns), 1);
		ItemTemplateProperty = AvaloniaProperty.Register<PreviewCodeSnippet, IDataTemplate>(nameof(ItemTemplate));
		ThemeColorProperty = AvaloniaProperty.Register<PreviewCodeSnippet, ThemeColor>(nameof(ThemeColor), Themes.ThemeColor.Blue);
		ToggleVariantCommandProperty = AvaloniaProperty.Register<PreviewCodeSnippet, ICommand>(nameof(ToggleVariantCommand));
		ItemsPanelProperty = AvaloniaProperty.Register<ItemsControl, ITemplate<Panel>>(nameof(ItemsPanel));
		ThemeVariantProperty = AvaloniaProperty.Register<PreviewCodeSnippet, ThemeVariant>(nameof(ThemeVariant), ThemeVariant.Default);
	}

	#endregion

	#region Properties

	public int Columns
	{
		get => GetValue(ColumnsProperty);
		set => SetValue(ColumnsProperty, value);
	}

	public ITemplate<Panel> ItemsPanel
	{
		get => GetValue(ItemsPanelProperty);
		set => SetValue(ItemsPanelProperty, value);
	}

	public IDataTemplate ItemTemplate
	{
		get => GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}

	public ThemeColor? ThemeColor
	{
		get => GetValue(ThemeColorProperty);
		set => SetValue(ThemeColorProperty, value);
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
		ThemeColor = Themes.Theme.GetThemeColor();
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ThemeColorProperty)
		{
			Themes.Theme.SetThemeColor(ThemeColor);
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