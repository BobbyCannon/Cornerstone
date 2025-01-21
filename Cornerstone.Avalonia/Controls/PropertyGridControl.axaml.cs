#region References

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Avalonia.PropertyGrid;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Controls;

public partial class PropertyGridControl : CornerstoneUserControl
{
	#region Fields

	public static readonly StyledProperty<object> SourceProperty;

	#endregion

	#region Constructors

	public PropertyGridControl()
	{
		var weakEventManager = GetInstance<WeakEventManager>();

		AllProperties = new SpeedyList<PropertyDescriptor>();
		Categories = new SpeedyDictionary<string, ISpeedyList<PropertyDescriptor>>();
		Factories = PropertyCellFactoryCollection.Default;
		PropertyViews = new SpeedyList<PropertyCellContext>();

		weakEventManager.AddCollectionChanged(AllProperties, AllPropertiesListChanged);

		InitializeComponent();
	}

	static PropertyGridControl()
	{
		SourceProperty = AvaloniaProperty.Register<PropertyGridControl, object>(nameof(Source));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets all properties.
	/// </summary>
	/// <value> All properties. </value>
	public ISpeedyList<PropertyDescriptor> AllProperties { get; }

	/// <summary>
	/// All properties by category.
	/// </summary>
	public SpeedyDictionary<string, ISpeedyList<PropertyDescriptor>> Categories { get; }

	/// <summary>
	/// All views for the properties.
	/// </summary>
	public SpeedyList<PropertyCellContext> PropertyViews { get; }

	/// <summary>
	/// The source of the property grid.
	/// </summary>
	public object Source
	{
		get => GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	private PropertyCellFactoryCollection Factories { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Clears this instance.
	/// </summary>
	public void Clear()
	{
		AllProperties.Clear();
		PropertyViews.ForEach(x => x.RemovePropertyChangedObserver());
		PropertyViews.Clear();
	}

	/// <summary>
	/// Builds the alphabetic properties view.
	/// </summary>
	/// <param name="target"> The target. </param>
	/// <param name="referencePath"> The reference path. </param>
	protected virtual IList<PropertyCellContext> BuildAlphabeticPropertiesView(object target, ReferencePath referencePath)
	{
		PropertiesGrid.ColumnDefinitions.Clear();
		PropertiesGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
		PropertiesGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

		return BuildPropertiesCellEdit(target, referencePath, AllProperties.OrderBy(x => x.DisplayName), PropertiesGrid);
	}

	/// <summary>
	/// Builds the category properties view.
	/// </summary>
	/// <param name="target"> The target. </param>
	/// <param name="referencePath"> The reference path. </param>
	protected virtual IList<PropertyCellContext> BuildCategoryPropertiesView(object target, ReferencePath referencePath)
	{
		PropertiesGrid.ColumnDefinitions.Clear();

		var categories = Categories;
		var response = new List<PropertyCellContext>();

		foreach (var categoryInfo in categories)
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

			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			grid.Margin = new Thickness(10);
			expander.Content = grid;

			var properties = categoryInfo.Value.OrderBy(x => x.DisplayName).ToList();
			response.Add(BuildPropertiesCellEdit(target, referencePath, properties, grid));

			expander.IsVisible = grid.Children.Any();
			PropertiesGrid.Children.Add(expander);
		}

		PropertiesGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

		return response;
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		Clear();
		base.OnDetachedFromVisualTree(e);
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == SourceProperty)
		{
			RefreshProperties();
		}

		base.OnPropertyChanged(change);
	}

	private void AllPropertiesListChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
	}

	/// <summary>
	/// Builds the properties cell edit.
	/// </summary>
	/// <param name="target"> The target. </param>
	/// <param name="referencePath"> The reference path. </param>
	/// <param name="properties"> The properties. </param>
	/// <param name="grid"> The grid. </param>
	private IList<PropertyCellContext> BuildPropertiesCellEdit(object target, ReferencePath referencePath, IEnumerable<PropertyDescriptor> properties, Grid grid)
	{
		var response = new List<PropertyCellContext>();

		foreach (var property in properties)
		{
			referencePath.BeginScope(property.Name);

			try
			{
				response.Add(BuildPropertyCellEdit(target, referencePath, property, grid));
			}
			finally
			{
				referencePath.EndScope();
			}
		}

		return response;
	}

	/// <summary>
	/// Builds the properties view.
	/// </summary>
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
			// I think this is a recursive check
			//_expandableObjectCache.Add(source);

			referencePath.BeginScope(source.GetType().Name);

			return BuildCategoryPropertiesView(source, referencePath);
			//BuildAlphabeticPropertiesView(source, referencePath);
		}
		finally
		{
			referencePath.EndScope();
		}
	}

	/// <summary>
	/// Builds the property cell edit.
	/// </summary>
	/// <param name="target"> The target. </param>
	/// <param name="referencePath"> The reference path. </param>
	/// <param name="propertyDescriptor"> The property descriptor. </param>
	/// <param name="grid"> The grid. </param>
	private PropertyCellContext BuildPropertyCellEdit(object target, ReferencePath referencePath,
		PropertyDescriptor propertyDescriptor, Grid grid)
	{
		var property = propertyDescriptor;
		var context = new PropertyCellContext(target, property);
		var control = Factories.BuildPropertyControl(context);

		if (control == null)
		{
			Debug.WriteLine($"Warning: Failed build property control for property:{property.Name}({property.PropertyType}");
			return null;
		}

		Debug.Assert(context.Factory != null);
		Debug.Assert(context.EditorControl != null);
		Debug.Assert(context.EditorControl == control);

		grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

		var nameBlock = new TextBlock();
		nameBlock.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
		nameBlock.SetValue(Grid.ColumnProperty, 0);
		nameBlock.VerticalAlignment = VerticalAlignment.Center;
		nameBlock.Margin = new Thickness(4);
		nameBlock.Text = property.DisplayName;

		if (property.GetCustomAttribute<DescriptionAttribute>() is { } descriptionAttribute
			&& descriptionAttribute.Description.IsNotNullOrEmpty())
		{
			nameBlock.SetValue(ToolTip.TipProperty, descriptionAttribute.Description);
		}

		grid.Children.Add(nameBlock);

		control.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
		control.SetValue(Grid.ColumnProperty, 1);
		control.Margin = nameBlock.Margin;
		control.HorizontalAlignment = HorizontalAlignment.Right;
		control.IsEnabled = !property.IsReadOnly;

		grid.Children.Add(control);

		context.Factory.HandlePropertyChanged(context);
		context.AddPropertyChangedObserver();

		return context;
	}

	/// <summary>
	/// Refreshes the properties.
	/// </summary>
	private void RefreshProperties()
	{
		Clear();

		var source = Source;
		if (source == null)
		{
			return;
		}

		var builder = new PropertyDescriptorBuilder(source);
		var defaultCategory = source.GetType().Name;

		AllProperties.AddRange(
			builder
				.GetProperties()
				.Cast<PropertyDescriptor>()
				.Where(x =>
					x.IsBrowsable
					&& !x.IsDefined<IgnoreDataMemberAttribute>()
				)
		);

		foreach (var property in AllProperties)
		{
			var category = property.GetCategory(defaultCategory);

			if (Categories.TryGetValue(category, out var categories))
			{
				categories.Add(property);
			}
			else
			{
				var list = new SpeedyList<PropertyDescriptor> { property };
				Categories.Add(category, list);
			}
		}

		PropertyViews.Load(BuildPropertiesView());
	}

	#endregion
}