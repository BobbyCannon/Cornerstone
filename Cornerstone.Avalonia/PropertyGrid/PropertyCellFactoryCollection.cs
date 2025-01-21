#region References

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Cornerstone.Avalonia.PropertyGrid.Factories;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid;

internal class PropertyCellFactoryCollection
{
	#region Fields

	/// <summary>
	/// The factories for creating the property cell controls.
	/// </summary>
	private readonly ISpeedyList<PropertyCellFactory> _factories;

	#endregion

	#region Constructors

	static PropertyCellFactoryCollection()
	{
		Default = new PropertyCellFactoryCollection();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PropertyCellFactoryCollection" /> class.
	/// </summary>
	private PropertyCellFactoryCollection()
	{
		_factories = new SpeedyList<PropertyCellFactory>
		{
			// Do not reorder these, order is every important
			new BooleanPropertyCellFactory(),
			new EnumPropertyCellFactory(),
			new NumericPropertyCellFactory(),
			new TextBlockPropertyCellFactory()
		};
	}

	#endregion

	#region Properties

	public static PropertyCellFactoryCollection Default { get; }

	/// <summary>
	/// Gets the factories.
	/// </summary>
	/// <value> The factories. </value>
	public IEnumerable<PropertyCellFactory> Factories => _factories.ToArray();

	#endregion

	#region Methods

	/// <summary>
	/// Adds the factory.
	/// </summary>
	/// <param name="factory"> The factory to add. </param>
	public void AddFactory(PropertyCellFactory factory)
	{
		_factories.Add(factory);
	}

	/// <summary>
	/// Builds the property control.
	/// </summary>
	/// <param name="context"> The context. </param>
	/// <returns> Control. </returns>
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

	/// <summary>
	/// Removes the factory.
	/// </summary>
	/// <param name="factory"> The factory to remove. </param>
	public void RemoveFactory(PropertyCellFactory factory)
	{
		_factories.Remove(factory);
	}

	#endregion
}