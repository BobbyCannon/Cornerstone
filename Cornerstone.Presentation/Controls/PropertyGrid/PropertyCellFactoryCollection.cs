#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.PropertyGrid.Factories;
using Cornerstone.Collections;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid;

internal class PropertyCellFactoryCollection
{
	#region Fields

	private readonly SpeedyList<PropertyCellFactory> _factories;

	#endregion

	#region Constructors

	static PropertyCellFactoryCollection()
	{
		Default = new PropertyCellFactoryCollection();
	}

	private PropertyCellFactoryCollection()
	{
		_factories =
		[
			new BooleanPropertyCellFactory(),
			new EnumPropertyCellFactory(),
			new NumericPropertyCellFactory(),
			new CharPropertyCellFactory(),
			new DateTimePropertyCellFactory(),
			new TimeSpanPropertyCellFactory(),
			new ColorPropertyCellFactory(),
			new ShortcutBindingPropertyCellFactory(),
			new GuidPropertyCellFactory(),
			new TextBoxPropertyCellFactory(),
			new TextBlockPropertyCellFactory()
		];
	}

	#endregion

	#region Properties

	public static PropertyCellFactoryCollection Default { get; }

	public IEnumerable<PropertyCellFactory> Factories => _factories;

	#endregion

	#region Methods

	public void AddFactory(PropertyCellFactory factory)
	{
		_factories.Add(factory);
	}

	public Control BuildPropertyControl(PropertyCellContext context)
	{
		foreach (var factory in _factories)
		{
			var control = factory.HandleNewProperty(context);
			if (control == null)
			{
				continue;
			}

			context.EditorControl = control;
			context.Factory = factory;
			return control;
		}

		return null;
	}

	public void RemoveFactory(PropertyCellFactory factory)
	{
		_factories.Remove(factory);
	}

	#endregion
}