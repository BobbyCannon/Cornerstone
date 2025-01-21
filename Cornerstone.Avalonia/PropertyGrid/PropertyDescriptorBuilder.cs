#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Avalonia.PropertyGrid.Descriptors;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid;

public class PropertyDescriptorBuilder
{
	#region Fields

	public readonly bool IsMultipleObjects;
	public readonly object Target;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="PropertyDescriptorBuilder" /> class.
	/// </summary>
	/// <param name="target"> The target. </param>
	/// <exception cref="System.ArgumentNullException"> target </exception>
	public PropertyDescriptorBuilder(object target)
	{
		if (target == null)
		{
			throw new ArgumentNullException(nameof(target));
		}

		if (target is IEnumerable enumerable)
		{
			IsMultipleObjects = true;
			Target = enumerable;
		}
		else
		{
			IsMultipleObjects = false;
			Target = target;
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Allows the merge.
	/// </summary>
	/// <param name="descriptor"> The descriptor. </param>
	/// <returns> True if allow merge otherwise false. </returns>
	public static bool AllowMerge(PropertyDescriptor descriptor)
	{
		var attr = descriptor.Attributes[typeof(MergablePropertyAttribute)] as MergablePropertyAttribute;

		return attr?.AllowMerge ?? MergablePropertyAttribute.Default.AllowMerge;
	}

	/// <summary>
	/// Gets the properties.
	/// </summary>
	/// <returns> PropertyDescriptorCollection. </returns>
	public PropertyDescriptorCollection GetProperties()
	{
		return IsMultipleObjects
			? GetMultipleProperties((Target as IEnumerable)!)
			: GetProperties(Target);
	}

	/// <summary>
	/// Gets the multiple properties.
	/// </summary>
	/// <param name="targets"> The targets. </param>
	/// <returns> PropertyDescriptorCollection. </returns>
	private static PropertyDescriptorCollection GetMultipleProperties(IEnumerable targets)
	{
		var collections = new List<PropertyDescriptorCollection>();

		foreach (var target in targets)
		{
			collections.Add(GetProperties(target));
		}

		if (collections.Count == 0)
		{
			return new PropertyDescriptorCollection(null);
		}

		var descriptors = new List<MultiObjectPropertyDescriptor>();
		var multiCollections = collections.Skip(1).ToList();

		foreach (PropertyDescriptor propertyDescriptor in collections.First())
		{
			if (!AllowMerge(propertyDescriptor))
			{
				continue;
			}

			var isMatched = true;
			var propertyDescriptors = new List<PropertyDescriptor>(collections.Count) { propertyDescriptor };

			foreach (var collection in multiCollections)
			{
				var pd = collection.Find(propertyDescriptor.Name, false);

				if ((pd == null) || !pd.Equals(propertyDescriptor))
				{
					isMatched = false;
					break;
				}

				propertyDescriptors.Add(pd);
			}

			if (!isMatched)
			{
				continue;
			}

			descriptors.Add(new MultiObjectPropertyDescriptor(propertyDescriptors.ToArray()));
		}

		// ReSharper disable once CoVariantArrayConversion
		return new PropertyDescriptorCollection(descriptors.ToArray());
	}

	/// <summary>
	/// Gets the properties.
	/// </summary>
	/// <param name="target"> The target. </param>
	/// <returns> PropertyDescriptorCollection. </returns>
	private static PropertyDescriptorCollection GetProperties(object target)
	{
		if (target is ICustomTypeDescriptor ctd)
		{
			return ctd.GetProperties();
		}

		return TypeDescriptor.GetProperties(target);
	}

	#endregion
}