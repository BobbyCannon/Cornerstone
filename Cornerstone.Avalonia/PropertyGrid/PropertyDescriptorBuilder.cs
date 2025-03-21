#region References

using System;
using System.ComponentModel;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid;

public class PropertyDescriptorBuilder
{
	#region Fields

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
		Target = target ?? throw new ArgumentNullException(nameof(target));
	}

	#endregion

	#region Methods

	/// <summary>
	/// Gets the properties.
	/// </summary>
	/// <returns> PropertyDescriptorCollection. </returns>
	public PropertyDescriptorCollection GetProperties()
	{
		return GetProperties(Target);
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