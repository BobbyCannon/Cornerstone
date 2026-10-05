#region References

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Cornerstone.Collections;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Layout;
using ResponsiveGridControl = Cornerstone.Presentation.Controls.ResponsiveGrid;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.PropertyGrid;

#endregion

namespace Cornerstone.Presentation.Controls;

[TemplatePart("PropertiesGrid", typeof(Grid), IsRequired = true)]
public partial class PropertyGridControl : ContentControl
{
	#region Constructors

	public PropertyGridControl()
	{
		AllProperties = [];
		Categories = new SpeedyDictionary<string, SpeedyList<PropertyDescriptor>>();
		Factories = PropertyCellFactoryCollection.Default;
		PropertyViews = [];
	}

	#endregion

	#region Properties

	public SpeedyList<PropertyDescriptor> AllProperties { get; }

	public SpeedyDictionary<string, SpeedyList<PropertyDescriptor>> Categories { get; }

	[StyledProperty]
	public partial bool IsReadOnly { get; set; }

	public Grid PropertiesGrid { get; private set; }

	public SpeedyList<PropertyCellContext> PropertyViews { get; }

	[StyledProperty]
	public partial object Source { get; set; }

	private PropertyCellFactoryCollection Factories { get; }

	#endregion

	#region Methods

	public void Clear()
	{
		AllProperties.Clear();
		Categories.Clear();
		foreach (var view in PropertyViews)
		{
			view.RemovePropertyChangedObserver();
		}
		PropertyViews.Clear();
	}

	protected virtual IList<PropertyCellContext> BuildCategoryPropertiesView(object target, ReferencePath referencePath)
	{
		PropertiesGrid.ColumnDefinitions.Clear();

		var response = new List<PropertyCellContext>();

		foreach (var categoryInfo in Categories)
		{
			PropertiesGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

			var expander = new Expander { ExpandDirection = ExpandDirection.Down };
			expander.SetValue(Grid.RowProperty, PropertiesGrid.RowDefinitions.Count - 1);
			expander.IsExpanded = true;
			expander.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			expander.HorizontalAlignment = HorizontalAlignment.Stretch;
			expander.Margin = new Thickness(2);
			expander.Padding = new Thickness(2);
			expander.Header = categoryInfo.Key;

			var grid = new ResponsiveGridControl { Margin = new Thickness(4, 0) };
			expander.Content = grid;

			var properties = categoryInfo.Value.OrderBy(x => x.DisplayName).ToList();
			response.AddRange(BuildPropertiesCellEdit(target, referencePath, properties, grid));

			expander.IsVisible = grid.Children.Count > 0;
			PropertiesGrid.Children.Add(expander);
		}

		PropertiesGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
		return response;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		PropertiesGrid = e.NameScope.Find<Grid>("PropertiesGrid");
		RefreshProperties();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		Clear();
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if ((change.Property == SourceProperty) && (PropertiesGrid != null))
		{
			RefreshProperties();
		}

		base.OnPropertyChanged(change);
	}

	private IList<PropertyCellContext> BuildPropertiesCellEdit(object target, ReferencePath referencePath, IEnumerable<PropertyDescriptor> properties, Grid grid)
	{
		var response = new List<PropertyCellContext>();

		foreach (var property in properties)
		{
			referencePath.BeginScope(property.Name);
			try
			{
				var cell = BuildPropertyCellEdit(target, referencePath, property, grid);
				if (cell != null)
				{
					response.Add(cell);
				}
			}
			finally
			{
				referencePath.EndScope();
			}
		}

		return response;
	}

	private IList<PropertyCellContext> BuildPropertiesView()
	{
		PropertiesGrid.RowDefinitions.Clear();
		PropertiesGrid.Children.Clear();

		var source = Source;
		if (source == null)
		{
			return [];
		}

		var referencePath = new ReferencePath();
		try
		{
			referencePath.BeginScope(source.GetType().Name);
			return BuildCategoryPropertiesView(source, referencePath);
		}
		finally
		{
			referencePath.EndScope();
		}
	}

	private PropertyCellContext BuildPropertyCellEdit(object target, ReferencePath referencePath,
		PropertyDescriptor propertyDescriptor, Grid grid)
	{
		var context = new PropertyCellContext(target, propertyDescriptor);
		var control = Factories.BuildPropertyControl(context);
		if (control == null)
		{
			return null;
		}

		var nameBlock = new TextBlock
		{
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(4),
			Text = propertyDescriptor.DisplayName
		};
		nameBlock.SetValue(ResponsiveGridControl.SMProperty, 12);
		nameBlock.SetValue(ResponsiveGridControl.XSProperty, 24);

		if (propertyDescriptor.GetCustomAttribute<DescriptionAttribute>() is { } descriptionAttribute
			&& !string.IsNullOrEmpty(descriptionAttribute.Description))
		{
			nameBlock.SetValue(ToolTip.TipProperty, descriptionAttribute.Description);
		}

		grid.Children.Add(nameBlock);

		control.SetValue(ResponsiveGridControl.SMProperty, 12);
		control.SetValue(ResponsiveGridControl.XSProperty, 24);
		control.Margin = nameBlock.Margin;
		control.HorizontalAlignment = HorizontalAlignment.Stretch;
		control.IsEnabled = !propertyDescriptor.IsReadOnly && !IsReadOnly;
		grid.Children.Add(control);

		context.Factory.HandlePropertyChanged(context);
		context.AddPropertyChangedObserver();
		return context;
	}

	private void RefreshProperties()
	{
		Clear();

		if ((Source == null) || (PropertiesGrid == null))
		{
			return;
		}

		var builder = new PropertyDescriptorBuilder(Source);
		var defaultCategory = Source.GetType().Name;

		foreach (PropertyDescriptor property in builder.GetProperties())
		{
			if (!property.IsBrowsable || property.IsDefined<IgnoreDataMemberAttribute>())
			{
				continue;
			}

			AllProperties.Add(property);
			var category = property.GetCategory(defaultCategory);
			if (Categories.TryGetValue(category, out var list))
			{
				list.Add(property);
			}
			else
			{
				Categories.Add(category, [property]);
			}
		}

		foreach (var view in BuildPropertiesView())
		{
			PropertyViews.Add(view);
		}
	}

	#endregion
}